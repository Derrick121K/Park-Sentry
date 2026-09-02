using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkSentry.Infrastructure.Authorization;
using ParkSentry.Application.DTOs.Sites;
using ParkSentry.Application.Services;

namespace ParkSentry.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = AuthorizationPolicies.SiteManagerOrAbove)]
public class SitesController : ControllerBase
{
    private readonly SiteService _service;

    public SitesController(SiteService service) => _service = service;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SiteDto>>> GetAll(CancellationToken ct)
        => Ok(await _service.GetSitesAsync(ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SiteDto>> GetById(Guid id, CancellationToken ct)
    {
        var site = await _service.GetByIdAsync(id, ct);
        return site is null ? NotFound() : Ok(site);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.OrgAdminOrAbove)]
    public async Task<ActionResult<SiteDto>> Create([FromBody] CreateSiteRequest request, CancellationToken ct)
    {
        var site = await _service.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = site.Id }, site);
    }
}
