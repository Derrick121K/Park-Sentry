using FluentAssertions;
using ParkSentry.Domain.Constants;
using ParkSentry.Infrastructure.Authorization;
using ParkSentry.Infrastructure.Identity;

namespace ParkSentry.UnitTests;

public class OrganizationMembershipValidatorTests
{
    [Fact]
    public void CanAccessOrganization_AllowsMatchingOrganizationUser()
    {
        var orgId = Guid.NewGuid();
        var user = new ApplicationUser { OrganizationId = orgId };
        OrganizationMembershipValidator.CanAccessOrganization(user, [AppRoles.SecurityGuard], orgId)
            .Should().BeTrue();
    }

    [Fact]
    public void CanAccessOrganization_DeniesCrossTenantUser()
    {
        var user = new ApplicationUser { OrganizationId = Guid.NewGuid() };
        OrganizationMembershipValidator.CanAccessOrganization(user, [AppRoles.SecurityGuard], Guid.NewGuid())
            .Should().BeFalse();
    }

    [Fact]
    public void CanAccessOrganization_AllowsSuperAdmin()
    {
        var user = new ApplicationUser { OrganizationId = null };
        OrganizationMembershipValidator.CanAccessOrganization(user, [AppRoles.SuperAdmin], Guid.NewGuid())
            .Should().BeTrue();
    }

    [Fact]
    public void CanAccessOrganization_DeniesUserWithoutOrganization()
    {
        var user = new ApplicationUser { OrganizationId = null };
        OrganizationMembershipValidator.CanAccessOrganization(user, [AppRoles.SecurityGuard], Guid.NewGuid())
            .Should().BeFalse();
    }
}
