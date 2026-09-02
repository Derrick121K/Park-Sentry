using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkSentry.Infrastructure.Authorization;
using ParkSentry.Application.DTOs.Sessions;
using ParkSentry.Application.Services;

namespace ParkSentry.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = AuthorizationPolicies.GuardOrAbove)]
public class SessionsController : ControllerBase
{
    private readonly ParkingSessionService _service;

    public SessionsController(ParkingSessionService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ParkingSessionDto>>> GetActive([FromQuery] Guid? siteId, CancellationToken ct)
        => Ok(await _service.GetActiveSessionsAsync(siteId, ct));

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<SessionSummaryDto>>> GetHistory([FromQuery] int limit = 50, CancellationToken ct = default)
        => Ok(await _service.GetHistoryAsync(limit, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ParkingSessionDto>> GetById(Guid id, CancellationToken ct)
    {
        var session = await _service.GetByIdAsync(id, ct);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost("entry")]
    public async Task<ActionResult<VehicleEntryResult>> Entry([FromBody] VehicleEntryRequest request, CancellationToken ct)
    {
        var result = await _service.ProcessEntryAsync(request, ct);
        return Ok(result);
    }

    [HttpPost("exit")]
    public async Task<ActionResult<VehicleExitResult>> Exit([FromBody] VehicleExitRequest request, CancellationToken ct)
    {
        var result = await _service.ProcessExitAsync(request, ct);
        return Ok(result);
    }
}
