#!/bin/bash
# =============================================================================
# stop-terminals.sh — SpaceOS terminálok finom leállítása
#
# Használat:
#   ./stop-terminals.sh              # Minden terminál leállítása (kivéve root)
#   ./stop-terminals.sh all          # Minden terminál leállítása (root-tal együtt)
#   ./stop-terminals.sh fe conductor # Csak megadott terminálok leállítása
#   ./stop-terminals.sh --force      # Azonnali kill (nem vár)
#
# A szkript:
#   1. Küld /handoff parancsot (context mentés)
#   2. Küld /exit parancsot (session lezárás kérése)
#   3. Vár amíg a session természetesen befejeződik (max 60 mp)
#   4. Ha timeout: Ctrl+C + kill
# =============================================================================

set -euo pipefail

SCRIPTS="$(dirname "$0")"
LOG_DIR="/opt/spaceos/logs/dispatcher"
LOG_FILE="$LOG_DIR/stop-terminals.log"

# Konfiguráció
GRACEFUL_TIMEOUT=60    # Max várakozás másodpercben
POLL_INTERVAL=2        # Ellenőrzési intervallum
FORCE_MODE=false

# Színek
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

log() {
    local msg="[$(date '+%Y-%m-%d %H:%M:%S')] $1"
    echo -e "$msg"
    echo "$msg" >> "$LOG_FILE" 2>/dev/null || true
}

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

# Ellenőrzi, hogy a session még fut-e
is_session_alive() {
    tmux has-session -t "$1" 2>/dev/null
}

# Finom leállítás - vár amíg a session természetesen befejeződik
graceful_stop() {
    local session="$1"

    # Ellenőrizzük, hogy fut-e
    if ! is_session_alive "$session"; then
        log "${YELLOW}[SKIP]${NC} $session - nem fut"
        return 0
    fi

    log "${CYAN}[GRACEFUL]${NC} $session - finom leállítás indítása..."

    # 1. HANDOFF küldése (context mentés)
    log "  → /handoff küldése..."
    tmux send-keys -t "$session" "/handoff" Enter 2>/dev/null || true
    sleep 3

    # 2. /exit küldése (session lezárás kérése)
    log "  → /exit küldése..."
    tmux send-keys -t "$session" "/exit" Enter 2>/dev/null || true

    # 3. Várakozás a természetes befejezésre
    local waited=0
    log "  → Várakozás a befejezésre (max ${GRACEFUL_TIMEOUT}s)..."

    while is_session_alive "$session" && [[ $waited -lt $GRACEFUL_TIMEOUT ]]; do
        sleep $POLL_INTERVAL
        waited=$((waited + POLL_INTERVAL))
        # Státusz kijelzés 10 másodpercenként
        if [[ $((waited % 10)) -eq 0 ]]; then
            log "  → Még fut... (${waited}s/${GRACEFUL_TIMEOUT}s)"
        fi
    done

    # 4. Ellenőrzés - sikerült-e természetesen leállni
    if ! is_session_alive "$session"; then
        log "${GREEN}[DONE]${NC} $session - természetesen befejeződött (${waited}s)"
        return 0
    fi

    # 5. Timeout - kényszerített leállítás
    log "${YELLOW}[TIMEOUT]${NC} $session - ${GRACEFUL_TIMEOUT}s után kényszerített leállítás..."

    # Ctrl+C küldése
    tmux send-keys -t "$session" C-c 2>/dev/null || true
    sleep 2

    # Még egy Ctrl+C
    tmux send-keys -t "$session" C-c 2>/dev/null || true
    sleep 1

    # Kill session
    if tmux kill-session -t "$session" 2>/dev/null; then
        log "${YELLOW}[KILLED]${NC} $session - kényszerítve leállítva"
        return 0
    else
        log "${RED}[FAIL]${NC} $session - nem sikerült leállítani"
        return 1
    fi
}

# Azonnali kill (--force mód)
force_stop() {
    local session="$1"

    if ! is_session_alive "$session"; then
        log "${YELLOW}[SKIP]${NC} $session - nem fut"
        return 0
    fi

    log "${RED}[FORCE]${NC} $session - azonnali leállítás..."

    tmux send-keys -t "$session" C-c 2>/dev/null || true
    sleep 1

    if tmux kill-session -t "$session" 2>/dev/null; then
        log "${GREEN}[DONE]${NC} $session - leállítva"
        return 0
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

usage() {
    echo "Használat: $0 [opciók] [terminálok...]"
    echo ""
    echo "Opciók:"
    echo "  --force     Azonnali kill (nem vár a természetes befejezésre)"
    echo "  --timeout N Graceful timeout másodpercben (alapért: 60)"
    echo "  all         Minden terminál (root-tal együtt)"
    echo ""
    echo "Példák:"
    echo "  $0                    # Minden terminál (root kivételével)"
    echo "  $0 all                # Minden terminál (root-tal együtt)"
    echo "  $0 fe nexus           # Csak fe és nexus"
    echo "  $0 --force            # Azonnali kill mindenhol"
    echo "  $0 --timeout 30 fe    # 30s timeout az fe-re"
}

main() {
    local terminals_to_stop=()
    local positional_args=()

    # Argumentumok feldolgozása
    while [[ $# -gt 0 ]]; do
        case $1 in
            --force)
                FORCE_MODE=true
                shift
                ;;
            --timeout)
                GRACEFUL_TIMEOUT="$2"
                shift 2
                ;;
            --help|-h)
                usage
                exit 0
                ;;
            *)
                positional_args+=("$1")
                shift
                ;;
        esac
    done

    # Pozícionális argumentumok visszaállítása
    set -- "${positional_args[@]:-}"

    log "=== SpaceOS terminálok leállítása ==="

    if $FORCE_MODE; then
        log "Mód: FORCE (azonnali kill)"
    else
        log "Mód: GRACEFUL (timeout: ${GRACEFUL_TIMEOUT}s)"
    fi

    if [[ $# -eq 0 ]]; then
        # Alapértelmezett: minden terminál KIVÉVE root
        terminals_to_stop=("${DEFAULT_TERMINALS[@]}")
        log "Terminálok: Alapértelmezett (root kivételével)"
    elif [[ "$1" == "all" ]]; then
        # Minden terminál, beleértve root-ot is
        terminals_to_stop=("${ALL_TERMINALS[@]}")
        log "Terminálok: MINDEN (root-tal együtt)"
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
        log "Terminálok: ${terminals_to_stop[*]}"
    fi

    show_status

    local failed=0
    for session in "${terminals_to_stop[@]}"; do
        if $FORCE_MODE; then
            if ! force_stop "$session"; then
                ((failed++))
            fi
        else
            if ! graceful_stop "$session"; then
                ((failed++))
            fi
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
