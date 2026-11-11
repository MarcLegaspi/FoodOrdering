using FoodOrdering.Application.Catalog;
using FoodOrdering.Application.Common.Interfaces;
using Xunit;

namespace FoodOrdering.UnitTests;

public sealed class MenuItemImageServiceTests
{
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];

    [Fact]
    public async Task UploadAsync_Saves_Valid_Png_And_Returns_Url()
    {
        var tenant = new TestTenantContext { StoreId = 1, StoreSlug = "burger-barn" };
        var storage = new FakeImageStorageService();
        var service = new MenuItemImageService(tenant, storage);

        await using var stream = new MemoryStream(PngHeader);
        var result = await service.UploadAsync("burger.PNG", stream.Length, stream);

        Assert.Null(result.Error);
        Assert.Equal("/uploads/menu/burger-barn/saved.png", result.ImageUrl);
        Assert.Equal("burger-barn", storage.LastSlug);
        Assert.Equal(".png", storage.LastExtension);
        Assert.Equal(PngHeader, storage.SavedBytes);
    }

    [Fact]
    public async Task UploadAsync_Rejects_Spoofed_Image_Bytes_And_Unsupported_Extensions()
    {
        var tenant = new TestTenantContext { StoreId = 1, StoreSlug = "burger-barn" };
        var service = new MenuItemImageService(tenant, new FakeImageStorageService());

        await using var svgStream = new MemoryStream("<svg></svg>"u8.ToArray());
        var svgResult = await service.UploadAsync("icon.svg", svgStream.Length, svgStream);
        Assert.NotNull(svgResult.Error);

        await using var fakePng = new MemoryStream("not really a png"u8.ToArray());
        var spoofResult = await service.UploadAsync("fake.png", fakePng.Length, fakePng);
        Assert.NotNull(spoofResult.Error);
    }

    [Fact]
    public async Task UploadAsync_Rejects_Oversized_File()
    {
        var tenant = new TestTenantContext { StoreId = 1, StoreSlug = "burger-barn" };
        var service = new MenuItemImageService(tenant, new FakeImageStorageService());

        await using var stream = new MemoryStream(PngHeader);
        var result = await service.UploadAsync("huge.png", MenuItemImageService.MaxFileSizeBytes + 1, stream);

        Assert.NotNull(result.Error);
    }

    private sealed class FakeImageStorageService : IImageStorageService
    {
        public string? LastSlug { get; private set; }
        public string? LastExtension { get; private set; }
        public byte[] SavedBytes { get; private set; } = [];

        public async Task<string> SaveMenuImageAsync(
            string storeSlug,
            Stream content,
            string extension,
            CancellationToken ct = default)
        {
            LastSlug = storeSlug;
            LastExtension = extension;

            await using var ms = new MemoryStream();
            await content.CopyToAsync(ms, ct);
            SavedBytes = ms.ToArray();

            return $"/uploads/menu/{storeSlug}/saved{extension}";
        }
    }
}
