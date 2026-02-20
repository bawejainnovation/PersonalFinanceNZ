#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

echo "[verify] Starting PostgreSQL..."
DOCKER_AVAILABLE=true
if ! docker info >/dev/null 2>&1; then
  DOCKER_AVAILABLE=false
  echo "[verify] Docker daemon unavailable. Docker-dependent checks (health + e2e) will be skipped."
else
  docker compose up -d postgres

  echo "[verify] Waiting for PostgreSQL health..."
  until [ "$(docker inspect --format='{{json .State.Health.Status}}' financial-insights-postgres 2>/dev/null || echo '"starting"')" = '"healthy"' ]; do
    sleep 2
  done

  echo "[verify] Backing up metadata before tests..."
  ./scripts/backup_metadata.sh
fi

echo "[verify] Running backend tests..."
dotnet test backend/FinancialInsights.Api.Tests/FinancialInsights.Api.Tests.csproj

echo "[verify] Running frontend unit/UI tests..."
cd frontend
npm install
npm run test

npx playwright install chromium

cd "$ROOT_DIR"

if [ "$DOCKER_AVAILABLE" = true ]; then
  echo "[verify] Running backend health check..."
  ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://127.0.0.1:5072 dotnet run --no-launch-profile --project backend/FinancialInsights.Api/FinancialInsights.Api.csproj > /tmp/financial-insights-verify-backend.log 2>&1 &
  BACKEND_PID=$!

  cleanup() {
    kill "$BACKEND_PID" >/dev/null 2>&1 || true
  }
  trap cleanup EXIT INT TERM

  for _ in {1..30}; do
    if curl -fsS http://127.0.0.1:5072/health >/dev/null; then
      break
    fi
    sleep 1
  done

  curl -fsS http://127.0.0.1:5072/health >/dev/null
  kill "$BACKEND_PID" >/dev/null 2>&1 || true
  wait "$BACKEND_PID" 2>/dev/null || true

  trap - EXIT INT TERM

  echo "[verify] Running e2e tests..."
  cd frontend
  VITE_API_BASE_URL=http://127.0.0.1:5072 npm run e2e
else
  echo "[verify] Skipping health and e2e checks because Docker is unavailable."
fi

cd "$ROOT_DIR"
echo "[verify] All checks passed."
