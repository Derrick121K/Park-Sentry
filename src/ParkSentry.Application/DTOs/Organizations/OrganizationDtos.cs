namespace ParkSentry.Application.DTOs.Organizations;

public record OrganizationDto(Guid Id, string Name, string DisplayName, string Currency, string Timezone, bool IsActive);
public record CreateOrganizationRequest(string Name, string DisplayName, string? ContactEmail, string Currency = "ZAR", string Timezone = "Africa/Johannesburg");
public record OrganizationDetailDto(Guid Id, string Name, string DisplayName, string? ContactEmail, string? ContactPhone, string? Address, string Currency, string Timezone, bool IsActive, bool SafetyFeeEnabled, decimal SafetyFeeAmount, string? LogoUrl = null, string PrimaryColor = "#1B2A4A", string SecondaryColor = "#00A896", string? AccentColor = null);
public record UpdateOrganizationRequest(string DisplayName, string? ContactEmail, string? ContactPhone, string? Address, bool IsActive, bool SafetyFeeEnabled, decimal SafetyFeeAmount, string? Currency = null, string? Timezone = null);
public record UpdateBrandingRequest(string? LogoUrl, string PrimaryColor, string SecondaryColor, string? AccentColor);
