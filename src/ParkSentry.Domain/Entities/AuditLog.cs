using ParkSentry.Domain.Common;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Domain.Entities;

public class AuditLog : BaseEntity
{
    public Guid? OrganizationId { get; set; }
    public string? UserId { get; set; }
    public AuditAction Action { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? BeforeValue { get; set; }
    public string? AfterValue { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
}
