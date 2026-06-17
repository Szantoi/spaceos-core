#!/bin/bash
# =============================================================================
# telegram-datahaven-bot.sh — Datahaven kétirányú Telegram bot
#
# Cron: * * * * * bash /opt/spaceos/scripts/telegram-datahaven-bot.sh
#
# Parancsok:
#   /status    → Nexus + Datahaven állapot
#   /roadmap   → Fázis 1/2/3 haladás
#   /log       → legutóbbi Nexus aktivitás
#   /sessions  → futó sessionök
#   bármi más  → spaceos-nexus sessionbe küldi
# =============================================================================

source "$(dirname "$0")/common.sh"

DH_CONF="/home/gabor/datahaven/telegram.conf"
source "$DH_CONF" 2>/dev/null || exit 1

OFFSET_FILE="$SPACEOS_ROOT/scripts/.telegram-dh-offset"
OFFSET=$(cat "$OFFSET_FILE" 2>/dev/null || echo "0")

tg_reply() {
  curl -s -X POST "https://api.telegram.org/bot${DATAHAVEN_TOKEN}/sendMessage" \
    -d chat_id="$DATAHAVEN_CHAT_ID" \
    --data-urlencode "text=$1" \
    -o /dev/null
}

UPDATES=$(curl -s "https://api.telegram.org/bot${DATAHAVEN_TOKEN}/getUpdates?offset=$((OFFSET+1))&timeout=0")
COUNT=$(echo "$UPDATES" | python3 -c "import sys,json; d=json.load(sys.stdin); print(len(d['result']))" 2>/dev/null || echo "0")
[ "$COUNT" = "0" ] && exit 0

echo "$UPDATES" | python3 -c "
import sys, json
d = json.load(sys.stdin)
for u in d['result']:
    uid = u['update_id']
    msg = u.get('message', {})
    text = msg.get('text', '').strip()
    chat_id = str(msg.get('chat', {}).get('id', ''))
    print(f'{uid}|||{chat_id}|||{text}')
" 2>/dev/null | while IFS='|||' read -r UPDATE_ID CHAT_ID TEXT; do

  echo "$UPDATE_ID" > "$OFFSET_FILE"
  [ "$CHAT_ID" != "$DATAHAVEN_CHAT_ID" ] && continue
  [ -z "$TEXT" ] && continue

  case "$TEXT" in
    /status|/nexus)
      NEXUS_INBOX=$(grep -rl "status: UNREAD" "$SPACEOS_ROOT/docs/mailbox/nexus/inbox/" 2>/dev/null | wc -l)
      NEXUS_STATE=$(tmux_s list-panes -t spaceos-nexus -F "#{pane_current_command}" 2>/dev/null | head -1)
      ROADMAP_DONE=$(grep -c "\[x\]" "$SPACEOS_ROOT/docs/agent-infrastructure/ROADMAP.md" 2>/dev/null || echo 0)
      ROADMAP_TOTAL=$(grep -c "\[ \]\|\[x\]" "$SPACEOS_ROOT/docs/agent-infrastructure/ROADMAP.md" 2>/dev/null || echo 0)
      tg_reply "Datahaven / Nexus:
Session: ${NEXUS_STATE:-nem fut}
Inbox: $NEXUS_INBOX UNREAD feladat
Roadmap: $ROADMAP_DONE/$ROADMAP_TOTAL checkpoint"
      ;;

    /roadmap)
      ROADMAP=$(grep -E "^\- \[.\]|^## Fázis" "$SPACEOS_ROOT/docs/agent-infrastructure/ROADMAP.md" 2>/dev/null \
        | sed 's/## /\n/' | head -30)
      tg_reply "Datahaven Roadmap:
$ROADMAP"
      ;;

    /log)
      LOG=$(tail -5 "$LOG_DIR/pipeline.log" 2>/dev/null)
      tg_reply "Legutóbbi aktivitás:
$LOG"
      ;;

    /sessions)
      OUT=$(tmux_s list-sessions 2>/dev/null | cut -d: -f1 | while read s; do
        tmux_s list-panes -t "$s" -F "#{pane_current_command}" 2>/dev/null | grep -q "claude" \
          && echo "● $s" || echo "○ $s (idle)"
      done)
      tg_reply "Futó sessionok:
$OUT"
      ;;

    /help)
      tg_reply "Datahaven parancsok:
/status    Nexus + Datahaven allapot
/roadmap   Fazis 1/2/3 haladas
/log       legutobbi aktivitas
/sessions  futó sessionok
/help      ez a lista

Szabad szoveget is kuldhetsz - Nexus sessionbe kerul."
      ;;

    /*)
      tg_reply "Ismeretlen parancs. Probald: /help"
      ;;

    *)
      tmux_s send-keys -t spaceos-nexus "$TEXT" 2>/dev/null
      sleep 0.5
      tmux_s send-keys -t spaceos-nexus Enter 2>/dev/null
      sleep 1
      tmux_s send-keys -t spaceos-nexus Enter 2>/dev/null
      tg_reply "Nexus sessionbe tovabbitva."
      ;;
  esac

done
