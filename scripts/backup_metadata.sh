#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"
source "$ROOT_DIR/scripts/lib/docker.sh"

ensure_docker_ready
compose_up_postgres
wait_for_postgres_health

BACKUP_DIR="$ROOT_DIR/backups/metadata"
mkdir -p "$BACKUP_DIR"
TIMESTAMP="$(date -u +%Y%m%dT%H%M%SZ)"
BACKUP_FILE="$BACKUP_DIR/metadata_${TIMESTAMP}.sql"

TABLES=(
  'public."AccountProfiles"'
  'public."Categories"'
  'public."TransactionAnnotations"'
)

ARGS=(
  -U postgres
  -d financial_insights
  --data-only
  --column-inserts
  --no-owner
  --no-privileges
)

for table in "${TABLES[@]}"; do
  ARGS+=("--table=$table")
done

docker exec financial-insights-postgres pg_dump "${ARGS[@]}" > "$BACKUP_FILE"

echo "[backup] Metadata backup created: $BACKUP_FILE"
