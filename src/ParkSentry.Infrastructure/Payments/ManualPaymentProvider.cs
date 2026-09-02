using ParkSentry.Application.Interfaces.Payments;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Infrastructure.Payments;

/// <summary>
/// Records an operational payment (cash / card-at-desk / EFT) without claiming an external gateway success.
/// Suitable for Production when no card gateway credentials are configured.
/// </summary>
public sealed class ManualPaymentProvider : IPaymentProvider
{
    public string ProviderName => "Manual";
    public bool IsMock => false;

    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount < 0)
        {
            return Task.FromResult(new PaymentResult(
                false, null, "Payment amount cannot be negative.", ProviderName,
                PaymentStatus.Failed, "Payment amount cannot be negative."));
        }

        var transactionId = $"MAN-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..28].ToUpperInvariant();
        return Task.FromResult(new PaymentResult(
            true, transactionId, null, ProviderName,
            PaymentStatus.Successful, null, transactionId));
    }
}
