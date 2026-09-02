namespace ParkSentry.Domain.Common;

public interface ITenantEntity
{
    Guid OrganizationId { get; set; }
}
