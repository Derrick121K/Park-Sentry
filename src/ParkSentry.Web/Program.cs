using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using ParkSentry.Application.Interfaces;
using ParkSentry.Application.Interfaces.Notifications;
using ParkSentry.Application.Interfaces.Scanning;
using ParkSentry.Infrastructure.Authorization;
using ParkSentry.Infrastructure;
using ParkSentry.Infrastructure.Identity;
using ParkSentry.Infrastructure.Middleware;
using ParkSentry.Infrastructure.Persistence;
using ParkSentry.Web.Components;
using ParkSentry.Web.Hubs;
using ParkSentry.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization(AuthorizationPolicies.Configure);
builder.Services.AddScoped<AuthenticationStateProvider, IdentityAuthenticationStateProvider>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddControllers();
builder.Services.AddSignalR();

// Web-specific services (override infrastructure defaults)
builder.Services.AddScoped<IParkingNotificationService, SignalRParkingNotificationService>();
builder.Services.AddScoped<ICameraScannerInterop, CameraScannerInterop>();
builder.Services.AddScoped<ILicenceDiscScanner, BrowserLicenceDiscScanner>();
builder.Services.AddScoped<ParkingRealtimeService>();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

var healthChecks = builder.Services.AddHealthChecks();
if (!string.IsNullOrWhiteSpace(connectionString))
    healthChecks.AddNpgSql(connectionString, name: "database", tags: ["ready"]);

var app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
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

public partial class Program { }
