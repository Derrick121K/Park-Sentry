namespace ParkSentry.Application.Interfaces;

public interface ITenantContext
{
    Guid? OrganizationId { get; }
    string? UserId { get; }
    bool IsSuperAdmin { get; }
    bool HasOrganization => OrganizationId.HasValue;
    bool HasTenantAccess => IsSuperAdmin || OrganizationId.HasValue;
}
