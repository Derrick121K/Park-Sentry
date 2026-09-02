using ParkSentry.Domain.Common;

namespace ParkSentry.Domain.Entities;

public class WatchlistEntry : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string NormalizedRegistration { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool BlockEntry { get; set; }
    public bool ShowWarning { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;

    public Organization Organization { get; set; } = null!;
}
