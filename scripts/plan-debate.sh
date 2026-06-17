#!/bin/bash
# =============================================================================
# plan-debate.sh — 2× párhuzamos tervezés + keresztértékelés → konsenzus
#
# 1. Fázis: Planner-A és Planner-B egymástól függetlenül tervet ír (párhuzam)
# 2. Fázis: Mindkettő elolvassa a másik tervét és értékeli
# 3. Fázis: Konsenzus dokumentum szintézise
# → Konsenzus a queue/-ba kerül
# → Ha a queue elérte a küszöböt, Conductor inbox értesítés
#
# Minden paraméter a plan-config.yaml fájlból jön.
# Promptok a prompts/ mappából jönnek.
#
# Hívja: plan-select.sh
# =============================================================================

set -uo pipefail

SPACEOS_ROOT="${SPACEOS_ROOT:-/opt/spaceos}"
SCRIPT_DIR="$(dirname "$0")"
CONFIG_FILE="$SCRIPT_DIR/plan-config.yaml"

source "$SCRIPT_DIR/common.sh"
source "$SCRIPT_DIR/yaml-parser.sh"

# ── Konfiguráció betöltése ───────────────────────────────────────────────────

if [ ! -f "$CONFIG_FILE" ]; then
    echo "HIBA: Konfiguráció nem található: $CONFIG_FILE" >&2
    exit 1
fi

# Modellek
PLANNER_A_MODEL=$(yaml_get "$CONFIG_FILE" "models.planner_a" "sonnet")
PLANNER_B_MODEL=$(yaml_get "$CONFIG_FILE" "models.planner_b" "sonnet")
REVIEWER_MODEL=$(yaml_get "$CONFIG_FILE" "models.reviewer" "sonnet")
CONSENSUS_MODEL=$(yaml_get "$CONFIG_FILE" "models.consensus" "sonnet")

# Timing
PLANNER_TIMEOUT=$(yaml_get "$CONFIG_FILE" "timing.planner_timeout" "180")
REVIEWER_TIMEOUT=$(yaml_get "$CONFIG_FILE" "timing.reviewer_timeout" "90")
CONSENSUS_TIMEOUT=$(yaml_get "$CONFIG_FILE" "timing.consensus_timeout" "120")
FILE_WAIT=$(yaml_get "$CONFIG_FILE" "timing.file_wait" "2")

# Throttling
QUEUE_NOTIFY_THRESHOLD=$(yaml_get "$CONFIG_FILE" "throttling.queue_notify_threshold" "2")

# Útvonalak
SELECTED_DIR="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "paths.selected_dir" "docs/planning/selected")"
PLANS_DIR="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "paths.plans_dir" "docs/planning/plans")"
CONSENSUS_DIR="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "paths.consensus_dir" "docs/planning/consensus")"
QUEUE_DIR="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "paths.queue_dir" "docs/planning/queue")"
DOMAIN_FOCUS_FILE="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "paths.domain_focus" "docs/planning/domain-focus.md")"
CODEBASE_STATUS_FILE="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "paths.codebase_status" "docs/Codebase_Status.md")"

# Prompt fájlok
PLANNER_PROMPT_FILE="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "prompts.planner" "scripts/prompts/plan-debate-prompt.md")"
REVIEWER_PROMPT_FILE="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "prompts.reviewer" "scripts/prompts/plan-review-prompt.md")"
CONSENSUS_PROMPT_FILE="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "prompts.consensus" "scripts/prompts/plan-consensus-prompt.md")"

# Notifications
NOTIFY_CONSENSUS=$(yaml_get "$CONFIG_FILE" "notifications.on_consensus" "true")
NOTIFY_QUEUE_READY=$(yaml_get "$CONFIG_FILE" "notifications.on_queue_ready" "true")

mkdir -p "$PLANS_DIR" "$CONSENSUS_DIR" "$QUEUE_DIR"

SELECTED_FILE="$SELECTED_DIR/pending.md"
if [ ! -f "$SELECTED_FILE" ]; then
    echo "$TIMESTAMP Plan-debate: nincs selected fájl, kilépés" >> "$LOG_DIR/pipeline.log"
    exit 1
