#!/bin/bash
# =============================================================================
# stop-idle.sh — Feladat nélküli terminálok leállítása
#
# Használat:
#   ./stop-idle.sh              # Leállítja azokat akiknek nincs UNREAD inbox
#   ./stop-idle.sh --dry-run    # Csak kiírja mit csinálna
#   ./stop-idle.sh --force      # Azonnali kill (nem vár)
#
# A szkript:
#   1. Végigmegy minden futó terminálon
#   2. Ellenőrzi van-e UNREAD inbox üzenet
#   3. Ha nincs → leállítja (graceful vagy force)
#   4. Priority terminálokat (conductor) megkíméli
#
# Hideg indításhoz: ez a szkript takarítja le a felesleges session-öket.
# =============================================================================

set -euo pipefail

SCRIPTS="$(dirname "$0")"
source "$SCRIPTS/common.sh"

LOG_FILE="$LOG_DIR/stop-idle.log"

# Konfiguráció
GRACEFUL_TIMEOUT=30    # Rövidebb mint a teljes stop
POLL_INTERVAL=2
FORCE_MODE=false
DRY_RUN=false

# Színek
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
GRAY='\033[0;90m'
NC='\033[0m'

log() {
    local msg="[$(date '+%Y-%m-%d %H:%M:%S')] $1"
    echo -e "$msg"
    echo "$msg" >> "$LOG_FILE" 2>/dev/null || true
}

# Ellenőrzi, hogy a session még fut-e
is_session_alive() {
    tmux_s has-session -t "$1" 2>/dev/null
}

# Van-e UNREAD inbox a terminálnak?
has_unread_inbox() {
    local terminal="$1"
    local inbox_dir="$SPACEOS_ROOT/docs/mailbox/${terminal}/inbox/"

    if [ ! -d "$inbox_dir" ]; then
        return 1  # Nincs inbox mappa → nincs feladat
    fi

    # Keresünk UNREAD üzenetet
    grep -rl "status: UNREAD" "$inbox_dir" 2>/dev/null | head -1 | grep -q .
}

# Priority session-e?
is_priority_session() {
    local session="$1"
    for ps in "${PRIORITY_SESSIONS[@]}"; do
        [ "$session" = "$ps" ] && return 0
    done
    return 1
}

# Finom leállítás
graceful_stop() {
    local session="$1"

    if $DRY_RUN; then
        log "${CYAN}[DRY-RUN]${NC} Leállítanám: $session"
        return 0
    fi

    log "${CYAN}[GRACEFUL]${NC} $session leállítása..."

    # /handoff küldése
    tmux_s send-keys -t "$session" "/handoff" Enter 2>/dev/null || true
    sleep 2

    # /exit küldése
    tmux_s send-keys -t "$session" "/exit" Enter 2>/dev/null || true

    # Várakozás
    local waited=0
    while is_session_alive "$session" && [[ $waited -lt $GRACEFUL_TIMEOUT ]]; do
        sleep $POLL_INTERVAL
        waited=$((waited + POLL_INTERVAL))
    done

    if ! is_session_alive "$session"; then
        log "${GREEN}[DONE]${NC} $session befejeződött (${waited}s)"
        return 0
    fi

    # Timeout → kill
    log "${YELLOW}[TIMEOUT]${NC} $session kényszerített leállítás..."
    tmux_s send-keys -t "$session" C-c 2>/dev/null || true
    sleep 1
    tmux_s kill-session -t "$session" 2>/dev/null || true
    log "${YELLOW}[KILLED]${NC} $session"
}

# Azonnali kill
force_stop() {
    local session="$1"

    if $DRY_RUN; then
        log "${RED}[DRY-RUN]${NC} Force kill: $session"
        return 0
    fi

    log "${RED}[FORCE]${NC} $session azonnali leállítás..."
    tmux_s send-keys -t "$session" C-c 2>/dev/null || true
    sleep 1
    tmux_s kill-session -t "$session" 2>/dev/null || true
    log "${GREEN}[DONE]${NC} $session leállítva"
}

usage() {
    echo "Használat: $0 [opciók]"
    echo ""
    echo "Leállítja azokat a terminálokat, akiknek nincs UNREAD inbox üzenetük."
    echo "Hideg indításhoz használd ezt a szkriptet a felesleges session-ök takarítására."
    echo ""
    echo "Opciók:"
    echo "  --dry-run   Csak kiírja mit csinálna, nem állít le semmit"
    echo "  --force     Azonnali kill (nem vár a természetes befejezésre)"
    echo "  --help      Ez a súgó"
    echo ""
    echo "Példák:"
    echo "  $0              # Leállítja a feladat nélküli terminálokat"
    echo "  $0 --dry-run    # Előnézet: mit csinálna"
    echo "  $0 --force      # Gyors leállítás"
}

main() {
    # Argumentumok feldolgozása
    while [[ $# -gt 0 ]]; do
        case $1 in
            --force)
                FORCE_MODE=true
                shift
                ;;
            --dry-run)
                DRY_RUN=true
                shift
                ;;
            --help|-h)
                usage
                exit 0
                ;;
            *)
                echo "Ismeretlen opció: $1"
                usage
                exit 1
                ;;
        esac
    done

    log "=== Feladat nélküli terminálok leállítása ==="

    if $DRY_RUN; then
        log "Mód: DRY-RUN (nem csinál semmit)"
    elif $FORCE_MODE; then
        log "Mód: FORCE"
    else
        log "Mód: GRACEFUL (timeout: ${GRACEFUL_TIMEOUT}s)"
    fi

    local stopped=0
    local skipped=0
    local kept=0

    # Végigmegyünk minden ismert session-ön
    for session in "${!SESSIONS[@]}"; do
        local terminal="${SESSIONS[$session]}"

        # Fut-e?
        if ! is_session_alive "$session"; then
            continue  # Nem fut, kihagyjuk
        fi

        # Priority session?
        if is_priority_session "$session"; then
            log "${GRAY}[PRIORITY]${NC} $session — mindig fut, kihagyva"
            ((skipped++))
            continue
        fi

        # Root session?
        if [ "$session" = "spaceos-root" ]; then
            log "${GRAY}[ROOT]${NC} $session — kihagyva"
            ((skipped++))
            continue
        fi

        # Van-e feladata?
        if has_unread_inbox "$terminal"; then
            log "${GREEN}[ACTIVE]${NC} $session — van UNREAD inbox, megtartva"
            ((kept++))
            continue
        fi

        # Nincs feladata → leállítjuk
        log "${YELLOW}[IDLE]${NC} $session — nincs UNREAD inbox"

        if $FORCE_MODE; then
            force_stop "$session"
        else
            graceful_stop "$session"
        fi

        ((stopped++))
    done

    echo ""
    log "=== Összefoglaló ==="
    log "Leállítva: $stopped"
    log "Megtartva (aktív): $kept"
    log "Kihagyva (priority/root): $skipped"

    # Futó session-ök listája
    echo ""
    echo "=== Futó session-ök ==="
    tmux_s list-sessions 2>/dev/null || echo "Nincs futó session"
}

main "$@"
