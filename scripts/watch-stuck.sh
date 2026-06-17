#!/bin/bash
# =============================================================================
# watch-stuck.sh — Beakadt session detektor
#
# Egyetlen felelőssége: végigmegy az ismert tmux session-ökön,
# detektálja a beakadt állapotokat és Enter-t küld 5 percenként.
#
# Beakadt állapotok:
#   - "Press up to edit queued messages" prompt
#   - Model-választó dialog (claude opus/sonnet/haiku)
#   - Üres ❯ prompt
#
# Hívja: nightwatch.sh
# =============================================================================

source "$(dirname "$0")/common.sh"

for SESSION in "${!SESSIONS[@]}"; do
  TERMINAL="${SESSIONS[$SESSION]}"

  # Root kihagyása — nem kap automatikus triggert
  if [ "$SESSION" = "spaceos-root" ]; then
    continue
  fi

  if ! tmux_s has-session -t "$SESSION" 2>/dev/null; then
    continue
  fi

  PANE_OUT=$(tmux_s capture-pane -t "$SESSION" -p 2>/dev/null | tail -10)

  # ── Model-választó dialog kezelése ─────────────────────────────────────────
  if echo "$PANE_OUT" | grep -qiE "(claude opus|claude sonnet|claude haiku|Choose a model|Select model|which model|Default model)"; then
    WANTED_MODEL=$(inbox_model "$TERMINAL")
    tmux_s send-keys -t "$SESSION" "$WANTED_MODEL" Enter 2>/dev/null
    echo "$TIMESTAMP Model választó kezelve: $SESSION → $WANTED_MODEL" >> "$LOG_DIR/nightwatch.log"
    tg "🤖 *${TERMINAL^^} model választó* — \`$WANTED_MODEL\` kiválasztva"
    continue
  fi

  # ── Stuck állapot azonosítása ───────────────────────────────────────────────
  STUCK=0
  STUCK_REASON=""

  if echo "$PANE_OUT" | grep -q "Press up to edit queued messages"; then
    STUCK=1; STUCK_REASON="queued-messages prompt"
  elif echo "$PANE_OUT" | grep -qE "^\s*❯\s*$"; then
    STUCK=1; STUCK_REASON="empty prompt"
  elif echo "$PANE_OUT" | grep -qE "^\s*>\s*$"; then
    STUCK=1; STUCK_REASON="input prompt"
  elif echo "$PANE_OUT" | grep -q "shift+tab to cycle"; then
    STUCK=1; STUCK_REASON="input field"
  fi

  [ "$STUCK" -eq 0 ] && continue

  # ── Escape + Enter küldés 5 percenként ─────────────────────────────────────
  # Escape kilép a szerkesztő módból, Enter elküldi az üres promptot
  STUCK_KEY="${SESSION}_stuck_sent"
  LAST_SENT=$(grep "^${STUCK_KEY}=" "$STATE" 2>/dev/null | cut -d= -f2 || echo "0")
  ELAPSED=$(( NOW - LAST_SENT ))

  if [ "$ELAPSED" -gt 300 ]; then
    # Inbox fájl és files: mező kiolvasása
    UNREAD_INBOX=$(grep -rl "status: UNREAD" "$SPACEOS_ROOT/docs/mailbox/${TERMINAL}/inbox/" 2>/dev/null | sort | head -1)
    FILES_LIST=""
    if [ -n "$UNREAD_INBOX" ]; then
      FILES_LIST=$(grep -A20 "^---" "$UNREAD_INBOX" | grep -E "^\s*-\s" | sed 's/^\s*-\s*//' | tr '\n' ' ' | head -c 500)
    fi

    # Memory fájl ellenőrzése
    MEMORY_FILE="$SPACEOS_ROOT/docs/memory/${TERMINAL}.md"
    MEMORY_MSG=""
    [ -f "$MEMORY_FILE" ] && MEMORY_MSG="Memory: docs/memory/${TERMINAL}.md"

    # Üzenet + dupla Enter — beírja és elküldi
    STUCK_MSG="Te a ${TERMINAL^^} terminál vagy. Folytasd a munkát."
    [ -n "$MEMORY_MSG" ] && STUCK_MSG="${STUCK_MSG} ${MEMORY_MSG}"
    [ -n "$UNREAD_INBOX" ] && STUCK_MSG="${STUCK_MSG} Olvasd el: $(basename $UNREAD_INBOX)"
    [ -n "$FILES_LIST" ] && STUCK_MSG="${STUCK_MSG} Fájlok: ${FILES_LIST}"
    tmux_s send-keys -t "$SESSION" "$STUCK_MSG" 2>/dev/null
    sleep 0.5
    tmux_s send-keys -t "$SESSION" Enter 2>/dev/null
    sleep 1
    tmux_s send-keys -t "$SESSION" Enter 2>/dev/null

    if grep -q "^${STUCK_KEY}=" "$STATE" 2>/dev/null; then
      sed -i "s/^${STUCK_KEY}=.*/${STUCK_KEY}=${NOW}/" "$STATE"
    else
      echo "${STUCK_KEY}=${NOW}" >> "$STATE"
    fi
    echo "$TIMESTAMP Escape+Enter küldve: $SESSION ($STUCK_REASON)" >> "$LOG_DIR/nightwatch.log"
    tg "🔧 *${TERMINAL^^} beakadt* (${STUCK_REASON}) — Escape+Enter elküldve"
  fi
done
