#!/bin/bash
# scripts/health/check-all.sh
# Leírás: Teljes SpaceOS rendszer health check
# Használat: ./check-all.sh
# Példa: ./check-all.sh

set -uo pipefail

SOCKET="/opt/spaceos/sockets/spaceos.sock"

echo "=== SpaceOS Health Check $(date '+%Y-%m-%d %H:%M:%S') ==="
echo ""

# Services
echo "Services:"
curl -sf http://localhost:3456/health >/dev/null 2>&1 && echo "  ✅ Knowledge Service: OK" || echo "  ❌ Knowledge Service: DOWN"
curl -sf http://localhost:3457/health >/dev/null 2>&1 && echo "  ✅ Datahaven: OK" || echo "  ⚠️  Datahaven: N/A"

# Sessions
echo ""
echo "Sessions:"
if [ -S "$SOCKET" ]; then
  session_count=$(tmux -S "$SOCKET" ls 2>/dev/null | wc -l || echo 0)
  echo "  Active: $session_count"
  tmux -S "$SOCKET" ls -F "  - #{session_name}" 2>/dev/null || true
else
  echo "  No sessions (socket not found)"
fi

# UNREAD inbox
echo ""
echo "UNREAD Inbox:"
total_unread=0
for term in root conductor backend frontend architect librarian explorer designer monitor; do
  count=$(grep -rl "status: UNREAD" /opt/spaceos/terminals/$term/inbox/ 2>/dev/null | wc -l)
  if [ "$count" -gt 0 ]; then
    echo "  $term: $count"
    total_unread=$((total_unread + count))
  fi
done
[ "$total_unread" -eq 0 ] && echo "  (none)"

# BLOCKED
echo ""
echo "BLOCKED Messages:"
blocked=$(grep -rl "type: blocked" /opt/spaceos/terminals/*/outbox/*.md 2>/dev/null || true)
if [ -n "$blocked" ]; then
  echo "$blocked" | while read -r f; do
    id=$(grep "^id:" "$f" 2>/dev/null | head -1 | cut -d: -f2 | tr -d ' ')
    echo "  🚨 $id"
  done
else
  echo "  (none)"
fi

# Workers (ADR-049)
echo ""
echo "Workers:"
workers=$(curl -sf http://localhost:3456/api/dashboard/workers 2>/dev/null || echo "{}")
active=$(echo "$workers" | grep -o '"totalActiveWorkers":[0-9]*' | cut -d: -f2 || echo "0")
cost=$(echo "$workers" | grep -o '"totalHourlyCost":"[^"]*"' | cut -d'"' -f4 || echo "0")
if [ "$active" != "0" ] && [ -n "$active" ]; then
  echo "  Active: $active"
  echo "  Cost: \$$cost/h"
else
  echo "  (none active)"
fi

# Pipeline errors (utolsó 1 óra)
echo ""
echo "Pipeline Log:"
log_file="/opt/spaceos/logs/dispatcher/pipeline.log"
if [ -f "$log_file" ]; then
  errors=$(grep -c -i "error\|fail\|exception" "$log_file" 2>/dev/null || echo 0)
  echo "  Errors in log: $errors"
else
  echo "  (no log file)"
fi

echo ""
echo "=== End Health Check ==="
