namespace SalesCobrosGeo.Web.Services.Sales;

/// <summary>
/// Abstraction for persisting and serving sale evidence photos.
/// Swap implementations for local disk, Azure Blob, S3, etc.
/// </summary>
public interface IPhotoStorage
{
    /// <summary>
    /// Persists a photo stream and returns the stored URL/path.
    /// </summary>
    /// <param name="photoStream">Raw bytes of the photo.</param>
    /// <param name="context">Logical context, e.g. "fachada", "cliente", "contrato".</param>
    /// <param name="saleId">ID of the related sale (used to organise storage).</param>
    /// <param name="maxWidthPx">
    /// Maximum width to resize to before save. 0 = no resize.
    /// Helps enforce mobile-friendly file sizes.
    /// </param>
    Task<string> SaveAsync(Stream photoStream, string context, string saleId, int maxWidthPx = 800);

    /// <summary>Returns a URL to a thumbnail version of an already-stored photo.</summary>
    /// <param name="storedUrl">URL/path returned by <see cref="SaveAsync"/>.</param>
    /// <param name="maxWidthPx">Thumbnail max width in pixels.</param>
    string GetThumbnailUrl(string storedUrl, int maxWidthPx = 200);

    /// <summary>Deletes a stored photo by its URL/path.</summary>
    Task DeleteAsync(string storedUrl);
}

// ---------------------------------------------------------------------------
// Local disk implementation – suitable for development and single-server prod.
// Replace with AzureBlobPhotoStorage (or similar) for cloud deployments.
// ---------------------------------------------------------------------------

/// <summary>
/// Stores photos on the local file system under <c>wwwroot/uploads/{saleId}/{context}/</c>.
/// Thumbnail is served as the same URL with a <c>?w={maxWidth}</c> query (no actual resize
/// is performed here – add middleware or a CDN transform if you need real thumbnails).
/// </summary>
public sealed class LocalDiskPhotoStorage : IPhotoStorage
{
    private readonly string _uploadsRoot;

    public LocalDiskPhotoStorage(IWebHostEnvironment env)
    {
        _uploadsRoot = Path.Combine(env.WebRootPath, "uploads");
        Directory.CreateDirectory(_uploadsRoot);
    }

    public async Task<string> SaveAsync(Stream photoStream, string context, string saleId, int maxWidthPx = 800)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(saleId);

        var folder = Path.Combine(_uploadsRoot, SanitizeSegment(saleId), SanitizeSegment(context));
        Directory.CreateDirectory(folder);

        var fileName = $"{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}[..8].jpg";
        var filePath = Path.Combine(folder, fileName);

        await using var file = File.OpenWrite(filePath);
        await photoStream.CopyToAsync(file);

        // Return web-accessible relative URL
        return $"/uploads/{SanitizeSegment(saleId)}/{SanitizeSegment(context)}/{fileName}";
    }

    public string GetThumbnailUrl(string storedUrl, int maxWidthPx = 200)
    {
        if (string.IsNullOrWhiteSpace(storedUrl))
        {
            return storedUrl;
        }

        // Append query hint – actual resize can be handled by a CDN or image middleware
        return $"{storedUrl}?w={maxWidthPx}";
    }

    public Task DeleteAsync(string storedUrl)
    {
        if (string.IsNullOrWhiteSpace(storedUrl) || !storedUrl.StartsWith("/uploads/", StringComparison.OrdinalIgnoreCase))
        {
            return Task.CompletedTask;
        }

        var relative = storedUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_uploadsRoot, "..", relative);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private static string SanitizeSegment(string segment)
        => string.Concat(segment.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_'));
}
