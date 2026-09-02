using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Constants;

namespace ParkSentry.Infrastructure.Services;

public class TenantContext : ITenantContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? OrganizationId
    {
        get
        {
            var claim = _httpContextAccessor.HttpContext?.User?.FindFirst(TenantConstants.OrganizationIdClaimType)?.Value;
            return Guid.TryParse(claim, out var id) ? id : null;
        }
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    public bool IsSuperAdmin =>
        _httpContextAccessor.HttpContext?.User?.IsInRole(AppRoles.SuperAdmin) ?? false;
}
