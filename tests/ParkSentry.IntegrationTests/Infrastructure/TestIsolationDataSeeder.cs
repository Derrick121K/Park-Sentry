using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ParkSentry.Domain.Constants;
using ParkSentry.Domain.Entities;
using ParkSentry.Domain.Enums;
using ParkSentry.Domain.ValueObjects;
using ParkSentry.Infrastructure.Identity;
using ParkSentry.Infrastructure.Persistence;
using System.Text.Json;

namespace ParkSentry.IntegrationTests.Infrastructure;

public static class TestIsolationDataSeeder
{
    public const string Password = DataSeeder.DevPassword;

    public const string OrgAAdminEmail = "admin-a@test.parksentry.dev";
    public const string OrgAGuardEmail = "guard-a@test.parksentry.dev";
    public const string OrgBAdminEmail = "admin-b@test.parksentry.dev";
    public const string OrgBGuardEmail = "guard-b@test.parksentry.dev";
    public const string OrgBCustomerEmail = "customer-b@test.parksentry.dev";
    public const string SuperAdminEmail = "superadmin@test.parksentry.dev";

    public const string OrgAVehicleReg = "ORGA123";
    public const string OrgBVehicleReg = "ORGB456";

    public static Guid OrgAId { get; private set; }
    public static Guid OrgBId { get; private set; }
    public static Guid OrgASiteId { get; private set; }
    public static Guid OrgBSiteId { get; private set; }
    public static Guid OrgAVehicleId { get; private set; }
    public static Guid OrgBVehicleId { get; private set; }
    public static Guid OrgASessionId { get; private set; }
    public static Guid OrgBSessionId { get; private set; }
    public static Guid OrgBPaymentId { get; private set; }
    public static Guid OrgBAuditLogId { get; private set; }

    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ParkSentryDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (await db.Organizations.AnyAsync(o => o.Name == "ORG-A Test Organization"))
            return;

        var orgA = CreateOrganization("ORG-A Test Organization", "ORG-A");
        var orgB = CreateOrganization("ORG-B Test Organization", "ORG-B");
        db.Organizations.AddRange(orgA, orgB);
        await db.SaveChangesAsync();

        OrgAId = orgA.Id;
        OrgBId = orgB.Id;

        AddRate(db, orgA.Id);
        AddRate(db, orgB.Id);

        var siteA = CreateSiteWithBay(orgA.Id, "ORG-A Site", "A1");
        var siteB = CreateSiteWithBay(orgB.Id, "ORG-B Site", "B1");
        db.Sites.AddRange(siteA, siteB);
        await db.SaveChangesAsync();

        OrgASiteId = siteA.Id;
        OrgBSiteId = siteB.Id;

        var vehicleA = new Vehicle
        {
            OrganizationId = orgA.Id,
            RegistrationNumber = OrgAVehicleReg,
            NormalizedRegistration = OrgAVehicleReg
        };
        var vehicleB = new Vehicle
        {
            OrganizationId = orgB.Id,
            RegistrationNumber = OrgBVehicleReg,
            NormalizedRegistration = OrgBVehicleReg
        };
        db.Vehicles.AddRange(vehicleA, vehicleB);
        await db.SaveChangesAsync();

        OrgAVehicleId = vehicleA.Id;
        OrgBVehicleId = vehicleB.Id;

        var sessionA = new ParkingSession
        {
            OrganizationId = orgA.Id,
            SiteId = siteA.Id,
            VehicleId = vehicleA.Id,
            Status = SessionStatus.Active,
            EntryTime = DateTime.UtcNow.AddHours(-1)
        };
        var sessionB = new ParkingSession
        {
            OrganizationId = orgB.Id,
            SiteId = siteB.Id,
            VehicleId = vehicleB.Id,
            Status = SessionStatus.Completed,
            EntryTime = DateTime.UtcNow.AddHours(-3),
            ExitTime = DateTime.UtcNow.AddHours(-1),
            ParkingFee = 10,
            SafetyFee = 5,
            AmountPaid = 15
        };
        db.ParkingSessions.AddRange(sessionA, sessionB);
        await db.SaveChangesAsync();

