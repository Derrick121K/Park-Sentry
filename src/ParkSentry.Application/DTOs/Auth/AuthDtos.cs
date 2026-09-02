namespace ParkSentry.Application.DTOs.Auth;

public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, string Email, string DisplayName, IEnumerable<string> Roles, Guid? OrganizationId);
public record UserInfoDto(string Id, string Email, string DisplayName, IEnumerable<string> Roles, Guid? OrganizationId);
