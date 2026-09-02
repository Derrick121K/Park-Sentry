# Real-Time Operations

## Overview

ParkSentry uses SignalR for organization-scoped real-time parking updates.

## Architecture

```
ParkingSessionService (Application)
    ↓ IParkingNotificationService
SignalRParkingNotificationService (Web)
    ↓ IHubContext<ParkingHub>
ParkingHub → org-{organizationId} group
    ↓ ParkingUpdate event
ParkingRealtimeService (Web client)
    ↓
Dashboard / Availability / Guard pages
```

## Security (Build 2A — preserved)

- `ParkingHub` requires `[Authorize]`
- `JoinOrganization` validates membership server-side
- Clients cannot subscribe to another organization's group
- Anonymous connections rejected

## Events

| Event Type | Trigger |
|------------|---------|
| `entry` | Vehicle entry recorded |
| `exit` | Vehicle exit completed |
| `payment` | Payment processed |
| `bay` | Bay status changed |

## Idempotency

Duplicate exit/payment requests with the same idempotency key do **not** emit duplicate notifications.

## Client Subscription

`ParkingRealtimeService` connects authenticated users to their organization group. Pages subscribe via `ParkingUpdateReceived` event.

## API vs Web

- **Web**: `SignalRParkingNotificationService` broadcasts events
- **API**: `NullParkingNotificationService` (no-op) — API clients do not trigger Web SignalR directly in Build 2B

## Tenant Isolation

Events are scoped to `org-{organizationId}` groups. Cross-tenant event leakage is prevented by server-side group membership validation.
