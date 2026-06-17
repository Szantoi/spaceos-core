#!/bin/bash
# spaceos-notify.sh — SpaceOS root → Kova Telegram értesítés
#
# Használat:
#   spaceos-notify.sh "Üzenet szövege"
#
# Elküldi Gábornak a Kova botodon keresztül.

set -uo pipefail

KOVA_TOKEN="8065944034:AAEidE5zUsdNh4yDCHvyyRhIQXo4xlYwEGI"
CHAT_ID="8426048796"
MESSAGE="${1:-}"

if [ -z "$MESSAGE" ]; then
  echo "Használat: $0 \"Üzenet\"" >&2
  exit 1
fi

RESPONSE=$(curl -s -X POST "https://api.telegram.org/bot${KOVA_TOKEN}/sendMessage" \
  --data-urlencode "text=[SpaceOS] $MESSAGE" \
  -d "chat_id=$CHAT_ID" \
  -d "parse_mode=")

if echo "$RESPONSE" | grep -q '"ok":true'; then
  echo "✅ Elküldve Kovának"
else
  echo "❌ Hiba: $RESPONSE" >&2
  exit 1
fi
