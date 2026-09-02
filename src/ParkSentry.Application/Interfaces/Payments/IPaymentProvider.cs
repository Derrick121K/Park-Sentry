namespace ParkSentry.Application.Interfaces.Payments;

public record PaymentRequest(Guid ParkingSessionId, decimal Amount, string Currency, string Description);
public record PaymentResult(bool Success, string? TransactionId, string? ErrorMessage, string Provider = "Mock");

public interface IPaymentProvider
{
    string ProviderName { get; }
    bool IsMock { get; }
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);
}
