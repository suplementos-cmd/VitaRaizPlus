using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Scalar.AspNetCore;
using SalesCobrosGeo.Api.Audit;
using SalesCobrosGeo.Api.Business;
using SalesCobrosGeo.Api.Catalogs;
using SalesCobrosGeo.Api.Data;
using SalesCobrosGeo.Api.Initialization;
using SalesCobrosGeo.Api.Security;
using SalesCobrosGeo.Shared.Security;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Controladores + ProblemDetails estandarizado ──────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

// ── Documentación OpenAPI (spec en /openapi/v1.json, UI en /scalar/v1) ────────────
builder.Services.AddOpenApi();

// ── CORS: únicamente para orígenes locales ────────────────────────────────────────
builder.Services.AddCors(options =>
{
    options.AddPolicy("LocalDev", policy =>
        policy
            .WithOrigins(
                "http://localhost:5000", "https://localhost:5001",
                "http://localhost:7000", "https://localhost:7001",
                "http://localhost:4200", "http://localhost:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials());
});

// ── Rate Limiting: máx 10 intentos por minuto en login ───────────────────────────
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", opt =>
    {
        opt.PermitLimit = 10;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ── Servicios de datos basados en Excel (reemplaza InMemory stores) ───────────────
var excelFilePath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "SalesCobrosGeo.xlsx");
builder.Services.AddSingleton(new ExcelDataService(excelFilePath));
builder.Services.AddSingleton<IUserStore, ExcelUserStore>();
builder.Services.AddSingleton<ITokenService, InMemoryTokenService>(); // Tokens siguen en memoria (sesiones temporales)
builder.Services.AddSingleton<IAuditTrailStore, ExcelAuditTrailStore>();
builder.Services.AddSingleton<IBusinessStore, ExcelBusinessStore>();
builder.Services.AddSingleton<ICatalogService, ExcelCatalogService>();
builder.Services.AddScoped<ISalesStore, ExcelSalesStore>(); // Fase 2: Ventas y cobros desde Excel

// ── Autenticación Bearer personalizada ────────────────────────────────────────────
builder.Services
    .AddAuthentication(BearerTokenAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, BearerTokenAuthenticationHandler>(
        BearerTokenAuthenticationHandler.SchemeName,
        _ => { });

// ── Autorización por políticas de rol ─────────────────────────────────────────────
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

// ── Inicialización de datos en Excel ──────────────────────────────────────────────
var excelService = app.Services.GetRequiredService<ExcelDataService>();
var logger = app.Services.GetRequiredService<ILogger<Program>>();
ExcelDataInitializer.Initialize(excelService, logger);

// ── Pipeline HTTP ─────────────────────────────────────────────────────────────────
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference(opts =>
    {
        opts.Title = "SalesCobrosGeo API";
        opts.Theme = ScalarTheme.Purple;
    });
}
else
{
    app.UseExceptionHandler();
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCors("LocalDev");
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<AuditTrailMiddleware>();

app.MapControllers();
app.MapGet("/", () => Results.Ok(new
{
    service = "SalesCobrosGeo.Api",
    status  = "running",
    docs    = "/scalar/v1",
    timestampUtc = DateTime.UtcNow
}));

app.Run();
