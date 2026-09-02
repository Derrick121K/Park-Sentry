using ParkSentry.Domain.Common;

namespace ParkSentry.Domain.Entities;

public class Device : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? DeviceType { get; set; }
    public string? Identifier { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastSeenAt { get; set; }

    public Organization Organization { get; set; } = null!;
}
