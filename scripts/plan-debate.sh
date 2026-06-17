#!/bin/bash
# =============================================================================
# plan-debate.sh — 2× Sonnet párhuzamos tervezés + keresztértékelés → konsenzus
#
# Egyetlen felelőssége:
#   1. Fázis: Sonnet-A és Sonnet-B egymástól függetlenül tervet ír (párhuzam)
#   2. Fázis: Mindkettő elolvassa a másik tervét és értékeli
#   3. Fázis: Konsenzus dokumentum (Sonnet szintetizálja)
#   → Konsenzus a queue/-ba kerül (2-3 pufferelt terv)
#   → Ha a queue tele (3+), a Conductor inbox-ba értesítés megy
#
# Hívja: plan-select.sh
# =============================================================================

source "$(dirname "$0")/common.sh"

PLANNING="$SPACEOS_ROOT/docs/planning"
SELECTED_FILE="$PLANNING/selected/pending.md"
PLANS_DIR="$PLANNING/plans"
CONSENSUS_DIR="$PLANNING/consensus"
mkdir -p "$PLANS_DIR" "$CONSENSUS_DIR"

if [ ! -f "$SELECTED_FILE" ]; then
  echo "$TIMESTAMP Plan-debate: nincs selected fájl, kilépés" >> "$LOG_DIR/pipeline.log"
  exit 1
fi

SELECTED_CONTENT=$(cat "$SELECTED_FILE")
CODEBASE_STATUS=$(head -30 "$SPACEOS_ROOT/docs/Codebase_Status.md" 2>/dev/null || echo "")
DOMAIN_FOCUS=$(cat "$PLANNING/domain-focus.md" 2>/dev/null || echo "")

DATE=$(date +%Y-%m-%d)
PLAN_A="$PLANS_DIR/${DATE}_plan-sonnet-a.md"
PLAN_B="$PLANS_DIR/${DATE}_plan-sonnet-b.md"

OPUS_BASE_PROMPT="# SpaceOS Implementációs Terv — Független tervező

Te egy tapasztalt szoftverarchitekt vagy, aki a SpaceOS projektre dolgozol.
A SpaceOS a magyar faipar ERP/MES platformja — .NET 8 backend, React 18 frontend.

## Projekt kontextus:
${CODEBASE_STATUS}

## Domain fókusz:
${DOMAIN_FOCUS}

## Kiválasztott fejlesztési irányok (Sonnet által szűrve + kutatva):
${SELECTED_CONTENT}

## Feladatod — Független implementációs terv írása

Hozz létre egy KONKRÉT implementációs tervet a fenti irányokhoz.
Te FÜGGETLEN vagy — ne befolyásoljon semmilyen más terv, csak a saját ítéleted.

A tervnek tartalmaznia kell:
1. **Prioritás sorrend** — melyik funkciót mikor érdemes implementálni és miért
2. **Backend szükségletek** — milyen endpoint-ok, adatmodellek kellenek
3. **Frontend megközelítés** — komponens struktúra, state management, UX flow
4. **Kockázatok** — mi nehéz, mi bukhat el
5. **Sorrend és függőségek** — mi blokkolja mit

Légy konkrét. Adj tényleges API path javaslatokat, komponens neveket, adatmodelleket.
Maximum 600 szó."

# ── 1. Fázis: Párhuzamos független tervek ──────────────────────────────────

echo "$TIMESTAMP Sonnet tervek indulnak (párhuzam)..." >> "$LOG_DIR/pipeline.log"

echo "${OPUS_BASE_PROMPT}

## Te vagy: Sonnet-A tervező
Fókuszálj az inkrementális, biztonságos megközelítésre.

Válaszolj KÖZVETLENÜL markdown formátumban (ne használj tool-okat)." \
  | claude -p --model sonnet \
    > "$PLAN_A" 2>/dev/null &
PID_A=$!

echo "${OPUS_BASE_PROMPT}

## Te vagy: Sonnet-B tervező
Fókuszálj a merészebb, innováltívabb megközelítésre.

Válaszolj KÖZVETLENÜL markdown formátumban (ne használj tool-okat)." \
  | claude -p --model sonnet \
    > "$PLAN_B" 2>/dev/null &
PID_B=$!

wait $PID_A
wait $PID_B
sleep 2

echo "$TIMESTAMP Opus tervek elkészültek" >> "$LOG_DIR/pipeline.log"

# ── 2. Fázis: Keresztértékelés ──────────────────────────────────────────────

