using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkSentry.Infrastructure.Authorization;
using ParkSentry.Application.DTOs.Parking;
using ParkSentry.Application.Services;

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
}
