#!/bin/bash
# =============================================================================
# critical-notify.sh — Kritikus Telegram értesítés Gábornak
#
# Csak KRITIKUS esetekben hívódik:
#   - Conductor eszkaláció (segítséget kér)
#   - Deploy kész (oldal élesbe rakva, tesztelésre vár)
#   - User action szükséges (regisztráció, manuális lépés)
#   - Rendszer hiba
#
# Használat:
#   critical-notify.sh escalation "Conductor üzenete"
#   critical-notify.sh deploy "Service neve" "https://url"
#   critical-notify.sh user_action "Mit kell csinálni"
#   critical-notify.sh error "Hiba leírása"
# =============================================================================

set -uo pipefail

SCRIPT_DIR="$(dirname "$0")"
LOG_DIR="${LOG_DIR:-/opt/spaceos/logs/dispatcher}"

# SpaceOS Telegram bot konfiguráció
source "$SCRIPT_DIR/telegram.conf" 2>/dev/null || {
    echo "HIBA: telegram.conf nem található" >&2
    exit 1
}

TYPE="${1:-}"
shift

case "$TYPE" in
  escalation)
    MESSAGE="🚨 *Conductor eszkaláció*

$1

Beavatkozás szükséges!"
    ;;

  deploy)
    SERVICE="${1:-}"
    URL="${2:-}"
    MESSAGE="🚀 *Deploy kész* — $SERVICE

$URL

Tesztelésre vár!"
    ;;

  user_action)
    MESSAGE="👤 *User action szükséges*

$1"
    ;;

  error)
    MESSAGE="❌ *Rendszer hiba*

$1"
    ;;

  *)
    echo "Használat: $0 {escalation|deploy|user_action|error} [args...]" >&2
    exit 1
    ;;
esac

# Küldés SpaceOS Telegram bottal
RESPONSE=$(curl -s -X POST "https://api.telegram.org/bot${TELEGRAM_TOKEN}/sendMessage" \
    -d chat_id="$TELEGRAM_CHAT_ID" \
    --data-urlencode "text=$MESSAGE" \
    -d parse_mode="Markdown")

if echo "$RESPONSE" | grep -q '"ok":true'; then
    echo "✅ Elküldve SpaceOS Telegram-ra"
else
    echo "❌ Hiba: $RESPONSE" >&2
    exit 1
fi

# Log
TIMESTAMP=$(date +"%Y-%m-%d %H:%M:%S")
echo "$TIMESTAMP Critical-notify: $TYPE" >> "$LOG_DIR/pipeline.log"
