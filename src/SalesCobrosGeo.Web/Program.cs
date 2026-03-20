using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesCobrosGeo.Api.Catalogs;
using SalesCobrosGeo.Api.Data;
using SalesCobrosGeo.Web.Data;
using SalesCobrosGeo.Web.Security;
using SalesCobrosGeo.Web.Services.Catalogs;
using SalesCobrosGeo.Web.Services.Sales;

var builder = WebApplication.CreateBuilder(args);
var secureCookiePolicy = builder.Environment.IsDevelopment()
    ? CookieSecurePolicy.SameAsRequest
    : CookieSecurePolicy.Always;

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// Configurar sesiones para almacenar token de API
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = secureCookiePolicy;
});

// Servicios de catálogos compartidos con API (usando Excel)
var excelFilePath = Path.Combine(builder.Environment.ContentRootPath, "..", "SalesCobrosGeo.Api", "App_Data", "SalesCobrosGeo.xlsx");
builder.Services.AddSingleton(new ExcelDataService(excelFilePath));
builder.Services.AddSingleton<ICatalogService, ExcelCatalogService>();
builder.Services.AddScoped<ICatalogViewService, CatalogViewService>();

var dataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataPath);
var securityDbPath = Path.Combine(dataPath, "security.db");

// FASE 1 & 2: HttpClient configurado para consumir API con autenticación
var apiBaseUrl = builder.Configuration["ApiBaseUrl"] ?? "http://localhost:5207";

// Registrar handler que inyecta el token en cada petición
builder.Services.AddTransient<ApiTokenDelegatingHandler>();

// HttpClient para autenticación
builder.Services.AddHttpClient<IApplicationUserService, ApiAuthenticationService>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// HttpClient para repositorio de ventas/cobros con autenticación
builder.Services.AddHttpClient<ISalesRepository, ApiSalesRepository>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddHttpMessageHandler<ApiTokenDelegatingHandler>();

// SQLite solo para sesiones (NO para usuarios)
builder.Services.AddDbContext<AppSecurityDbContext>(options =>
    options.UseSqlite($"Data Source={securityDbPath}"));
builder.Services.AddScoped<IUserSessionTracker, SqliteUserSessionTracker>();

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "VRP.AntiXsrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = secureCookiePolicy;
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.Secure = secureCookiePolicy;
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "VRP.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = secureCookiePolicy;
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(AppPolicies.DashboardAccess, policy =>
        policy.RequireAssertion(context => context.User.HasPermission(AppPermissions.DashboardView)));

    options.AddPolicy(AppPolicies.SalesAccess, policy =>
        policy.RequireAssertion(context => context.User.HasPermission(AppPermissions.SalesView)));

    options.AddPolicy(AppPolicies.CollectionsAccess, policy =>
        policy.RequireAssertion(context => context.User.HasPermission(AppPermissions.CollectionsView)));

    options.AddPolicy(AppPolicies.MaintenanceAccess, policy =>
        policy.RequireAssertion(context => context.User.HasPermission(AppPermissions.MaintenanceView)));

    options.AddPolicy(AppPolicies.AdministrationAccess, policy =>
        policy.RequireAssertion(context => context.User.HasPermission(AppPermissions.AdministrationView)));
});

var app = builder.Build();

// Crear base de datos SQLite si no existe
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppSecurityDbContext>();
    dbContext.Database.EnsureCreated();
}

var contentSecurityPolicy = app.Environment.IsDevelopment()
    ? "default-src 'self'; img-src 'self' data: blob: https:; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; font-src 'self' data:; connect-src 'self' http://localhost:* https://localhost:* ws://localhost:* wss://localhost:*;"
    : "default-src 'self'; img-src 'self' data: blob: https:; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; font-src 'self' data:; connect-src 'self';";

// FASE 1: Comentado - Ya no inicializamos usuarios desde SQLite
// Los usuarios ahora vienen de Excel vía API
/*
using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<SecurityDatabaseInitializer>();
    initializer.Initialize();
}
*/

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCookiePolicy();
app.UseStaticFiles();
app.UseRouting();
app.UseSession(); // FASE 1: Session para guardar token de API
app.UseAuthentication();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(self), camera=(self)";
    context.Response.Headers["Content-Security-Policy"] = contentSecurityPolicy;

    var shouldApplyNoCache = context.User.Identity?.IsAuthenticated == true ||
        context.Request.Path.StartsWithSegments("/Account", StringComparison.OrdinalIgnoreCase);

    if (shouldApplyNoCache)
    {
        context.Response.OnStarting(() =>
        {
            context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
            context.Response.Headers["Pragma"] = "no-cache";
            context.Response.Headers["Expires"] = "0";
            return Task.CompletedTask;
        });
    }

    if (context.User.Identity?.IsAuthenticated == true)
    {
        using var scope = app.Services.CreateScope();
        var tracker = scope.ServiceProvider.GetRequiredService<IUserSessionTracker>();
        if (!tracker.IsSessionValid(context.User))
        {
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            context.Response.Redirect("/Account/Login");
            return;
        }

        tracker.TouchRequest(context.User, context);
    }

    await next();
});

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
