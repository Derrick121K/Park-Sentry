#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
COMPOSE_FILE="$ROOT/deploy/docker/docker-compose.prod.yml"
ENV_FILE="${ENV_FILE:-$ROOT/deploy/.env.production}"

if [[ ! -f "$ENV_FILE" ]]; then
  echo "Missing env file: $ENV_FILE"
  echo "Copy deploy/.env.production.example and set secrets."
  exit 1
fi

required=(POSTGRES_PASSWORD JWT_KEY PUBLIC_ORIGIN PUBLIC_HOST)
# shellcheck disable=SC1090
set -a; source "$ENV_FILE"; set +a
for key in "${required[@]}"; do
  if [[ -z "${!key:-}" ]]; then
    echo "Missing required variable: $key"
    exit 1
  fi
done

docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" pull || true
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" build
docker compose --env-file "$ENV_FILE" -f "$COMPOSE_FILE" up -d

echo "Deployed. Check https://$PUBLIC_HOST/health and /health/ready"
