#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"
source "$ROOT_DIR/scripts/lib/docker.sh"

echo "[deploy] Starting PostgreSQL..."
ensure_docker_ready
compose_up_postgres
wait_for_postgres_health

echo "[deploy] Restoring backend..."
dotnet restore FinancialInsightsApp.sln

echo "[deploy] Applying migrations..."
dotnet ef database update --project backend/FinancialInsights.Api/FinancialInsights.Api.csproj --startup-project backend/FinancialInsights.Api/FinancialInsights.Api.csproj

echo "[deploy] Publishing backend..."
dotnet publish backend/FinancialInsights.Api/FinancialInsights.Api.csproj -c Release -o artifacts/backend

echo "[deploy] Installing frontend dependencies..."
cd frontend
npm install

echo "[deploy] Building frontend..."
npm run build

cd "$ROOT_DIR"
echo "[deploy] Complete. Artifacts available in artifacts/backend and frontend/dist."
