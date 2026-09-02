using ParkSentry.Domain.Constants;
using ParkSentry.Infrastructure.Identity;

namespace ParkSentry.Infrastructure.Authorization;

public static class OrganizationMembershipValidator
{
    public static bool CanAccessOrganization(ApplicationUser user, IList<string> roles, Guid organizationId)
    {
        if (roles.Contains(AppRoles.SuperAdmin))
            return true;

        return user.OrganizationId.HasValue && user.OrganizationId.Value == organizationId;
    }
}
