using ParkSentry.Domain.Common;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Domain.Entities;

public class SecurityEvent : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid SiteId { get; set; }
    public Guid? VehicleId { get; set; }
    public string? UserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public SecurityEventSeverity Severity { get; set; } = SecurityEventSeverity.Low;
    public SecurityEventStatus Status { get; set; } = SecurityEventStatus.Open;
    public string? Resolution { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public Site Site { get; set; } = null!;
    public Vehicle? Vehicle { get; set; }
}
