using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
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

    public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, JwtTokenService jwtTokenService, IAuditService audit)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtTokenService = jwtTokenService;
        _audit = audit;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
            return Unauthorized(new { error = "Invalid credentials." });

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return Unauthorized(new { error = "Invalid credentials." });

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
