# Deployment

## Docker Compose (Development)

```bash
docker compose up -d
```

## Production Checklist

1. Set strong `Jwt:Key` and database credentials via environment variables
2. Configure HTTPS/TLS termination (reverse proxy) — **required for camera access**
3. Run migrations: `dotnet ef database update`
4. Set `ASPNETCORE_ENVIRONMENT=Production`
5. Replace `MockPaymentProvider` with a real `IPaymentProvider`
6. Replace demo scanner with production OCR provider (Build 2C)
7. Generate PWA icons (192×192, 512×512 PNG) from `parksentrylogo.png`
8. Configure logging (structured logs to file/service)
9. Set up health check monitoring on `/health`
10. Verify SignalR WebSocket connectivity through reverse proxy

## HTTPS & Mobile Requirements

- Camera access requires a **secure context** (HTTPS in production)
- PWA installation requires valid manifest and service worker over HTTPS
- SignalR requires WebSocket or long-polling support through any reverse proxy
- See [mobile-scanning.md](mobile-scanning.md) and [pwa.md](pwa.md)

## Environment Variables

```
ConnectionStrings__DefaultConnection=Host=...;Database=parksentry;...
Jwt__Key=...
Jwt__Issuer=ParkSentry
Jwt__Audience=ParkSentry.Api
ASPNETCORE_ENVIRONMENT=Production
```

## Hosting

- **Web:** `dotnet publish src/ParkSentry.Web -c Release`
- **API:** `dotnet publish src/ParkSentry.Api -c Release`

Both can run on Windows, Linux, or macOS. Container deployment supported via standard .NET Docker images.

## PWA Assets

Place production icons at:

- `src/ParkSentry.Web/wwwroot/icons/icon-192.png`
- `src/ParkSentry.Web/wwwroot/icons/icon-512.png`

Update `manifest.json` to reference PNG icons when available. Build 2B ships SVG placeholders derived from the ParkSentry brand colors until PNG exports are supplied.
