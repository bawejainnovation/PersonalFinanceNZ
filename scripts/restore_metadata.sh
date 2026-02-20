#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"
source "$ROOT_DIR/scripts/lib/docker.sh"

BACKUP_ARG="${1:-latest}"
BACKUP_DIR="$ROOT_DIR/backups/metadata"

if [ ! -d "$BACKUP_DIR" ]; then
  echo "[restore] Backup directory does not exist: $BACKUP_DIR"
  exit 1
fi

if [ "$BACKUP_ARG" = "latest" ]; then
  BACKUP_FILE="$(ls -1t "$BACKUP_DIR"/metadata_*.sql 2>/dev/null | head -n 1 || true)"
else
  BACKUP_FILE="$BACKUP_ARG"
  if [ ! -f "$BACKUP_FILE" ]; then
    BACKUP_FILE="$BACKUP_DIR/$BACKUP_ARG"
  fi
fi

if [ -z "${BACKUP_FILE:-}" ] || [ ! -f "$BACKUP_FILE" ]; then
  echo "[restore] Backup file not found. Provide a file path or ensure backups exist in $BACKUP_DIR."
  exit 1
fi

ensure_docker_ready
compose_up_postgres
wait_for_postgres_health

echo "[restore] Restoring metadata from: $BACKUP_FILE"
docker exec -i financial-insights-postgres psql -v ON_ERROR_STOP=1 -U postgres -d financial_insights -c 'TRUNCATE TABLE "TransactionAnnotations", "AccountProfiles", "Categories";'
docker exec -i financial-insights-postgres psql -v ON_ERROR_STOP=1 -U postgres -d financial_insights < "$BACKUP_FILE"

echo "[restore] Metadata restore complete."
