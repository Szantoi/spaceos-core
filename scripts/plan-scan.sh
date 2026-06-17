#!/bin/bash
# =============================================================================
# plan-scan.sh — Ötletgyűjtő scanner (hideg indítás, konfigurálható)
#
# Minden futás EGY szűk szegmenst vizsgál mélyen — adaptív sorrendben.
# Minden paraméter a plan-config.yaml fájlból jön.
# A prompt a prompts/plan-scan-prompt.md fájlból jön.
#
# Cron: */10 * * * *
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
SCANNER_MODEL=$(yaml_get "$CONFIG_FILE" "models.scanner" "haiku")
SCANNER_TIMEOUT=$(yaml_get "$CONFIG_FILE" "timing.scanner_timeout" "60")

# Throttling
IDEAS_THRESHOLD=$(yaml_get "$CONFIG_FILE" "throttling.ideas_threshold" "5")
SKIP_THRESHOLD=$(yaml_get "$CONFIG_FILE" "throttling.skip_threshold" "3")

# Stratégia
ROTATING_PCT=$(yaml_get "$CONFIG_FILE" "strategy.rotating_percent" "50")
HOTSPOT_PCT=$(yaml_get "$CONFIG_FILE" "strategy.hotspot_percent" "30")

# Útvonalak
PLANNING_DIR="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "paths.planning_dir" "docs/planning")"
IDEAS_DIR="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "paths.ideas_dir" "docs/planning/ideas")"
DOMAIN_FOCUS_FILE="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "paths.domain_focus" "docs/planning/domain-focus.md")"
SCAN_STATE="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "paths.scan_state" "scripts/.plan-scan-state")"
PROMPT_FILE="$SPACEOS_ROOT/$(yaml_get "$CONFIG_FILE" "prompts.scanner" "scripts/prompts/plan-scan-prompt.md")"

mkdir -p "$IDEAS_DIR"

