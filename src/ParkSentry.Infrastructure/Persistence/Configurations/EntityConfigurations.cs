using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ParkSentry.Domain.Entities;
using ParkSentry.Domain.Enums;
using ParkSentry.Infrastructure.Identity;

namespace ParkSentry.Infrastructure.Persistence.Configurations;

public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Name).HasMaxLength(200).IsRequired();
        builder.Property(o => o.DisplayName).HasMaxLength(200).IsRequired();
        builder.HasIndex(o => o.Name).IsUnique();
        builder.HasOne(o => o.Branding).WithOne(b => b.Organization).HasForeignKey<OrganizationBranding>(b => b.OrganizationId);
    }
}

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasIndex(u => u.OrganizationId);
        builder.HasOne<Organization>()
            .WithMany()
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class VehicleConfiguration : IEntityTypeConfiguration<Vehicle>
{
    public void Configure(EntityTypeBuilder<Vehicle> builder)
    {
        builder.HasIndex(v => new { v.OrganizationId, v.NormalizedRegistration }).IsUnique();
        builder.Property(v => v.RegistrationNumber).HasMaxLength(20).IsRequired();
    }
}

public class ParkingBayConfiguration : IEntityTypeConfiguration<ParkingBay>
{
    public void Configure(EntityTypeBuilder<ParkingBay> builder)
    {
        builder.HasIndex(b => new { b.ParkingZoneId, b.BayNumber }).IsUnique();
        builder.Property(b => b.BayNumber).HasMaxLength(20).IsRequired();
    }
}

public class ParkingSessionConfiguration : IEntityTypeConfiguration<ParkingSession>
{
    public void Configure(EntityTypeBuilder<ParkingSession> builder)
    {
        builder.HasOne(s => s.Vehicle).WithMany(v => v.ParkingSessions).HasForeignKey(s => s.VehicleId);
        builder.HasOne(s => s.ParkingBay).WithMany(b => b.ParkingSessions).HasForeignKey(s => s.ParkingBayId);
        builder.HasIndex(s => new { s.OrganizationId, s.Status });
        builder.HasIndex(s => new { s.OrganizationId, s.EntryTime });
        builder.HasIndex(s => s.VehicleId)
            .IsUnique()
            .HasFilter($"\"{nameof(ParkingSession.Status)}\" = {(int)SessionStatus.Active}");
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.Property(p => p.IdempotencyKey).HasMaxLength(128);
        builder.HasIndex(p => new { p.OrganizationId, p.IdempotencyKey })
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => a.OrganizationId);
    }
}

public class WatchlistEntryConfiguration : IEntityTypeConfiguration<WatchlistEntry>
{
    public void Configure(EntityTypeBuilder<WatchlistEntry> builder)
    {
        builder.HasIndex(w => new { w.OrganizationId, w.NormalizedRegistration })
            .IsUnique()
            .HasFilter("\"IsActive\" = true");
    }
}

public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.HasIndex(s => s.Key).IsUnique();
        builder.Property(s => s.Key).HasMaxLength(200).IsRequired();
    }
}

public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
{
    public void Configure(EntityTypeBuilder<UserProfile> builder)
    {
        builder.HasIndex(u => u.UserId).IsUnique();
        builder.HasIndex(u => u.OrganizationId);
    }
}
