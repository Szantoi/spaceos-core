#!/bin/bash
# =============================================================================
# reviewer.sh — SpaceOS Dual Reviewer
#
# Két párhuzamos, független értékelő fut minden DONE üzenetre.
# Mindkettőnek jóvá kell hagynia — ha bármelyik elutasít, visszaküldi
# a feladatot javításra a kivitelezőnek.
#
# Értékelési szempontok:
#   - Stabilitás: error handling, edge case-ek, nincs mock leak
#   - Újrafelhasználhatóság: DRY, tiszta interfészek, nincs hardcode
#   - DoD teljesítés: minden feltétel teljesül-e?
#   - Build/teszt: zöld?
#
# Használat: reviewer.sh <done_file_path>
# Trigger: nightwatch.sh (automatikus DONE detektáláskor)
# =============================================================================

set -uo pipefail

SPACEOS_ROOT="${SPACEOS_ROOT:-/opt/spaceos}"
CONF="$SPACEOS_ROOT/scripts/telegram.conf"
LOG_DIR="$SPACEOS_ROOT/logs/dispatcher"
REVIEW_DIR="$SPACEOS_ROOT/logs/reviews"

source "$CONF" 2>/dev/null || exit 1
mkdir -p "$LOG_DIR" "$REVIEW_DIR"

DONE_FILE="${1:-}"
if [ -z "$DONE_FILE" ] || [ ! -f "$DONE_FILE" ]; then
  echo "Használat: $0 <done_file>" >&2
  exit 1
fi

TIMESTAMP=$(date +"%Y-%m-%d %H:%M:%S")
DONE_BASE=$(basename "$DONE_FILE" .md)
TERMINAL=$(echo "$DONE_FILE" | sed 's|.*/mailbox/\([^/]*\)/.*|\1|')

echo "$TIMESTAMP Review indul: $DONE_BASE" >> "$LOG_DIR/reviewer.log"

tg() {
  curl -s -X POST "https://api.telegram.org/bot${TELEGRAM_TOKEN}/sendMessage" \
    -d chat_id="$TELEGRAM_CHAT_ID" \
    --data-urlencode "text=$1" \
    -d parse_mode="Markdown" -o /dev/null
}

# ── Eredeti inbox task megkeresése ────────────────────────────────────────────

MSG_REF=$(grep -m1 "^ref:" "$DONE_FILE" 2>/dev/null | sed 's/ref:\s*//' | tr -d '[:space:]')
INBOX_FILE=""
if [ -n "$MSG_REF" ] && [ "$MSG_REF" != "—" ]; then
  INBOX_FILE=$(grep -rl "^id: ${MSG_REF}$" "$SPACEOS_ROOT/docs/mailbox/${TERMINAL}/inbox/" 2>/dev/null | head -1)
fi

# ── Review prompt összeállítása ───────────────────────────────────────────────

DONE_CONTENT=$(cat "$DONE_FILE")
INBOX_CONTENT=""
if [ -n "$INBOX_FILE" ]; then
  INBOX_CONTENT=$(cat "$INBOX_FILE")
fi

# Projekt kontextus + minőségi elvárások betöltése
REVIEWER_CONTEXT=""
CONTEXT_FILE="$SPACEOS_ROOT/scripts/reviewer-context.md"
if [ -f "$CONTEXT_FILE" ]; then
  REVIEWER_CONTEXT=$(cat "$CONTEXT_FILE")
fi

REVIEW_PROMPT="# SpaceOS DONE Review

Te egy SpaceOS code reviewer vagy. Ismered a projektet, a célokat és a minőségi elvárásokat.

---

## Projekt kontextus és minőségi elvárások:

${REVIEWER_CONTEXT}

---

## Értékelési szempontok:

### 1. Stabilitás
- Van-e error handling minden API hívásnál?
- Kezelve vannak-e az edge case-ek (üres lista, null, timeout)?
- Nincs-e mock leak (mock adat bekerülhet-e prod-ba)?
- Vannak-e hardcoded értékek ahol nem kellene?

### 2. Újrafelhasználhatóság
- DRY elvek betartva? (nincs copy-paste logika)
- Tiszták-e az interfészek?
- Komponensek/függvények egy felelősséget látnak-e el?
- Megjegyzés nélkül érthető-e a kód szándéka?

### 3. DoD teljesítés
- Minden feltétel teljesül az eredeti taskban?
- Build zöld? Tesztek zöldek?
- Nincs hiányzó implementáció?
- Mock-mentes? (ha Slice 1 feladat)

### 4. Sprint céloknak megfelel-e?
- Az aktuális sprint fókuszával összhangban van?
- Ha EndpointPending kell: be van-e állítva?
- Ha real API kell: be van-e kötve?

---

## Eredeti task (inbox):
${INBOX_CONTENT:-'(nem található)'}

---

## DONE üzenet:
${DONE_CONTENT}

---

## Válasz formátuma (KÖTELEZŐ):

VERDICT: APPROVE
vagy
VERDICT: REJECT

FEEDBACK:
- [konkrét, konstruktív észrevétel 1]
- [konkrét, konstruktív észrevétel 2]

