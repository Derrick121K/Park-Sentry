using ParkSentry.Domain.Common;

namespace ParkSentry.Domain.Entities;

public class Site : SoftDeletableEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; } = true;

    public Organization Organization { get; set; } = null!;
    public ICollection<ParkingArea> ParkingAreas { get; set; } = [];
}
