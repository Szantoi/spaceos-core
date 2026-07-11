#!/usr/bin/env bash
# test-rag.sh — SpaceOS Knowledge Service RAG smoke test
# Usage: ./scripts/test-rag.sh [service_url]
# Default URL: http://localhost:3456

set -euo pipefail

KS_URL="${1:-${KNOWLEDGE_SERVICE_URL:-http://localhost:3456}}"
PASS=0
FAIL=0

echo "╔══════════════════════════════════════════════════════╗"
echo "║     SpaceOS Knowledge Service — RAG Smoke Test       ║"
echo "╚══════════════════════════════════════════════════════╝"
echo "Endpoint: $KS_URL"
echo ""

# ── Helper ────────────────────────────────────────────────────────────────────
check_jq() {
  if ! command -v jq &>/dev/null; then
    echo "⚠  jq not found — install with: apt install jq"
    echo "   Continuing without pretty-print."
    JQ="cat"
  else
    JQ="jq ."
  fi
}
check_jq

# ── Health ────────────────────────────────────────────────────────────────────
echo "── Health check ─────────────────────────────────────────"
HEALTH=$(curl -sf "$KS_URL/health" || echo '{"status":"error"}')
echo "$HEALTH" | $JQ
STATUS=$(echo "$HEALTH" | grep -o '"status":"[^"]*"' | cut -d'"' -f4)
DOCS=$(echo "$HEALTH" | grep -o '"documents":[0-9]*' | cut -d: -f2)
echo ""

if [ "$STATUS" != "ok" ]; then
  echo "❌ Service not healthy — is it running?"
  exit 1
fi

if [ "${DOCS:-0}" -eq 0 ]; then
  echo "⚠  No documents indexed. Run: POST $KS_URL/api/knowledge/index"
  exit 1
fi

echo "✅ Service healthy — $DOCS documents indexed"
echo ""

# ── Search queries ────────────────────────────────────────────────────────────
run_query() {
  local label="$1"
  local query="$2"
  echo "── Query: $label ────────────────────────────────────────"
  local result
  result=$(curl -sf -X POST "$KS_URL/api/knowledge/search" \
    -H "Content-Type: application/json" \
    -d "{\"q\": \"$query\", \"topK\": 2}" || echo '{"error":"request failed"}')

  if echo "$result" | grep -q '"error"'; then
    echo "❌ $result"
    FAIL=$((FAIL + 1))
  else
    local count
    count=$(echo "$result" | grep -o '"count":[0-9]*' | cut -d: -f2)
    if [ "${count:-0}" -gt 0 ]; then
      echo "✅ $count results"
      # Print source + first 120 chars of each result
      echo "$result" | grep -o '"source":"[^"]*"' | while read -r s; do
        echo "   📄 $s"
      done
      PASS=$((PASS + 1))
    else
      echo "⚠  0 results — check knowledge base content"
      FAIL=$((FAIL + 1))
    fi
  fi
  echo ""
}

run_query "EF Core migration"      "EF Core migration hogyan kell futtatni"
run_query "React component"        "React frontend komponens minta"
run_query "testing strategy"       "tesztelési stratégia backend"
run_query ".NET dependency inject" ".NET dependency injection példa"
run_query "VPS deploy lépések"     "VPS deployment lépések production"

# ── Summary ───────────────────────────────────────────────────────────────────
echo "══════════════════════════════════════════════════════════"
echo "Results: ✅ $PASS passed  ❌ $FAIL failed"
if [ "$FAIL" -eq 0 ]; then
  echo "🎉 All tests passed — RAG service operational"
  exit 0
else
  echo "⚠  Some queries returned no results — check indexing"
  exit 1
fi
