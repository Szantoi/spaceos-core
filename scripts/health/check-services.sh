#!/bin/bash
# scripts/health/check-services.sh
# Leírás: Service health check
# Használat: ./check-services.sh
# Példa: ./check-services.sh

set -euo pipefail

echo "=== Service Health ==="

# Knowledge Service
echo -n "Knowledge Service (3456): "
response=$(curl -sf http://localhost:3456/health 2>/dev/null || echo "")
if echo "$response" | grep -q '"status":"ok"'; then
  echo "OK"
else
  echo "DOWN"
fi

# Datahaven
echo -n "Datahaven (3457): "
response=$(curl -sf http://localhost:3457/health 2>/dev/null || echo "")
if [ -n "$response" ]; then
  echo "OK"
else
  echo "N/A"
fi

# Remote Datahaven
echo -n "Datahaven Remote: "
response=$(curl -sf https://datahaven.joinerytech.hu/api/health 2>/dev/null || echo "")
if [ -n "$response" ]; then
  echo "OK"
else
  echo "N/A"
fi
