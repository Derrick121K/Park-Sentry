# Security

## Authentication

- **Web:** ASP.NET Core Identity with cookie authentication
- **API:** JWT Bearer tokens

## Authorization

Role-based policies:
- `SUPER_ADMIN` — Platform administration
- `ORGANIZATION_ADMIN` — Tenant administration
- `SITE_MANAGER` — Site management
- `SECURITY_GUARD` / `PARKING_ATTENDANT` — Operations
- `CUSTOMER` — Self-service (future)

## Tenant Isolation

- Never trust `OrganizationId` from client requests
- Resolved from authenticated user claims via `ITenantContext`
- EF Core global query filters on all `ITenantEntity` types
- Integration tests verify cross-tenant access is blocked

## Data Protection

- Passwords hashed by Identity (never stored plain)
- No raw card data stored
- Secrets in environment variables / `.env` (never committed)
- Safe error responses (no stack traces to users)

## Audit

Append-only `AuditLogs` for login, entry/exit, payments, configuration changes.

## Production release notes

- FluentAssertions pinned to 7.x (Apache 2.0) for commercial use without Xceed licensing.
- Production payments default to Manual; Mock requires explicit AllowDemoProviders.
- Cookie auth uses HttpOnly + Secure (Production) + SameSite=Lax.
- Security headers middleware and auth rate limiting are enabled.
- Secrets must come from environment / user secrets / platform secret store � never source control.
