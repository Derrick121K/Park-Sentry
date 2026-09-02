using ParkSentry.Domain.Common;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Domain.Entities;

public class Vehicle : SoftDeletableEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public string RegistrationNumber { get; set; } = string.Empty;
    public string NormalizedRegistration { get; set; } = string.Empty;
    public string? VehicleMake { get; set; }
    public string? VehicleModel { get; set; }
    public string? VehicleColour { get; set; }
    public VehicleType VehicleType { get; set; } = VehicleType.Car;
    public string? LicenceDiscNumber { get; set; }
    public DateTime? LicenceDiscExpiry { get; set; }
    public string? Vin { get; set; }
    public Guid? CustomerId { get; set; }
    public string? Notes { get; set; }

    public Organization Organization { get; set; } = null!;
    public Customer? Customer { get; set; }
    public ICollection<ParkingSession> ParkingSessions { get; set; } = [];
}
