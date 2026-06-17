#!/bin/bash
# =============================================================================
# plan-scan.sh — Haiku ötletgyűjtő scanner (szegmentált, adaptív throttling)
#
# Cron: */10 * * * * SPACEOS_ROOT=/opt/spaceos bash /opt/spaceos/scripts/plan-scan.sh
#
# Minden futás EGY szűk szegmenst vizsgál mélyen — adaptív sorrendben.
# Szegmensek: api-status · fe-memory · kernel-memory · joinery-memory ·
#             knowledge-adr · knowledge-api · knowledge-patterns · infra-memory · datahaven-resonance
#
# Adaptív működés:
#   - Exponenciális throttling: minél több ötlet van, annál ritkábban fut
#   - Hot spot prioritás: ahol korábban hasznos ötletet találtunk, oda többször megy
#   - Ha 5+ ötlet gyűlt össze → plan-select.sh indul
#
# Állapot: scripts/.plan-scan-state
#   - last_segment=N (forgó index)
#   - last_run=TIMESTAMP (utolsó tényleges futás)
#   - hotspots=segment1:count,segment2:count (sikeres találatok)
#
# A pipeline végén a konsenzus a queue/-ba kerül (2-3 pufferelt terv).
# A Conductor dolgozza fel a queue-t és adja ki a termináloknak.
# =============================================================================

source "$(dirname "$0")/common.sh"

PLANNING="$SPACEOS_ROOT/docs/planning"
IDEAS_DIR="$PLANNING/ideas"
SCAN_STATE="$SPACEOS_ROOT/scripts/.plan-scan-state"
CLAUDE_PROJ="/home/gabor/.claude/projects"
mkdir -p "$IDEAS_DIR"

