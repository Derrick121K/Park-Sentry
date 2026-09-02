using ParkSentry.Application.Interfaces.Payments;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Infrastructure.Payments;

/// <summary>
/// MOCK PAYMENT PROVIDER — Demo/Testing only. Do not use in production without AllowDemoProviders.
/// </summary>
public class MockPaymentProvider : IPaymentProvider
{
    public string ProviderName => "Mock Payment Provider";
    public bool IsMock => true;

    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount < 0)
        {
            return Task.FromResult(new PaymentResult(
                false, null, "Payment amount cannot be negative.", ProviderName,
                PaymentStatus.Failed, "Payment amount cannot be negative."));
        }

        var transactionId = $"MOCK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        return Task.FromResult(new PaymentResult(
            true, transactionId, null, ProviderName,
            PaymentStatus.Successful, null, transactionId));
    }
}
