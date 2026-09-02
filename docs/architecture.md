# Architecture

## Overview

ParkSentry follows Clean Architecture with five projects:

| Layer | Responsibility |
|-------|----------------|
| Domain | Entities, enums, value objects, domain helpers |
| Application | Services, DTOs, interfaces, business logic |
| Infrastructure | EF Core, Identity, external integrations |
| Api | Versioned REST endpoints, JWT auth |
| Web | Blazor UI, cookie auth, SignalR |

## Multi-Tenancy

- Shared database with `OrganizationId` on all tenant entities
- `ITenantContext` resolved from authenticated user claims
- EF Core global query filters enforce row-level isolation
- `SUPER_ADMIN` bypasses filters for platform operations

## Key Flows

### Vehicle Entry (transactional)
1. Normalize registration number
2. Find or create vehicle
3. Check watchlist
4. Create parking session
5. Assign bay and mark occupied
6. Write audit log

### Vehicle Exit (transactional)
1. Find active session
2. Calculate parking + safety fees via `PricingService`
3. Process payment via `IPaymentProvider`
4. Close session, free bay
5. Write audit log

## Real-Time

`ParkingHub` (SignalR) broadcasts entry/exit/payment/bay events to organization groups.

Application services trigger notifications via `IParkingNotificationService`. Web implements this with SignalR; API uses a no-op implementation.

See [realtime.md](realtime.md) for details.

## Mobile & PWA

Guard operations use browser camera scanning with `ILicenceDiscScanner` abstraction. PWA support includes service worker and offline detection.

See [mobile-scanning.md](mobile-scanning.md) and [pwa.md](pwa.md).

## Extensibility

| Interface | Default Implementation |
|-----------|----------------------|
| `ILicenceDiscScanner` | `BrowserLicenceDiscScanner` (Web), `DemoLicenceDiscScanner` (API) |
| `IPaymentProvider` | `MockPaymentProvider` |
| `IPricingService` | `PricingService` |
