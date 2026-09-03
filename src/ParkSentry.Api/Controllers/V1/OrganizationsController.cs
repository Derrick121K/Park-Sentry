using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkSentry.Infrastructure.Authorization;
using ParkSentry.Application.DTOs.Organizations;
using ParkSentry.Application.Services;

namespace ParkSentry.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly OrganizationService _service;

    public OrganizationsController(OrganizationService service) => _service = service;

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    public async Task<ActionResult<IReadOnlyList<OrganizationDto>>> GetAll(CancellationToken ct)
        => Ok(await _service.GetAllAsync(ct));

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.OrgAdminOrAbove)]
    public async Task<ActionResult<OrganizationDetailDto>> GetById(Guid id, CancellationToken ct)
    {
        var org = await _service.GetByIdAsync(id, ct);
        return org is null ? NotFound() : Ok(org);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
    public async Task<ActionResult<OrganizationDto>> Create([FromBody] CreateOrganizationRequest request, CancellationToken ct)
    {
        var org = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = org.Id }, org);
    }

    [HttpGet("current")]
    [Authorize(Policy = AuthorizationPolicies.OperationalStaff)]
    public async Task<ActionResult<OrganizationDetailDto>> GetCurrent(CancellationToken ct)
    {
        var org = await _service.GetCurrentAsync(ct);
        return org is null ? NotFound() : Ok(org);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.OrgAdminOrAbove)]
    public async Task<ActionResult<OrganizationDetailDto>> Update(Guid id, [FromBody] UpdateOrganizationRequest request, CancellationToken ct)
        => Ok(await _service.UpdateAsync(id, request, ct));

    [HttpPut("{id:guid}/branding")]
    [Authorize(Policy = AuthorizationPolicies.OrgAdminOrAbove)]
    public async Task<ActionResult<OrganizationDetailDto>> UpdateBranding(Guid id, [FromBody] UpdateBrandingRequest request, CancellationToken ct)
        => Ok(await _service.UpdateBrandingAsync(id, request, ct));
}
