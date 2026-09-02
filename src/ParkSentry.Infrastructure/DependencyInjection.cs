using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using ParkSentry.Application;
using ParkSentry.Application.Configuration;
using ParkSentry.Application.Interfaces;
using ParkSentry.Application.Interfaces.Notifications;
using ParkSentry.Application.Interfaces.Payments;
using ParkSentry.Application.Interfaces.Scanning;
using ParkSentry.Infrastructure.Identity;
using ParkSentry.Infrastructure.Notifications;
using ParkSentry.Infrastructure.Payments;
using ParkSentry.Infrastructure.Persistence;
using ParkSentry.Infrastructure.Scanning;
using ParkSentry.Infrastructure.Services;

namespace ParkSentry.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<IntegrationsOptions>(configuration.GetSection(IntegrationsOptions.SectionName));
        services.Configure<PaymentsOptions>(configuration.GetSection(PaymentsOptions.SectionName));
        services.Configure<ScanningOptions>(configuration.GetSection(ScanningOptions.SectionName));
        services.Configure<ApplicationCorsOptions>(configuration.GetSection(ApplicationCorsOptions.SectionName));
        services.Configure<SeedOptions>(configuration.GetSection(SeedOptions.SectionName));

        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IBayOccupancyService, BayOccupancyService>();
        services.AddScoped<IParkingNotificationService, NullParkingNotificationService>();
        services.AddScoped<IEmailSender, NullEmailSender>();
        services.AddScoped<ISmsSender, NullSmsSender>();
        services.AddScoped<JwtTokenService>();

        RegisterPaymentProvider(services, configuration);
        RegisterScannerProvider(services, configuration);

        services.AddDbContext<ParkSentryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IParkSentryDbContext>(sp => sp.GetRequiredService<ParkSentryDbContext>());

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
        {
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.User.RequireUniqueEmail = true;
        })
        .AddEntityFrameworkStores<ParkSentryDbContext>()
        .AddDefaultTokenProviders()
        .AddClaimsPrincipalFactory<ApplicationUserClaimsPrincipalFactory>();

        services.AddApplication();
        services.AddScoped<UserAdminService>();

        return services;
    }

    private static void RegisterPaymentProvider(IServiceCollection services, IConfiguration configuration)
    {
        var integrations = configuration.GetSection(IntegrationsOptions.SectionName).Get<IntegrationsOptions>()
            ?? new IntegrationsOptions();
        var payments = configuration.GetSection(PaymentsOptions.SectionName).Get<PaymentsOptions>()
            ?? new PaymentsOptions();

        var provider = (payments.Provider ?? "Mock").Trim();
        var isProduction = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"] ?? Environments.Production,
            Environments.Production,
            StringComparison.OrdinalIgnoreCase)
            || integrations.Mode == IntegrationMode.Production;

        // Testing environment always allows Mock.
        var env = configuration["ASPNETCORE_ENVIRONMENT"] ?? "";
        var isTesting = string.Equals(env, "Testing", StringComparison.OrdinalIgnoreCase);
        var isDevelopment = string.Equals(env, Environments.Development, StringComparison.OrdinalIgnoreCase);

        if (string.Equals(provider, "Mock", StringComparison.OrdinalIgnoreCase))
        {
            if (isProduction && !isTesting && !integrations.AllowDemoProviders && integrations.Mode == IntegrationMode.Production)
            {
                throw new InvalidOperationException(
                    "Payments:Provider=Mock is not allowed in Production. " +
                    "Set Payments:Provider=Manual (or a configured gateway) or Integrations:AllowDemoProviders=true for emergency demo only.");
            }

            // Demo/Development/Testing default
            if (integrations.Mode == IntegrationMode.Production && !integrations.AllowDemoProviders && !isTesting && !isDevelopment)
            {
                throw new InvalidOperationException(
                    "Mock payment provider cannot be used when Integrations:Mode=Production without AllowDemoProviders.");
            }

            services.AddScoped<IPaymentProvider, MockPaymentProvider>();
            return;
        }

        if (string.Equals(provider, "Manual", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IPaymentProvider, ManualPaymentProvider>();
            return;
        }

        throw new InvalidOperationException(
            $"Unknown Payments:Provider '{provider}'. Supported: Mock, Manual. Configure a real gateway adapter before using other names.");
    }

    private static void RegisterScannerProvider(IServiceCollection services, IConfiguration configuration)
    {
        var scanning = configuration.GetSection(ScanningOptions.SectionName).Get<ScanningOptions>()
            ?? new ScanningOptions();
        var provider = (scanning.Provider ?? "Demo").Trim();

        // Web host overrides with BrowserLicenceDiscScanner. API/default uses Demo or Manual.
        if (string.Equals(provider, "Manual", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<ILicenceDiscScanner, DemoLicenceDiscScanner>();
            return;
        }

        // Future OCR providers would register here when credentials exist.
        if (!string.IsNullOrWhiteSpace(scanning.OcrApiKey) &&
            !string.Equals(provider, "Demo", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(provider, "Browser", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Scanning provider '{provider}' is not implemented. Configure OCR credentials and an adapter, or use Demo/Browser/Manual.");
        }

        services.AddScoped<ILicenceDiscScanner, DemoLicenceDiscScanner>();
    }
}
