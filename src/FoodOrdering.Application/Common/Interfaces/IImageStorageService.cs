namespace FoodOrdering.Application.Common.Interfaces;

/// <summary>
/// Persists uploaded menu images outside the relational database and returns a relative or absolute URL.
/// </summary>
public interface IImageStorageService
{
    Task<string> SaveMenuImageAsync(
        string storeSlug,
        Stream content,
        string extension,
        CancellationToken ct = default);
}
