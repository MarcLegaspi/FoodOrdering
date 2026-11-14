using System.Net;
using FoodOrdering.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace FoodOrdering.UnitTests;

public sealed class SupabaseStorageTests
{
    private sealed class MockHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(response);
        }
    }

    [Fact]
    public async Task SaveMenuImageAsync_Uploads_To_Supabase_And_Returns_Public_Url()
    {
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"Key\":\"menu-images/elis-goodies/test.jpg\"}")
        };
        var handler = new MockHttpMessageHandler(mockResponse);
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new StorageOptions
        {
            SupabaseUrl = "https://my-test-project.supabase.co",
            SupabaseKey = "test-service-role-key",
            SupabaseBucket = "menu-images"
        });

        var service = new SupabaseImageStorageService(
            httpClient,
            options,
            NullLogger<SupabaseImageStorageService>.Instance);

        using var memoryStream = new MemoryStream([1, 2, 3, 4]);
        var resultUrl = await service.SaveMenuImageAsync("elis-goodies", memoryStream, ".jpg");

        Assert.NotNull(resultUrl);
        Assert.StartsWith("https://my-test-project.supabase.co/storage/v1/object/public/menu-images/elis-goodies/", resultUrl);
        Assert.EndsWith(".jpg", resultUrl);

        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
        Assert.Contains("apikey", handler.LastRequest.Headers.Select(h => h.Key));
        Assert.Contains("Authorization", handler.LastRequest.Headers.Select(h => h.Key));
    }

    [Fact]
    public async Task SaveMenuImageAsync_Throws_When_Not_Configured()
    {
        var handler = new MockHttpMessageHandler(new HttpResponseMessage(HttpStatusCode.OK));
        var httpClient = new HttpClient(handler);

        var options = Options.Create(new StorageOptions
        {
            SupabaseUrl = null,
            SupabaseKey = null
        });

        var service = new SupabaseImageStorageService(
            httpClient,
            options,
            NullLogger<SupabaseImageStorageService>.Instance);

        using var memoryStream = new MemoryStream([1, 2, 3]);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SaveMenuImageAsync("test-store", memoryStream, ".png"));
    }
}