fi

SELECTED_CONTENT=$(cat "$SELECTED_FILE")
CODEBASE_STATUS=$(head -30 "$CODEBASE_STATUS_FILE" 2>/dev/null || echo "")
DOMAIN_FOCUS=$(cat "$DOMAIN_FOCUS_FILE" 2>/dev/null || echo "")

DATE=$(date +%Y-%m-%d)
PLAN_A="$PLANS_DIR/${DATE}_plan-a.md"
PLAN_B="$PLANS_DIR/${DATE}_plan-b.md"

# ── Prompt template betöltése ────────────────────────────────────────────────

if [ ! -f "$PLANNER_PROMPT_FILE" ]; then
    echo "HIBA: Planner prompt nem található: $PLANNER_PROMPT_FILE" >&2
    exit 1
fi

PLANNER_PROMPT_TEMPLATE=$(cat "$PLANNER_PROMPT_FILE")

# ── 1. Fázis: Párhuzamos független tervek ────────────────────────────────────

echo "$TIMESTAMP Tervezés indul (A: $PLANNER_A_MODEL, B: $PLANNER_B_MODEL)..." >> "$LOG_DIR/pipeline.log"

# Planner-A prompt
PLANNER_A_PROMPT="${PLANNER_PROMPT_TEMPLATE}"
PLANNER_A_PROMPT="${PLANNER_A_PROMPT//\{\{CODEBASE_STATUS\}\}/$CODEBASE_STATUS}"
PLANNER_A_PROMPT="${PLANNER_A_PROMPT//\{\{DOMAIN_FOCUS\}\}/$DOMAIN_FOCUS}"
PLANNER_A_PROMPT="${PLANNER_A_PROMPT//\{\{SELECTED_CONTENT\}\}/$SELECTED_CONTENT}"
PLANNER_A_PROMPT="${PLANNER_A_PROMPT//\{\{PLANNER_ID\}\}/Planner-A}"
PLANNER_A_PROMPT="${PLANNER_A_PROMPT//\{\{PLANNER_STYLE\}\}/Fókuszálj az inkrementális, biztonságos megközelítésre.}"

# Planner-B prompt
PLANNER_B_PROMPT="${PLANNER_PROMPT_TEMPLATE}"
PLANNER_B_PROMPT="${PLANNER_B_PROMPT//\{\{CODEBASE_STATUS\}\}/$CODEBASE_STATUS}"
PLANNER_B_PROMPT="${PLANNER_B_PROMPT//\{\{DOMAIN_FOCUS\}\}/$DOMAIN_FOCUS}"
PLANNER_B_PROMPT="${PLANNER_B_PROMPT//\{\{SELECTED_CONTENT\}\}/$SELECTED_CONTENT}"
PLANNER_B_PROMPT="${PLANNER_B_PROMPT//\{\{PLANNER_ID\}\}/Planner-B}"
PLANNER_B_PROMPT="${PLANNER_B_PROMPT//\{\{PLANNER_STYLE\}\}/Fókuszálj a merészebb, innovatívabb megközelítésre.}"

# Párhuzamos futtatás (hideg indítás)
echo "$PLANNER_A_PROMPT" | timeout "$PLANNER_TIMEOUT" claude -p --model "$PLANNER_A_MODEL" \
    > "$PLAN_A" 2>/dev/null &
PID_A=$!

echo "$PLANNER_B_PROMPT" | timeout "$PLANNER_TIMEOUT" claude -p --model "$PLANNER_B_MODEL" \
    > "$PLAN_B" 2>/dev/null &
PID_B=$!

wait $PID_A
wait $PID_B
sleep "$FILE_WAIT"

echo "$TIMESTAMP Tervek elkészültek" >> "$LOG_DIR/pipeline.log"

# ── 2. Fázis: Keresztértékelés ───────────────────────────────────────────────

PLAN_A_CONTENT=$(cat "$PLAN_A" 2>/dev/null || echo "")
PLAN_B_CONTENT=$(cat "$PLAN_B" 2>/dev/null || echo "")

