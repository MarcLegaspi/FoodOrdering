using FoodOrdering.Application.Common.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FoodOrdering.Infrastructure.Storage;

public sealed class LocalFileImageStorageService(
    IOptions<StorageOptions> options,
    IHostEnvironment hostEnvironment) : IImageStorageService
{
    public async Task<string> SaveMenuImageAsync(
        string storeSlug,
        Stream content,
        string extension,
        CancellationToken ct = default)
    {
        var safeSlug = SanitizeSlug(storeSlug);
        var safeExtension = extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";

        var uploadsRoot = StorageOptions.ResolveUploadsRoot(options.Value, hostEnvironment.ContentRootPath);
        var storeMenuFolder = Path.Combine(uploadsRoot, "menu", safeSlug);
        Directory.CreateDirectory(storeMenuFolder);

        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var fullPath = Path.Combine(storeMenuFolder, fileName);

        await using var fileStream = File.Create(fullPath);
        await content.CopyToAsync(fileStream, ct);

        return $"/uploads/menu/{safeSlug}/{fileName}";
    }

    private static string SanitizeSlug(string slug)
    {
        var clean = new string(slug
            .Trim()
            .ToLowerInvariant()
            .Where(c => char.IsAsciiLetterOrDigit(c) || c == '-')
            .ToArray());

        return string.IsNullOrWhiteSpace(clean) ? "store" : clean;
    }
}
