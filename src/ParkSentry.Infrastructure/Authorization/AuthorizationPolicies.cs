using Microsoft.AspNetCore.Authorization;
using ParkSentry.Domain.Constants;

namespace ParkSentry.Infrastructure.Authorization;

public static class AuthorizationPolicies
{
    public const string SuperAdminOnly = "SuperAdminOnly";
    public const string OrgAdminOrAbove = "OrgAdminOrAbove";
    public const string SiteManagerOrAbove = "SiteManagerOrAbove";
    public const string GuardOrAbove = "GuardOrAbove";
    public const string OperationalStaff = "OperationalStaff";
    public const string CustomerOnly = "CustomerOnly";
    public const string AnyAuthenticated = "AnyAuthenticated";

    private static readonly string[] StaffRoles =
    [
        AppRoles.SuperAdmin,
        AppRoles.OrganizationAdmin,
        AppRoles.SiteManager,
        AppRoles.SecurityGuard,
        AppRoles.ParkingAttendant
    ];

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(SuperAdminOnly, p => p.RequireRole(AppRoles.SuperAdmin));
        options.AddPolicy(OrgAdminOrAbove, p => p.RequireRole(AppRoles.SuperAdmin, AppRoles.OrganizationAdmin));
        options.AddPolicy(SiteManagerOrAbove, p => p.RequireRole(AppRoles.SuperAdmin, AppRoles.OrganizationAdmin, AppRoles.SiteManager));
        options.AddPolicy(GuardOrAbove, p => p.RequireRole(AppRoles.SuperAdmin, AppRoles.OrganizationAdmin, AppRoles.SiteManager, AppRoles.SecurityGuard, AppRoles.ParkingAttendant));
        options.AddPolicy(OperationalStaff, p => p.RequireRole(StaffRoles));
        options.AddPolicy(CustomerOnly, p => p.RequireRole(AppRoles.Customer));
        options.AddPolicy(AnyAuthenticated, p => p.RequireAuthenticatedUser());
    }
}
