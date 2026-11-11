using FoodOrdering.Application.Common.Interfaces;

namespace FoodOrdering.Application.Catalog;

public sealed class MenuItemImageService(
    ITenantContext tenantContext,
    IImageStorageService imageStorage)
{
    public const int MaxFileSizeBytes = 5 * 1024 * 1024;

    private static readonly HashSet<string> AllowedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".gif"
    ];

    public async Task<UploadMenuImageResult> UploadAsync(
        string fileName,
        long length,
        Stream content,
        CancellationToken ct = default)
    {
        if (tenantContext.StoreId is null || string.IsNullOrWhiteSpace(tenantContext.StoreSlug))
        {
            return UploadMenuImageResult.ValidationError("Store tenant context is required.");
        }

        if (length <= 0)
        {
            return UploadMenuImageResult.ValidationError("Image file is empty.");
        }

        if (length > MaxFileSizeBytes)
        {
            return UploadMenuImageResult.ValidationError("Image size cannot exceed 5 MB.");
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
        {
            return UploadMenuImageResult.ValidationError(
                "Unsupported image format. Allowed types: .jpg, .jpeg, .png, .webp, .gif.");
        }

        await using var buffer = new MemoryStream();
        await CopyBoundedAsync(content, buffer, MaxFileSizeBytes + 1, ct);

        if (buffer.Length == 0)
        {
            return UploadMenuImageResult.ValidationError("Image file is empty.");
        }

        if (buffer.Length > MaxFileSizeBytes)
        {
            return UploadMenuImageResult.ValidationError("Image size cannot exceed 5 MB.");
        }

        if (!HasValidImageSignature(buffer.GetBuffer().AsSpan(0, (int)buffer.Length), extension))
        {
            return UploadMenuImageResult.ValidationError(
                "Uploaded file contents do not match a valid image format.");
        }

        buffer.Position = 0;
        var imageUrl = await imageStorage.SaveMenuImageAsync(
            tenantContext.StoreSlug,
            buffer,
            extension,
            ct);

        return UploadMenuImageResult.Success(imageUrl);
    }

    private static async Task CopyBoundedAsync(
        Stream source,
        Stream destination,
        int maxBytes,
        CancellationToken ct)
    {
        var chunk = new byte[16 * 1024];
        var remaining = maxBytes;

        while (remaining > 0)
        {
            var read = await source.ReadAsync(chunk.AsMemory(0, Math.Min(chunk.Length, remaining)), ct);
            if (read == 0)
            {
                break;
            }

            await destination.WriteAsync(chunk.AsMemory(0, read), ct);
            remaining -= read;
        }
    }

    private static bool HasValidImageSignature(ReadOnlySpan<byte> header, string extension)
    {
        return extension switch
        {
            ".jpg" or ".jpeg" => header.Length >= 3
                && header[0] == 0xFF
                && header[1] == 0xD8
                && header[2] == 0xFF,
            ".png" => header.Length >= 8
                && header[0] == 0x89
                && header[1] == 0x50
                && header[2] == 0x4E
                && header[3] == 0x47
                && header[4] == 0x0D
                && header[5] == 0x0A
                && header[6] == 0x1A
                && header[7] == 0x0A,
            ".gif" => header.Length >= 4
                && header[0] == 0x47
                && header[1] == 0x49
                && header[2] == 0x46
                && header[3] == 0x38,
            ".webp" => header.Length >= 12
                && header[0] == 0x52
                && header[1] == 0x49
                && header[2] == 0x46
                && header[3] == 0x46
                && header[8] == 0x57
                && header[9] == 0x45
                && header[10] == 0x42
                && header[11] == 0x50,
            _ => false
        };
    }
}

public sealed record UploadMenuImageResult(string? ImageUrl, string? Error)
{
    public static UploadMenuImageResult Success(string imageUrl) => new(imageUrl, null);
    public static UploadMenuImageResult ValidationError(string error) => new(null, error);
}
