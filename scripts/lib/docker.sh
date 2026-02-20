#!/usr/bin/env bash

ensure_docker_ready() {
  if docker info >/dev/null 2>&1; then
    return 0
  fi

  echo "[docker] Docker daemon is not reachable."

  if command -v colima >/dev/null 2>&1; then
    echo "[docker] Attempting to start Colima..."
    if colima start >/tmp/financial-insights-colima.log 2>&1; then
      if docker info >/dev/null 2>&1; then
        echo "[docker] Colima started successfully."
        return 0
      fi
    fi

    echo "[docker] Unable to start Colima automatically."
    echo "[docker] Colima log: /tmp/financial-insights-colima.log"
  fi

  cat <<'MSG'
[docker] Start a Docker runtime, then retry:
  Option 1: Docker Desktop
    - Open Docker Desktop and wait until status is "Engine running".

  Option 2: Colima
    - Run: colima start --vm-type=qemu
      (If your current Colima profile uses VZ and fails to boot, QEMU is more compatible.)

After Docker is running, re-run this script.
MSG

  return 1
}

compose_up_postgres() {
  if docker compose version >/dev/null 2>&1; then
    docker compose up -d postgres
    return 0
  fi

  if command -v docker-compose >/dev/null 2>&1; then
    docker-compose up -d postgres
    return 0
  fi

  echo "[docker] Neither 'docker compose' nor 'docker-compose' is available."
  return 1
}

wait_for_postgres_health() {
  local container_name="financial-insights-postgres"
  local retries=90

  echo "[docker] Waiting for PostgreSQL health..."
  for _ in $(seq 1 "$retries"); do
    local status
    status=$(docker inspect --format='{{json .State.Health.Status}}' "$container_name" 2>/dev/null || echo '"starting"')
    if [ "$status" = '"healthy"' ]; then
      echo "[docker] PostgreSQL is healthy."
      return 0
    fi
    sleep 2
  done

  echo "[docker] PostgreSQL did not become healthy in time."
  return 1
}
