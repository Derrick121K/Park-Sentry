using Microsoft.Extensions.DependencyInjection;
using ParkSentry.Application.Interfaces;
using ParkSentry.Application.Services;

namespace ParkSentry.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<OrganizationService>();
        services.AddScoped<SiteService>();
        services.AddScoped<ParkingBayService>();
        services.AddScoped<VehicleService>();
        services.AddScoped<ParkingSessionService>();
        services.AddScoped<DashboardService>();
        services.AddScoped<ScanningPipelineService>();
        services.AddScoped<ReportingService>();
        services.AddScoped<WatchlistService>();
        services.AddScoped<SecurityEventService>();
        services.AddScoped<ParkingRateService>();
        services.AddScoped<DeviceService>();
        services.AddScoped<SystemSettingService>();
        return services;
    }
}
