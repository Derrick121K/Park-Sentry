using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Common;

namespace ParkSentry.Infrastructure.Persistence;

internal static class TenantQueryFilter
{
    /// <summary>
    /// Deny-by-default: only super-admin or matching organization rows are visible.
    /// </summary>
    public static bool MatchesTenant(ITenantContext tenant, Guid organizationId) =>
        tenant.IsSuperAdmin
        || (tenant.OrganizationId.HasValue && tenant.OrganizationId.Value == organizationId);

    public static bool MatchesTenantEntity(ITenantContext tenant, ITenantEntity entity) =>
        MatchesTenant(tenant, entity.OrganizationId);

    public static bool MatchesSoftDeletableTenant(ITenantContext tenant, ITenantEntity entity, bool isDeleted) =>
        MatchesTenantEntity(tenant, entity) && !isDeleted;
}
