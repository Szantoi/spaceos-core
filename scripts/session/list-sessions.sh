#!/bin/bash
# scripts/session/list-sessions.sh
# Leírás: Összes SpaceOS session listázása
# Használat: ./list-sessions.sh
# Példa: ./list-sessions.sh

set -euo pipefail

SOCKET="/opt/spaceos/sockets/spaceos.sock"

echo "=== SpaceOS Sessions ==="
echo ""

if [ ! -S "$SOCKET" ]; then
  echo "No sessions (socket not found)"
  exit 0
fi

sessions=$(tmux -S "$SOCKET" ls -F "#{session_name}|#{session_activity}|#{session_windows}" 2>/dev/null || true)

if [ -z "$sessions" ]; then
  echo "No sessions running"
  exit 0
fi

echo "SESSION|LAST_ACTIVITY|WINDOWS"
echo "-------|-------------|-------"

echo "$sessions" | while read -r line; do
  name=$(echo "$line" | cut -d'|' -f1)
  activity=$(echo "$line" | cut -d'|' -f2)
  windows=$(echo "$line" | cut -d'|' -f3)

  # Activity timestamp konvertálás
  if [ -n "$activity" ]; then
    activity_date=$(date -d "@$activity" "+%H:%M:%S" 2>/dev/null || echo "$activity")
  else
    activity_date="unknown"
  fi

  echo "$name|$activity_date|$windows"
done
