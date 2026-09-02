using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Enums;
using ParkSentry.Infrastructure.Identity;

namespace ParkSentry.Web.Controllers;

[Route("account")]
[ApiController]
public class AccountController : ControllerBase
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IAuditService _audit;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        IAuditService audit,
        ILogger<AccountController> logger)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _audit = audit;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromForm] string email, [FromForm] string password, [FromForm] string? returnUrl)
    {
        var user = await _userManager.FindByEmailAsync(email);
        if (user is null || !user.IsActive)
        {
            _logger.LogWarning("Web login failed for {Email}: invalid credentials", email);
            return Redirect("/login?error=invalid");
        }

        var result = await _signInManager.PasswordSignInAsync(user, password, isPersistent: true, lockoutOnFailure: true);
        if (!result.Succeeded)
        {
            _logger.LogWarning("Web login failed for {Email}: sign-in rejected", email);
            await _audit.LogAsync(AuditAction.Login, "User", user.Id, "Failed web login attempt");
            return Redirect("/login?error=invalid");
        }

        await _audit.LogAsync(AuditAction.Login, "User", user.Id, $"Web login: {user.Email}");
        _logger.LogInformation("Web login succeeded for user {UserId}", user.Id);
        return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var user = await _userManager.GetUserAsync(User);
        await _signInManager.SignOutAsync();
        if (user is not null)
        {
            await _audit.LogAsync(AuditAction.Logout, "User", user.Id, $"Web logout: {user.Email}");
            _logger.LogInformation("Web logout for user {UserId}", user.Id);
        }
        return Redirect("/login");
    }
}
