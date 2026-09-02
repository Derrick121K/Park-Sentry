using ParkSentry.Domain.Common;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Domain.Entities;

public class ParkingRate : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid? SiteId { get; set; }
    public string Name { get; set; } = string.Empty;
    public PricingModel Model { get; set; } = PricingModel.Tiered;
    public int? GracePeriodMinutes { get; set; }
    public decimal? DailyMaximum { get; set; }
    public bool IsActive { get; set; } = true;
    public string TiersJson { get; set; } = "[]";

    public Organization Organization { get; set; } = null!;
    public Site? Site { get; set; }
}
