# Deployment

## Local development database

```bash
docker compose up -d
```

Uses `postgres:16-alpine` with a healthcheck. Do not expose this Postgres to the public Internet in production.

## Production architecture

```
Internet → Caddy (HTTPS) → Web/API containers → PostgreSQL (private network) → volume + backups
```

Artifacts:

- `deploy/docker/Dockerfile.api`
- `deploy/docker/Dockerfile.web`
- `deploy/docker/docker-compose.prod.yml`
- `deploy/docker/Caddyfile`
- `deploy/scripts/deploy.sh` / `deploy.ps1`
- `deploy/scripts/backup.sh` / `restore.sh`
- `deploy/.env.production.example`

## Deploy steps

1. Copy `deploy/.env.production.example` → `deploy/.env.production` and set secrets
2. Point DNS A/AAAA records for `PUBLIC_HOST` to the VPS
3. Open firewall 80/443 only (not 5432)
4. Run `./deploy/scripts/deploy.sh` (or `deploy.ps1`)
5. Apply migrations in a controlled window
6. Execute [smoke-test.md](smoke-test.md)

## HTTPS / PWA / camera

HTTPS is mandatory in production for camera access and PWA installability.

## Payments / scanning

Production defaults:

- `Payments:Provider=Manual` (records desk payments; not a card gateway)
- `Scanning:Provider=Browser` (capture + manual confirmation; not OCR)

Do not enable Mock payments in Production without `Integrations:AllowDemoProviders=true`.

## Health

Monitor `/health/ready` behind the reverse proxy.
