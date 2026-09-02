using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using ParkSentry.Application.Interfaces;
using ParkSentry.Domain.Constants;
using ParkSentry.Infrastructure.Authorization;
using ParkSentry.Infrastructure.Identity;

namespace ParkSentry.Web.Hubs;

[Authorize(Policy = AuthorizationPolicies.AnyAuthenticated)]
public class ParkingHub : Hub
{
    private readonly UserManager<ApplicationUser> _userManager;

    public ParkingHub(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task JoinOrganization(string organizationId)
    {
        if (!Guid.TryParse(organizationId, out var requestedOrgId))
            throw new HubException("Invalid organization identifier.");

        var user = await _userManager.GetUserAsync(Context.User!)
            ?? throw new HubException("User not found.");

        var roles = await _userManager.GetRolesAsync(user);
        if (!OrganizationMembershipValidator.CanAccessOrganization(user, roles, requestedOrgId))
            throw new HubException("Access denied for this organization.");

        await Groups.AddToGroupAsync(Context.ConnectionId, BuildGroupName(requestedOrgId));
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }

    public static string BuildGroupName(Guid organizationId) => $"org-{organizationId}";

    public static Task NotifyParkingUpdate(IHubContext<ParkingHub> hub, Guid organizationId, string eventType, object data, CancellationToken cancellationToken = default) =>
        hub.Clients.Group(BuildGroupName(organizationId)).SendAsync("ParkingUpdate", eventType, data, cancellationToken);
}
