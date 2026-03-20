using System.Net.Http.Headers;

namespace SalesCobrosGeo.Web.Security;

/// <summary>
/// Handler que inyecta el token de autenticación de la API en todas las peticiones HTTP
/// </summary>
public sealed class ApiTokenDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ApiTokenDelegatingHandler> _logger;

    public ApiTokenDelegatingHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApiTokenDelegatingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            // Intentar obtener token del claim primero
            var tokenClaim = httpContext.User.FindFirst("ApiToken")?.Value;
            
            // Si no está en claim, buscar en sesión
            var token = tokenClaim ?? httpContext.Session.GetString("ApiToken");
            
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                _logger.LogDebug("Token de API agregado a la petición: {Method} {Uri}", 
                    request.Method, request.RequestUri);
            }
            else
            {
                _logger.LogWarning("No se encontró token de API para la petición: {Method} {Uri}", 
                    request.Method, request.RequestUri);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