Ha APPROVE: rövid pozitív megerősítés + maximum 2 opcionális javaslat.
Ha REJECT: konkrét hiánylista amit javítani kell, mielőtt újra DONE-t küldhet.
Emlékezz: csak valódi hiba esetén REJECT — konstruktív javaslat nem ok REJECT-re.

Légy tömör. Maximum 250 szó."

# ── Két párhuzamos review futtatása ──────────────────────────────────────────

VERDICT_A_FILE="$REVIEW_DIR/${DONE_BASE}_reviewer_a.txt"
VERDICT_B_FILE="$REVIEW_DIR/${DONE_BASE}_reviewer_b.txt"

echo "$REVIEW_PROMPT" | claude -p --model haiku \
  > "$VERDICT_A_FILE" 2>/dev/null &
PID_A=$!

echo "$REVIEW_PROMPT" | claude -p --model haiku \
  > "$VERDICT_B_FILE" 2>/dev/null &
PID_B=$!

wait $PID_A
wait $PID_B

# Fájlok lezárásának bevárása
sleep 2

# ── Verdict kiolvasása ────────────────────────────────────────────────────────

extract_verdict() {
  grep -i "VERDICT:" "$1" 2>/dev/null | head -1 | sed 's/.*VERDICT:\s*//' | tr -d '[:space:]#*' | tr '[:lower:]' '[:upper:]'
}

extract_feedback() {
  sed -n '/^FEEDBACK:/,$ p' "$1" 2>/dev/null | tail -n +2
}

VERDICT_A=$(extract_verdict "$VERDICT_A_FILE")
VERDICT_B=$(extract_verdict "$VERDICT_B_FILE")
FEEDBACK_A=$(extract_feedback "$VERDICT_A_FILE")
FEEDBACK_B=$(extract_feedback "$VERDICT_B_FILE")

echo "$TIMESTAMP Reviewer-A: $VERDICT_A | Reviewer-B: $VERDICT_B" >> "$LOG_DIR/reviewer.log"

# ── Döntés ───────────────────────────────────────────────────────────────────

if [ "$VERDICT_A" = "APPROVE" ] && [ "$VERDICT_B" = "APPROVE" ]; then
  echo "$TIMESTAMP APPROVED: $DONE_BASE — pipeline indul" >> "$LOG_DIR/reviewer.log"
  bash "$SPACEOS_ROOT/scripts/pipeline.sh" "$DONE_FILE" &
  exit 0
fi

# ── Legalább egy REJECT → visszaküldés a kivitelezőnek ───────────────────────

# Következő inbox sorszám
INBOX_DIR="$SPACEOS_ROOT/docs/mailbox/${TERMINAL}/inbox"
LAST_NUM=$(ls "$INBOX_DIR"/*.md 2>/dev/null | sed 's/.*_\([0-9]\{3\}\)_.*/\1/' | sort -n | tail -1)
NEXT_NUM=$(printf "%03d" $(( ${LAST_NUM:-0} + 1 )))
DATE=$(date +%Y-%m-%d)
REJECT_FILE="${INBOX_DIR}/${DATE}_${NEXT_NUM}_review-reject-${DONE_BASE}.md"

# Eredeti inbox model kiolvasása (visszaküldéshez ugyanaz a modell)
ORIG_MODEL="sonnet"
if [ -n "$INBOX_FILE" ]; then
  ORIG_MODEL=$(grep -m1 "^model:" "$INBOX_FILE" 2>/dev/null | sed 's/model:\s*//' | tr -d '[:space:]')
  ORIG_MODEL="${ORIG_MODEL:-sonnet}"
fi

# Reject inbox üzenet
cat > "$REJECT_FILE" <<EOF
---
id: MSG-${TERMINAL^^}-${NEXT_NUM}-REVIEW-REJECT
from: reviewer
to: ${TERMINAL}
type: task
priority: high
status: UNREAD
model: ${ORIG_MODEL}
ref: ${DONE_BASE}
created: ${DATE}
---

# Review visszadobás: ${DONE_BASE}

A dual review **nem fogadta el** a DONE-t. Az alábbi pontokat kell javítani, majd új DONE-t kell küldeni.

## Reviewer-A verdict: ${VERDICT_A}

${FEEDBACK_A}

## Reviewer-B verdict: ${VERDICT_B}

${FEEDBACK_B}

## Teendő

Javítsd a fenti pontokat, majd küldd újra a DONE outbox üzenetet.
EOF

REJECT_MSG="⚠️ *${TERMINAL^^} DONE visszadobva*"
[ "$VERDICT_A" != "APPROVE" ] && REJECT_MSG="${REJECT_MSG} — Reviewer-A: REJECT"
[ "$VERDICT_B" != "APPROVE" ] && REJECT_MSG="${REJECT_MSG} — Reviewer-B: REJECT"
REJECT_MSG="${REJECT_MSG}\n\`$DONE_BASE\`\n\nJavítási inbox elküldve a terminálnak."

tg "$REJECT_MSG"
echo "$TIMESTAMP REJECTED: $DONE_BASE (A:$VERDICT_A B:$VERDICT_B)" >> "$LOG_DIR/reviewer.log"
exit 1
