#!/bin/bash
# scripts/mailbox/mark-read.sh
# Leírás: Inbox üzenet READ-ként jelölése
# Használat: ./mark-read.sh <terminal> <message_file>
# Példa: ./mark-read.sh backend 2026-06-30_001_task.md

set -euo pipefail

TERMINAL="$1"
MESSAGE="$2"
INBOX_DIR="/opt/spaceos/terminals/$TERMINAL/inbox"
FILE="$INBOX_DIR/$MESSAGE"

[ -f "$FILE" ] || { echo "ERROR: File not found: $FILE"; exit 1; }

# UNREAD → READ csere
sed -i 's/status: UNREAD/status: READ/' "$FILE"

echo "Marked as READ: $FILE"
