#!/bin/bash
# =============================================================================
# pipeline-archive.sh — Fájl housekeeping (claude nélkül, azonnali)
#
# Egyetlen felelőssége:
#   1. Outbox DONE fájl: UNREAD → READ
#   2. Hozzá tartozó stale reviewer-reject inbox-ok: UNREAD → READ
#
# Args: $1 = done_file  $2 = terminal
# Hívja: pipeline.sh
# =============================================================================

source "$(dirname "$0")/common.sh"

DONE_FILE="$1"
TERMINAL="$2"

# 1. Outbox READ
sed -i 's/status: UNREAD/status: READ/' "$DONE_FILE"
echo "$TIMESTAMP Archive: outbox READ — $(basename $DONE_FILE)" >> "$LOG_DIR/pipeline.log"

# 2. Stale reviewer-reject inbox-ok tisztítása
DONE_BASE=$(basename "$DONE_FILE" .md)
find "$SPACEOS_ROOT/docs/mailbox/${TERMINAL}/inbox/" \
  -name "*review-reject*" 2>/dev/null | while read f; do
  if grep -q "status: UNREAD" "$f" 2>/dev/null; then
    sed -i 's/status: UNREAD/status: READ/' "$f"
    echo "$TIMESTAMP Archive: stale reject READ — $(basename $f)" >> "$LOG_DIR/pipeline.log"
  fi
done
