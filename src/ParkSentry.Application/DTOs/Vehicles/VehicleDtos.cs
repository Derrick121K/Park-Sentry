using ParkSentry.Domain.Enums;

namespace ParkSentry.Application.DTOs.Vehicles;

public record VehicleDto(Guid Id, string RegistrationNumber, string? VehicleMake, string? VehicleModel, string? VehicleColour, VehicleType VehicleType);
public record CreateVehicleRequest(string RegistrationNumber, string? VehicleMake, string? VehicleModel, string? VehicleColour, VehicleType VehicleType = VehicleType.Car, string? LicenceDiscNumber = null);
public record UpdateVehicleRequest(string? VehicleMake, string? VehicleModel, string? VehicleColour, VehicleType VehicleType, string? LicenceDiscNumber = null);
public record VehicleSearchResult(VehicleDto? Vehicle, bool IsWatchlisted, string? WatchlistReason, bool BlockEntry);
