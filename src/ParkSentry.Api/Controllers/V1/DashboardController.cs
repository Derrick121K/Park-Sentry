using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkSentry.Infrastructure.Authorization;
using ParkSentry.Application.DTOs.Dashboard;
using ParkSentry.Application.Services;

namespace ParkSentry.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = AuthorizationPolicies.OperationalStaff)]
public class DashboardController : ControllerBase
{
    private readonly DashboardService _service;

    public DashboardController(DashboardService service) => _service = service;

    [HttpGet("stats")]
    public async Task<ActionResult<DashboardStatsDto>> GetStats([FromQuery] Guid? siteId, CancellationToken ct)
        => Ok(await _service.GetStatsAsync(siteId, ct));

    [HttpGet("audit")]
    [Authorize(Policy = AuthorizationPolicies.OrgAdminOrAbove)]
    public async Task<ActionResult<IReadOnlyList<AuditLogDto>>> GetAuditLogs([FromQuery] int limit = 50, CancellationToken ct = default)
        => Ok(await _service.GetAuditLogsAsync(limit, ct));
}
