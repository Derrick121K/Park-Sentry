using ParkSentry.Domain.Enums;

namespace ParkSentry.Application.DTOs.Parking;

public record ParkingBayDto(Guid Id, string BayNumber, string ZoneName, BayType Type, BayStatus Status, bool IsReserved, Guid? ZoneId = null, Guid? AreaId = null);
public record ParkingAreaDto(Guid Id, string Name, IEnumerable<ParkingZoneDto> Zones);
public record ParkingZoneDto(Guid Id, string Name, string Code, IEnumerable<ParkingBayDto> Bays);

public record CreateParkingAreaRequest(Guid SiteId, string Name);
public record CreateParkingZoneRequest(Guid AreaId, string Name, string Code);
public record CreateParkingBayRequest(Guid ZoneId, string BayNumber, BayType Type = BayType.Standard);
public record UpdateParkingBayRequest(string BayNumber, BayType Type, BayStatus Status, bool IsReserved);
public record BulkCreateBaysRequest(Guid SiteId, string AreaName, string ZoneName, string ZoneCode, string BayPrefix, int StartNumber, int Count, BayType Type = BayType.Standard);
