using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SalesCobrosGeo.Web.Data;
using SalesCobrosGeo.Web.Security;
using SalesCobrosGeo.Web.Services.Sales;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
});
builder.Services.AddSingleton<ISalesRepository, JsonSalesRepository>();

var dataPath = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
Directory.CreateDirectory(dataPath);
var securityDbPath = Path.Combine(dataPath, "security.db");

builder.Services.AddDbContext<AppSecurityDbContext>(options =>
    options.UseSqlite($"Data Source={securityDbPath}"));
builder.Services.AddScoped<SecurityDatabaseInitializer>();
builder.Services.AddScoped<IApplicationUserService, SqliteApplicationUserService>();
builder.Services.AddScoped<IUserSessionTracker, SqliteUserSessionTracker>();

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.Name = "VRP.AntiXsrf";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.HttpOnly = HttpOnlyPolicy.Always;
    options.Secure = CookieSecurePolicy.Always;
});

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "VRP.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<SecurityDatabaseInitializer>();
    initializer.Initialize();
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseCookiePolicy();
app.UseRouting();
app.UseAuthentication();

app.Use(async (context, next) =>
{
    context.Response.Headers["X-Frame-Options"] = "SAMEORIGIN";
    context.Response.Headers["X-Content-Type-Options"] = "nosniff";
    context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
    context.Response.Headers["Permissions-Policy"] = "geolocation=(self), camera=(self)";
    context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' data: blob: https:; style-src 'self' 'unsafe-inline'; script-src 'self' 'unsafe-inline'; font-src 'self' data:; connect-src 'self';";

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

    if (context.User.Identity?.IsAuthenticated == true ||
        context.Request.Path.StartsWithSegments("/Account", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.Headers["Cache-Control"] = "no-store, no-cache, must-revalidate";
        context.Response.Headers["Pragma"] = "no-cache";
        context.Response.Headers["Expires"] = "0";
    }
});

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
