#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"
source "$ROOT_DIR/scripts/lib/docker.sh"

export ASPNETCORE_ENVIRONMENT=Development
export ASPNETCORE_URLS=http://127.0.0.1:5072
export VITE_API_BASE_URL=http://127.0.0.1:5072

echo "[run] Starting PostgreSQL..."
ensure_docker_ready
compose_up_postgres
wait_for_postgres_health

echo "[run] Applying migrations..."
dotnet ef database update --project backend/FinancialInsights.Api/FinancialInsights.Api.csproj --startup-project backend/FinancialInsights.Api/FinancialInsights.Api.csproj

echo "[run] Ensuring frontend deps are installed..."
cd frontend
npm install
cd "$ROOT_DIR"

echo "[run] Starting backend and frontend..."
dotnet run --no-launch-profile --project backend/FinancialInsights.Api/FinancialInsights.Api.csproj > /tmp/financial-insights-backend.log 2>&1 &
BACKEND_PID=$!

cd frontend
npm run dev -- --host 127.0.0.1 --port 4173 > /tmp/financial-insights-frontend.log 2>&1 &
FRONTEND_PID=$!
cd "$ROOT_DIR"

cleanup() {
  echo "[run] Stopping processes..."
  kill "$BACKEND_PID" "$FRONTEND_PID" >/dev/null 2>&1 || true
}

trap cleanup EXIT INT TERM

echo "[run] Backend:  http://127.0.0.1:5072"
echo "[run] Frontend: http://127.0.0.1:4173"
echo "[run] Logs: /tmp/financial-insights-backend.log and /tmp/financial-insights-frontend.log"

wait "$BACKEND_PID" "$FRONTEND_PID"
