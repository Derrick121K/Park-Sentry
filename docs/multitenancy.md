# Multi-Tenancy

## Strategy

Shared database, shared schema, row-level isolation via `OrganizationId`.

## Implementation

### ITenantEntity
All tenant-owned entities implement:
```csharp
public interface ITenantEntity
{
    Guid OrganizationId { get; set; }
}
```

### ITenantContext
Resolved from HTTP context claims:
- `organization_id` claim from JWT/cookie auth
- `IsSuperAdmin` bypasses tenant filters

### EF Core Global Query Filters
Applied in `ParkSentryDbContext` for automatic scoping:
```csharp
builder.Entity<Site>().HasQueryFilter(e =>
    !_tenantContext.OrganizationId.HasValue
    || _tenantContext.IsSuperAdmin
    || e.OrganizationId == _tenantContext.OrganizationId);
```

## Rules

1. **Never** accept `OrganizationId` from client request body for authorization
2. Super Admin can manage all organizations via dedicated queries
3. All API operations verify permissions server-side
4. Integration tests must prove Org A cannot access Org B data

## White-Label (Future)

`OrganizationBranding` stores per-tenant:
- Logo, primary/secondary colours
- Display name, terminology, terms & conditions

Default branding: ParkSentry dark blue (#1B2A4A) + teal (#00A896).