DOMAIN=$(grep "^domain:" "$PLANNING/domain-focus.md" 2>/dev/null | sed 's/domain:\s*//' | tr -d '[:space:]')
DOMAIN="${DOMAIN:-all}"
DOMAIN_FOCUS=$(cat "$PLANNING/domain-focus.md" 2>/dev/null || echo "")
RECENT_IDEAS=$(ls -t "$IDEAS_DIR"/*.md 2>/dev/null | head -10 | xargs -I{} basename {} 2>/dev/null | tr '\n' ', ')

# ── Exponenciális throttling ─────────────────────────────────────────────────
#
# Ötletek száma → skip valószínűség
#   0-2 ötlet: nincs skip (mindig fut)
#   3-4 ötlet: 50% skip (10 percenként átlag)
#   5+ ötlet: nem fut (select trigger-re vár)
#
IDEA_COUNT=$(ls "$IDEAS_DIR"/*.md 2>/dev/null | wc -l)

if [ "$IDEA_COUNT" -ge 5 ]; then
  # Elég ötlet van, select indul
  echo "$TIMESTAMP Plan-scan skip: $IDEA_COUNT ötlet van, select trigger" >> "$LOG_DIR/pipeline.log"
  bash "$SPACEOS_ROOT/scripts/plan-select.sh" >> "$LOG_DIR/pipeline.log" 2>&1 &
  exit 0
fi

if [ "$IDEA_COUNT" -ge 3 ]; then
  # 50% skip valószínűség
  RANDOM_SKIP=$(( RANDOM % 2 ))
  if [ "$RANDOM_SKIP" -eq 1 ]; then
    echo "$TIMESTAMP Plan-scan throttle skip: $IDEA_COUNT ötlet, 50% skip" >> "$LOG_DIR/pipeline.log"
    exit 0
  fi
fi

# ── Szegmens kiválasztása (hot spot prioritással) ────────────────────────────

SEGMENTS=(
  "api-status"
  "fe-memory"
  "kernel-memory"
  "joinery-memory"
  "knowledge-adr"
  "knowledge-api"
  "knowledge-patterns"
  "infra-memory"
  "datahaven-resonance"
)
SEGMENT_COUNT=${#SEGMENTS[@]}

# ── Szegmens kiválasztási stratégia ──────────────────────────────────────────
#
# Stratégiák (random választás):
#   50% — Forgó sorrend (exploitation: szisztematikus lefedés)
#   30% — Hot spot (exploitation: ahol korábban találtunk)
#   20% — Véletlen (exploration: új területek felfedezése)
#
# Hot spot decay: minden select után 20%-kal csökken minden count
# (lásd plan-select.sh)

LAST=$(grep "^last_segment=" "$SCAN_STATE" 2>/dev/null | cut -d= -f2 || echo "-1")
HOTSPOTS=$(grep "^hotspots=" "$SCAN_STATE" 2>/dev/null | cut -d= -f2 || echo "")

pick_hotspot_segment() {
  [ -z "$HOTSPOTS" ] && return 1

  # Legnagyobb súlyú hot spot
  BEST=$(echo "$HOTSPOTS" | tr ',' '\n' | sort -t: -k2 -nr | head -1 | cut -d: -f1)
  [ -z "$BEST" ] && return 1

  for i in "${!SEGMENTS[@]}"; do
    [ "${SEGMENTS[$i]}" = "$BEST" ] && echo "$i" && return 0
  done
  return 1
}

# Stratégia választás: 50% forgó, 30% hot spot, 20% random
STRATEGY_ROLL=$(( RANDOM % 10 ))

if [ "$STRATEGY_ROLL" -lt 2 ]; then
  # 20% — Exploration: teljesen véletlenszerű szegmens
  NEXT=$(( RANDOM % SEGMENT_COUNT ))
  echo "$TIMESTAMP Plan-scan EXPLORATION mód: ${SEGMENTS[$NEXT]}" >> "$LOG_DIR/pipeline.log"

elif [ "$STRATEGY_ROLL" -lt 5 ] && [ -n "$HOTSPOTS" ]; then
  # 30% — Hot spot (ha van)
  HOTSPOT_IDX=$(pick_hotspot_segment)
  if [ -n "$HOTSPOT_IDX" ]; then
    NEXT=$HOTSPOT_IDX
    echo "$TIMESTAMP Plan-scan HOT SPOT mód: ${SEGMENTS[$NEXT]}" >> "$LOG_DIR/pipeline.log"
  else
    NEXT=$(( (LAST + 1) % SEGMENT_COUNT ))
  fi

else
  # 50% — Forgó sorrend
  NEXT=$(( (LAST + 1) % SEGMENT_COUNT ))
fi

SEGMENT="${SEGMENTS[$NEXT]}"

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

echo "$TIMESTAMP Plan-scan szegmens: $SEGMENT (domain: $DOMAIN, ideas: $IDEA_COUNT)" >> "$LOG_DIR/pipeline.log"

# ── Szegmens tartalom betöltése ───────────────────────────────────────────────

SEGMENT_LABEL=""
SEGMENT_CONTENT=""

case "$SEGMENT" in
  api-status)
    SEGMENT_LABEL="Frontend API integráció státusz"
    SEGMENT_CONTENT=$(cat "$SPACEOS_ROOT/frontend/joinerytech-portal/API_INTEGRATION_STATUS.md" 2>/dev/null \
      || echo "Fájl nem található.")
    ;;
  fe-memory)
    SEGMENT_LABEL="Frontend terminál memória (mit tanult az FE fejlesztés során)"
    for d in \
      "$CLAUDE_PROJ/-opt-spaceos-frontend-joinerytech-portal/memory" \
      "$CLAUDE_PROJ/-opt-spaceos-spaceos-doorstar-portal/memory"
    do
      [ -d "$d" ] || continue
      for f in "$d"/*.md; do
        [ -f "$f" ] || continue
        SEGMENT_CONTENT="${SEGMENT_CONTENT}\n\n--- $(basename $f) ---\n$(cat $f)"
      done
    done
    ;;
  kernel-memory)
    SEGMENT_LABEL="Kernel terminál memória (auth, tenant, FSM, audit tudás)"
    for d in \
      "$CLAUDE_PROJ/-opt-spaceos-backend-spaceos-kernel/memory" \
      "$CLAUDE_PROJ/-opt-spaceos-SpaceOS-Kerner/memory"
    do
      [ -d "$d" ] || continue
      for f in "$d"/*.md; do
        [ -f "$f" ] || continue
        SEGMENT_CONTENT="${SEGMENT_CONTENT}\n\n--- $(basename $f) ---\n$(cat $f)"
      done
    done
    ;;
  joinery-memory)
    SEGMENT_LABEL="Joinery + Cutting + Orchestrator terminál memória"
    for d in \
      "$CLAUDE_PROJ/-opt-spaceos-backend-spaceos-modules-joinery/memory" \
      "$CLAUDE_PROJ/-opt-spaceos-spaceos-modules-joinery/memory" \
      "$CLAUDE_PROJ/-opt-spaceos-spaceos-orchestrator/memory"
    do
      [ -d "$d" ] || continue
      for f in "$d"/*.md; do
        [ -f "$f" ] || continue
        SEGMENT_CONTENT="${SEGMENT_CONTENT}\n\n--- $(basename $f) ---\n$(cat $f)"
      done
    done
    ;;
  knowledge-adr)
    SEGMENT_LABEL="Architekturális döntések katalógusa (ADR)"
    SEGMENT_CONTENT=$(cat "$SPACEOS_ROOT/docs/knowledge/architecture/ADR_CATALOGUE.md" 2>/dev/null \
      || echo "Nem létezik még.")
    ;;
  knowledge-api)
    SEGMENT_LABEL="API contract katalógus (minden létező endpoint)"
    SEGMENT_CONTENT=$(cat "$SPACEOS_ROOT/docs/knowledge/architecture/API_CONTRACT_CATALOGUE.md" 2>/dev/null \
      || echo "Nem létezik még.")
    ;;
  knowledge-patterns)
    SEGMENT_LABEL="Fejlesztési minták és visszatérő problémák"
    SEGMENT_CONTENT=$(cat "$SPACEOS_ROOT/docs/knowledge/patterns/DEV_DIFFICULTIES.md" 2>/dev/null; \
                      cat "$SPACEOS_ROOT/docs/knowledge/patterns/DATABASE_PATTERNS.md" 2>/dev/null; \
                      cat "$SPACEOS_ROOT/docs/knowledge/deployment/KNOWN_GOTCHAS.md" 2>/dev/null)
    ;;
  infra-memory)
    SEGMENT_LABEL="Infra + E2E terminál memória (deploy, tesztelés tudás)"
    for d in \
      "$CLAUDE_PROJ/-opt-spaceos-infra/memory" \
      "$CLAUDE_PROJ/-opt-spaceos-e2e/memory"
    do
      [ -d "$d" ] || continue
      for f in "$d"/*.md; do
        [ -f "$f" ] || continue
        SEGMENT_CONTENT="${SEGMENT_CONTENT}\n\n--- $(basename $f) ---\n$(cat $f)"
      done
    done
    ;;
  datahaven-resonance)
    SEGMENT_LABEL="Datahaven + Resonance fejlesztési állapot (agent infrastruktúra)"
    SEGMENT_CONTENT=$(cat "$SPACEOS_ROOT/docs/agent-infrastructure/ROADMAP.md" 2>/dev/null)
    SEGMENT_CONTENT="${SEGMENT_CONTENT}\n\n--- DECISIONS ---\n$(cat "$SPACEOS_ROOT/docs/agent-infrastructure/DECISIONS.md" 2>/dev/null)"
    for d in "$CLAUDE_PROJ/-opt-spaceos-spaceos-nexus/memory" \
              "$CLAUDE_PROJ/-home-gabor-datahaven/memory" \
              "$CLAUDE_PROJ/-home-gabor-resonance/memory"
    do
      [ -d "$d" ] || continue
      for f in "$d"/*.md; do
        [ -f "$f" ] || continue
        SEGMENT_CONTENT="${SEGMENT_CONTENT}\n\n--- $(basename $f) ---\n$(cat $f)"
      done
    done
    ;;
esac

# Üres szegmens → kihagyás
if [ -z "$(echo "$SEGMENT_CONTENT" | tr -d '[:space:]')" ]; then
  echo "$TIMESTAMP Plan-scan skip: $SEGMENT üres" >> "$LOG_DIR/pipeline.log"
  exit 0
fi

# ── Haiku prompt (csak a kiválasztott szegmens) ───────────────────────────────

NEXT_NUM=$(printf "%03d" $(( $(ls "$IDEAS_DIR"/*.md 2>/dev/null | wc -l) + 1 )))

SCAN_PROMPT="# SpaceOS Ötletgyűjtő Scanner — Szegmens: ${SEGMENT}

Te egy SpaceOS tervezési scanner vagy. Most EGYETLEN szűk területet vizsgálsz mélyen.

## Domain fókusz:
${DOMAIN_FOCUS}

## Vizsgált szegmens: ${SEGMENT_LABEL}

${SEGMENT_CONTENT}

## Már összegyűjtött ötletek (ne duplikáld):
${RECENT_IDEAS:-nincs még}

## Feladatod

Vizsgáld meg a fenti ${SEGMENT_LABEL} tartalmát és azonosíts
1-2 konkrét fejlesztési lehetőséget a \`${DOMAIN}\` domain fókuszban.

Kérdések amiket tegyél fel magadnak:
- Mit mutat ez a szegmens ami hiányzik, megoldatlan, vagy jobban lehetne?
- Van-e olyan minta ami máshol működik de itt még nincs alkalmazva?
- Van-e ismert probléma (memóriában, döntésben) ami most megoldható lenne?
- Kapcsolódik-e valami a domain fókuszhoz amit érdemes fejleszteni?

Ha nem találsz valódi, konkrét ötletet → írj 0 fájlt. Jobb semmi mint erőltetett.

Minden ötletre írj egy fájlt:
Mappa: $IDEAS_DIR/
Fájlnév: $(date +%Y-%m-%d)_${NEXT_NUM}_<slug>.md

Frontmatter:
\`\`\`
---
domain: ${DOMAIN}
segment: ${SEGMENT}
type: feature_gap | endpoint_gap | ux_gap | industry_pattern | memory_insight
priority: high | medium | low
created: $(date +%Y-%m-%d)
---
\`\`\`

Tartalom: Mit old meg · Jelenlegi állapot · Bekötési lehetőség · Iparági relevancia

Csak az ideas/ mappába írj."

echo "$SCAN_PROMPT" | claude -p --model haiku \
  --allowedTools "Write" \
  2>/dev/null

# ── Threshold ellenőrzés ──────────────────────────────────────────────────────

IDEA_COUNT=$(ls "$IDEAS_DIR"/*.md 2>/dev/null | wc -l)
echo "$TIMESTAMP Plan-scan kész: $IDEA_COUNT ötlet (szegmens: $SEGMENT)" >> "$LOG_DIR/pipeline.log"

if [ "$IDEA_COUNT" -ge 5 ]; then
  echo "$TIMESTAMP Plan-select trigger: $IDEA_COUNT ötlet" >> "$LOG_DIR/pipeline.log"
  bash "$SPACEOS_ROOT/scripts/plan-select.sh" >> "$LOG_DIR/pipeline.log" 2>&1 &
fi
