using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ParkSentry.Infrastructure.Authorization;
using ParkSentry.Infrastructure.Services;

namespace ParkSentry.Api.Controllers.V1;

[ApiController]
[Route("api/v1/users")]
[Authorize(Policy = AuthorizationPolicies.OrgAdminOrAbove)]
public class UsersController(UserAdminService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<UserAdminDto>>> List(CancellationToken ct)
        => Ok(await service.ListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<UserAdminDto>> Create([FromBody] CreateUserRequest request, CancellationToken ct)
        => Ok(await service.CreateAsync(request, ct));

    [HttpPost("{id}/active")]
    public async Task<IActionResult> SetActive(string id, [FromBody] SetActiveRequest request, CancellationToken ct)
    {
        await service.SetActiveAsync(id, request.IsActive, ct);
        return NoContent();
    }

    [HttpPost("{id}/role")]
    public async Task<IActionResult> SetRole(string id, [FromBody] SetRoleRequest request, CancellationToken ct)
    {
        await service.SetRoleAsync(id, request.Role, ct);
        return NoContent();
    }
}

public record SetRoleRequest(string Role);
