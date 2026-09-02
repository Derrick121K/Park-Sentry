namespace ParkSentry.Application.Interfaces;

public interface IBayOccupancyService
{
    /// <summary>
    /// Atomically marks a bay as occupied if it is currently available.
    /// </summary>
    Task<bool> TryOccupyBayAsync(Guid bayId, Guid organizationId, CancellationToken cancellationToken = default);
}
