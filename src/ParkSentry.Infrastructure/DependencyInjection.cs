using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ParkSentry.Application;
using ParkSentry.Application.Interfaces;
using ParkSentry.Application.Interfaces.Notifications;
using ParkSentry.Application.Interfaces.Payments;
using ParkSentry.Application.Interfaces.Scanning;
using ParkSentry.Infrastructure.Identity;
using ParkSentry.Infrastructure.Payments;
using ParkSentry.Infrastructure.Notifications;
using ParkSentry.Infrastructure.Persistence;
using ParkSentry.Infrastructure.Scanning;
using ParkSentry.Infrastructure.Services;

namespace ParkSentry.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ITenantContext, TenantContext>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IBayOccupancyService, BayOccupancyService>();
        services.AddScoped<IParkingNotificationService, NullParkingNotificationService>();
        services.AddScoped<IPaymentProvider, MockPaymentProvider>();
        services.AddScoped<ILicenceDiscScanner, DemoLicenceDiscScanner>();
        services.AddScoped<JwtTokenService>();

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

        return services;
    }
}
