#!/usr/bin/env bash
set -euo pipefail

: "${POSTGRES_USER:=parksentry}"
: "${POSTGRES_DB:=parksentry}"
: "${BACKUP_DIR:=./backups}"
STAMP="$(date -u +%Y%m%dT%H%M%SZ)"
OUT="$BACKUP_DIR/parksentry-$STAMP.sql.gz"

mkdir -p "$BACKUP_DIR"

if [[ -z "${POSTGRES_PASSWORD:-}" ]]; then
  echo "POSTGRES_PASSWORD is required"
  exit 1
fi

CONTAINER="${POSTGRES_CONTAINER:-$(docker compose -f deploy/docker/docker-compose.prod.yml ps -q postgres)}"
if [[ -z "$CONTAINER" ]]; then
  echo "Could not resolve postgres container. Set POSTGRES_CONTAINER."
  exit 1
fi

docker exec -e PGPASSWORD="$POSTGRES_PASSWORD" "$CONTAINER" \
  pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" --no-owner --format=plain \
  | gzip > "$OUT"

echo "Backup written: $OUT"