PLAN_A_CONTENT=$(cat "$PLAN_A" 2>/dev/null || echo "")
PLAN_B_CONTENT=$(cat "$PLAN_B" 2>/dev/null || echo "")

REVIEW_A="$PLANS_DIR/${DATE}_review-a-on-b.md"
REVIEW_B="$PLANS_DIR/${DATE}_review-b-on-a.md"

echo "$TIMESTAMP Keresztértékelés indul..." >> "$LOG_DIR/pipeline.log"

echo "# Keresztértékelés — Sonnet-A értékeli Sonnet-B tervét

## Sonnet-B terve:
${PLAN_B_CONTENT}

## A te saját terved (Sonnet-A):
${PLAN_A_CONTENT}

## Feladatod
Értékeld Sonnet-B tervét:
- Miben erősebb a tiédnél? (legyél őszinte)
- Miben gyengébb?
- Mit átvennél belőle?
- Mi az a 2-3 pont ahol egyetértetek? (ezek a konsenzus magja)
- Mi az ahol nem értetek egyet? (indokold meg miért a te megközelítésed jobb)

Maximum 300 szó. Légy konkrét és konstruktív.
Válaszolj KÖZVETLENÜL markdown formátumban." \
  | claude -p --model sonnet \
    > "$REVIEW_A" 2>/dev/null &
PID_RA=$!

echo "# Keresztértékelés — Sonnet-B értékeli Sonnet-A tervét

## Sonnet-A terve:
${PLAN_A_CONTENT}

## A te saját terved (Sonnet-B):
${PLAN_B_CONTENT}

## Feladatod
Értékeld Sonnet-A tervét:
- Miben erősebb a tiédnél? (legyél őszinte)
- Miben gyengébb?
- Mit átvennél belőle?
- Mi az a 2-3 pont ahol egyetértetek? (ezek a konsenzus magja)
- Mi az ahol nem értetek egyet? (indokold meg miért a te megközelítésed jobb)

Maximum 300 szó. Légy konkrét és konstruktív.
Válaszolj KÖZVETLENÜL markdown formátumban." \
  | claude -p --model sonnet \
    > "$REVIEW_B" 2>/dev/null &
PID_RB=$!

wait $PID_RA
wait $PID_RB
sleep 2

echo "$TIMESTAMP Keresztértékelés kész" >> "$LOG_DIR/pipeline.log"

# ── 3. Fázis: Konsenzus szintézis (Sonnet) ─────────────────────────────────

REVIEW_A_CONTENT=$(cat "$REVIEW_A" 2>/dev/null || echo "")
REVIEW_B_CONTENT=$(cat "$REVIEW_B" 2>/dev/null || echo "")
CONSENSUS_FILE="$CONSENSUS_DIR/${DATE}_consensus.md"

CONSENSUS_PROMPT="# SpaceOS Konsenzus Szintézis

Két független Sonnet tervező elkészítette a terveit és értékelte egymásét.
A te feladatod a legjobb elemek szintézise egy megvalósítható konsenzusba.

## Sonnet-A terve:
${PLAN_A_CONTENT}

## Sonnet-B terve:
${PLAN_B_CONTENT}

## Sonnet-A értékelése Sonnet-B tervéről:
${REVIEW_A_CONTENT}

## Sonnet-B értékelése Sonnet-A tervéről:
${REVIEW_B_CONTENT}

## Feladatod

Írj egy KONSENZUS dokumentumot az alábbi formátumban:

---
created: ${DATE}
plan_a: ${PLAN_A}
plan_b: ${PLAN_B}
status: ready_for_conductor
---

# SpaceOS Konsenzus Implementációs Terv

## Összefoglalás (2-3 mondat)

## Elfogadott prioritás sorrend
1. [feature] — mindkét tervező egyetért
2. ...

## Backend szükségletek (összesített)
- endpoint: ...
- adatmodell: ...

## Frontend megközelítés (legjobb elemek)
- ...

## Amit Sonnet-A-tól veszünk át
- ...

## Amit Sonnet-B-től veszünk át
- ...

## Nyitott kérdések a Conductor-nak
- ...

Válaszolj KÖZVETLENÜL markdown formátumban a fenti struktúrával."

echo "$CONSENSUS_PROMPT" | claude -p --model sonnet \
  > "$CONSENSUS_FILE" 2>/dev/null

echo "$TIMESTAMP Konsenzus kész: $(basename $CONSENSUS_FILE)" >> "$LOG_DIR/pipeline.log"

