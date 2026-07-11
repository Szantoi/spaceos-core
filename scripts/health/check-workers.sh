#!/bin/bash
# scripts/health/check-workers.sh
# Leírás: ADR-049 Worker státusz check
# Használat: ./check-workers.sh [terminal]
# Példa: ./check-workers.sh backend

set -euo pipefail

TERMINAL="${1:-}"

echo "=== Worker Status ==="

if [ -n "$TERMINAL" ]; then
  # Specifikus terminál
  status=$(curl -sf "http://localhost:3456/api/dashboard/workers?terminal=$TERMINAL" 2>/dev/null || echo "{}")
  active=$(echo "$status" | grep -o '"activeCount":[0-9]*' | cut -d: -f2 || echo "0")
  cost=$(echo "$status" | grep -o '"currentHourlyCost":[0-9.]*' | cut -d: -f2 || echo "0")
  alert=$(echo "$status" | grep -o '"alertLevel":"[^"]*"' | cut -d'"' -f4 || echo "ok")

  echo "$TERMINAL:"
  echo "  Active: $active"
  echo "  Cost: \$$cost/h"
  echo "  Alert: $alert"
else
  # Minden terminál
  for term in backend frontend architect librarian explorer designer; do
    status=$(curl -sf "http://localhost:3456/api/dashboard/workers?terminal=$term" 2>/dev/null || echo "{}")
    active=$(echo "$status" | grep -o '"activeCount":[0-9]*' | cut -d: -f2 || echo "0")
    cost=$(echo "$status" | grep -o '"currentHourlyCost":[0-9.]*' | cut -d: -f2 || echo "0")
    alert=$(echo "$status" | grep -o '"alertLevel":"[^"]*"' | cut -d'"' -f4 || echo "ok")

    [ "$active" != "0" ] && [ -n "$active" ] && echo "$term: $active workers, \$$cost/h, alert: $alert"
  done

  # Összesítés
  total=$(curl -sf http://localhost:3456/api/dashboard/workers 2>/dev/null || echo "{}")
  total_active=$(echo "$total" | grep -o '"totalActiveWorkers":[0-9]*' | cut -d: -f2 || echo "0")
  total_cost=$(echo "$total" | grep -o '"totalHourlyCost":"[^"]*"' | cut -d'"' -f4 || echo "0")

  echo ""
  echo "Total: $total_active active, \$$total_cost/h"
fi
