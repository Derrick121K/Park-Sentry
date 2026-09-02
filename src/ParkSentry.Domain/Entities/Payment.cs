using ParkSentry.Domain.Common;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Domain.Entities;

public class Payment : BaseEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public Guid ParkingSessionId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "ZAR";
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public PaymentMethod Method { get; set; } = PaymentMethod.Mock;
    public string Provider { get; set; } = "Mock";
    public string? ProviderTransactionId { get; set; }
    public string? IdempotencyKey { get; set; }
    public DateTime? CompletedAt { get; set; }

    public Organization Organization { get; set; } = null!;
    public ParkingSession ParkingSession { get; set; } = null!;
    public ICollection<PaymentItem> Items { get; set; } = [];
}
