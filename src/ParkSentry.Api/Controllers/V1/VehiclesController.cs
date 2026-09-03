using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkSentry.Infrastructure.Authorization;
using ParkSentry.Application.DTOs.Vehicles;
using ParkSentry.Application.Services;

namespace ParkSentry.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = AuthorizationPolicies.GuardOrAbove)]
public class VehiclesController : ControllerBase
{
    private readonly VehicleService _service;

    public VehiclesController(VehicleService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<VehicleDto>>> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpGet("search")]
    public async Task<ActionResult<VehicleSearchResult>> Search([FromQuery] string registration, CancellationToken ct)
        => Ok(await _service.SearchAsync(registration, ct));

    [HttpPost]
    public async Task<ActionResult<VehicleDto>> Create([FromBody] CreateVehicleRequest request, CancellationToken ct)
    {
        var vehicle = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(Search), new { registration = vehicle.RegistrationNumber }, vehicle);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<VehicleDto>> Update(Guid id, [FromBody] UpdateVehicleRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));
}
