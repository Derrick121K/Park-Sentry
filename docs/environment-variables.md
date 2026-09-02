# Environment variables

See also `.env.example` and `deploy/.env.production.example`.

| Variable | Purpose |
|----------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection |
| `Jwt__Key` | HMAC signing key (required, 32+ chars) |
| `Jwt__Issuer` / `Jwt__Audience` | Token validation |
| `Jwt__ExpiryMinutes` | Token lifetime |
| `Integrations__Mode` | `Demo` or `Production` |
| `Integrations__AllowDemoProviders` | Emergency only |
| `Payments__Provider` | `Mock` / `Manual` / future |
| `Payments__WebhookSecret` | Future gateway HMAC |
| `Scanning__Provider` | `Demo` / `Browser` / `Manual` / future OCR |
| `Scanning__RetainImages` | Default `false` |
| `Scanning__OcrApiKey` / `Scanning__OcrEndpoint` | Future OCR |
| `Cors__AllowedOrigins__0` | API CORS origin |
| `ASPNETCORE_ENVIRONMENT` | `Development` / `Production` / `Testing` |
| `Swagger__Enabled` | Explicit Swagger in non-Dev |
| `POSTGRES_PASSWORD` | Compose production DB |
| `PUBLIC_HOST` / `PUBLIC_ORIGIN` | Reverse proxy / CORS |

Never commit real values.
