using Microsoft.EntityFrameworkCore;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Enums;
using ParkSentry.Infrastructure.Persistence;

namespace ParkSentry.Infrastructure.Services;

public class BayOccupancyService : IBayOccupancyService
{
    private readonly ParkSentryDbContext _db;

    public BayOccupancyService(ParkSentryDbContext db) => _db = db;

    public async Task<bool> TryOccupyBayAsync(Guid bayId, Guid organizationId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE "ParkingBays"
            SET "Status" = {(int)BayStatus.Occupied}, "UpdatedAt" = {DateTime.UtcNow}
            WHERE "Id" = {bayId}
              AND "OrganizationId" = {organizationId}
              AND "Status" = {(int)BayStatus.Available}
              AND "IsDeleted" = FALSE
            """, cancellationToken);

        return rows > 0;
    }
}
