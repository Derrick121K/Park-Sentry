using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Common;
using ParkSentry.Domain.Entities;
using ParkSentry.Infrastructure.Identity;

namespace ParkSentry.Infrastructure.Persistence;

public class ParkSentryDbContext : IdentityDbContext<ApplicationUser>, IParkSentryDbContext
{
    private readonly ITenantContext _tenantContext;
    private IDbContextTransaction? _transaction;

    public ParkSentryDbContext(DbContextOptions<ParkSentryDbContext> options, ITenantContext tenantContext) : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<OrganizationBranding> OrganizationBrandings => Set<OrganizationBranding>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<ParkingArea> ParkingAreas => Set<ParkingArea>();
    public DbSet<ParkingZone> ParkingZones => Set<ParkingZone>();
    public DbSet<ParkingBay> ParkingBays => Set<ParkingBay>();
    public DbSet<ParkingRate> ParkingRates => Set<ParkingRate>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ParkingSession> ParkingSessions => Set<ParkingSession>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<PaymentItem> PaymentItems => Set<PaymentItem>();
    public DbSet<SecurityEvent> SecurityEvents => Set<SecurityEvent>();
    public DbSet<WatchlistEntry> WatchlistEntries => Set<WatchlistEntry>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<ScannerConfiguration> ScannerConfigurations => Set<ScannerConfiguration>();
    public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
            throw new InvalidOperationException("A database transaction is already active.");

        _transaction = await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.CommitAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Added)
                entry.Entity.CreatedAt = now;
            if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                entry.Entity.UpdatedAt = now;
        }
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ParkSentryDbContext).Assembly);
        ApplyTenantFilters(builder);
    }

    private void ApplyTenantFilters(ModelBuilder builder)
    {
        builder.Entity<Site>().HasQueryFilter(e =>
            (_tenantContext.IsSuperAdmin
             || (_tenantContext.OrganizationId.HasValue && e.OrganizationId == _tenantContext.OrganizationId))
            && !e.IsDeleted);

        builder.Entity<ParkingArea>().HasQueryFilter(e =>
            (_tenantContext.IsSuperAdmin
             || (_tenantContext.OrganizationId.HasValue && e.OrganizationId == _tenantContext.OrganizationId))
            && !e.IsDeleted);

        builder.Entity<ParkingZone>().HasQueryFilter(e =>
            (_tenantContext.IsSuperAdmin
             || (_tenantContext.OrganizationId.HasValue && e.OrganizationId == _tenantContext.OrganizationId))
            && !e.IsDeleted);

        builder.Entity<ParkingBay>().HasQueryFilter(e =>
            (_tenantContext.IsSuperAdmin
             || (_tenantContext.OrganizationId.HasValue && e.OrganizationId == _tenantContext.OrganizationId))
            && !e.IsDeleted);

        builder.Entity<ParkingRate>().HasQueryFilter(e =>
            _tenantContext.IsSuperAdmin
            || (_tenantContext.OrganizationId.HasValue && e.OrganizationId == _tenantContext.OrganizationId));

        builder.Entity<Vehicle>().HasQueryFilter(e =>
            (_tenantContext.IsSuperAdmin
             || (_tenantContext.OrganizationId.HasValue && e.OrganizationId == _tenantContext.OrganizationId))
            && !e.IsDeleted);

        builder.Entity<Customer>().HasQueryFilter(e =>
            (_tenantContext.IsSuperAdmin
             || (_tenantContext.OrganizationId.HasValue && e.OrganizationId == _tenantContext.OrganizationId))
            && !e.IsDeleted);

        builder.Entity<ParkingSession>().HasQueryFilter(e =>
            _tenantContext.IsSuperAdmin
            || (_tenantContext.OrganizationId.HasValue && e.OrganizationId == _tenantContext.OrganizationId));

        builder.Entity<Payment>().HasQueryFilter(e =>
            _tenantContext.IsSuperAdmin
            || (_tenantContext.OrganizationId.HasValue && e.OrganizationId == _tenantContext.OrganizationId));

        builder.Entity<PaymentItem>().HasQueryFilter(pi =>
            _tenantContext.IsSuperAdmin
            || (_tenantContext.OrganizationId.HasValue && pi.Payment.OrganizationId == _tenantContext.OrganizationId));

        builder.Entity<SecurityEvent>().HasQueryFilter(e =>
            _tenantContext.IsSuperAdmin
            || (_tenantContext.OrganizationId.HasValue && e.OrganizationId == _tenantContext.OrganizationId));

        builder.Entity<WatchlistEntry>().HasQueryFilter(e =>
            _tenantContext.IsSuperAdmin
            || (_tenantContext.OrganizationId.HasValue && e.OrganizationId == _tenantContext.OrganizationId));

        builder.Entity<Device>().HasQueryFilter(e =>
            _tenantContext.IsSuperAdmin
            || (_tenantContext.OrganizationId.HasValue && e.OrganizationId == _tenantContext.OrganizationId));

        builder.Entity<ScannerConfiguration>().HasQueryFilter(e =>
            _tenantContext.IsSuperAdmin
            || (_tenantContext.OrganizationId.HasValue && e.OrganizationId == _tenantContext.OrganizationId));

        builder.Entity<OrganizationBranding>().HasQueryFilter(e =>
            _tenantContext.IsSuperAdmin
            || (_tenantContext.OrganizationId.HasValue && e.OrganizationId == _tenantContext.OrganizationId));

        builder.Entity<AuditLog>().HasQueryFilter(e =>
            _tenantContext.IsSuperAdmin
            || (_tenantContext.OrganizationId.HasValue && e.OrganizationId == _tenantContext.OrganizationId));

        builder.Entity<UserProfile>().HasQueryFilter(e =>
            _tenantContext.IsSuperAdmin
            || (_tenantContext.OrganizationId.HasValue && e.OrganizationId == _tenantContext.OrganizationId));

        builder.Entity<SystemSetting>().HasQueryFilter(_ => _tenantContext.IsSuperAdmin);
    }
}