# ── 4. Konsenzus → Queue puffer + Conductor értesítés ───────────────────────

if [ ! -f "$CONSENSUS_FILE" ]; then
  echo "$TIMESTAMP Konsenzus fájl nem jött létre, queue skip" >> "$LOG_DIR/pipeline.log"
  exit 1
fi

# Stale pending.md átnevezése
mv "$PLANNING/selected/pending.md" \
   "$PLANNING/selected/${DATE}_selected-done.md" 2>/dev/null

# ── Konsenzus másolása a queue-ba ────────────────────────────────────────────

QUEUE_DIR="$PLANNING/queue"
mkdir -p "$QUEUE_DIR"
QUEUE_FILE="$QUEUE_DIR/${DATE}_$(date +%H%M)_consensus.md"
cp "$CONSENSUS_FILE" "$QUEUE_FILE"

echo "$TIMESTAMP Konsenzus queue-ba: $(basename $QUEUE_FILE)" >> "$LOG_DIR/pipeline.log"

# ── Queue méret ellenőrzés (max 3 pufferelt terv) ────────────────────────────

QUEUE_COUNT=$(ls "$QUEUE_DIR"/*.md 2>/dev/null | wc -l)

if [ "$QUEUE_COUNT" -ge 2 ]; then
  # Conductor inbox értesítés — vannak feldolgozandó tervek
  COND_INBOX="$SPACEOS_ROOT/docs/mailbox/conductor/inbox"
  mkdir -p "$COND_INBOX"
  LAST_NUM=$(ls "$COND_INBOX"/*.md 2>/dev/null | sed 's/.*_\([0-9]\{3\}\)_.*/\1/' | sort -n | tail -1)
  NEXT_NUM=$(printf "%03d" $(( ${LAST_NUM:-0} + 1 )))

  # Csak akkor írunk új inbox-ot, ha nincs már UNREAD
  EXISTING_UNREAD=$(grep -rl "status: UNREAD" "$COND_INBOX/" 2>/dev/null | head -1)

  if [ -z "$EXISTING_UNREAD" ]; then
    cat > "${COND_INBOX}/${DATE}_${NEXT_NUM}_planning-queue-ready.md" <<EOF
---
id: MSG-COND-${NEXT_NUM}
from: planning-pipeline
to: conductor
type: task
priority: high
status: UNREAD
model: sonnet
created: ${DATE}
---

# Conductor — Tervezési queue feldolgozás

A planning pipeline ${QUEUE_COUNT} kész konsenzus tervet pufferelt.
A te feladatod ezeket feldolgozni és termináloknak kiadni.

## Queue tartalom

\`\`\`
$(ls -1 "$QUEUE_DIR"/*.md 2>/dev/null | xargs -I{} basename {})
\`\`\`

## Teendők

1. Olvasd el a queue-ban lévő konsenzusokat: \`docs/planning/queue/\`
2. Minden konsenzushoz:
   - Használd a \`spaceos-arch-planner\` skill-t a v1→v4 pipeline-hoz
   - Verifikáld az API feltételezéseket a kódbázis ellen
   - Határozd meg melyik terminál valósítsa meg
   - Írd ki a terminálnak inbox üzenetet
3. Feldolgozott konsenzust mozgasd \`docs/planning/archive/\`-ba
4. Küldj DONE outbox-ot a feldolgozás végeztével
EOF

    echo "$TIMESTAMP Conductor inbox kiadva: ${DATE}_${NEXT_NUM}_planning-queue-ready.md" >> "$LOG_DIR/pipeline.log"
    tg "📋 *Planning queue: ${QUEUE_COUNT} terv* — Conductor-nak kiadva\nDomain: $(grep '^domain:' $SPACEOS_ROOT/docs/planning/domain-focus.md | cut -d: -f2 | tr -d ' ')"
  else
    echo "$TIMESTAMP Conductor már dolgozik (UNREAD inbox létezik), skip" >> "$LOG_DIR/pipeline.log"
  fi
else
  echo "$TIMESTAMP Queue: ${QUEUE_COUNT} terv (< 2, Conductor még nem értesítve)" >> "$LOG_DIR/pipeline.log"
  tg "🧠 *Konsenzus kész* — Queue: ${QUEUE_COUNT}/3\nDomain: $(grep '^domain:' $SPACEOS_ROOT/docs/planning/domain-focus.md | cut -d: -f2 | tr -d ' ')"
fi
