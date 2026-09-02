using ParkSentry.Domain.Enums;

namespace ParkSentry.Application.DTOs.Sessions;

public record ParkingSessionDto(Guid Id, string RegistrationNumber, string SiteName, string? BayNumber, SessionStatus Status, DateTime EntryTime, DateTime? ExitTime, decimal ParkingFee, decimal SafetyFee, decimal AmountPaid, decimal OutstandingBalance);
public record VehicleEntryRequest(Guid SiteId, string RegistrationNumber, string? VehicleMake, string? VehicleModel, string? VehicleColour, Guid? ParkingBayId, string? Notes);
public record VehicleEntryResult(Guid SessionId, Guid VehicleId, string RegistrationNumber, Guid? ParkingBayId, string? BayNumber, bool WatchlistWarning, string? WatchlistMessage);
public record VehicleExitRequest(Guid SessionId, bool ProcessPayment = true, string? IdempotencyKey = null);
public record VehicleExitResult(Guid SessionId, decimal ParkingFee, decimal SafetyFee, decimal TotalAmount, decimal AmountPaid, decimal OutstandingBalance, bool PaymentProcessed, string? ReceiptNumber);
public record SessionSummaryDto(Guid Id, string RegistrationNumber, DateTime EntryTime, DateTime? ExitTime, SessionStatus Status, string SiteName);
