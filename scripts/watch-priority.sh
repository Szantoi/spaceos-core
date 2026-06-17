#!/bin/bash
# =============================================================================
# watch-priority.sh — Prioritásos session-ök kezelése
#
# Egyetlen felelőssége:
#   A Root és Conductor session-ök MINDIG futnak.
#   Ha leálltak, automatikusan újraindítja őket.
#
# Hívja: nightwatch.sh (elsőként)
# =============================================================================

source "$(dirname "$0")/common.sh"

for SESSION in "${PRIORITY_SESSIONS[@]}"; do
  TERMINAL="${SESSIONS[$SESSION]}"
  WORKDIR="${SESSION_WORKDIR[$SESSION]:-$SPACEOS_ROOT}"

  # Ellenőrzés: fut-e a session?
  if tmux_s has-session -t "$SESSION" 2>/dev/null; then
    # Session fut — nincs teendő
    continue
  fi

  # Session nem fut → indítás
  WANTED_MODEL=$(inbox_model "$TERMINAL")
  # Ha nincs inbox, alapértelmezett modell
  [ "$WANTED_MODEL" = "sonnet" ] || WANTED_MODEL="sonnet"

  tmux_s new-session -d -s "$SESSION" -c "$WORKDIR" 2>/dev/null
  sleep 1
  tmux_s send-keys -t "$SESSION" "claude --model $WANTED_MODEL" 2>/dev/null
  sleep 0.5
  tmux_s send-keys -t "$SESSION" Enter 2>/dev/null

  echo "$TIMESTAMP Priority session indítva: $SESSION (model: $WANTED_MODEL)" >> "$LOG_DIR/nightwatch.log"
  tg "🚀 *${TERMINAL^^} priority session indítva*\nModell: \`$WANTED_MODEL\`"
done
