using ParkSentry.Domain.Enums;

namespace ParkSentry.Application.DTOs.Dashboard;

public record DashboardStatsDto(
    int TotalBays,
    int AvailableBays,
    int OccupiedBays,
    int ReservedBays,
    int MaintenanceBays,
    int CurrentVehicles,
    int TodayEntries,
    int TodayExits,
    decimal TodayRevenue,
    int OpenSecurityEvents = 0,
    int ActiveWatchlistEntries = 0);
public record AuditLogDto(Guid Id, string? UserId, AuditAction Action, string EntityType, string? EntityId, string? Details, DateTime CreatedAt);
