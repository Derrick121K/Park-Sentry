using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using ParkSentry.Application.Configuration;
using ParkSentry.Application.Interfaces;
using ParkSentry.Application.Interfaces.Notifications;
using ParkSentry.Application.Interfaces.Scanning;
using ParkSentry.Infrastructure;
using ParkSentry.Infrastructure.Authorization;
using ParkSentry.Infrastructure.Identity;
using ParkSentry.Infrastructure.Middleware;
using ParkSentry.Infrastructure.Persistence;
using ParkSentry.Web.Components;
using ParkSentry.Web.Hubs;
using ParkSentry.Web.Services;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);
    builder.Host.UseSerilog((ctx, services, cfg) => cfg
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "ParkSentry.Web")
        .WriteTo.Console());

    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddCascadingAuthenticationState();

    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.LoginPath = "/login";
            options.AccessDeniedPath = "/access-denied";
            options.ExpireTimeSpan = TimeSpan.FromHours(8);
            options.SlidingExpiration = true;
            options.Cookie.Name = "ParkSentry.Auth";
            options.Cookie.HttpOnly = true;
            options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                ? CookieSecurePolicy.SameAsRequest
                : CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

    builder.Services.AddAuthorization(AuthorizationPolicies.Configure);
    builder.Services.AddScoped<AuthenticationStateProvider, IdentityAuthenticationStateProvider>();

    builder.Services.AddRazorComponents()
        .AddInteractiveServerComponents();

    builder.Services.AddControllers();
    builder.Services.AddSignalR();
    builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 10 * 1024 * 1024);

    builder.Services.AddScoped<IParkingNotificationService, SignalRParkingNotificationService>();
    builder.Services.AddScoped<ICameraScannerInterop, CameraScannerInterop>();
    builder.Services.AddScoped<ILicenceDiscScanner, BrowserLicenceDiscScanner>();
    builder.Services.AddScoped<ParkingRealtimeService>();

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy("auth", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 20,
                    Window = TimeSpan.FromMinutes(1)
                }));
    });

    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    var healthChecks = builder.Services.AddHealthChecks();
    if (!string.IsNullOrWhiteSpace(connectionString))
        healthChecks.AddNpgSql(connectionString, name: "database", tags: ["ready"]);

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseSerilogRequestLogging();

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error", createScopeForErrors: true);
        app.UseHsts();
    }

    app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
    app.UseHttpsRedirection();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseAntiforgery();

    app.MapStaticAssets();
    app.MapControllers();
    app.MapRazorComponents<App>()
        .AddInteractiveServerRenderMode();
    app.MapHub<ParkingHub>("/hubs/parking").DisableAntiforgery();
    app.MapHealthChecks("/health/live", new() { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new() { Predicate = check => check.Tags.Contains("ready") });
    app.MapHealthChecks("/health");

    if (app.Environment.IsDevelopment())
    {
        await DataSeeder.ApplyMigrationsAsync(app.Services);
        await DataSeeder.SeedDevelopmentDataAsync(app.Services);
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ParkSentry Web terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
