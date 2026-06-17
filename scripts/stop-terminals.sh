#!/bin/bash
# =============================================================================
# stop-terminals.sh — SpaceOS terminálok leállítása
#
# Használat:
#   ./stop-terminals.sh              # Minden terminál leállítása (kivéve root)
#   ./stop-terminals.sh all          # Minden terminál leállítása (root-tal együtt)
#   ./stop-terminals.sh fe conductor # Csak megadott terminálok leállítása
#
# A szkript:
#   1. Küld egy HANDOFF üzenetet a terminálnak (context mentés)
#   2. Vár 5 másodpercet
#   3. Küld Ctrl+C-t
#   4. Kilövi a tmux session-t
# =============================================================================

set -euo pipefail

SCRIPTS="$(dirname "$0")"
LOG_DIR="/opt/spaceos/logs/dispatcher"
LOG_FILE="$LOG_DIR/stop-terminals.log"

# Színek
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

log() {
    local msg="[$(date '+%Y-%m-%d %H:%M:%S')] $1"
    echo -e "$msg"
    echo "$msg" >> "$LOG_FILE" 2>/dev/null || true
}

# Prioritásos terminálok (root mindig utolsó, ha egyáltalán leállítjuk)
PRIORITY_TERMINALS=("spaceos-conductor" "spaceos-root")

# Összes terminál (prioritási sorrendben - root utolsó)
ALL_TERMINALS=(
    "spaceos-fe"
    "spaceos-kernel"
    "spaceos-joinery"
    "spaceos-cutting"
    "spaceos-identity"
    "spaceos-inventory"
    "spaceos-procurement"
    "spaceos-sales"
    "spaceos-abstractions"
    "spaceos-cabinet"
    "spaceos-infra"
    "spaceos-e2e"
    "spaceos-nexus"
    "spaceos-librarian"
    "spaceos-architect"
    "spaceos-orch"
    "spaceos-conductor"
    "spaceos-root"
)

# Terminálok leállítása root nélkül (alapértelmezett)
DEFAULT_TERMINALS=(
    "spaceos-fe"
    "spaceos-kernel"
    "spaceos-joinery"
    "spaceos-cutting"
    "spaceos-identity"
    "spaceos-inventory"
    "spaceos-procurement"
    "spaceos-sales"
    "spaceos-abstractions"
    "spaceos-cabinet"
    "spaceos-infra"
    "spaceos-e2e"
    "spaceos-nexus"
    "spaceos-librarian"
    "spaceos-architect"
    "spaceos-orch"
    "spaceos-conductor"
)

stop_terminal() {
    local session="$1"

    # Ellenőrizzük, hogy fut-e
    if ! tmux has-session -t "$session" 2>/dev/null; then
        log "${YELLOW}[SKIP]${NC} $session - nem fut"
        return 0
    fi

    log "${YELLOW}[STOP]${NC} $session - leállítás..."

    # 1. HANDOFF üzenet küldése (context mentés)
    tmux send-keys -t "$session" "/handoff" Enter 2>/dev/null || true
    sleep 2

    # 2. Ctrl+C küldése
    tmux send-keys -t "$session" C-c 2>/dev/null || true
    sleep 1

    # 3. Még egy Ctrl+C ha kell
    tmux send-keys -t "$session" C-c 2>/dev/null || true
    sleep 1

    # 4. Session kilövése
    if tmux kill-session -t "$session" 2>/dev/null; then
        log "${GREEN}[DONE]${NC} $session - leállítva"
    else
        log "${RED}[FAIL]${NC} $session - nem sikerült leállítani"
        return 1
    fi
}

show_status() {
    echo ""
    echo "=== Futó tmux session-ök ==="
    tmux list-sessions 2>/dev/null || echo "Nincs futó session"
    echo ""
}

main() {
    log "=== SpaceOS terminálok leállítása ==="

    local terminals_to_stop=()

    if [[ $# -eq 0 ]]; then
        # Alapértelmezett: minden terminál KIVÉVE root
        terminals_to_stop=("${DEFAULT_TERMINALS[@]}")
        log "Mód: Alapértelmezett (root kivételével)"
    elif [[ "$1" == "all" ]]; then
        # Minden terminál, beleértve root-ot is
        terminals_to_stop=("${ALL_TERMINALS[@]}")
        log "Mód: MINDEN terminál (root-tal együtt)"
        echo -e "${RED}FIGYELEM: A root terminál is le lesz állítva!${NC}"
        read -p "Biztosan folytatod? (y/N) " -n 1 -r
        echo
        if [[ ! $REPLY =~ ^[Yy]$ ]]; then
            log "Megszakítva."
            exit 0
        fi
    else
        # Megadott terminálok
        for arg in "$@"; do
            # Hozzáadjuk a spaceos- prefixet ha nincs
            if [[ "$arg" == spaceos-* ]]; then
                terminals_to_stop+=("$arg")
            else
                terminals_to_stop+=("spaceos-$arg")
            fi
        done
        log "Mód: Megadott terminálok: ${terminals_to_stop[*]}"
    fi

    show_status

    local failed=0
    for session in "${terminals_to_stop[@]}"; do
        if ! stop_terminal "$session"; then
            ((failed++))
        fi
    done

    show_status

    if [[ $failed -eq 0 ]]; then
        log "${GREEN}=== Minden terminál sikeresen leállítva ===${NC}"
    else
        log "${RED}=== $failed terminál leállítása sikertelen ===${NC}"
        exit 1
    fi
}

main "$@"
