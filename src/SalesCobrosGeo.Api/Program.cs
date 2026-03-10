using Microsoft.AspNetCore.Authentication;
using SalesCobrosGeo.Api.Audit;
using SalesCobrosGeo.Api.Business;
using SalesCobrosGeo.Api.Security;
using SalesCobrosGeo.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();
builder.Services.AddSingleton<ITokenService, InMemoryTokenService>();
builder.Services.AddSingleton<IAuditTrailStore, InMemoryAuditTrailStore>();
builder.Services.AddSingleton<IBusinessStore, InMemoryBusinessStore>();

builder.Services
    .AddAuthentication(BearerTokenAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, BearerTokenAuthenticationHandler>(
        BearerTokenAuthenticationHandler.SchemeName,
        _ => { });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(RolePolicies.Authenticated, policy => policy.RequireAuthenticatedUser());
    options.AddPolicy(RolePolicies.CanManageSales, policy =>
        policy.RequireRole(UserRole.SupervisorVentas.ToString(), UserRole.Administrador.ToString()));
    options.AddPolicy(RolePolicies.CanCreateSales, policy =>
        policy.RequireRole(UserRole.Vendedor.ToString(), UserRole.SupervisorVentas.ToString(), UserRole.Administrador.ToString()));
    options.AddPolicy(RolePolicies.CanManageCatalogs, policy =>
        policy.RequireRole(UserRole.Administrador.ToString()));
    options.AddPolicy(RolePolicies.CanManageClients, policy =>
        policy.RequireRole(UserRole.Vendedor.ToString(), UserRole.SupervisorVentas.ToString(), UserRole.Administrador.ToString()));
    options.AddPolicy(RolePolicies.CanCollect, policy =>
        policy.RequireRole(UserRole.Cobrador.ToString(), UserRole.SupervisorCobranza.ToString(), UserRole.Administrador.ToString()));
    options.AddPolicy(RolePolicies.CanSuperviseCollections, policy =>
        policy.RequireRole(UserRole.SupervisorCobranza.ToString(), UserRole.Administrador.ToString()));
    options.AddPolicy(RolePolicies.AdminOnly, policy =>
        policy.RequireRole(UserRole.Administrador.ToString()));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditTrailMiddleware>();

app.MapControllers();
app.MapGet("/", () => Results.Ok(new
{
    service = "SalesCobrosGeo.Api",
    status = "running",
    security = "block-1",
    business = "blocks-2-3-4",
    timestampUtc = DateTime.UtcNow
}));

app.Run();
