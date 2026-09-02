namespace ParkSentry.Domain.Enums;

public enum AuditAction
{
    Login = 0,
    Logout = 1,
    VehicleEntry = 2,
    VehicleExit = 3,
    VehicleCreated = 4,
    VehicleModified = 5,
    Payment = 6,
    Refund = 7,
    BayAssignment = 8,
    PricingChanged = 9,
    UserCreated = 10,
    RoleChanged = 11,
    SecurityEvent = 12,
    OrganizationSettingsChanged = 13,
    ManualOverride = 14,
    OrganizationCreated = 15,
    SiteCreated = 16
}
