using ParkSentry.Application.Interfaces.Payments;

namespace ParkSentry.Infrastructure.Payments;

/// <summary>
/// MOCK PAYMENT PROVIDER — Development only. Do not use in production.
/// </summary>
public class MockPaymentProvider : IPaymentProvider
{
    public string ProviderName => "Mock Payment Provider";
    public bool IsMock => true;

    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
            return Task.FromResult(new PaymentResult(true, $"MOCK-{Guid.NewGuid():N}", null, ProviderName));

        var transactionId = $"MOCK-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..24].ToUpperInvariant();
        return Task.FromResult(new PaymentResult(true, transactionId, null, ProviderName));
    }
}
