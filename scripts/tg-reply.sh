#!/bin/bash
# tg-reply.sh — Telegram válasz küldése a Root-tól
# Használat: bash tg-reply.sh "üzenet"

source "$(dirname "$0")/common.sh"

MESSAGE="$1"
[ -z "$MESSAGE" ] && echo "Használat: tg-reply.sh 'üzenet'" && exit 1

curl -s -X POST "https://api.telegram.org/bot${TELEGRAM_TOKEN}/sendMessage" \
  -d chat_id="$TELEGRAM_CHAT_ID" \
  --data-urlencode "text=$MESSAGE" \
  -o /dev/null

echo "✅ Telegram üzenet elküldve"
