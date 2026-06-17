#!/bin/bash
# =============================================================================
# reviewer.sh — SpaceOS Dual Reviewer (hideg indítás, konfigurálható)
#
# Két párhuzamos, független értékelő fut minden DONE üzenetre.
# Minden paraméter a reviewer-config.yaml fájlból jön.
# A prompt a reviewer-prompt.md fájlból jön.
#
# Használat: reviewer.sh <done_file_path>
# Trigger: nightwatch.sh (automatikus DONE detektáláskor)
# =============================================================================

set -uo pipefail

SPACEOS_ROOT="${SPACEOS_ROOT:-/opt/spaceos}"
SCRIPT_DIR="$(dirname "$0")"
CONF="$SPACEOS_ROOT/scripts/telegram.conf"
CONFIG_FILE="$SCRIPT_DIR/reviewer-config.yaml"

source "$CONF" 2>/dev/null || exit 1

# ── YAML parser (egyszerű, bash-native) ──────────────────────────────────────

yaml_get() {
    local file="$1"
    local key="$2"
    local default="${3:-}"
    local value

    # Egyszerű kulcs (pl. "reviewer.model_a")
    # Konvertáljuk nested kulcsot: reviewer.model_a → keresés
    local section=$(echo "$key" | cut -d. -f1)
    local field=$(echo "$key" | cut -d. -f2-)

    if [ "$section" = "$field" ]; then
        # Egyszerű kulcs
        value=$(grep -E "^${key}:" "$file" 2>/dev/null | head -1 | sed 's/^[^:]*:\s*//' | tr -d '"'"'" | tr -d '[:space:]')
    else
        # Nested kulcs - keresünk a section után
        value=$(awk -v section="$section" -v field="$field" '
            /^[a-z_]+:/ { current_section = $1; gsub(/:/, "", current_section) }
            current_section == section && $1 ~ field":" {
                gsub(/^[^:]*:\s*/, "");
                gsub(/["'"'"']/, "");
                gsub(/^[[:space:]]+|[[:space:]]+$/, "");
                print;
                exit
            }
        ' "$file" 2>/dev/null)
    fi

    echo "${value:-$default}"
}

yaml_get_list() {
    local file="$1"
    local key="$2"
    local section=$(echo "$key" | cut -d. -f1)
    local field=$(echo "$key" | cut -d. -f2-)

    awk -v section="$section" -v field="$field" '
        /^[a-z_]+:/ { current_section = $1; gsub(/:/, "", current_section); in_list = 0 }
        current_section == section && $0 ~ field":" { in_list = 1; next }
        in_list && /^[[:space:]]*-/ {
            gsub(/^[[:space:]]*-[[:space:]]*/, "");
            gsub(/["'"'"']/, "");
            print
        }
        in_list && /^[[:space:]]*[a-z_]+:/ { in_list = 0 }
    ' "$file" 2>/dev/null
}

# ── Konfiguráció betöltése ───────────────────────────────────────────────────

if [ ! -f "$CONFIG_FILE" ]; then
    echo "HIBA: Konfiguráció nem található: $CONFIG_FILE" >&2
    exit 1
fi

MODEL_A=$(yaml_get "$CONFIG_FILE" "reviewer.model_a" "haiku")
MODEL_B=$(yaml_get "$CONFIG_FILE" "reviewer.model_b" "haiku")
PARALLEL=$(yaml_get "$CONFIG_FILE" "reviewer.parallel" "true")
REQUIRE_BOTH=$(yaml_get "$CONFIG_FILE" "reviewer.require_both" "true")

REVIEW_TIMEOUT=$(yaml_get "$CONFIG_FILE" "timing.review_timeout" "120")
FILE_WAIT=$(yaml_get "$CONFIG_FILE" "timing.file_wait" "2")

PROMPT_TEMPLATE=$(yaml_get "$CONFIG_FILE" "paths.prompt_template" "scripts/reviewer-prompt.md")
CONTEXT_FILE=$(yaml_get "$CONFIG_FILE" "paths.context_file" "scripts/reviewer-context.md")
LOG_DIR=$(yaml_get "$CONFIG_FILE" "paths.log_dir" "logs/dispatcher")
REVIEW_DIR=$(yaml_get "$CONFIG_FILE" "paths.review_dir" "logs/reviews")

REJECT_PRIORITY=$(yaml_get "$CONFIG_FILE" "reject_inbox.priority" "high")
MODEL_FALLBACK=$(yaml_get "$CONFIG_FILE" "reject_inbox.model_fallback" "sonnet")

NOTIFY_APPROVE=$(yaml_get "$CONFIG_FILE" "notifications.on_approve" "false")
NOTIFY_REJECT=$(yaml_get "$CONFIG_FILE" "notifications.on_reject" "true")
NOTIFY_ERROR=$(yaml_get "$CONFIG_FILE" "notifications.on_error" "true")

# Approve/reject kulcsszavak
APPROVE_KEYWORDS=$(yaml_get_list "$CONFIG_FILE" "verdict.approve_keywords")
REJECT_KEYWORDS=$(yaml_get_list "$CONFIG_FILE" "verdict.reject_keywords")

# Könyvtárak létrehozása
mkdir -p "$SPACEOS_ROOT/$LOG_DIR" "$SPACEOS_ROOT/$REVIEW_DIR"

# ── Argumentumok ─────────────────────────────────────────────────────────────

DONE_FILE="${1:-}"
if [ -z "$DONE_FILE" ] || [ ! -f "$DONE_FILE" ]; then
    echo "Használat: $0 <done_file>" >&2
    exit 1
fi

TIMESTAMP=$(date +"%Y-%m-%d %H:%M:%S")
DONE_BASE=$(basename "$DONE_FILE" .md)
TERMINAL=$(echo "$DONE_FILE" | sed 's|.*/mailbox/\([^/]*\)/.*|\1|')

LOG_FILE="$SPACEOS_ROOT/$LOG_DIR/reviewer.log"
echo "$TIMESTAMP Review indul: $DONE_BASE (model_a: $MODEL_A, model_b: $MODEL_B)" >> "$LOG_FILE"

# ── Telegram ─────────────────────────────────────────────────────────────────

tg() {
    curl -s -X POST "https://api.telegram.org/bot${TELEGRAM_TOKEN}/sendMessage" \
        -d chat_id="$TELEGRAM_CHAT_ID" \
        --data-urlencode "text=$1" \
        -d parse_mode="Markdown" -o /dev/null
}

# ── Eredeti inbox task megkeresése ───────────────────────────────────────────

MSG_REF=$(grep -m1 "^ref:" "$DONE_FILE" 2>/dev/null | sed 's/ref:\s*//' | tr -d '[:space:]')
INBOX_FILE=""
INBOX_PATH="(nem található)"
if [ -n "$MSG_REF" ] && [ "$MSG_REF" != "—" ]; then
    INBOX_FILE=$(grep -rl "^id: ${MSG_REF}$" "$SPACEOS_ROOT/docs/mailbox/${TERMINAL}/inbox/" 2>/dev/null | head -1)
    [ -n "$INBOX_FILE" ] && INBOX_PATH="$INBOX_FILE"
fi

# ── Prompt összeállítása a template-ből ──────────────────────────────────────

PROMPT_TEMPLATE_FILE="$SPACEOS_ROOT/$PROMPT_TEMPLATE"
CONTEXT_FILE_FULL="$SPACEOS_ROOT/$CONTEXT_FILE"

if [ ! -f "$PROMPT_TEMPLATE_FILE" ]; then
    echo "HIBA: Prompt template nem található: $PROMPT_TEMPLATE_FILE" >&2
    exit 1
fi

# Tartalmak betöltése
DONE_CONTENT=$(cat "$DONE_FILE")
INBOX_CONTENT=""
[ -n "$INBOX_FILE" ] && [ -f "$INBOX_FILE" ] && INBOX_CONTENT=$(cat "$INBOX_FILE")

CONTEXT_CONTENT=""
[ -f "$CONTEXT_FILE_FULL" ] && CONTEXT_CONTENT=$(cat "$CONTEXT_FILE_FULL")

# Template placeholder-ek cseréje
REVIEW_PROMPT=$(cat "$PROMPT_TEMPLATE_FILE")
REVIEW_PROMPT="${REVIEW_PROMPT//\{\{CONTEXT\}\}/$CONTEXT_CONTENT}"
REVIEW_PROMPT="${REVIEW_PROMPT//\{\{INBOX_PATH\}\}/$INBOX_PATH}"
REVIEW_PROMPT="${REVIEW_PROMPT//\{\{INBOX_CONTENT\}\}/${INBOX_CONTENT:-'(nem található)'}}"
REVIEW_PROMPT="${REVIEW_PROMPT//\{\{DONE_PATH\}\}/$DONE_FILE}"
REVIEW_PROMPT="${REVIEW_PROMPT//\{\{DONE_CONTENT\}\}/$DONE_CONTENT}"

# ── Review futtatás (hideg indítás) ──────────────────────────────────────────

VERDICT_A_FILE="$SPACEOS_ROOT/$REVIEW_DIR/${DONE_BASE}_reviewer_a.txt"
VERDICT_B_FILE="$SPACEOS_ROOT/$REVIEW_DIR/${DONE_BASE}_reviewer_b.txt"

# Hideg indítás: claude -p (print mode, nem interaktív session)
# Minden review friss processz, nincs megosztott kontextus
run_review() {
    local model="$1"
    local output_file="$2"
    local reviewer_id="$3"

    echo "$REVIEW_PROMPT" | timeout "$REVIEW_TIMEOUT" claude -p --model "$model" \
        > "$output_file" 2>/dev/null

    local exit_code=$?
    if [ $exit_code -ne 0 ]; then
        echo "VERDICT: ERROR" > "$output_file"
        echo "FEEDBACK:" >> "$output_file"
        echo "- Review timeout vagy hiba (exit: $exit_code)" >> "$output_file"
    fi
}

if [ "$PARALLEL" = "true" ]; then
    # Párhuzamos futtatás
    run_review "$MODEL_A" "$VERDICT_A_FILE" "A" &
    PID_A=$!

    run_review "$MODEL_B" "$VERDICT_B_FILE" "B" &
    PID_B=$!

    wait $PID_A
    wait $PID_B
else
    # Szekvenciális futtatás
    run_review "$MODEL_A" "$VERDICT_A_FILE" "A"
    run_review "$MODEL_B" "$VERDICT_B_FILE" "B"
fi

# Fájlok lezárásának bevárása
sleep "$FILE_WAIT"

# ── Verdict kiolvasása ───────────────────────────────────────────────────────

extract_verdict() {
    local file="$1"
    local raw_verdict

    raw_verdict=$(grep -i "VERDICT:" "$file" 2>/dev/null | head -1 | sed 's/.*VERDICT:\s*//' | tr -d '[:space:]#*' | tr '[:lower:]' '[:upper:]')

    # Ellenőrizzük az approve kulcsszavakkal
    for keyword in $APPROVE_KEYWORDS; do
        keyword_upper=$(echo "$keyword" | tr '[:lower:]' '[:upper:]')
        if [ "$raw_verdict" = "$keyword_upper" ]; then
            echo "APPROVE"
            return
        fi
    done

    # Ellenőrizzük a reject kulcsszavakkal
    for keyword in $REJECT_KEYWORDS; do
        keyword_upper=$(echo "$keyword" | tr '[:lower:]' '[:upper:]')
        if [ "$raw_verdict" = "$keyword_upper" ]; then
            echo "REJECT"
            return
        fi
    done

    # Ismeretlen verdict
    echo "UNKNOWN"
}

extract_feedback() {
    sed -n '/^FEEDBACK:/,$ p' "$1" 2>/dev/null | tail -n +2
}

VERDICT_A=$(extract_verdict "$VERDICT_A_FILE")
VERDICT_B=$(extract_verdict "$VERDICT_B_FILE")
FEEDBACK_A=$(extract_feedback "$VERDICT_A_FILE")
FEEDBACK_B=$(extract_feedback "$VERDICT_B_FILE")

echo "$TIMESTAMP Reviewer-A ($MODEL_A): $VERDICT_A | Reviewer-B ($MODEL_B): $VERDICT_B" >> "$LOG_FILE"

# ── Döntés ───────────────────────────────────────────────────────────────────

is_approved() {
    if [ "$REQUIRE_BOTH" = "true" ]; then
        [ "$VERDICT_A" = "APPROVE" ] && [ "$VERDICT_B" = "APPROVE" ]
    else
        [ "$VERDICT_A" = "APPROVE" ] || [ "$VERDICT_B" = "APPROVE" ]
    fi
}

if is_approved; then
    echo "$TIMESTAMP APPROVED: $DONE_BASE — pipeline indul" >> "$LOG_FILE"

    if [ "$NOTIFY_APPROVE" = "true" ]; then
        tg "✅ *${TERMINAL^^} DONE elfogadva*\n\`$DONE_BASE\`"
    fi

    bash "$SPACEOS_ROOT/scripts/pipeline.sh" "$DONE_FILE" &
    exit 0
fi

# ── Legalább egy REJECT/UNKNOWN → visszaküldés a kivitelezőnek ───────────────

# Következő inbox sorszám
INBOX_DIR="$SPACEOS_ROOT/docs/mailbox/${TERMINAL}/inbox"
LAST_NUM=$(ls "$INBOX_DIR"/*.md 2>/dev/null | sed 's/.*_\([0-9]\{3\}\)_.*/\1/' | sort -n | tail -1)
NEXT_NUM=$(printf "%03d" $(( ${LAST_NUM:-0} + 1 )))
DATE=$(date +%Y-%m-%d)
REJECT_FILE="${INBOX_DIR}/${DATE}_${NEXT_NUM}_review-reject-${DONE_BASE}.md"

# Eredeti inbox model kiolvasása (visszaküldéshez ugyanaz a modell)
ORIG_MODEL="$MODEL_FALLBACK"
if [ -n "$INBOX_FILE" ] && [ -f "$INBOX_FILE" ]; then
    ORIG_MODEL=$(grep -m1 "^model:" "$INBOX_FILE" 2>/dev/null | sed 's/model:\s*//' | tr -d '[:space:]')
    ORIG_MODEL="${ORIG_MODEL:-$MODEL_FALLBACK}"
fi

# Reject inbox üzenet
cat > "$REJECT_FILE" <<EOF
---
id: MSG-${TERMINAL^^}-${NEXT_NUM}-REVIEW-REJECT
from: reviewer
to: ${TERMINAL}
type: task
priority: ${REJECT_PRIORITY}
status: UNREAD
model: ${ORIG_MODEL}
ref: ${DONE_BASE}
created: ${DATE}
---

# Review visszadobás: ${DONE_BASE}

A dual review **nem fogadta el** a DONE-t. Az alábbi pontokat kell javítani, majd új DONE-t kell küldeni.

## Eredeti feladat

**Fájl:** \`${INBOX_PATH}\`

Olvasd el az eredeti feladatot és ellenőrizd, hogy minden követelmény teljesül-e.

## Reviewer-A verdict: ${VERDICT_A} (model: ${MODEL_A})

${FEEDBACK_A}

## Reviewer-B verdict: ${VERDICT_B} (model: ${MODEL_B})

${FEEDBACK_B}

## Teendő

1. Olvasd el az eredeti feladatot: \`${INBOX_PATH}\`
2. Javítsd a fenti pontokat
3. Küldd újra a DONE outbox üzenetet
EOF

if [ "$NOTIFY_REJECT" = "true" ]; then
    REJECT_MSG="⚠️ *${TERMINAL^^} DONE visszadobva*"
    [ "$VERDICT_A" != "APPROVE" ] && REJECT_MSG="${REJECT_MSG}\nReviewer-A ($MODEL_A): $VERDICT_A"
    [ "$VERDICT_B" != "APPROVE" ] && REJECT_MSG="${REJECT_MSG}\nReviewer-B ($MODEL_B): $VERDICT_B"
    REJECT_MSG="${REJECT_MSG}\n\`$DONE_BASE\`\n\nJavítási inbox elküldve."
    tg "$REJECT_MSG"
fi

echo "$TIMESTAMP REJECTED: $DONE_BASE (A:$VERDICT_A B:$VERDICT_B)" >> "$LOG_FILE"
exit 1
