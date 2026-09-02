using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ParkSentry.Infrastructure.Authorization;
using ParkSentry.Application.DTOs.Auth;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Enums;
using ParkSentry.Infrastructure.Identity;

namespace ParkSentry.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly JwtTokenService _jwtTokenService;
    private readonly IAuditService _audit;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        JwtTokenService jwtTokenService,
        IAuditService audit,
        ILogger<AuthController> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _audit = audit;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("API login failed for {Email}: invalid credentials", request.Email);
            return Unauthorized(new { error = "Invalid credentials." });
        }

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            _logger.LogWarning("API login failed for {Email}: sign-in rejected", request.Email);
            await _audit.LogAsync(AuditAction.Login, "User", user.Id, "Failed login attempt");
            return Unauthorized(new { error = "Invalid credentials." });
        }

        var token = await _jwtTokenService.GenerateTokenAsync(user);
        var roles = await _userManager.GetRolesAsync(user);
        await _audit.LogAsync(AuditAction.Login, "User", user.Id, $"Login: {user.Email}");

        return Ok(new LoginResponse(token, user.Email!, user.DisplayName, roles, user.OrganizationId));
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
    public async Task<ActionResult<UserInfoDto>> Me()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null) return Unauthorized();
        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserInfoDto(user.Id, user.Email!, user.DisplayName, roles, user.OrganizationId));
    }
}
