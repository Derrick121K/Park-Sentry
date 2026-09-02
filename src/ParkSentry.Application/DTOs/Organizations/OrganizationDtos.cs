namespace ParkSentry.Application.DTOs.Organizations;

public record OrganizationDto(Guid Id, string Name, string DisplayName, string Currency, string Timezone, bool IsActive);
public record CreateOrganizationRequest(string Name, string DisplayName, string? ContactEmail, string Currency = "ZAR", string Timezone = "Africa/Johannesburg");
public record OrganizationDetailDto(Guid Id, string Name, string DisplayName, string? ContactEmail, string? ContactPhone, string? Address, string Currency, string Timezone, bool IsActive, bool SafetyFeeEnabled, decimal SafetyFeeAmount);
