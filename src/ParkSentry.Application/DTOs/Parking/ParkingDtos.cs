using ParkSentry.Domain.Enums;

namespace ParkSentry.Application.DTOs.Parking;

public record ParkingBayDto(Guid Id, string BayNumber, string ZoneName, BayType Type, BayStatus Status, bool IsReserved);
public record ParkingAreaDto(Guid Id, string Name, IEnumerable<ParkingZoneDto> Zones);
public record ParkingZoneDto(Guid Id, string Name, string Code, IEnumerable<ParkingBayDto> Bays);
