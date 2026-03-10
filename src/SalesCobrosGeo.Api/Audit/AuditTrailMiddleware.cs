namespace SalesCobrosGeo.Api.Audit;

public sealed class AuditTrailMiddleware
{
    private static readonly HashSet<string> TrackedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete
    };

    private readonly RequestDelegate _next;

    public AuditTrailMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuditTrailStore store, ILogger<AuditTrailMiddleware> logger)
    {
        await _next(context);

        var method = context.Request.Method;
        if (!TrackedMethods.Contains(method))
        {
            return;
        }

        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var userName = context.User.Identity?.IsAuthenticated == true
            ? context.User.Identity!.Name ?? "unknown"
            : "anonymous";

        var entry = new AuditEntry(
            TimestampUtc: DateTime.UtcNow,
            UserName: userName,
            Method: method,
            Path: context.Request.Path.ToString(),
            StatusCode: context.Response.StatusCode,
            TraceId: context.TraceIdentifier);

        store.Add(entry);
        logger.LogInformation("AUDIT {Method} {Path} {StatusCode} by {UserName}", entry.Method, entry.Path, entry.StatusCode, entry.UserName);
    }
}
