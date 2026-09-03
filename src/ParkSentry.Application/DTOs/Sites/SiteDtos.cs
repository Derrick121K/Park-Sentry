namespace ParkSentry.Application.DTOs.Sites;

public record SiteDto(Guid Id, Guid OrganizationId, string Name, string? Address, bool IsActive, int TotalBays, int AvailableBays, int OccupiedBays, string? Description = null);
public record CreateSiteRequest(string Name, string? Description, string? Address);
public record UpdateSiteRequest(string Name, string? Description, string? Address, bool IsActive);
