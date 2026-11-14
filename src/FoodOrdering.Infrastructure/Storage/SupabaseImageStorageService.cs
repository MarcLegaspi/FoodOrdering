using System.Net.Http.Headers;
using FoodOrdering.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FoodOrdering.Infrastructure.Storage;

/// <summary>
/// Persists uploaded menu images to a public Supabase Storage bucket and returns direct CDN URLs.
/// </summary>
public sealed class SupabaseImageStorageService(
    HttpClient httpClient,
    IOptions<StorageOptions> options,
    ILogger<SupabaseImageStorageService> logger) : IImageStorageService
{
    public async Task<string> SaveMenuImageAsync(
        string storeSlug,
        Stream content,
        string extension,
        CancellationToken ct = default)
    {
        var config = options.Value;
        var supabaseUrl = config.SupabaseUrl?.Trim().TrimEnd('/');
        var supabaseKey = config.SupabaseKey?.Trim();
        var bucket = string.IsNullOrWhiteSpace(config.SupabaseBucket) ? "menu-images" : config.SupabaseBucket.Trim();

        if (string.IsNullOrWhiteSpace(supabaseUrl) || string.IsNullOrWhiteSpace(supabaseKey))
        {
            throw new InvalidOperationException(
                "Supabase Storage is not configured. Ensure Storage:SupabaseUrl and Storage:SupabaseKey are set.");
        }

        var safeSlug = SanitizeSlug(storeSlug);
        var safeExtension = extension.StartsWith('.') ? extension.ToLowerInvariant() : $".{extension.ToLowerInvariant()}";
        var fileName = $"{Guid.NewGuid():N}{safeExtension}";
        var objectPath = $"{safeSlug}/{fileName}";

        var requestUri = $"{supabaseUrl}/storage/v1/object/{bucket}/{objectPath}";
        var contentType = GetContentType(safeExtension);

        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri);
        request.Headers.Add("apikey", supabaseKey);
        request.Headers.Add("Authorization", $"Bearer {supabaseKey}");
        request.Headers.Add("x-upsert", "true");

        using var streamContent = new StreamContent(content);
        streamContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        request.Content = streamContent;

        logger.LogInformation("Uploading menu image to Supabase Storage: {Bucket}/{Path}", bucket, objectPath);

        var response = await httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            logger.LogError("Supabase Storage upload failed ({StatusCode}): {Error}", response.StatusCode, errorBody);
            throw new InvalidOperationException($"Failed to upload image to Supabase Storage ({response.StatusCode}): {errorBody}");
        }

        // Return permanent public CDN URL
        var publicUrl = $"{supabaseUrl}/storage/v1/object/public/{bucket}/{objectPath}";
        logger.LogInformation("Menu image successfully uploaded to Supabase: {PublicUrl}", publicUrl);
        return publicUrl;
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

    private static string GetContentType(string extension) =>
        extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".svg" => "image/svg+xml",
            _ => "application/octet-stream"
        };
}
