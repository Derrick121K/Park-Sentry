using Microsoft.EntityFrameworkCore;
using ParkSentry.Domain.Entities;

namespace ParkSentry.Application.Interfaces;

public interface IParkSentryDbContext
{
    DbSet<Organization> Organizations { get; }
    DbSet<OrganizationBranding> OrganizationBrandings { get; }
    DbSet<Site> Sites { get; }
    DbSet<ParkingArea> ParkingAreas { get; }
    DbSet<ParkingZone> ParkingZones { get; }
    DbSet<ParkingBay> ParkingBays { get; }
    DbSet<ParkingRate> ParkingRates { get; }
    DbSet<Vehicle> Vehicles { get; }
    DbSet<Customer> Customers { get; }
    DbSet<ParkingSession> ParkingSessions { get; }
    DbSet<Payment> Payments { get; }
    DbSet<PaymentItem> PaymentItems { get; }
    DbSet<SecurityEvent> SecurityEvents { get; }
    DbSet<WatchlistEntry> WatchlistEntries { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<Device> Devices { get; }
    DbSet<ScannerConfiguration> ScannerConfigurations { get; }
    DbSet<SystemSetting> SystemSettings { get; }
    DbSet<UserProfile> UserProfiles { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
