# Troubleshooting

| Symptom | Likely cause | Action |
|---------|--------------|--------|
| Integration tests hang | Docker Desktop not running / image pull stall | Start Docker; check `postgres:16-alpine`; read fixture diagnostics |
| `Jwt:Key is not configured` | Missing secret | Set User Secrets or env `Jwt__Key` |
| Mock payment refused in Production | Fail-closed DI | Set `Payments:Provider=Manual` or temporary `AllowDemoProviders` |
| Camera denied | Insecure context | Serve over HTTPS; localhost exception only for local HTTPS |
| SignalR disconnects behind proxy | WebSocket not forwarded | Ensure Caddy/Nginx upgrades WebSockets |
| Cross-tenant empty results | Expected isolation | Confirm user `organization_id` claim |
| PWA not installable | Missing icons / HTTP | Use HTTPS + PNG icons in manifest |

## Logs

Apps log to stdout/stderr (Serilog). Collect via Docker/logging agent. Never log passwords, JWTs, or provider secrets.
