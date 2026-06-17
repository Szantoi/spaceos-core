#!/bin/bash
# =============================================================================
# telegram-bot.sh — Kétirányú Telegram interface
#
# Fogadja a Root Telegram csatornán bejövő üzeneteket és válaszol.
# Cron: * * * * * bash /opt/spaceos/scripts/telegram-bot.sh
#
# Parancsok:
#   /status    → status.sh kimenet
#   /sessions  → futó tmux sessionök
#   /nexus     → Nexus utolsó aktivitás
#   /log       → pipeline.log utolsó 5 sor
#   /ideas     → összegyűlt planning ötletek száma
#   /help      → parancsok listája
#   bármi más  → Root tmux sessionbe küldi
# =============================================================================

source "$(dirname "$0")/common.sh"

OFFSET_FILE="$SPACEOS_ROOT/scripts/.telegram-offset"
OFFSET=$(cat "$OFFSET_FILE" 2>/dev/null || echo "0")

# Bejövő üzenetek lekérdezése
UPDATES=$(curl -s "https://api.telegram.org/bot${TELEGRAM_TOKEN}/getUpdates?offset=$((OFFSET+1))&timeout=0")

COUNT=$(echo "$UPDATES" | python3 -c "import sys,json; d=json.load(sys.stdin); print(len(d['result']))" 2>/dev/null || echo "0")
[ "$COUNT" = "0" ] && exit 0

# Minden üzenet feldolgozása
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

  [ "$CHAT_ID" != "$TELEGRAM_CHAT_ID" ] && continue
  [ -z "$TEXT" ] && continue

  REPLY=""

  case "$TEXT" in
    /status)
      REPLY=$(bash "$SPACEOS_ROOT/scripts/status.sh" 2>/dev/null \
        | sed 's/\x1B\[[0-9;]*m//g' \
        | head -50)
      ;;

    /sessions)
      SESSIONS_OUT=""
      tmux_s list-sessions 2>/dev/null | while read line; do
        sess=$(echo "$line" | cut -d: -f1)
        if tmux_s list-panes -t "$sess" -F "#{pane_current_command}" 2>/dev/null | grep -q "claude"; then
          echo "● $sess (claude fut)"
        else
          echo "○ $sess (idle)"
        fi
      done
      REPLY="Futó sessionök:
$(tmux_s list-sessions 2>/dev/null | cut -d: -f1 | while read s; do
  tmux_s list-panes -t "$s" -F "#{pane_current_command}" 2>/dev/null | grep -q "claude" \
    && echo "● $s" || echo "○ $s (idle)"
done)"
      ;;

    /nexus)
      NEXUS_INBOX=$(grep -rl "status: UNREAD" "$SPACEOS_ROOT/docs/mailbox/nexus/inbox/" 2>/dev/null | wc -l)
      ROADMAP_DONE=$(grep -c "\[x\]" "$SPACEOS_ROOT/docs/agent-infrastructure/ROADMAP.md" 2>/dev/null || echo 0)
      ROADMAP_TOTAL=$(grep -c "\[ \]\|\[x\]" "$SPACEOS_ROOT/docs/agent-infrastructure/ROADMAP.md" 2>/dev/null || echo 0)
      REPLY="Nexus / Datahaven:
Inbox: $NEXUS_INBOX UNREAD feladat
Roadmap: $ROADMAP_DONE/$ROADMAP_TOTAL checkpoint kész"
      ;;

    /log)
      REPLY="Pipeline log (utolsó 5):
$(tail -5 "$LOG_DIR/pipeline.log" 2>/dev/null)"
      ;;

    /ideas)
      IDEAS=$(ls "$SPACEOS_ROOT/docs/planning/ideas/"*.md 2>/dev/null | wc -l)
      LAST_SEG=$(grep "^last_segment=" "$SPACEOS_ROOT/scripts/.plan-scan-state" 2>/dev/null | cut -d= -f2)
      SEGS=("api-status" "fe-memory" "kernel-memory" "joinery-memory" "knowledge-adr" "knowledge-api" "knowledge-patterns" "infra-memory" "datahaven-resonance")
      REPLY="Planning pipeline:
Osszeegyult otletek: $IDEAS / 5
Utolso szegmens: ${SEGS[$LAST_SEG]:-?}
5 otletnel debate indul automatikusan."
      ;;

    /help)
      REPLY="Sarkany parancsok:
/status    teljes attekintes
/sessions  futó daemon sessionok
/nexus     Datahaven allapot
/log       pipeline log
/ideas     planning pipeline
/help      ez a lista

Szabad szoveget is kuldhetsz — Root sessionbe kerul."
      ;;

    /*)
      REPLY="Ismeretlen parancs. Probald: /help"
      ;;

    *)
      tmux_s send-keys -t spaceos-root "$TEXT" 2>/dev/null
      sleep 0.5
      tmux_s send-keys -t spaceos-root Enter 2>/dev/null
      sleep 1
      tmux_s send-keys -t spaceos-root Enter 2>/dev/null
      REPLY="Root sessionbe tovabbitva."
      ;;
  esac

  if [ -n "$REPLY" ]; then
    curl -s -X POST "https://api.telegram.org/bot${TELEGRAM_TOKEN}/sendMessage" \
      -d chat_id="$CHAT_ID" \
      --data-urlencode "text=$REPLY" \
      -o /dev/null
  fi

done
