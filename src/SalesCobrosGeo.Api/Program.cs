using Microsoft.AspNetCore.Authentication;
using SalesCobrosGeo.Api.Audit;
using SalesCobrosGeo.Api.Security;
using SalesCobrosGeo.Shared.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddSingleton<IUserStore, InMemoryUserStore>();
builder.Services.AddSingleton<ITokenService, InMemoryTokenService>();
builder.Services.AddSingleton<IAuditTrailStore, InMemoryAuditTrailStore>();

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
    timestampUtc = DateTime.UtcNow
}));

app.Run();
