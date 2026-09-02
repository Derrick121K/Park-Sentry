using ParkSentry.Domain.Common;

namespace ParkSentry.Domain.Entities;

public class Customer : SoftDeletableEntity, ITenantEntity
{
    public Guid OrganizationId { get; set; }
    public string? UserId { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public Organization Organization { get; set; } = null!;
    public ICollection<Vehicle> Vehicles { get; set; } = [];
}
