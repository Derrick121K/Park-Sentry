using ParkSentry.Domain.Common;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Domain.Entities;

public class Organization : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? Website { get; set; }
    public string Currency { get; set; } = "ZAR";
    public string Timezone { get; set; } = "Africa/Johannesburg";
    public bool IsActive { get; set; } = true;
    public SafetyFeeType SafetyFeeType { get; set; } = SafetyFeeType.None;
    public decimal SafetyFeeAmount { get; set; }
    public bool SafetyFeeEnabled { get; set; }
    public ExitPolicy ExitPolicy { get; set; } = ExitPolicy.BlockUntilPaid;
    public string ParkingTerminology { get; set; } = "Bay";
    public string? TermsAndConditions { get; set; }

    public OrganizationBranding? Branding { get; set; }
    public ICollection<Site> Sites { get; set; } = [];
    public ICollection<Vehicle> Vehicles { get; set; } = [];
    public ICollection<ParkingRate> ParkingRates { get; set; } = [];
}
