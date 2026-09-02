# Operations runbook

## Services

| Service | Role |
|---------|------|
| `web` | Blazor UI + SignalR hub |
| `api` | JWT REST API |
| `postgres` | Private network only |
| `caddy` | TLS termination |

## Health

- Liveness: `/health/live`
- Readiness: `/health/ready` (includes PostgreSQL)
- Aggregate: `/health`

## Common tasks

### Apply migrations

From a release host with connectivity:

```bash
dotnet ef database update --project src/ParkSentry.Infrastructure --startup-project src/ParkSentry.Api
```

Or start apps in an ops container that runs migrate-on-start only when explicitly configured (Development does this automatically; Production should use controlled migrate).

### Restart stack

```bash
docker compose -f deploy/docker/docker-compose.prod.yml --env-file deploy/.env.production up -d
```

### Rollback application images

Redeploy previous `IMAGE_TAG` and restart. Database rollbacks require a restore from backup (do not delete migrations).

### Watchlist / security incidents

1. Open `/admin/security-events`
2. Resolve with notes
3. Adjust `/admin/watchlist` block/warn flags

## Alerts to wire

- Ready probe failures
- Container restart loops
- Disk usage on Postgres volume
- Backup job failure
