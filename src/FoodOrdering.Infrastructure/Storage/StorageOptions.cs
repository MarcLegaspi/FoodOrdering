namespace FoodOrdering.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Root folder where uploaded files are stored. Relative paths resolve from ContentRootPath.
    /// </summary>
    public string UploadsPath { get; set; } = "uploads";

    public static string ResolveUploadsRoot(StorageOptions options, string contentRootPath)
    {
        var configured = string.IsNullOrWhiteSpace(options.UploadsPath)
            ? "uploads"
            : options.UploadsPath.Trim();

        return Path.IsPathRooted(configured)
            ? Path.GetFullPath(configured)
            : Path.GetFullPath(Path.Combine(contentRootPath, configured));
    }
}
