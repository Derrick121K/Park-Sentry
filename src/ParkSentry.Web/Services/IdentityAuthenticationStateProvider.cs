using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using ParkSentry.Domain.Constants;
using ParkSentry.Infrastructure.Identity;

namespace ParkSentry.Web.Services;

public class IdentityAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IdentityAuthenticationStateProvider(UserManager<ApplicationUser> userManager, IHttpContextAccessor httpContextAccessor)
    {
        _userManager = userManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated != true)
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var appUser = await _userManager.GetUserAsync(user);
        if (appUser is null)
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));

        var roles = await _userManager.GetRolesAsync(appUser);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, appUser.Id),
            new(ClaimTypes.Email, appUser.Email ?? string.Empty),
            new(ClaimTypes.Name, appUser.DisplayName)
        };

        if (appUser.OrganizationId.HasValue)
            claims.Add(new Claim(TenantConstants.OrganizationIdClaimType, appUser.OrganizationId.Value.ToString()));

        foreach (var role in roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        var identity = new ClaimsIdentity(claims, "Identity.Application");
        return new AuthenticationState(new ClaimsPrincipal(identity));
    }

    public void NotifyAuthenticationStateChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