if [ -z "$PLAN_A_CONTENT" ] || [ -z "$PLAN_B_CONTENT" ]; then
    echo "$TIMESTAMP Plan-debate HIBA: üres terv(ek)" >> "$LOG_DIR/pipeline.log"
    exit 1
fi

REVIEW_A="$PLANS_DIR/${DATE}_review-a-on-b.md"
REVIEW_B="$PLANS_DIR/${DATE}_review-b-on-a.md"

if [ ! -f "$REVIEWER_PROMPT_FILE" ]; then
    echo "HIBA: Reviewer prompt nem található: $REVIEWER_PROMPT_FILE" >&2
    exit 1
fi

REVIEWER_PROMPT_TEMPLATE=$(cat "$REVIEWER_PROMPT_FILE")

echo "$TIMESTAMP Keresztértékelés indul (model: $REVIEWER_MODEL)..." >> "$LOG_DIR/pipeline.log"

# A értékeli B-t
REVIEW_A_PROMPT="${REVIEWER_PROMPT_TEMPLATE}"
REVIEW_A_PROMPT="${REVIEW_A_PROMPT//\{\{REVIEWER_ID\}\}/Planner-A}"
REVIEW_A_PROMPT="${REVIEW_A_PROMPT//\{\{OTHER_ID\}\}/Planner-B}"
REVIEW_A_PROMPT="${REVIEW_A_PROMPT//\{\{OTHER_PLAN\}\}/$PLAN_B_CONTENT}"
REVIEW_A_PROMPT="${REVIEW_A_PROMPT//\{\{OWN_PLAN\}\}/$PLAN_A_CONTENT}"

# B értékeli A-t
REVIEW_B_PROMPT="${REVIEWER_PROMPT_TEMPLATE}"
REVIEW_B_PROMPT="${REVIEW_B_PROMPT//\{\{REVIEWER_ID\}\}/Planner-B}"
REVIEW_B_PROMPT="${REVIEW_B_PROMPT//\{\{OTHER_ID\}\}/Planner-A}"
REVIEW_B_PROMPT="${REVIEW_B_PROMPT//\{\{OTHER_PLAN\}\}/$PLAN_A_CONTENT}"
REVIEW_B_PROMPT="${REVIEW_B_PROMPT//\{\{OWN_PLAN\}\}/$PLAN_B_CONTENT}"

# Párhuzamos futtatás
echo "$REVIEW_A_PROMPT" | timeout "$REVIEWER_TIMEOUT" claude -p --model "$REVIEWER_MODEL" \
    > "$REVIEW_A" 2>/dev/null &
PID_RA=$!

echo "$REVIEW_B_PROMPT" | timeout "$REVIEWER_TIMEOUT" claude -p --model "$REVIEWER_MODEL" \
    > "$REVIEW_B" 2>/dev/null &
PID_RB=$!

wait $PID_RA
wait $PID_RB
sleep "$FILE_WAIT"

echo "$TIMESTAMP Keresztértékelés kész" >> "$LOG_DIR/pipeline.log"

# ── 3. Fázis: Konsenzus szintézis ────────────────────────────────────────────

REVIEW_A_CONTENT=$(cat "$REVIEW_A" 2>/dev/null || echo "")
REVIEW_B_CONTENT=$(cat "$REVIEW_B" 2>/dev/null || echo "")
CONSENSUS_FILE="$CONSENSUS_DIR/${DATE}_consensus.md"

if [ ! -f "$CONSENSUS_PROMPT_FILE" ]; then
    echo "HIBA: Consensus prompt nem található: $CONSENSUS_PROMPT_FILE" >&2
    exit 1
fi

CONSENSUS_PROMPT=$(cat "$CONSENSUS_PROMPT_FILE")
CONSENSUS_PROMPT="${CONSENSUS_PROMPT//\{\{PLAN_A_CONTENT\}\}/$PLAN_A_CONTENT}"
CONSENSUS_PROMPT="${CONSENSUS_PROMPT//\{\{PLAN_B_CONTENT\}\}/$PLAN_B_CONTENT}"
CONSENSUS_PROMPT="${CONSENSUS_PROMPT//\{\{REVIEW_A_CONTENT\}\}/$REVIEW_A_CONTENT}"
CONSENSUS_PROMPT="${CONSENSUS_PROMPT//\{\{REVIEW_B_CONTENT\}\}/$REVIEW_B_CONTENT}"
CONSENSUS_PROMPT="${CONSENSUS_PROMPT//\{\{DATE\}\}/$DATE}"
CONSENSUS_PROMPT="${CONSENSUS_PROMPT//\{\{PLAN_A_PATH\}\}/$PLAN_A}"
CONSENSUS_PROMPT="${CONSENSUS_PROMPT//\{\{PLAN_B_PATH\}\}/$PLAN_B}"

