# Backup and restore

## Daily backup (Docker Compose production)

```bash
export POSTGRES_PASSWORD=...
./deploy/scripts/backup.sh
```

Creates `backups/parksentry-YYYYMMDDTHHMMSSZ.sql.gz`.

Suggested retention: keep 7 daily + 4 weekly copies off-box.

## Restore

```bash
export POSTGRES_PASSWORD=...
./deploy/scripts/restore.sh backups/parksentry-....sql.gz
```

This is destructive to the target database. Take a fresh backup before restore.

## Verification

After restore:

1. `GET /health/ready`
2. Login with a known organization admin
3. Confirm sites/bays/sessions appear
4. Perform a dry-run entry/exit in a test org

Backup/restore scripts are provided; verify them in your environment before relying on them for DR.
