namespace ParkSentry.Domain.Constants;

public static class AppRoles
{
    public const string SuperAdmin = "SUPER_ADMIN";
    public const string OrganizationAdmin = "ORGANIZATION_ADMIN";
    public const string SiteManager = "SITE_MANAGER";
    public const string SecurityGuard = "SECURITY_GUARD";
    public const string ParkingAttendant = "PARKING_ATTENDANT";
    public const string Customer = "CUSTOMER";

    public static readonly string[] All =
    [
        SuperAdmin,
        OrganizationAdmin,
        SiteManager,
        SecurityGuard,
        ParkingAttendant,
        Customer
    ];
}
