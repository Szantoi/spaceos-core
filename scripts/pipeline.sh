#!/bin/bash
# =============================================================================
# pipeline.sh — SpaceOS Auto Pipeline (orchestrator)
#
# Reviewer dual APPROVE után fut. Csak koordinál — a logika a lépésekben van:
#   pipeline-archive.sh   → outbox READ + stale reject cleanup (azonnali)
#   pipeline-docs.sh      → README + Codebase_Status + next inbox (claude)
#   pipeline-librarian.sh → Librarian trigger ha deploy/slice-vége (azonnali)
#   pipeline-notify.sh    → Telegram összefoglaló (azonnali)
#
# Használat: pipeline.sh <done_file_path>
# Hívja: reviewer.sh
# =============================================================================

set -uo pipefail

SPACEOS_ROOT="${SPACEOS_ROOT:-/opt/spaceos}"
SCRIPTS="$SPACEOS_ROOT/scripts"
LOG_DIR="$SPACEOS_ROOT/logs/dispatcher"
mkdir -p "$LOG_DIR"

TIMESTAMP=$(date +"%Y-%m-%d %H:%M:%S")

DONE_FILE="${1:-}"
if [ -z "$DONE_FILE" ] || [ ! -f "$DONE_FILE" ]; then
  echo "Használat: $0 <done_file>" >&2
  exit 1
fi

DONE_BASE=$(basename "$DONE_FILE" .md)
TERMINAL=$(echo "$DONE_FILE" | sed 's|.*/mailbox/\([^/]*\)/.*|\1|')

echo "$TIMESTAMP Pipeline indul: $DONE_BASE" >> "$LOG_DIR/pipeline.log"

# ── 1. Fájl housekeeping (azonnali, claude nélkül) ────────────────────────────
bash "$SCRIPTS/pipeline-archive.sh" "$DONE_FILE" "$TERMINAL"

# ── 2. Dokumentáció frissítés + következő task (claude, lassabb) ─────────────
DOCS_OUTPUT=$(bash "$SCRIPTS/pipeline-docs.sh" "$DONE_FILE" "$TERMINAL")

# Eredmény kinyerése
NEXT_INFO=$(echo "$DOCS_OUTPUT" | grep "^PIPELINE_RESULT:" | sed 's/PIPELINE_RESULT:\s*//')
TESTS=$(echo "$NEXT_INFO" | grep -o 'TESTS:[0-9]*' | cut -d: -f2)
NEXT=$(echo "$NEXT_INFO" | grep -o 'NEXT:[^|]*' | cut -d: -f2-)

echo "$TIMESTAMP Pipeline docs kész: NEXT=${NEXT:-NONE} TESTS=${TESTS:-?}" >> "$LOG_DIR/pipeline.log"

# ── 3. Telegram összefoglaló (azonnali) ──────────────────────────────────────
bash "$SCRIPTS/pipeline-notify.sh" "$DONE_BASE" "$TERMINAL" "${TESTS:-}" "${NEXT:-}"

# Librarian nem kell ide — a 0 */5 cron-librarian.sh kezeli periodikusan

echo "$TIMESTAMP Pipeline kész: $DONE_BASE" >> "$LOG_DIR/pipeline.log"
exit 0
