#!/bin/bash
# =============================================================================
# pause-dispatcher.sh — SpaceOS dispatcher szüneteltetése
#
# Használat:
#   ./pause-dispatcher.sh on       # Szünet BE (új munka nem indul)
#   ./pause-dispatcher.sh off      # Szünet KI (normál működés)
#   ./pause-dispatcher.sh status   # Aktuális állapot
#
# A szkriptek (nightwatch, watch-inbox, stb.) ellenőrzik a PAUSE_FILE-t.
# Ha létezik, nem indítanak új munkát, de a cron job-ok futnak.
# =============================================================================

PAUSE_FILE="/opt/spaceos/scripts/.dispatcher-paused"

# Színek
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

case "${1:-status}" in
    on|pause|stop)
        echo "$(date '+%Y-%m-%d %H:%M:%S') - Dispatcher paused by $(whoami)" > "$PAUSE_FILE"
        echo -e "${YELLOW}⏸  DISPATCHER SZÜNETELTETVE${NC}"
        echo "   Új munka nem indul, de a cron fut."
        echo "   Visszakapcsolás: $0 off"
        ;;
    off|resume|start)
        rm -f "$PAUSE_FILE"
        echo -e "${GREEN}▶  DISPATCHER AKTÍV${NC}"
        echo "   Normál működés."
        ;;
    status)
        if [[ -f "$PAUSE_FILE" ]]; then
            echo -e "${YELLOW}⏸  SZÜNETELTETVE${NC}"
            echo "   $(cat "$PAUSE_FILE")"
        else
            echo -e "${GREEN}▶  AKTÍV${NC}"
        fi
        ;;
    *)
        echo "Használat: $0 <on|off|status>"
        echo ""
        echo "  on/pause   Szüneteltetés (új munka nem indul)"
        echo "  off/resume Visszakapcsolás"
        echo "  status     Állapot lekérdezése"
        exit 1
        ;;
esac
