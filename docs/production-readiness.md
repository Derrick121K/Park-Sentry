# Production readiness

ParkSentry release candidate criteria and verified posture.

## Verified

- Multi-tenant EF filters + service fail-closed org context
- JWT API + cookie Web auth with hardened cookies in Production
- Security headers, rate limiting on auth, optional CORS allow-list
- Atomic bay occupy + unique active session + payment idempotency
- SignalR join membership checks
- Health endpoints `/health`, `/health/live`, `/health/ready`
- Serilog console logging + correlation IDs
- Demo vs Production provider registration
- Docker multi-stage images + compose production stack artifacts
- CI workflow (build, unit, integration, docker build, heuristic secret scan)
- FluentAssertions 7.x (Apache 2.0) for commercial-safe tests

## Explicitly not claimed

- Live payment gateway settlement
- Production OCR extraction from licence discs
- Offline transactional sync
- Live production deployment (requires customer credentials/DNS)

## Required before customer go-live

1. Set strong `JWT_KEY` and Postgres password via secret store
2. HTTPS reverse proxy (camera + PWA require secure context)
3. Choose `Payments:Provider=Manual` or integrate a real gateway
4. Configure OCR provider only when credentials exist
5. Generate PNG PWA icons
6. Run migrations and smoke test two organizations
7. Configure backups and restore drill
