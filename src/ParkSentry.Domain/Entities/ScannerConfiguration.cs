using ParkSentry.Domain.Common;

namespace ParkSentry.Domain.Entities;

public class ScannerConfiguration : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public string ProviderName { get; set; } = "Demo";
    public string? SettingsJson { get; set; }
    public bool IsActive { get; set; } = true;

    public Organization Organization { get; set; } = null!;
}
