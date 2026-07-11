#!/bin/bash
# scripts/mailbox/list-unread.sh
# Leírás: Listázza az UNREAD inbox üzeneteket
# Használat: ./list-unread.sh [terminal|all]
# Példa: ./list-unread.sh backend

set -euo pipefail

TARGET="${1:-all}"
TERMINALS_DIR="/opt/spaceos/terminals"

list_unread() {
  local term="$1"
  local inbox="$TERMINALS_DIR/$term/inbox"
  [ -d "$inbox" ] || return 0

  grep -rl "status: UNREAD" "$inbox"/*.md 2>/dev/null | while read -r file; do
    id=$(grep "^id:" "$file" | head -1 | cut -d: -f2 | tr -d ' ')
    priority=$(grep "^priority:" "$file" | head -1 | cut -d: -f2 | tr -d ' ')
    title=$(grep "^# " "$file" | head -1 | sed 's/^# //')
    echo "$term|$id|$priority|$title"
  done
}

echo "TERMINAL|ID|PRIORITY|TITLE"
echo "--------|--|--------|-----"

if [ "$TARGET" = "all" ]; then
  for term in root conductor backend frontend architect librarian explorer designer monitor; do
    list_unread "$term"
  done
else
  list_unread "$TARGET"
fi
