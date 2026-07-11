#!/bin/bash
# scripts/session/capture-output.sh
# Leírás: Session output mentése fájlba
# Használat: ./capture-output.sh <terminal> [lines]
# Példa: ./capture-output.sh backend 100

set -euo pipefail

TERMINAL="$1"
LINES="${2:-50}"
SOCKET="/opt/spaceos/sockets/spaceos.sock"
SESSION="spaceos-$TERMINAL"

# Ellenőrzés: session fut?
if ! tmux -S "$SOCKET" has-session -t "$SESSION" 2>/dev/null; then
  echo "ERROR: Session $SESSION not running"
  exit 1
fi

# Capture
tmux -S "$SOCKET" capture-pane -t "$SESSION" -p -S -"$LINES"
