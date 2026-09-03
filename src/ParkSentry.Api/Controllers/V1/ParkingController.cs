using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkSentry.Application.DTOs.Parking;
using ParkSentry.Application.Services;
using ParkSentry.Infrastructure.Authorization;

namespace ParkSentry.Api.Controllers.V1;

[ApiController]
[Route("api/v1/parking")]
[Authorize(Policy = AuthorizationPolicies.GuardOrAbove)]
public class ParkingController : ControllerBase
{
    private readonly ParkingBayService _service;

    public ParkingController(ParkingBayService service) => _service = service;

    [HttpGet("sites/{siteId:guid}/bays")]
    public async Task<ActionResult<IReadOnlyList<ParkingBayDto>>> GetBays(Guid siteId, CancellationToken ct)
        => Ok(await _service.GetBaysBySiteAsync(siteId, ct));

    [HttpGet("sites/{siteId:guid}/structure")]
    public async Task<ActionResult<IReadOnlyList<ParkingAreaDto>>> GetStructure(Guid siteId, CancellationToken ct)
        => Ok(await _service.GetParkingStructureAsync(siteId, ct));

    [HttpPost("areas")]
    [Authorize(Policy = AuthorizationPolicies.OrgAdminOrAbove)]
    public async Task<ActionResult<ParkingAreaDto>> CreateArea([FromBody] CreateParkingAreaRequest request, CancellationToken ct)
        => Ok(await _service.CreateAreaAsync(request, ct));

    [HttpPost("zones")]
    [Authorize(Policy = AuthorizationPolicies.OrgAdminOrAbove)]
    public async Task<ActionResult<ParkingZoneDto>> CreateZone([FromBody] CreateParkingZoneRequest request, CancellationToken ct)
        => Ok(await _service.CreateZoneAsync(request, ct));

    [HttpPost("bays")]
    [Authorize(Policy = AuthorizationPolicies.OrgAdminOrAbove)]
    public async Task<ActionResult<ParkingBayDto>> CreateBay([FromBody] CreateParkingBayRequest request, CancellationToken ct)
        => Ok(await _service.CreateBayAsync(request, ct));

    [HttpPut("bays/{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.SiteManagerOrAbove)]
    public async Task<ActionResult<ParkingBayDto>> UpdateBay(Guid id, [FromBody] UpdateParkingBayRequest request, CancellationToken ct)
        => Ok(await _service.UpdateBayAsync(id, request, ct));

    [HttpPost("bays/bulk")]
    [Authorize(Policy = AuthorizationPolicies.OrgAdminOrAbove)]
    public async Task<ActionResult<object>> BulkCreateBays([FromBody] BulkCreateBaysRequest request, CancellationToken ct)
        => Ok(new { created = await _service.BulkCreateBaysAsync(request, ct) });
}
