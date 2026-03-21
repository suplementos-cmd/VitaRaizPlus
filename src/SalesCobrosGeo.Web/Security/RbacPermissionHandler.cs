using Microsoft.AspNetCore.Authorization;
using SalesCobrosGeo.Web.Services.Rbac;
using System.Security.Claims;

namespace SalesCobrosGeo.Web.Security;

/// <summary>
/// Authorization Handler que consulta permisos RBAC exclusivamente
/// Sistema unificado: Solo RBAC, sistema legacy eliminado
/// </summary>
public sealed class RbacPermissionHandler : AuthorizationHandler<RbacPermissionRequirement>
{
    private readonly IRbacService _rbacService;
    private readonly ILogger<RbacPermissionHandler> _logger;

    public RbacPermissionHandler(
        IRbacService rbacService,
        ILogger<RbacPermissionHandler> logger)
    {
        _rbacService = rbacService;
        _logger = logger;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        RbacPermissionRequirement requirement)
    {
        var user = context.User;
        var userName = user.Identity?.Name;

        if (string.IsNullOrEmpty(userName))
        {
            _logger.LogWarning("Usuario no autenticado intentando acceder a recurso protegido");
            return;
        }

        // VALIDACIÓN ÚNICA: Verificar permisos RBAC
        try
        {
            var hasPermission = await _rbacService.HasPermissionAsync(userName, requirement.PermissionCode);
            
            if (hasPermission)
            {
                _logger.LogDebug("Usuario {UserName} tiene permiso RBAC {Permission}", userName, requirement.PermissionCode);
                context.Succeed(requirement);
                return;
            }

            _logger.LogWarning(
                "Usuario {UserName} NO tiene permiso RBAC para {Permission}",
                userName,
                requirement.PermissionCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando permisos RBAC para usuario {UserName}", userName);
            // En caso de error de servicio, denegar acceso por seguridad
        }
    }
}

/// <summary>
/// Requirement personalizado para validar permisos con código específico
/// </summary>
public sealed class RbacPermissionRequirement : IAuthorizationRequirement
{
    public string PermissionCode { get; }

    public RbacPermissionRequirement(string permissionCode)
    {
        PermissionCode = permissionCode ?? throw new ArgumentNullException(nameof(permissionCode));
    }
}
