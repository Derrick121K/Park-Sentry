# Database

## Engine

PostgreSQL 16 via Docker Compose (default) or local install.

## Connection String

```
Host=localhost;Port=5432;Database=parksentry;Username=parksentry;Password=parksentry_dev
```

## Core Tables

- `Organizations`, `OrganizationBrandings`
- `Sites`, `ParkingAreas`, `ParkingZones`, `ParkingBays`
- `Vehicles`, `Customers`, `ParkingSessions`
- `Payments`, `PaymentItems`, `ParkingRates`
- `SecurityEvents`, `WatchlistEntries`, `AuditLogs`
- `AspNetUsers`, `AspNetRoles` (Identity)

## Migrations

```bash
dotnet ef migrations add <Name> --project src/ParkSentry.Infrastructure --startup-project src/ParkSentry.Api
dotnet ef database update --project src/ParkSentry.Infrastructure --startup-project src/ParkSentry.Api
```

## Seed Data

Auto-seeded on startup via `DataSeeder`:
- Demo organization with 2 sites
- Bays A001–A050 and B001–B050
- Tiered parking rates
- Users for all roles

## Conventions

- UTC timestamps (`CreatedAt`, `UpdatedAt`)
- Soft delete on sites, bays, vehicles
- Unique: registration per organization, bay number per zone
