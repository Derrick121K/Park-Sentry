using ParkSentry.Domain.Common;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Domain.Entities;

public class ParkingBay : SoftDeletableEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid ParkingZoneId { get; set; }
    public string BayNumber { get; set; } = string.Empty;
    public BayType Type { get; set; } = BayType.Standard;
    public BayStatus Status { get; set; } = BayStatus.Available;
    public string? VehicleRestrictions { get; set; }
    public bool IsReserved { get; set; }

    public ParkingZone ParkingZone { get; set; } = null!;
    public ICollection<ParkingSession> ParkingSessions { get; set; } = [];
}
