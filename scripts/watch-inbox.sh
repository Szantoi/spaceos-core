#!/bin/bash
# =============================================================================
# watch-inbox.sh — Inbox nudge és session auto-indítás (termináloknak)
#
# Egyetlen felelőssége:
#   A) Ha session fut de van 3+ perces UNREAD inbox → Enter nudge 5 percenként
#   B) Ha session nem fut de van 2+ perces UNREAD inbox → session auto-indítás
#      a helyes modellel (inbox frontmatter model: mezőjéből)
#
# FONTOS: Priority session-ök (root, conductor) NEM itt kezelődnek!
#         Azokat a watch-priority.sh kezeli — mindig futnak.
#         Itt CSAK a TASK_ONLY_TERMINALS indulnak ha van inbox.
#
# Hívja: nightwatch.sh
# =============================================================================

source "$(dirname "$0")/common.sh"

# Helper: ellenőrzi hogy ez priority session-e
is_priority_session() {
  local session="$1"
  for ps in "${PRIORITY_SESSIONS[@]}"; do
    [ "$session" = "$ps" ] && return 0
  done
  return 1
}

for SESSION in "${!SESSIONS[@]}"; do
  # Priority session-öket kihagyjuk — azokat watch-priority.sh kezeli
  if is_priority_session "$SESSION"; then
    continue
  fi

  TERMINAL="${SESSIONS[$SESSION]}"
  SESSION_RUNNING=$(tmux_s has-session -t "$SESSION" 2>/dev/null && echo "yes" || echo "no")

  UNREAD=$(grep -rl "status: UNREAD" "$SPACEOS_ROOT/docs/mailbox/${TERMINAL}/inbox/" 2>/dev/null | sort | head -1)
  [ -z "$UNREAD" ] && continue

  FAGE=$(( NOW - $(stat -c %Y "$UNREAD" 2>/dev/null || echo "$NOW") ))

  # ── A) Session fut, de inbox 3+ perce olvasatlan → nudge ─────────────────
  if [ "$SESSION_RUNNING" = "yes" ]; then
    if [ "$FAGE" -gt 180 ]; then
      NUDGE_KEY="${SESSION}_inbox_nudge"
      LAST_NUDGE=$(grep "^${NUDGE_KEY}=" "$STATE" 2>/dev/null | cut -d= -f2 || echo "0")

      if [ $(( NOW - LAST_NUDGE )) -gt 300 ]; then
        # Fájlok kiolvasása az inbox frontmatter-ből (files: mező)
        FILES_LIST=$(grep -A20 "^---" "$UNREAD" | grep -E "^\s*-\s" | sed 's/^\s*-\s*//' | tr '\n' ' ' | head -c 500)

        NUDGE_MSG="Te a ${TERMINAL^^} terminál vagy. Olvasd be: MEMORY.md — Inbox: $(basename $UNREAD)"
        [ -n "$FILES_LIST" ] && NUDGE_MSG="${NUDGE_MSG} Fájlok: ${FILES_LIST}"
        tmux_s send-keys -t "$SESSION" "$NUDGE_MSG" 2>/dev/null
        sleep 0.5
        tmux_s send-keys -t "$SESSION" Enter 2>/dev/null
        sleep 1
        tmux_s send-keys -t "$SESSION" Enter 2>/dev/null
        if grep -q "^${NUDGE_KEY}=" "$STATE" 2>/dev/null; then
          sed -i "s/^${NUDGE_KEY}=.*/${NUDGE_KEY}=${NOW}/" "$STATE"
        else
          echo "${NUDGE_KEY}=${NOW}" >> "$STATE"
        fi
        echo "$TIMESTAMP Inbox nudge: $SESSION → $(basename $UNREAD)" >> "$LOG_DIR/nightwatch.log"
      fi
    fi
    continue
  fi

  # ── B) Session nem fut, inbox 2+ perces → auto-indítás ───────────────────
  [ "$FAGE" -lt 120 ] && continue  # 2 perc türelmi idő

  START_KEY="${SESSION}_autostart"
  LAST_START=$(grep "^${START_KEY}=" "$STATE" 2>/dev/null | cut -d= -f2 || echo "0")
  [ $(( NOW - LAST_START )) -lt 1800 ] && continue  # max 30 percenként próbál

  WANTED_MODEL=$(inbox_model "$TERMINAL")
  WORKDIR="${SESSION_WORKDIR[$SESSION]:-$SPACEOS_ROOT}"

  tmux_s new-session -d -s "$SESSION" -c "$WORKDIR" 2>/dev/null
  sleep 1
  tmux_s send-keys -t "$SESSION" "claude --model $WANTED_MODEL" 2>/dev/null
  sleep 0.5
  tmux_s send-keys -t "$SESSION" Enter 2>/dev/null

  if grep -q "^${START_KEY}=" "$STATE" 2>/dev/null; then
    sed -i "s/^${START_KEY}=.*/${START_KEY}=${NOW}/" "$STATE"
  else
    echo "${START_KEY}=${NOW}" >> "$STATE"
  fi

  echo "$TIMESTAMP Auto-indítva: $SESSION (model: $WANTED_MODEL, inbox: $(basename $UNREAD))" >> "$LOG_DIR/nightwatch.log"
  tg "🚀 *${TERMINAL^^} auto-indítva*\nModell: \`$WANTED_MODEL\`\nInbox: \`$(basename $UNREAD)\`"
done
