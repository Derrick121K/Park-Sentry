using Microsoft.AspNetCore.Identity;

namespace ParkSentry.Infrastructure.Identity;

public class ApplicationUser : IdentityUser
{
    public Guid? OrganizationId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
