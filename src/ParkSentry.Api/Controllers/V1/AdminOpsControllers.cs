using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ParkSentry.Application.Services;
using ParkSentry.Domain.Enums;
using ParkSentry.Infrastructure.Authorization;

namespace ParkSentry.Api.Controllers.V1;

[ApiController]
[Route("api/v1/watchlist")]
[Authorize(Policy = AuthorizationPolicies.OrgAdminOrAbove)]
public class WatchlistController(WatchlistService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WatchlistDto>>> List(CancellationToken ct) =>
        Ok(await service.ListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<WatchlistDto>> Create([FromBody] CreateWatchlistRequest request, CancellationToken ct) =>
        Ok(await service.CreateAsync(request, ct));

    [HttpPost("{id:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await service.DeactivateAsync(id, ct);
        return NoContent();
    }
}

[ApiController]
[Route("api/v1/security-events")]
[Authorize(Policy = AuthorizationPolicies.OrgAdminOrAbove)]
public class SecurityEventsController(SecurityEventService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SecurityEventDto>>> List([FromQuery] SecurityEventStatus? status, [FromQuery] int limit = 100, CancellationToken ct = default) =>
        Ok(await service.ListAsync(status, limit, ct));

    [HttpPost("{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, [FromBody] ResolveSecurityEventRequest request, CancellationToken ct)
    {
        await service.ResolveAsync(id, request.Resolution, ct);
        return NoContent();
    }
}

public record ResolveSecurityEventRequest(string Resolution);

[ApiController]
[Route("api/v1/rates")]
[Authorize(Policy = AuthorizationPolicies.OrgAdminOrAbove)]
public class ParkingRatesController(ParkingRateService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ParkingRateDto>>> List(CancellationToken ct) =>
        Ok(await service.ListAsync(ct));

    [HttpPost]
    public async Task<ActionResult<ParkingRateDto>> Create([FromBody] UpsertParkingRateRequest request, CancellationToken ct) =>
        Ok(await service.CreateAsync(request, ct));

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ParkingRateDto>> Update(Guid id, [FromBody] UpsertParkingRateRequest request, CancellationToken ct) =>
        Ok(await service.UpdateAsync(id, request, ct));
}

[ApiController]
[Route("api/v1/devices")]
[Authorize(Policy = AuthorizationPolicies.OrgAdminOrAbove)]
public class DevicesController(DeviceService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DeviceDto>>> List(CancellationToken ct) =>
        Ok(await service.ListDevicesAsync(ct));

    [HttpPost]
    public async Task<ActionResult<DeviceDto>> Register([FromBody] RegisterDeviceRequest request, CancellationToken ct) =>
        Ok(await service.RegisterAsync(request, ct));

    [HttpPost("{id:guid}/active")]
    public async Task<IActionResult> SetActive(Guid id, [FromBody] SetActiveRequest request, CancellationToken ct)
    {
        await service.SetActiveAsync(id, request.IsActive, ct);
        return NoContent();
    }

    [HttpGet("scanners")]
    public async Task<ActionResult<IReadOnlyList<ScannerConfigDto>>> Scanners(CancellationToken ct) =>
        Ok(await service.ListScannersAsync(ct));
}

public record SetActiveRequest(bool IsActive);

[ApiController]
[Route("api/v1/reports")]
[Authorize(Policy = AuthorizationPolicies.OperationalStaff)]
public class ReportsController(ReportingService service) : ControllerBase
{
    [HttpGet("operational")]
    public async Task<ActionResult<OperationalReportDto>> Operational(
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] Guid? siteId,
        CancellationToken ct) =>
        Ok(await service.GetOperationalReportAsync(fromUtc, toUtc, siteId, ct));
}

[ApiController]
[Route("api/v1/system-settings")]
[Authorize(Policy = AuthorizationPolicies.SuperAdminOnly)]
public class SystemSettingsController(SystemSettingService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SystemSettingDto>>> List(CancellationToken ct) =>
        Ok(await service.ListAsync(ct));

    [HttpPut]
    public async Task<ActionResult<SystemSettingDto>> Upsert([FromBody] UpsertSystemSettingRequest request, CancellationToken ct) =>
        Ok(await service.UpsertAsync(request.Key, request.Value, request.Description, ct));
}

public record UpsertSystemSettingRequest(string Key, string Value, string? Description);