        OrgASessionId = sessionA.Id;
        OrgBSessionId = sessionB.Id;

        var paymentB = new Payment
        {
            OrganizationId = orgB.Id,
            ParkingSessionId = sessionB.Id,
            Amount = 15,
            Status = PaymentStatus.Successful,
            Method = PaymentMethod.Mock,
            Provider = "Mock",
            CompletedAt = DateTime.UtcNow.AddHours(-1)
        };
        db.Payments.Add(paymentB);
        await db.SaveChangesAsync();
        OrgBPaymentId = paymentB.Id;

        var auditB = new AuditLog
        {
            OrganizationId = orgB.Id,
            Action = AuditAction.VehicleEntry,
            EntityType = nameof(ParkingSession),
            EntityId = sessionB.Id.ToString(),
            Details = "ORG-B test audit entry"
        };
        db.AuditLogs.Add(auditB);
        await db.SaveChangesAsync();
        OrgBAuditLogId = auditB.Id;

        await CreateUserAsync(userManager, SuperAdminEmail, "Test Super Admin", null, AppRoles.SuperAdmin);
        await CreateUserAsync(userManager, OrgAAdminEmail, "ORG-A Admin", orgA.Id, AppRoles.OrganizationAdmin);
        await CreateUserAsync(userManager, OrgAGuardEmail, "ORG-A Guard", orgA.Id, AppRoles.SecurityGuard);
        await CreateUserAsync(userManager, OrgBAdminEmail, "ORG-B Admin", orgB.Id, AppRoles.OrganizationAdmin);
        await CreateUserAsync(userManager, OrgBGuardEmail, "ORG-B Guard", orgB.Id, AppRoles.SecurityGuard);
        await CreateUserAsync(userManager, OrgBCustomerEmail, "ORG-B Customer", orgB.Id, AppRoles.Customer);
    }

    private static Organization CreateOrganization(string name, string displayName) => new()
    {
        Name = name,
        DisplayName = displayName,
        ContactEmail = $"{displayName.ToLowerInvariant()}@test.parksentry.dev",
        Currency = "ZAR",
        Timezone = "Africa/Johannesburg",
        SafetyFeeEnabled = true,
        SafetyFeeType = SafetyFeeType.Fixed,
        SafetyFeeAmount = 5m
    };

    private static void AddRate(ParkSentryDbContext db, Guid orgId)
    {
        var tiers = new List<PricingTier>
        {
            new(0, 30, 0),
            new(31, 120, 10),
            new(121, null, 20)
        };
        db.ParkingRates.Add(new ParkingRate
        {
            OrganizationId = orgId,
            Name = "Test Rate",
            Model = PricingModel.Tiered,
            GracePeriodMinutes = 30,
            DailyMaximum = 50,
            TiersJson = JsonSerializer.Serialize(tiers)
        });
    }

    private static Site CreateSiteWithBay(Guid orgId, string siteName, string bayNumber)
    {
        var site = new Site { OrganizationId = orgId, Name = siteName, Address = "Test Address" };
        var area = new ParkingArea { OrganizationId = orgId, Name = "Level 1" };
        var zone = new ParkingZone { OrganizationId = orgId, Name = "Zone 1", Code = "Z1" };
        zone.ParkingBays.Add(new ParkingBay
        {
            OrganizationId = orgId,
            BayNumber = bayNumber,
            Type = BayType.Standard,
            Status = BayStatus.Available
        });
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

        var result = await userManager.CreateAsync(user, Password);
        if (!result.Succeeded)
            throw new InvalidOperationException($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");

        await userManager.AddToRoleAsync(user, role);
    }
}