# Domain fókusz betöltése
DOMAIN=$(grep "^domain:" "$DOMAIN_FOCUS_FILE" 2>/dev/null | sed 's/domain:\s*//' | tr -d '[:space:]')
DOMAIN="${DOMAIN:-all}"
DOMAIN_FOCUS=$(cat "$DOMAIN_FOCUS_FILE" 2>/dev/null || echo "")
RECENT_IDEAS=$(ls -t "$IDEAS_DIR"/*.md 2>/dev/null | head -10 | xargs -I{} basename {} 2>/dev/null | tr '\n' ', ')

# ── Exponenciális throttling ─────────────────────────────────────────────────

IDEA_COUNT=$(ls "$IDEAS_DIR"/*.md 2>/dev/null | wc -l)

if [ "$IDEA_COUNT" -ge "$IDEAS_THRESHOLD" ]; then
    echo "$TIMESTAMP Plan-scan skip: $IDEA_COUNT ötlet van, select trigger" >> "$LOG_DIR/pipeline.log"
    bash "$SCRIPT_DIR/plan-select.sh" >> "$LOG_DIR/pipeline.log" 2>&1 &
    exit 0
fi

if [ "$IDEA_COUNT" -ge "$SKIP_THRESHOLD" ]; then
    RANDOM_SKIP=$(( RANDOM % 2 ))
    if [ "$RANDOM_SKIP" -eq 1 ]; then
        echo "$TIMESTAMP Plan-scan throttle skip: $IDEA_COUNT ötlet, 50% skip" >> "$LOG_DIR/pipeline.log"
        exit 0
    fi
fi

# ── Szegmensek betöltése a konfigból ─────────────────────────────────────────

mapfile -t SEGMENTS < <(yaml_get_segments "$CONFIG_FILE")
SEGMENT_COUNT=${#SEGMENTS[@]}

if [ "$SEGMENT_COUNT" -eq 0 ]; then
    echo "$TIMESTAMP Plan-scan HIBA: nincs szegmens a konfigban" >> "$LOG_DIR/pipeline.log"
    exit 1
fi

# ── Szegmens kiválasztási stratégia ──────────────────────────────────────────

LAST=$(grep "^last_segment=" "$SCAN_STATE" 2>/dev/null | cut -d= -f2 || echo "-1")
HOTSPOTS=$(grep "^hotspots=" "$SCAN_STATE" 2>/dev/null | cut -d= -f2 || echo "")

pick_hotspot_segment() {
    [ -z "$HOTSPOTS" ] && return 1
    BEST=$(echo "$HOTSPOTS" | tr ',' '\n' | sort -t: -k2 -nr | head -1 | cut -d: -f1)
    [ -z "$BEST" ] && return 1

    for i in "${!SEGMENTS[@]}"; do
        [ "${SEGMENTS[$i]}" = "$BEST" ] && echo "$i" && return 0
    done
    return 1
}

# Stratégia választás: rotating vs hotspot vs exploration
STRATEGY_ROLL=$(( RANDOM % 100 ))
EXPLORATION_THRESHOLD=$((100 - ROTATING_PCT - HOTSPOT_PCT))

if [ "$STRATEGY_ROLL" -lt "$EXPLORATION_THRESHOLD" ]; then
    # Exploration: véletlenszerű
    NEXT=$(( RANDOM % SEGMENT_COUNT ))
    echo "$TIMESTAMP Plan-scan EXPLORATION: ${SEGMENTS[$NEXT]}" >> "$LOG_DIR/pipeline.log"

elif [ "$STRATEGY_ROLL" -lt "$((EXPLORATION_THRESHOLD + HOTSPOT_PCT))" ] && [ -n "$HOTSPOTS" ]; then
    # Hot spot
    HOTSPOT_IDX=$(pick_hotspot_segment)
    if [ -n "$HOTSPOT_IDX" ]; then
        NEXT=$HOTSPOT_IDX
        echo "$TIMESTAMP Plan-scan HOT SPOT: ${SEGMENTS[$NEXT]}" >> "$LOG_DIR/pipeline.log"
    else
        NEXT=$(( (LAST + 1) % SEGMENT_COUNT ))
    fi
else
    # Forgó sorrend
    NEXT=$(( (LAST + 1) % SEGMENT_COUNT ))
fi

SEGMENT_NAME="${SEGMENTS[$NEXT]}"

# Állapot frissítése
touch "$SCAN_STATE"
if grep -q "^last_segment=" "$SCAN_STATE" 2>/dev/null; then
    sed -i "s/^last_segment=.*/last_segment=${NEXT}/" "$SCAN_STATE"
else
    echo "last_segment=${NEXT}" >> "$SCAN_STATE"
fi

if grep -q "^last_run=" "$SCAN_STATE" 2>/dev/null; then
    sed -i "s/^last_run=.*/last_run=${NOW}/" "$SCAN_STATE"
else
    echo "last_run=${NOW}" >> "$SCAN_STATE"
fi

echo "$TIMESTAMP Plan-scan szegmens: $SEGMENT_NAME (domain: $DOMAIN, ideas: $IDEA_COUNT)" >> "$LOG_DIR/pipeline.log"

# ── Szegmens tartalom betöltése ──────────────────────────────────────────────

SEGMENT_LABEL=$(yaml_get_segment_field "$CONFIG_FILE" "$SEGMENT_NAME" "label")
SEGMENT_CONTENT=""

# Sources kiolvasása
while IFS= read -r source_path; do
    [ -z "$source_path" ] && continue
    FULL_PATH="$SPACEOS_ROOT/$source_path"
    if [ -f "$FULL_PATH" ]; then
        SEGMENT_CONTENT="${SEGMENT_CONTENT}\n\n--- $(basename "$source_path") ---\n$(cat "$FULL_PATH")"
    fi
done < <(yaml_get_segment_sources "$CONFIG_FILE" "$SEGMENT_NAME")

# Üres szegmens → kihagyás
if [ -z "$(echo -e "$SEGMENT_CONTENT" | tr -d '[:space:]')" ]; then
    echo "$TIMESTAMP Plan-scan skip: $SEGMENT_NAME üres" >> "$LOG_DIR/pipeline.log"
    exit 0
fi

# ── Prompt összeállítása ─────────────────────────────────────────────────────

if [ ! -f "$PROMPT_FILE" ]; then
    echo "HIBA: Prompt file nem található: $PROMPT_FILE" >&2
    exit 1
fi

NEXT_NUM=$(printf "%03d" $(( $(ls "$IDEAS_DIR"/*.md 2>/dev/null | wc -l) + 1 )))
DATE=$(date +%Y-%m-%d)

SCAN_PROMPT=$(cat "$PROMPT_FILE")
SCAN_PROMPT="${SCAN_PROMPT//\{\{SEGMENT_NAME\}\}/$SEGMENT_NAME}"
SCAN_PROMPT="${SCAN_PROMPT//\{\{SEGMENT_LABEL\}\}/$SEGMENT_LABEL}"
SCAN_PROMPT="${SCAN_PROMPT//\{\{SEGMENT_CONTENT\}\}/$(echo -e "$SEGMENT_CONTENT")}"
SCAN_PROMPT="${SCAN_PROMPT//\{\{DOMAIN_FOCUS\}\}/$DOMAIN_FOCUS}"
SCAN_PROMPT="${SCAN_PROMPT//\{\{DOMAIN\}\}/$DOMAIN}"
SCAN_PROMPT="${SCAN_PROMPT//\{\{RECENT_IDEAS\}\}/${RECENT_IDEAS:-nincs még}}"
SCAN_PROMPT="${SCAN_PROMPT//\{\{IDEAS_DIR\}\}/$IDEAS_DIR}"
SCAN_PROMPT="${SCAN_PROMPT//\{\{DATE\}\}/$DATE}"
SCAN_PROMPT="${SCAN_PROMPT//\{\{NEXT_NUM\}\}/$NEXT_NUM}"

# ── Hideg indítás: Claude futtatás ───────────────────────────────────────────

echo "$SCAN_PROMPT" | timeout "$SCANNER_TIMEOUT" claude -p --model "$SCANNER_MODEL" \
    --allowedTools "Write" \
    2>/dev/null

# ── Threshold ellenőrzés ─────────────────────────────────────────────────────

IDEA_COUNT=$(ls "$IDEAS_DIR"/*.md 2>/dev/null | wc -l)
echo "$TIMESTAMP Plan-scan kész: $IDEA_COUNT ötlet (szegmens: $SEGMENT_NAME, model: $SCANNER_MODEL)" >> "$LOG_DIR/pipeline.log"

if [ "$IDEA_COUNT" -ge "$IDEAS_THRESHOLD" ]; then
    echo "$TIMESTAMP Plan-select trigger: $IDEA_COUNT ötlet" >> "$LOG_DIR/pipeline.log"
    bash "$SCRIPT_DIR/plan-select.sh" >> "$LOG_DIR/pipeline.log" 2>&1 &
fi
