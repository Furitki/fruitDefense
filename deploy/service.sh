#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
RUNTIME_DIR="$ROOT_DIR/runtime"
PID_FILE="$RUNTIME_DIR/fruit-defense.pid"
LOG_FILE="$RUNTIME_DIR/fruit-defense.log"

mkdir -p "$RUNTIME_DIR"

stop() {
  if [[ -f "$PID_FILE" ]]; then
    pid="$(cat "$PID_FILE" 2>/dev/null || true)"
    if [[ -n "$pid" ]] && kill -0 "$pid" 2>/dev/null; then
      kill "$pid"
      for _ in {1..20}; do
        kill -0 "$pid" 2>/dev/null || break
        sleep 0.1
      done
    fi
    rm -f "$PID_FILE"
  fi
}

start() {
  stop
  nohup env PORT=3000 STATIC_ROOT="$ROOT_DIR/dist" \
    node "$ROOT_DIR/deploy/server.mjs" >>"$LOG_FILE" 2>&1 &
  echo $! >"$PID_FILE"
  sleep 1
  kill -0 "$(cat "$PID_FILE")"
}

case "${1:-}" in
  start) start ;;
  stop) stop ;;
  restart) start ;;
  status)
    if [[ -f "$PID_FILE" ]] && kill -0 "$(cat "$PID_FILE")" 2>/dev/null; then
      echo "running pid=$(cat "$PID_FILE")"
    else
      echo "stopped"
      exit 1
    fi
    ;;
  *) echo "Usage: $0 {start|stop|restart|status}"; exit 2 ;;
esac
