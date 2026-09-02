using ParkSentry.Domain.Common;

namespace ParkSentry.Domain.Entities;

public class UserProfile : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public Guid? OrganizationId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public Guid? AssignedSiteId { get; set; }
    public string? Phone { get; set; }

    public Organization? Organization { get; set; }
    public Site? AssignedSite { get; set; }
}
