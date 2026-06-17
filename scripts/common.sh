#!/bin/bash
# =============================================================================
# common.sh — SpaceOS szkript közös alap
#
# Minden watch-*.sh és pipeline-*.sh source-olja ezt.
# Tartalmazza: ENV, tmux wrapper, Telegram, inbox model olvasó, SESSIONS map.
# =============================================================================

SPACEOS_ROOT="${SPACEOS_ROOT:-/opt/spaceos}"
CONF="$SPACEOS_ROOT/scripts/telegram.conf"
STATE="$SPACEOS_ROOT/scripts/.nightwatch-state"
LOG_DIR="$SPACEOS_ROOT/logs/dispatcher"
TMUX_SOCK="/tmp/spaceos.tmux"

source "$CONF" 2>/dev/null || exit 1
mkdir -p "$LOG_DIR"

NOW=$(date +%s)
TIMESTAMP=$(date +"%Y-%m-%d %H:%M:%S")

# tmux wrapper — mindig a spaceos socketet használja
tmux_s() { tmux -S "$TMUX_SOCK" "$@"; }

# Telegram üzenet küldő
tg() {
  curl -s -X POST "https://api.telegram.org/bot${TELEGRAM_TOKEN}/sendMessage" \
    -d chat_id="$TELEGRAM_CHAT_ID" \
    --data-urlencode "text=$1" \
    -d parse_mode="Markdown" -o /dev/null
}

# A legfrissebb UNREAD inbox fájl model: mezőjét olvassa ki
# Fallback: sonnet
inbox_model() {
  local terminal="$1"
  local inbox_file model
  inbox_file=$(grep -rl "status: UNREAD" "$SPACEOS_ROOT/docs/mailbox/${terminal}/inbox/" 2>/dev/null | sort | head -1)
  if [ -n "$inbox_file" ]; then
    model=$(grep -m1 "^model:" "$inbox_file" 2>/dev/null | sed 's/model:\s*//' | tr -d '[:space:]')
    echo "${model:-sonnet}"
  else
    echo "sonnet"
  fi
}

# Ismert session → terminál hozzárendelések
declare -A SESSIONS=(
  [spaceos-fe]="fe"
  [spaceos-fe-b]="fe2"
  [spaceos-architect]="architect"
  [spaceos-root]="root"
  [spaceos-conductor]="conductor"
  [spaceos-kernel]="kernel"
  [spaceos-identity]="identity"
  [spaceos-orchestrator]="orchestrator"
  [spaceos-joinery]="joinery"
  [spaceos-cutting]="cutting"
  [spaceos-infra]="infra"
  [spaceos-e2e]="e2e"
  [spaceos-librarian]="librarian"
  [spaceos-nexus]="nexus"
)

# Prioritásos session-ök — ezek indulnak ELSŐKÉNT
# Root NINCS itt, mert stratégiai döntéseket hoz, nem kell mindig futni
# A Conductor koordinálja a terminálokat és feldolgozza a DONE-okat
PRIORITY_SESSIONS=("spaceos-conductor")

# Session → munkamappa hozzárendelések
declare -A SESSION_WORKDIR=(
  [spaceos-fe]="$SPACEOS_ROOT/spaceos-doorstar-portal"
  [spaceos-fe-b]="$SPACEOS_ROOT/spaceos-doorstar-portal"
  [spaceos-architect]="$SPACEOS_ROOT"
  [spaceos-root]="$SPACEOS_ROOT"
  [spaceos-conductor]="$SPACEOS_ROOT"
  [spaceos-kernel]="$SPACEOS_ROOT/SpaceOS.Kernel"
  [spaceos-identity]="$SPACEOS_ROOT/SpaceOS.Kernel"
  [spaceos-orchestrator]="$SPACEOS_ROOT/spaceos-orchestrator"
  [spaceos-joinery]="$SPACEOS_ROOT/spaceos-modules-joinery"
  [spaceos-cutting]="$SPACEOS_ROOT/spaceos-modules-cutting"
  [spaceos-infra]="$SPACEOS_ROOT/infra"
  [spaceos-e2e]="$SPACEOS_ROOT/e2e"
  [spaceos-librarian]="$SPACEOS_ROOT"
  [spaceos-nexus]="$SPACEOS_ROOT/spaceos-nexus"
)

# Terminálok amik CSAK feladattal indulnak (nem priority, nem always-on)
# A priority session-ök (root, conductor) mindig futnak
# A többi csak ha van UNREAD inbox üzenet
TASK_ONLY_TERMINALS=("fe" "fe2" "kernel" "identity" "joinery" "cutting" "infra" "e2e" "nexus" "architect" "librarian" "orchestrator")
