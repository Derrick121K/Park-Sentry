using Microsoft.AspNetCore.Http;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Entities;
using ParkSentry.Domain.Enums;

namespace ParkSentry.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly Persistence.ParkSentryDbContext _db;
    private readonly ITenantContext _tenant;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditService(Persistence.ParkSentryDbContext db, ITenantContext tenant, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _tenant = tenant;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(AuditAction action, string entityType, string? entityId = null, string? details = null, string? before = null, string? after = null, CancellationToken cancellationToken = default)
    {
        var http = _httpContextAccessor.HttpContext;
        var log = new AuditLog
        {
            OrganizationId = _tenant.OrganizationId,
            UserId = _tenant.UserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            BeforeValue = before,
            AfterValue = after,
            IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = http?.Request.Headers["User-Agent"].ToString()
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
