#!/bin/bash
# =============================================================================
# watch-done.sh — DONE outbox scanner
#
# Egyetlen felelőssége: megtalálja az új UNREAD type:done outbox üzeneteket
# és elindítja a reviewer.sh-t mindegyikre — egyszer.
#
# Hívja: nightwatch.sh
# =============================================================================

source "$(dirname "$0")/common.sh"

SEEN_FILE="$SPACEOS_ROOT/scripts/.seen-dones"
touch "$SEEN_FILE"

UNREAD_DONES=$(grep -rl "status: UNREAD" "$SPACEOS_ROOT/docs/mailbox/*/outbox/" 2>/dev/null \
  | xargs grep -l "type: done" 2>/dev/null || true)

[ -z "$UNREAD_DONES" ] && exit 0

while IFS= read -r f; do
  BASE=$(basename "$f" .md)
  REVIEW_KEY="review_${BASE}"
  LAST_REVIEW=$(grep "^${REVIEW_KEY}=" "$STATE" 2>/dev/null | cut -d= -f2 || echo "0")

  # Review csak egyszer indul per DONE fájl
  if [ "$LAST_REVIEW" = "0" ]; then
    echo "$TIMESTAMP Review triggerelve: $BASE" >> "$LOG_DIR/nightwatch.log"
    bash "$SPACEOS_ROOT/scripts/reviewer.sh" "$f" >> "$LOG_DIR/reviewer.log" 2>&1 &
    echo "${REVIEW_KEY}=${NOW}" >> "$STATE"
  fi
done <<< "$UNREAD_DONES"
