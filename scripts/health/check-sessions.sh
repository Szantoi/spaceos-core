#!/bin/bash
# scripts/health/check-sessions.sh
# Leírás: Session health és stuck detection
# Használat: ./check-sessions.sh
# Példa: ./check-sessions.sh

set -euo pipefail

SOCKET="/opt/spaceos/sockets/spaceos.sock"
STUCK_THRESHOLD=1800  # 30 perc

echo "=== Session Health ==="

if [ ! -S "$SOCKET" ]; then
  echo "No sessions (socket not found)"
  exit 0
fi

now=$(date +%s)

tmux -S "$SOCKET" ls -F "#{session_name}|#{session_activity}" 2>/dev/null | while read -r line; do
  name=$(echo "$line" | cut -d'|' -f1)
  activity=$(echo "$line" | cut -d'|' -f2)

  if [ -n "$activity" ]; then
    diff=$((now - activity))
    activity_date=$(date -d "@$activity" "+%H:%M:%S" 2>/dev/null || echo "unknown")

    if [ "$diff" -gt "$STUCK_THRESHOLD" ]; then
      echo "⚠️  STUCK: $name (${diff}s idle, last: $activity_date)"
    else
      echo "✅ OK: $name (last: $activity_date)"
    fi
  else
    echo "? $name (unknown activity)"
  fi
done
