#!/bin/bash
# =============================================================================
# cold-shutdown.sh — SpaceOS hideg, lassú leállítás
#
# NEM küld semmit a termináloknak. Csak vár amíg maguktól befejezik
# a munkájukat és elhalnak a session-ök.
#
# Használat:
#   ./cold-shutdown.sh              # Vár amíg minden session befejeződik
#   ./cold-shutdown.sh --timeout 300  # Max 5 perc várakozás
#   ./cold-shutdown.sh --kill-after   # Timeout után kilövi ami maradt
#
# A szkript:
#   1. Listázza a futó session-öket
#   2. Vár amíg maguktól befejeződnek (nincs beavatkozás)
#   3. Timeout után vagy kilép vagy kilövi a maradékot
# =============================================================================

set -euo pipefail

LOG_DIR="/opt/spaceos/logs/dispatcher"
LOG_FILE="$LOG_DIR/cold-shutdown.log"

# Konfiguráció
TIMEOUT=600              # Alapértelmezett: 10 perc
POLL_INTERVAL=5          # Ellenőrzés 5 másodpercenként
KILL_AFTER_TIMEOUT=false # Timeout után kilövi-e a maradékot

# Színek
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BLUE='\033[0;34m'
NC='\033[0m'

log() {
    local msg="[$(date '+%Y-%m-%d %H:%M:%S')] $1"
    echo -e "$msg"
    echo "$msg" >> "$LOG_FILE" 2>/dev/null || true
}

# SpaceOS session-ök listázása
get_spaceos_sessions() {
    tmux list-sessions -F "#{session_name}" 2>/dev/null | grep "^spaceos-" || true
}

# Session-ök számának lekérdezése
count_sessions() {
    get_spaceos_sessions | wc -l
}

# Részletes státusz kijelzés
show_status() {
    local sessions
    sessions=$(get_spaceos_sessions)

    echo ""
    echo -e "${BLUE}=== Futó SpaceOS session-ök ===${NC}"
    if [[ -z "$sessions" ]]; then
        echo -e "${GREEN}Nincs futó session${NC}"
    else
        echo "$sessions" | while read -r session; do
            echo -e "  ${CYAN}●${NC} $session"
        done
        echo ""
        echo -e "Összesen: ${YELLOW}$(echo "$sessions" | wc -l)${NC} session"
    fi
    echo ""
}

usage() {
    echo "Használat: $0 [opciók]"
    echo ""
    echo "Hideg leállítás - NEM avatkozik be, csak vár."
    echo ""
    echo "Opciók:"
    echo "  --timeout N      Max várakozás másodpercben (alapért: 600 = 10 perc)"
    echo "  --kill-after     Timeout után kilövi a maradék session-öket"
    echo "  --poll N         Ellenőrzési intervallum (alapért: 5s)"
    echo ""
    echo "Példák:"
    echo "  $0                        # Vár max 10 percet"
    echo "  $0 --timeout 1800         # Vár max 30 percet"
    echo "  $0 --timeout 300 --kill-after  # 5 perc után kilövi"
}

main() {
    # Argumentumok feldolgozása
    while [[ $# -gt 0 ]]; do
        case $1 in
            --timeout)
                TIMEOUT="$2"
                shift 2
                ;;
            --kill-after)
                KILL_AFTER_TIMEOUT=true
                shift
                ;;
            --poll)
                POLL_INTERVAL="$2"
                shift 2
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

    log "${BLUE}=== SpaceOS HIDEG LEÁLLÍTÁS ===${NC}"
    log "Mód: Passzív várakozás (nincs beavatkozás)"
    log "Timeout: ${TIMEOUT}s ($(($TIMEOUT / 60)) perc)"
    log "Kill after timeout: $KILL_AFTER_TIMEOUT"
    echo ""

    show_status

    local initial_count
    initial_count=$(count_sessions)

    if [[ $initial_count -eq 0 ]]; then
        log "${GREEN}Nincs futó SpaceOS session. Kész.${NC}"
        exit 0
    fi

    log "Várakozás ${initial_count} session befejezésére..."
    log "${YELLOW}(A terminálok maguktól fejezik be a munkájukat)${NC}"
    echo ""

    local waited=0
    local last_count=$initial_count

    while [[ $waited -lt $TIMEOUT ]]; do
        sleep $POLL_INTERVAL
        waited=$((waited + POLL_INTERVAL))

        local current_count
        current_count=$(count_sessions)

        # Ha változott a szám, jelezzük
        if [[ $current_count -ne $last_count ]]; then
            local finished=$((last_count - current_count))
            log "${GREEN}[FINISHED]${NC} $finished session befejeződött (maradt: $current_count)"
            last_count=$current_count
        fi

        # Ha mind befejeződött
        if [[ $current_count -eq 0 ]]; then
            log ""
            log "${GREEN}=== MINDEN SESSION BEFEJEZŐDÖTT ===${NC}"
            log "Időtartam: ${waited}s"
            show_status
            exit 0
        fi

        # Státusz kijelzés percenként
        if [[ $((waited % 60)) -eq 0 ]]; then
            log "  → Várakozás... ${waited}s/${TIMEOUT}s (maradt: $current_count session)"
        fi
    done

    # Timeout
    log ""
    log "${YELLOW}=== TIMEOUT (${TIMEOUT}s) ===${NC}"

    local remaining
    remaining=$(count_sessions)

    if [[ $remaining -eq 0 ]]; then
        log "${GREEN}Minden session befejeződött.${NC}"
        exit 0
    fi

    log "Maradt $remaining session:"
    get_spaceos_sessions | while read -r session; do
        log "  - $session"
    done

    if $KILL_AFTER_TIMEOUT; then
        log ""
        log "${RED}Kill after timeout aktív - session-ök leállítása...${NC}"

        get_spaceos_sessions | while read -r session; do
            log "  → $session kilövése..."
            tmux send-keys -t "$session" C-c 2>/dev/null || true
            sleep 1
            tmux kill-session -t "$session" 2>/dev/null || true
        done

        log "${GREEN}Kész.${NC}"
        show_status
    else
        log ""
        log "${YELLOW}A session-ök még futnak.${NC}"
        log "Használd a --kill-after opciót ha ki akarod lőni őket."
        show_status
        exit 1
    fi
}

main "$@"