echo "$TIMESTAMP Konsenzus szintézis indul (model: $CONSENSUS_MODEL)..." >> "$LOG_DIR/pipeline.log"

echo "$CONSENSUS_PROMPT" | timeout "$CONSENSUS_TIMEOUT" claude -p --model "$CONSENSUS_MODEL" \
    > "$CONSENSUS_FILE" 2>/dev/null

sleep "$FILE_WAIT"

echo "$TIMESTAMP Konsenzus kész: $(basename "$CONSENSUS_FILE")" >> "$LOG_DIR/pipeline.log"

# ── 4. Konsenzus → Queue puffer + Conductor értesítés ────────────────────────

if [ ! -s "$CONSENSUS_FILE" ]; then
    echo "$TIMESTAMP Konsenzus fájl üres, queue skip" >> "$LOG_DIR/pipeline.log"
    exit 1
fi

# Stale pending.md átnevezése
mv "$SELECTED_DIR/pending.md" "$SELECTED_DIR/${DATE}_selected-done.md" 2>/dev/null

# Konsenzus másolása a queue-ba
QUEUE_FILE="$QUEUE_DIR/${DATE}_$(date +%H%M)_consensus.md"
cp "$CONSENSUS_FILE" "$QUEUE_FILE"

echo "$TIMESTAMP Konsenzus queue-ba: $(basename "$QUEUE_FILE")" >> "$LOG_DIR/pipeline.log"

# Telegram értesítés (ha engedélyezve)
if [ "$NOTIFY_CONSENSUS" = "true" ]; then
    DOMAIN=$(grep "^domain:" "$DOMAIN_FOCUS_FILE" 2>/dev/null | sed 's/domain:\s*//' | tr -d '[:space:]')
    tg "🧠 *Konsenzus kész*\nDomain: \`${DOMAIN:-all}\`\nFile: \`$(basename "$QUEUE_FILE")\`"
fi

# ── Queue méret ellenőrzés ───────────────────────────────────────────────────

QUEUE_COUNT=$(ls "$QUEUE_DIR"/*.md 2>/dev/null | wc -l)

if [ "$QUEUE_COUNT" -ge "$QUEUE_NOTIFY_THRESHOLD" ]; then
    COND_INBOX="$SPACEOS_ROOT/docs/mailbox/conductor/inbox"
    mkdir -p "$COND_INBOX"
    LAST_NUM=$(ls "$COND_INBOX"/*.md 2>/dev/null | sed 's/.*_\([0-9]\{3\}\)_.*/\1/' | sort -n | tail -1)
    NEXT_NUM=$(printf "%03d" $(( ${LAST_NUM:-0} + 1 )))

    # Csak akkor írunk inbox-ot, ha nincs már UNREAD
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

        if [ "$NOTIFY_QUEUE_READY" = "true" ]; then
            DOMAIN=$(grep "^domain:" "$DOMAIN_FOCUS_FILE" 2>/dev/null | sed 's/domain:\s*//' | tr -d '[:space:]')
            tg "📋 *Planning queue: ${QUEUE_COUNT} terv*\nConductor-nak kiadva\nDomain: \`${DOMAIN:-all}\`"
        fi
    else
        echo "$TIMESTAMP Conductor már dolgozik (UNREAD inbox létezik), skip" >> "$LOG_DIR/pipeline.log"
    fi
else
    echo "$TIMESTAMP Queue: ${QUEUE_COUNT} terv (< ${QUEUE_NOTIFY_THRESHOLD}, Conductor még nem értesítve)" >> "$LOG_DIR/pipeline.log"
fi
