namespace FoodOrdering.Infrastructure.Storage;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Storage provider to use: "Local" or "Supabase". Defaults to "Local" if Supabase is unconfigured.
    /// </summary>
    public string Provider { get; set; } = "Local";

    /// <summary>
    /// Root folder where uploaded files are stored locally when using Local provider.
    /// </summary>
    public string UploadsPath { get; set; } = "uploads";

    /// <summary>
    /// Supabase Project URL (e.g. "https://xyz.supabase.co").
    /// </summary>
    public string? SupabaseUrl { get; set; }

    /// <summary>
    /// Supabase Service Role Key (secret key for server-side uploads to bypass RLS).
    /// </summary>
    public string? SupabaseKey { get; set; }

    /// <summary>
    /// Supabase Storage bucket name (defaults to "menu-images"). Must be marked Public in Supabase.
    /// </summary>
    public string SupabaseBucket { get; set; } = "menu-images";

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
