using ParkSentry.Domain.Common;

namespace ParkSentry.Domain.Entities;

public class ParkingArea : SoftDeletableEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public Site Site { get; set; } = null!;
    public ICollection<ParkingZone> ParkingZones { get; set; } = [];
}
