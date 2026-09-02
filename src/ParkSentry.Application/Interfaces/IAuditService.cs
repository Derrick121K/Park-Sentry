using ParkSentry.Domain.Enums;

namespace ParkSentry.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(AuditAction action, string entityType, string? entityId = null, string? details = null, string? before = null, string? after = null, CancellationToken cancellationToken = default);
}
