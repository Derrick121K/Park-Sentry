# API Reference

Base URL: `/api/v1`

## Authentication

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/auth/login` | Login, returns JWT |
| GET | `/auth/me` | Current user info |

## Resources

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET/POST | `/organizations` | SuperAdmin | Manage organizations |
| GET/POST | `/sites` | SiteManager+ | Manage sites |
| GET | `/parking/sites/{id}/bays` | Guard+ | List bays |
| GET/POST | `/vehicles` | Guard+ | Vehicle management |
| GET/POST | `/sessions` | Guard+ | Parking sessions |
| POST | `/sessions/entry` | Guard+ | Vehicle entry |
| POST | `/sessions/exit` | Guard+ | Vehicle exit |
| GET | `/dashboard/stats` | Authenticated | Dashboard metrics |
| GET | `/dashboard/audit` | OrgAdmin+ | Audit logs |

## OpenAPI

Swagger UI available at `/swagger` in Development.

## JWT Configuration

```json
{
  "Jwt": {
    "Key": "your-secure-key-min-32-chars",
    "Issuer": "ParkSentry",
    "Audience": "ParkSentry.Api",
    "ExpiryMinutes": "60"
  }
}
```
