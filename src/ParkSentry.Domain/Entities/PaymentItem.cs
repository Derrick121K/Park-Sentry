using ParkSentry.Domain.Common;

namespace ParkSentry.Domain.Entities;

public class PaymentItem : BaseEntity
{
    public Guid PaymentId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }

    public Payment Payment { get; set; } = null!;
}
