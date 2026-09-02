using ParkSentry.Domain.Common;

namespace ParkSentry.Domain.Entities;

public class OrganizationBranding : BaseEntity
{
    public Guid OrganizationId { get; set; }
    public string? LogoUrl { get; set; }
    public string PrimaryColor { get; set; } = "#1B2A4A";
    public string SecondaryColor { get; set; } = "#00A896";
    public string? AccentColor { get; set; }

    public Organization Organization { get; set; } = null!;
}
