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
        builder.Property(o => o.SafetyFeeAmount).HasPrecision(18, 2);
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
        builder.Property(v => v.NormalizedRegistration).HasMaxLength(20).IsRequired();
    }
}

public class ParkingBayConfiguration : IEntityTypeConfiguration<ParkingBay>
{
    public void Configure(EntityTypeBuilder<ParkingBay> builder)
    {
        builder.HasIndex(b => new { b.ParkingZoneId, b.BayNumber }).IsUnique();
        builder.Property(b => b.BayNumber).HasMaxLength(20).IsRequired();
        builder.HasIndex(b => new { b.OrganizationId, b.Status });
    }
}

public class ParkingSessionConfiguration : IEntityTypeConfiguration<ParkingSession>
{
    public void Configure(EntityTypeBuilder<ParkingSession> builder)
    {
        builder.HasOne(s => s.Vehicle).WithMany(v => v.ParkingSessions).HasForeignKey(s => s.VehicleId);
        builder.HasOne(s => s.ParkingBay).WithMany(b => b.ParkingSessions).HasForeignKey(s => s.ParkingBayId);
        builder.Property(s => s.ParkingFee).HasPrecision(18, 2);
        builder.Property(s => s.SafetyFee).HasPrecision(18, 2);
        builder.Property(s => s.DiscountAmount).HasPrecision(18, 2);
        builder.Property(s => s.AmountPaid).HasPrecision(18, 2);
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
        builder.Property(p => p.Amount).HasPrecision(18, 2);
        builder.Property(p => p.Currency).HasMaxLength(3).IsRequired();
        builder.Property(p => p.IdempotencyKey).HasMaxLength(128);
        builder.Property(p => p.FailureReason).HasMaxLength(500);
        builder.Property(p => p.Provider).HasMaxLength(100);
        builder.Property(p => p.ProviderTransactionId).HasMaxLength(200);
        builder.HasIndex(p => new { p.OrganizationId, p.IdempotencyKey })
            .IsUnique()
            .HasFilter("\"IdempotencyKey\" IS NOT NULL");
        builder.HasIndex(p => new { p.OrganizationId, p.CreatedAt });
        builder.HasIndex(p => p.ParkingSessionId);
    }
}

public class PaymentItemConfiguration : IEntityTypeConfiguration<PaymentItem>
{
    public void Configure(EntityTypeBuilder<PaymentItem> builder)
    {
        builder.Property(i => i.Amount).HasPrecision(18, 2);
        builder.Property(i => i.Description).HasMaxLength(200).IsRequired();
    }
}

public class ParkingRateConfiguration : IEntityTypeConfiguration<ParkingRate>
{
    public void Configure(EntityTypeBuilder<ParkingRate> builder)
    {
        builder.Property(r => r.DailyMaximum).HasPrecision(18, 2);
        builder.Property(r => r.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(r => new { r.OrganizationId, r.IsActive });
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasIndex(a => a.CreatedAt);
        builder.HasIndex(a => a.OrganizationId);
        builder.HasIndex(a => new { a.OrganizationId, a.CreatedAt });
    }
}

public class WatchlistEntryConfiguration : IEntityTypeConfiguration<WatchlistEntry>
{
    public void Configure(EntityTypeBuilder<WatchlistEntry> builder)
    {
        builder.HasIndex(w => new { w.OrganizationId, w.NormalizedRegistration })
            .IsUnique()
            .HasFilter("\"IsActive\" = true");
        builder.Property(w => w.RegistrationNumber).HasMaxLength(20).IsRequired();
        builder.Property(w => w.NormalizedRegistration).HasMaxLength(20).IsRequired();
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

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Identifier).HasMaxLength(200);
        builder.HasIndex(d => new { d.OrganizationId, d.Identifier });
    }
}

public class ScannerConfigurationEntityConfiguration : IEntityTypeConfiguration<ScannerConfiguration>
{
    public void Configure(EntityTypeBuilder<ScannerConfiguration> builder)
    {
        builder.Property(s => s.ProviderName).HasMaxLength(100).IsRequired();
        builder.HasIndex(s => new { s.OrganizationId, s.ProviderName });
        // SettingsJson may contain secrets — never project to clients.
    }
}
