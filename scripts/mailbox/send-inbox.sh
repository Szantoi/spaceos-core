#!/bin/bash
# scripts/mailbox/send-inbox.sh
# Leírás: Inbox üzenet küldése terminálnak
# Használat: ./send-inbox.sh <terminal> <type> <priority> <model> <title> [content_file]
# Példa: ./send-inbox.sh backend task medium sonnet "Fix bug" ./task.md

set -euo pipefail

TERMINAL="$1"
TYPE="${2:-task}"
PRIORITY="${3:-medium}"
MODEL="${4:-sonnet}"
TITLE="$5"
CONTENT_FILE="${6:-}"

DATE=$(date +%Y-%m-%d)
INBOX_DIR="/opt/spaceos/terminals/$TERMINAL/inbox"

[ -d "$INBOX_DIR" ] || { echo "ERROR: Terminal '$TERMINAL' not found"; exit 1; }

# Next number
NUM=$(($(ls "$INBOX_DIR" 2>/dev/null | grep "^$DATE" | wc -l) + 1))

# Slug from title
SLUG=$(echo "$TITLE" | tr '[:upper:]' '[:lower:]' | tr ' ' '-' | tr -cd 'a-z0-9-' | cut -c1-40)

FILE="$INBOX_DIR/${DATE}_$(printf '%03d' $NUM)_${SLUG}.md"
TERM_UPPER=$(echo "$TERMINAL" | tr '[:lower:]' '[:upper:]')

# Content
if [ -n "$CONTENT_FILE" ] && [ -f "$CONTENT_FILE" ]; then
  CONTENT=$(cat "$CONTENT_FILE")
else
  CONTENT="$TITLE"
fi

cat > "$FILE" << EOF
---
id: MSG-${TERM_UPPER}-$(printf '%03d' $NUM)
from: script
to: $TERMINAL
type: $TYPE
priority: $PRIORITY
status: UNREAD
model: $MODEL
created: $DATE
---

# $TITLE

$CONTENT
EOF

echo "Created: $FILE"
echo "ID: MSG-${TERM_UPPER}-$(printf '%03d' $NUM)"
