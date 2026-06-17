#!/bin/bash
# =============================================================================
# plan-select.sh — Sonnet szűrő + web kutató (hideg indítás, konfigurálható)
#
# Összegyűjti az ötleteket, rangsorolja, webes mintákat keres hozzájuk,
# és egy összefogott "selected" dokumentumot ír.
#
# Minden paraméter a plan-config.yaml fájlból jön.
# A prompt a prompts/plan-select-prompt.md fájlból jön.
#
# Hívja: plan-scan.sh (ha elég ötlet van)
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

# Modellek és timing
SELECTOR_MODEL=$(yaml_get "$CONFIG_FILE" "models.selector" "sonnet")
SELECTOR_TIMEOUT=$(yaml_get "$CONFIG_FILE" "timing.selector_timeout" "120")
FILE_WAIT=$(yaml_get "$CONFIG_FILE" "timing.file_wait" "2")

# Stratégia
HOTSPOT_DECAY=$(yaml_get "$CONFIG_FILE" "strategy.hotspot_decay" "0.8")

# Útvonalak
IDEAS_DIR="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "paths.ideas_dir" "docs/planning/ideas")"
SELECTED_DIR="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "paths.selected_dir" "docs/planning/selected")"
DOMAIN_FOCUS_FILE="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "paths.domain_focus" "docs/planning/domain-focus.md")"
SCAN_STATE="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "paths.scan_state" "scripts/.plan-scan-state")"
PROMPT_FILE="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "prompts.selector" "scripts/prompts/plan-select-prompt.md")"

mkdir -p "$SELECTED_DIR"

# Ne fusson ha az előző selected még feldolgozatlan
if [ -f "$SELECTED_DIR/pending.md" ]; then
    echo "$TIMESTAMP Plan-select skip — pending már létezik" >> "$LOG_DIR/pipeline.log"
    exit 0
fi

# ── Ötletek összegyűjtése ────────────────────────────────────────────────────

ALL_IDEAS=""
for f in "$IDEAS_DIR"/*.md; do
    [ -f "$f" ] || continue
    ALL_IDEAS="${ALL_IDEAS}\n\n--- $(basename "$f") ---\n$(cat "$f")"
done

if [ -z "$(echo -e "$ALL_IDEAS" | tr -d '[:space:]')" ]; then
    echo "$TIMESTAMP Plan-select skip — nincs ötlet" >> "$LOG_DIR/pipeline.log"
    exit 0
fi

DOMAIN_FOCUS=$(cat "$DOMAIN_FOCUS_FILE" 2>/dev/null || echo "")
DATE=$(date +%Y-%m-%d)

# ── Prompt összeállítása ─────────────────────────────────────────────────────

if [ ! -f "$PROMPT_FILE" ]; then
    echo "HIBA: Prompt file nem található: $PROMPT_FILE" >&2
    exit 1
fi

SELECT_PROMPT=$(cat "$PROMPT_FILE")
SELECT_PROMPT="${SELECT_PROMPT//\{\{DOMAIN_FOCUS\}\}/$DOMAIN_FOCUS}"
SELECT_PROMPT="${SELECT_PROMPT//\{\{ALL_IDEAS\}\}/$(echo -e "$ALL_IDEAS")}"
SELECT_PROMPT="${SELECT_PROMPT//\{\{DATE\}\}/$DATE}"
SELECT_PROMPT="${SELECT_PROMPT//\{\{MODEL\}\}/$SELECTOR_MODEL}"

# ── Hideg indítás: Claude futtatás ───────────────────────────────────────────

SELECT_OUTPUT=$(echo "$SELECT_PROMPT" | timeout "$SELECTOR_TIMEOUT" claude -p --model "$SELECTOR_MODEL" \
    --allowedTools "WebSearch" \
    2>/dev/null)

# Frontmatter blokkot kivonjuk
echo "$SELECT_OUTPUT" | sed -n '/^---$/,$ p' > "$SELECTED_DIR/pending.md"

sleep "$FILE_WAIT"

echo "$TIMESTAMP Plan-select kész (model: $SELECTOR_MODEL)" >> "$LOG_DIR/pipeline.log"

# ── Hot spot tracking + archive + debate ─────────────────────────────────────

if [ -s "$SELECTED_DIR/pending.md" ]; then
    # Decay alkalmazása
    OLD_HOTSPOTS=$(grep "^hotspots=" "$SCAN_STATE" 2>/dev/null | cut -d= -f2 || echo "")
    HOTSPOTS=""

    if [ -n "$OLD_HOTSPOTS" ]; then
        IFS=',' read -ra PAIRS <<< "$OLD_HOTSPOTS"
        for pair in "${PAIRS[@]}"; do
            SEG=$(echo "$pair" | cut -d: -f1)
            CNT=$(echo "$pair" | cut -d: -f2)
            # Decay (bash integer: * 8 / 10 ≈ 0.8)
            NEW_CNT=$(( CNT * 8 / 10 ))
            if [ "$NEW_CNT" -gt 0 ]; then
                [ -n "$HOTSPOTS" ] && HOTSPOTS="${HOTSPOTS},"
                HOTSPOTS="${HOTSPOTS}${SEG}:${NEW_CNT}"
            fi
        done
    fi

    # Új találatok hozzáadása
    for f in "$IDEAS_DIR"/*.md; do
        [ -f "$f" ] || continue
        SEGMENT=$(grep -m1 "^segment:" "$f" 2>/dev/null | sed 's/segment:\s*//' | tr -d '[:space:]')
        [ -z "$SEGMENT" ] && continue

        if echo "$HOTSPOTS" | grep -q "${SEGMENT}:"; then
            OLD_COUNT=$(echo "$HOTSPOTS" | tr ',' '\n' | grep "^${SEGMENT}:" | cut -d: -f2)
            NEW_COUNT=$(( OLD_COUNT + 1 ))
            HOTSPOTS=$(echo "$HOTSPOTS" | sed "s/${SEGMENT}:${OLD_COUNT}/${SEGMENT}:${NEW_COUNT}/")
        else
            [ -n "$HOTSPOTS" ] && HOTSPOTS="${HOTSPOTS},"
            HOTSPOTS="${HOTSPOTS}${SEGMENT}:1"
        fi
    done

    # Hot spot mentése
    if grep -q "^hotspots=" "$SCAN_STATE" 2>/dev/null; then
        sed -i "s/^hotspots=.*/hotspots=${HOTSPOTS}/" "$SCAN_STATE"
    else
        echo "hotspots=${HOTSPOTS}" >> "$SCAN_STATE"
    fi

    echo "$TIMESTAMP Hot spot frissítve: $HOTSPOTS" >> "$LOG_DIR/pipeline.log"

    # Archive
    mkdir -p "$IDEAS_DIR/archive"
    for f in "$IDEAS_DIR"/*.md; do
        [ -f "$f" ] && mv "$f" "$IDEAS_DIR/archive/"
    done

    echo "$TIMESTAMP Ötletek archiválva, plan-debate indul" >> "$LOG_DIR/pipeline.log"
    bash "$SCRIPT_DIR/plan-debate.sh" >> "$LOG_DIR/pipeline.log" 2>&1 &
else
    echo "$TIMESTAMP Plan-select HIBA: pending.md üres" >> "$LOG_DIR/pipeline.log"
    rm -f "$SELECTED_DIR/pending.md"
fi
