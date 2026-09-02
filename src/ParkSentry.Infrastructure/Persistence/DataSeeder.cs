using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Constants;
using ParkSentry.Domain.Entities;
using ParkSentry.Domain.Enums;
using ParkSentry.Domain.ValueObjects;
using ParkSentry.Infrastructure.Identity;
using ParkSentry.Infrastructure.Persistence;
using System.Text.Json;

namespace ParkSentry.Infrastructure.Persistence;

public static class DataSeeder
{
    public const string DevPassword = "Dev@ParkSentry1!";

    public static async Task ApplyMigrationsAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ParkSentryDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ParkSentryDbContext>>();
        logger.LogInformation("Applying database migrations...");
        await db.Database.MigrateAsync(cancellationToken);
    }

    public static async Task SeedDevelopmentDataAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();
        var hostEnvironment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (!hostEnvironment.IsDevelopment())
        {
            return;
        }

        var db = scope.ServiceProvider.GetRequiredService<ParkSentryDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ParkSentryDbContext>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (await db.Organizations.AnyAsync(cancellationToken))
        {
            logger.LogInformation("Development seed data already present.");
            return;
        }

        var org = new Organization
        {
            Name = "ParkSentry Demo Organization",
            DisplayName = "ParkSentry Demo",
            ContactEmail = "demo@parksentry.dev",
            Currency = "ZAR",
            Timezone = "Africa/Johannesburg",
            SafetyFeeEnabled = true,
            SafetyFeeType = SafetyFeeType.Fixed,
            SafetyFeeAmount = 5m,
            Branding = new OrganizationBranding
            {
                PrimaryColor = "#1B2A4A",
                SecondaryColor = "#00A896"
            }
        };
        db.Organizations.Add(org);
        await db.SaveChangesAsync(cancellationToken);

        var tiers = new List<PricingTier>
        {
            new(0, 30, 0),
            new(31, 120, 10),
            new(121, 240, 20),
            new(241, 480, 35),
            new(481, null, 50)
        };

        db.ParkingRates.Add(new ParkingRate
        {
            OrganizationId = org.Id,
            Name = "Standard Rate",
            Model = PricingModel.Tiered,
            GracePeriodMinutes = 30,
            DailyMaximum = 50,
            TiersJson = JsonSerializer.Serialize(tiers)
        });

        var mainSite = CreateSiteWithBays(org.Id, "Main Parking Site", "A", 50);
        var staffSite = CreateSiteWithBays(org.Id, "Staff Parking Site", "B", 50);
        db.Sites.AddRange(mainSite, staffSite);
        await db.SaveChangesAsync(cancellationToken);

        await CreateUserAsync(userManager, "superadmin@parksentry.dev", "Platform Super Admin", null, AppRoles.SuperAdmin);
        await CreateUserAsync(userManager, "orgadmin@demo.parksentry.dev", "Demo Org Admin", org.Id, AppRoles.OrganizationAdmin);
        await CreateUserAsync(userManager, "sitemanager@demo.parksentry.dev", "Demo Site Manager", org.Id, AppRoles.SiteManager);
        await CreateUserAsync(userManager, "guard@demo.parksentry.dev", "Demo Security Guard", org.Id, AppRoles.SecurityGuard);
        await CreateUserAsync(userManager, "attendant@demo.parksentry.dev", "Demo Parking Attendant", org.Id, AppRoles.ParkingAttendant);
        await CreateUserAsync(userManager, "customer@demo.parksentry.dev", "Demo Customer", org.Id, AppRoles.Customer);

        logger.LogInformation("Development database seeded successfully.");
    }

    /// <summary>Legacy helper used by tests during transition.</summary>
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await ApplyMigrationsAsync(services, cancellationToken);
        await SeedDevelopmentDataAsync(services, cancellationToken);
    }

    private static Site CreateSiteWithBays(Guid orgId, string siteName, string zoneCode, int bayCount)
    {
        var site = new Site { OrganizationId = orgId, Name = siteName, Address = "123 Demo Street" };
        var area = new ParkingArea { OrganizationId = orgId, Name = "Ground Level" };
        var zone = new ParkingZone { OrganizationId = orgId, Name = $"Zone {zoneCode}", Code = zoneCode };

        for (int i = 1; i <= bayCount; i++)
        {
            zone.ParkingBays.Add(new ParkingBay
            {
                OrganizationId = orgId,
                BayNumber = $"{zoneCode}{i:D3}",
                Type = BayType.Standard,
                Status = BayStatus.Available
            });
        }

        area.ParkingZones.Add(zone);
        site.ParkingAreas.Add(area);
        return site;
    }

    private static async Task CreateUserAsync(UserManager<ApplicationUser> userManager, string email, string displayName, Guid? orgId, string role)
    {
        if (await userManager.FindByEmailAsync(email) is not null) return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            DisplayName = displayName,
            OrganizationId = orgId,
            EmailConfirmed = true
        };

        var result = await userManager.CreateAsync(user, DevPassword);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await userManager.AddToRoleAsync(user, role);
    }
}
