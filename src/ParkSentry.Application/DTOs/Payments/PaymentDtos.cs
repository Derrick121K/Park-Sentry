using ParkSentry.Domain.Enums;

namespace ParkSentry.Application.DTOs.Payments;

public record PaymentDto(Guid Id, Guid ParkingSessionId, decimal Amount, string Currency, PaymentStatus Status, PaymentMethod Method, string Provider, DateTime CreatedAt, DateTime? CompletedAt);
public record ProcessPaymentRequest(Guid ParkingSessionId, decimal Amount, PaymentMethod Method = PaymentMethod.Mock);
