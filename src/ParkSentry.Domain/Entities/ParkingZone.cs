using ParkSentry.Domain.Common;

namespace ParkSentry.Domain.Entities;

public class ParkingZone : SoftDeletableEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid ParkingAreaId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ParkingArea ParkingArea { get; set; } = null!;
    public ICollection<ParkingBay> ParkingBays { get; set; } = [];
}
