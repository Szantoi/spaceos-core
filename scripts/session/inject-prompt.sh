#!/bin/bash
# scripts/session/inject-prompt.sh
# Leírás: Prompt küldése futó session-be
# Használat: ./inject-prompt.sh <terminal> <prompt>
# Példa: ./inject-prompt.sh backend "Dolgozd fel az inbox-ot"

set -euo pipefail

TERMINAL="$1"
PROMPT="$2"
SOCKET="/opt/spaceos/sockets/spaceos.sock"
SESSION="spaceos-$TERMINAL"

# Ellenőrzés: session fut?
if ! tmux -S "$SOCKET" has-session -t "$SESSION" 2>/dev/null; then
  echo "ERROR: Session $SESSION not running"
  exit 1
fi

# Prompt küldése
tmux -S "$SOCKET" send-keys -t "$SESSION" "$PROMPT" Enter

echo "Injected to $SESSION: $PROMPT"
