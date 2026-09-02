#!/usr/bin/env bash
set -euo pipefail

BACKUP_FILE="${1:-}"
if [[ -z "$BACKUP_FILE" || ! -f "$BACKUP_FILE" ]]; then
  echo "Usage: $0 <backup.sql.gz>"
  exit 1
fi

: "${POSTGRES_USER:=parksentry}"
: "${POSTGRES_DB:=parksentry}"
if [[ -z "${POSTGRES_PASSWORD:-}" ]]; then
  echo "POSTGRES_PASSWORD is required"
  exit 1
fi

CONTAINER="${POSTGRES_CONTAINER:-$(docker compose -f deploy/docker/docker-compose.prod.yml ps -q postgres)}"
if [[ -z "$CONTAINER" ]]; then
  echo "Could not resolve postgres container. Set POSTGRES_CONTAINER."
  exit 1
fi

echo "Restoring $BACKUP_FILE into $POSTGRES_DB (destructive)."
gunzip -c "$BACKUP_FILE" | docker exec -i -e PGPASSWORD="$POSTGRES_PASSWORD" "$CONTAINER" \
  psql -U "$POSTGRES_USER" -d "$POSTGRES_DB"

echo "Restore completed."
