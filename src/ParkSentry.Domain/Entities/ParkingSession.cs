using ParkSentry.Domain.Common;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Domain.Entities;

public class ParkingSession : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid SiteId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid? ParkingBayId { get; set; }
    public SessionStatus Status { get; set; } = SessionStatus.Active;
    public DateTime EntryTime { get; set; }
    public DateTime? ExitTime { get; set; }
    public decimal ParkingFee { get; set; }
    public decimal SafetyFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public string? EntryUserId { get; set; }
    public string? ExitUserId { get; set; }
    public string? Notes { get; set; }

    public Organization Organization { get; set; } = null!;
    public Site Site { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public ParkingBay? ParkingBay { get; set; }
    public ICollection<Payment> Payments { get; set; } = [];
}
