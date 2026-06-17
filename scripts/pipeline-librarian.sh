#!/bin/bash
# =============================================================================
# pipeline-librarian.sh — Librarian trigger
#
# Egyetlen felelőssége:
#   Ha deploy vagy slice-lezárás történt → inbox üzenetet küld a Librarian-nak
#   hogy frissítse a docs/knowledge/ tudásbázist.
#   Max 1 trigger per 6 óra (ne spammelje).
#
# Args: $1 = done_file  $2 = terminal  $3 = next (NONE vagy fájlnév)
# Hívja: pipeline.sh
# =============================================================================

source "$(dirname "$0")/common.sh"

DONE_FILE="$1"
TERMINAL="$2"
NEXT="${3:-}"

DONE_CONTENT=$(cat "$DONE_FILE")
DONE_BASE=$(basename "$DONE_FILE" .md)

# Trigger feltétel: deploy VAGY slice vége
SHOULD_TRIGGER=0
REASON=""

if echo "$DONE_CONTENT" | grep -qi "DEPLOYED\|deploy"; then
  SHOULD_TRIGGER=1; REASON="deploy detected"
elif [ "$NEXT" = "NONE" ] || [ -z "$NEXT" ]; then
  SHOULD_TRIGGER=1; REASON="slice completed"
fi

[ "$SHOULD_TRIGGER" -eq 0 ] && exit 0

# Max 6 óránként
LIBRARIAN_INBOX="$SPACEOS_ROOT/docs/mailbox/librarian/inbox"
mkdir -p "$LIBRARIAN_INBOX"

LAST_LIB=$(ls -t "$LIBRARIAN_INBOX"/*.md 2>/dev/null | head -1)
LIB_AGE=99999
[ -n "$LAST_LIB" ] && LIB_AGE=$(( NOW - $(stat -c %Y "$LAST_LIB" 2>/dev/null || echo 0) ))

if [ "$LIB_AGE" -le 21600 ]; then
  echo "$TIMESTAMP Librarian skip (utolsó: ${LIB_AGE}s)" >> "$LOG_DIR/pipeline.log"
  exit 0
fi

LIB_DATE=$(date +%Y-%m-%d)
LIB_LAST_NUM=$(ls "$LIBRARIAN_INBOX"/*.md 2>/dev/null | sed 's/.*_\([0-9]\{3\}\)_.*/\1/' | sort -n | tail -1)
LIB_NEXT_NUM=$(printf "%03d" $(( ${LIB_LAST_NUM:-0} + 1 )))
LIB_FILE="${LIBRARIAN_INBOX}/${LIB_DATE}_${LIB_NEXT_NUM}_synthesize-${DONE_BASE:0:30}.md"

cat > "$LIB_FILE" <<EOF
---
id: MSG-LIBRARIAN-${LIB_NEXT_NUM}
from: pipeline
to: librarian
type: task
priority: low
status: UNREAD
model: haiku
ref: ${DONE_BASE}
created: ${LIB_DATE}
---

# Librarian — Tudásbázis frissítés

Trigger: ${REASON}
Kapcsolódó DONE: \`${DONE_BASE}\`
Terminál: \`${TERMINAL}\`

## Feladatod

Elemezd az utóbbi DONE üzeneteket és frissítsd a \`docs/knowledge/\` tudásbázist:

1. \`/opt/spaceos/docs/mailbox/${TERMINAL}/outbox/\` — legutóbbi 3-5 DONE fájl
2. Azonosítsd: új minta, döntés, ismert csapda, API contract változás
3. Frissítsd a releváns knowledge doc-ot (csak docs/knowledge/-ban írj)
4. Ha semmi új nincs: DONE outboxban jelezd

Mappák: patterns/ · deployment/ · architecture/ · context/ · security/
EOF

echo "$TIMESTAMP Librarian triggered: $(basename $LIB_FILE) ($REASON)" >> "$LOG_DIR/pipeline.log"
tg "📚 *Librarian feladat kiadva* — ${REASON}"
