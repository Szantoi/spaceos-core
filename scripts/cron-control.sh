#!/bin/bash
# =============================================================================
# cron-control.sh — SpaceOS cron job-ok ki/bekapcsolása
#
# Használat:
#   ./cron-control.sh stop      # Minden cron job kikapcsolása
#   ./cron-control.sh start     # Cron job-ok visszakapcsolása
#   ./cron-control.sh status    # Aktuális állapot
#
# A "stop" elmenti a jelenlegi crontab-ot és kiüríti.
# A "start" visszaállítja az elmentett crontab-ot.
# =============================================================================

set -euo pipefail

BACKUP_FILE="/opt/spaceos/scripts/.crontab-backup"
LOG_FILE="/opt/spaceos/logs/dispatcher/cron-control.log"

# Színek
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m'

log() {
    local msg="[$(date '+%Y-%m-%d %H:%M:%S')] $1"
    echo -e "$msg"
    echo "$msg" >> "$LOG_FILE" 2>/dev/null || true
}

stop_cron() {
    log "${YELLOW}=== CRON LEÁLLÍTÁS ===${NC}"

    # Ellenőrizzük, hogy van-e aktív crontab
    local current
    current=$(crontab -l 2>/dev/null || true)

    if [[ -z "$current" ]]; then
        log "${YELLOW}Nincs aktív crontab.${NC}"
        return 0
    fi

    # Ha már ki van kapcsolva (üres vagy csak komment)
    if [[ "$current" =~ ^[[:space:]]*#.*$ ]] || [[ -z "$(echo "$current" | grep -v '^#' | grep -v '^$')" ]]; then
        log "${YELLOW}A crontab már ki van kapcsolva.${NC}"
        return 0
    fi

    # Backup mentése
    log "Crontab mentése: $BACKUP_FILE"
    echo "$current" > "$BACKUP_FILE"

    # Crontab kiürítése
    log "Crontab kiürítése..."
    crontab -r 2>/dev/null || true

    log "${GREEN}✓ Cron job-ok LEÁLLÍTVA${NC}"
    log "  Backup: $BACKUP_FILE"
    log ""
    log "${CYAN}Visszakapcsolás:${NC} ./scripts/cron-control.sh start"
}

start_cron() {
    log "${GREEN}=== CRON VISSZAKAPCSOLÁS ===${NC}"

    # Ellenőrizzük, hogy van-e backup
    if [[ ! -f "$BACKUP_FILE" ]]; then
        log "${RED}Nincs mentett crontab: $BACKUP_FILE${NC}"
        log "Használd a 'stop' parancsot először."
        return 1
    fi

    # Ellenőrizzük, hogy nincs-e már aktív crontab
    local current
    current=$(crontab -l 2>/dev/null || true)

    if [[ -n "$current" ]] && [[ -n "$(echo "$current" | grep -v '^#' | grep -v '^$')" ]]; then
        log "${YELLOW}Már van aktív crontab!${NC}"
        log "Töröld először: crontab -r"
        return 1
    fi

    # Visszaállítás
    log "Crontab visszaállítása: $BACKUP_FILE"
    crontab "$BACKUP_FILE"

    log "${GREEN}✓ Cron job-ok VISSZAKAPCSOLVA${NC}"

    # Státusz mutatása
    echo ""
    crontab -l | head -20
}

show_status() {
    echo -e "${CYAN}=== CRON STÁTUSZ ===${NC}"
    echo ""

    local current
    current=$(crontab -l 2>/dev/null || true)

    if [[ -z "$current" ]]; then
        echo -e "Crontab: ${RED}ÜRES (leállítva)${NC}"
    elif [[ -z "$(echo "$current" | grep -v '^#' | grep -v '^$')" ]]; then
        echo -e "Crontab: ${RED}ÜRES (leállítva)${NC}"
    else
        echo -e "Crontab: ${GREEN}AKTÍV${NC}"
        echo ""
        echo "Aktív job-ok:"
        echo "$current" | grep -v '^#' | grep -v '^$' | while read -r line; do
            echo -e "  ${CYAN}●${NC} $line"
        done
    fi

    echo ""
    if [[ -f "$BACKUP_FILE" ]]; then
        echo -e "Backup: ${GREEN}$BACKUP_FILE${NC} ($(wc -l < "$BACKUP_FILE") sor)"
    else
        echo -e "Backup: ${YELLOW}nincs${NC}"
    fi
}

usage() {
    echo "Használat: $0 <parancs>"
    echo ""
    echo "Parancsok:"
    echo "  stop     Cron job-ok kikapcsolása (backup mentése)"
    echo "  start    Cron job-ok visszakapcsolása (backup-ból)"
    echo "  status   Aktuális állapot"
    echo ""
    echo "Példák:"
    echo "  $0 stop      # Éjszakára kikapcsolás"
    echo "  $0 start     # Reggel visszakapcsolás"
    echo "  $0 status    # Mi a helyzet?"
}

main() {
    if [[ $# -eq 0 ]]; then
        usage
        exit 1
    fi

    case $1 in
        stop)
            stop_cron
            ;;
        start)
            start_cron
            ;;
        status)
            show_status
            ;;
        --help|-h)
            usage
            ;;
        *)
            echo "Ismeretlen parancs: $1"
            usage
            exit 1
            ;;
    esac
}

main "$@"
