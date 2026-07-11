#!/bin/bash
# scripts/session/start-terminal.sh
# Leírás: Terminál session indítása
# Használat: ./start-terminal.sh <terminal> [model]
# Példa: ./start-terminal.sh backend sonnet

set -euo pipefail

TERMINAL="$1"
MODEL="${2:-sonnet}"
SOCKET="/opt/spaceos/sockets/spaceos.sock"
SESSION="spaceos-$TERMINAL"
WORKDIR="/opt/spaceos/terminals/$TERMINAL"

[ -d "$WORKDIR" ] || { echo "ERROR: Terminal '$TERMINAL' not found"; exit 1; }

# Ha már fut, skip
if tmux -S "$SOCKET" has-session -t "$SESSION" 2>/dev/null; then
  echo "Session $SESSION already running"
  exit 0
fi

# Socket dir létrehozása ha kell
mkdir -p "$(dirname "$SOCKET")"

# Indítás
tmux -S "$SOCKET" new-session -d -s "$SESSION" -c "$WORKDIR"
sleep 1
tmux -S "$SOCKET" send-keys -t "$SESSION" "claude --model $MODEL" Enter
sleep 3

echo "Started: $SESSION ($MODEL)"
echo "Attach: tmux -S $SOCKET attach -t $SESSION"
