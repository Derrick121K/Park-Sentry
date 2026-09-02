using ParkSentry.Domain.Enums;

namespace ParkSentry.Application.Interfaces.Payments;

public record PaymentRequest(
    Guid ParkingSessionId,
    decimal Amount,
    string Currency,
    string Description,
    string? IdempotencyKey = null,
    PaymentMethod Method = PaymentMethod.Cash);

public record PaymentResult(
    bool Success,
    string? TransactionId,
    string? ErrorMessage,
    string Provider = "Mock",
    PaymentStatus Status = PaymentStatus.Successful,
    string? FailureReason = null,
    string? ProviderReference = null);

public interface IPaymentProvider
{
    string ProviderName { get; }
    bool IsMock { get; }
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken cancellationToken = default);
}

/// <summary>
/// Optional webhook/callback contract for future gateway integrations.
/// Implementations must validate authenticity and remain idempotent.
/// </summary>
public interface IPaymentWebhookHandler
{
    string ProviderName { get; }
    Task<PaymentWebhookResult> HandleAsync(PaymentWebhookRequest request, CancellationToken cancellationToken = default);
}

public record PaymentWebhookRequest(
    string RawBody,
    IReadOnlyDictionary<string, string> Headers,
    string? Signature);

public record PaymentWebhookResult(
    bool Accepted,
    string? ProviderTransactionId,
    PaymentStatus? Status,
    string? ErrorMessage);
