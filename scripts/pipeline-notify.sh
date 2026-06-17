#!/bin/bash
# =============================================================================
# pipeline-notify.sh — Telegram összefoglaló
#
# Egyetlen felelőssége:
#   Telegram értesítő küldése a pipeline eredményéről.
#   Ha Slice 1 lezárult (NEXT: NONE) → Root figyelmeztetés is.
#
# Args: $1 = done_base  $2 = terminal  $3 = tests  $4 = next
# Hívja: pipeline.sh
# =============================================================================

source "$(dirname "$0")/common.sh"

# Datahaven Telegram (ha van konfig)
DH_CONF="/home/gabor/datahaven/telegram.conf"
source "$DH_CONF" 2>/dev/null

tg_dh() {
  [ -z "$DATAHAVEN_TOKEN" ] && return
  curl -s -X POST "https://api.telegram.org/bot${DATAHAVEN_TOKEN}/sendMessage" \
    -d chat_id="$DATAHAVEN_CHAT_ID" \
    --data-urlencode "text=$1" \
    -d parse_mode="Markdown" -o /dev/null
}

DONE_BASE="$1"
TERMINAL="$2"
TESTS="${3:-}"
NEXT="${4:-}"

# Datahaven/Nexus task → Datahaven csatornára
if [ "$TERMINAL" = "nexus" ]; then
  MSG="🐉 *Datahaven pipeline* — \`${DONE_BASE}\`"
  [ -n "$NEXT" ] && [ "$NEXT" != "NONE" ] && MSG="${MSG}\nKövetkező: \`${NEXT}\`"
  [ "$NEXT" = "NONE" ] || [ -z "$NEXT" ] && MSG="${MSG}\nFázis lezárva — következő fázis tervezés"
  tg_dh "$MSG"
  echo "$TIMESTAMP Datahaven notify: $DONE_BASE" >> "$LOG_DIR/pipeline.log"
  exit 0
fi

# SpaceOS task → SpaceOS csatornára
MSG="✅ *Pipeline auto-kész* — \`${DONE_BASE}\`"
[ -n "$TESTS" ] && MSG="${MSG}\nTesztek: ${TESTS}"

if [ "$NEXT" = "NONE" ] || [ -z "$NEXT" ]; then
  MSG="${MSG}\nKövetkező: Slice 2 szükséges"
  tg "$MSG"
  tg "🔔 *Root figyelmébe:* Slice 1 lezárva — Slice 2 tervezés szükséges (CRM/Finance/Project modul)"
else
  MSG="${MSG}\nKövetkező: \`${NEXT}\`"
  tg "$MSG"
fi

echo "$TIMESTAMP Notify elküldve: $DONE_BASE → NEXT: ${NEXT:-NONE}" >> "$LOG_DIR/pipeline.log"
