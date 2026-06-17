#!/bin/bash
# kova-bridge.sh — Kova → SpaceOS root inbox üzenet
#
# Használat:
#   kova-bridge.sh "Feladat rövid neve" "A feladat részletes leírása"
#
# Eredmény:
#   /opt/spaceos/docs/mailbox/root/inbox/YYYY-MM-DD_NNN_kova-<slug>.md

set -uo pipefail

SPACEOS_ROOT="/opt/spaceos"
INBOX="$SPACEOS_ROOT/docs/mailbox/root/inbox"

TITLE="${1:-}"
BODY="${2:-}"

if [ -z "$TITLE" ] || [ -z "$BODY" ]; then
  echo "Használat: $0 \"Feladat neve\" \"Feladat leírása\"" >&2
  exit 1
fi

# Következő sorszám
LAST=$(ls "$INBOX"/*.md 2>/dev/null | sed 's/.*_\([0-9]\{3\}\)_.*/\1/' | sort -n | tail -1)
NEXT=$(printf "%03d" $(( ${LAST:-0} + 1 )))

# Slug a title-ből
SLUG=$(echo "$TITLE" | tr '[:upper:]' '[:lower:]' | sed 's/[^a-z0-9]/-/g' | sed 's/--*/-/g' | sed 's/^-//;s/-$//' | cut -c1-40)

DATE=$(date +%Y-%m-%d)
FILENAME="${INBOX}/${DATE}_${NEXT}_kova-${SLUG}.md"
MSG_ID="MSG-ROOT-${NEXT}-KOVA"

cat > "$FILENAME" <<EOF
---
id: $MSG_ID
from: kova
to: root
type: task
priority: medium
status: UNREAD
ref: telegram
created: $DATE
---

# $TITLE

$BODY

---
*Kován keresztül érkezett Telegram üzenetből.*
EOF

echo "✅ Létrehozva: $FILENAME"
echo "   ID: $MSG_ID"
