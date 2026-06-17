#!/bin/bash
# =============================================================================
# plan-select.sh — Sonnet szűrő + web kutató
#
# Egyetlen felelőssége:
#   Összegyűjti az ötleteket, rangsorolja, webes mintákat keres hozzájuk,
#   és egy összefogott "selected" dokumentumot ír — amire az Opus tervező épít.
#   Ha kész → elindítja a plan-debate.sh-t.
#
# Hívja: plan-scan.sh (ha 5+ ötlet van)
# =============================================================================

source "$(dirname "$0")/common.sh"

PLANNING="$SPACEOS_ROOT/docs/planning"
IDEAS_DIR="$PLANNING/ideas"
SELECTED_DIR="$PLANNING/selected"
mkdir -p "$SELECTED_DIR"

# Ne fusson ha az előző selected még feldolgozatlan
if [ -f "$SELECTED_DIR/pending.md" ]; then
  echo "$TIMESTAMP Plan-select skip — pending már létezik" >> "$LOG_DIR/pipeline.log"
  exit 0
fi

# Összes ötlet összegyűjtése
ALL_IDEAS=""
for f in "$IDEAS_DIR"/*.md; do
  [ -f "$f" ] || continue
  ALL_IDEAS="${ALL_IDEAS}\n\n--- $(basename $f) ---\n$(cat $f)"
done

DOMAIN_FOCUS=$(cat "$PLANNING/domain-focus.md" 2>/dev/null || echo "")

SELECT_PROMPT="# SpaceOS Ötlet Szűrő és Kutató

Te a SpaceOS tervezési pipeline Sonnet komponense vagy.
Összegyűjtöttünk ötleteket a Haiku scanner-től — most szelektálsz és kutatasz.

## Domain fókusz:
${DOMAIN_FOCUS}

## Összegyűlt ötletek:
${ALL_IDEAS}

## Feladatod — 3 lépés:

### 1. Rangsorolás
Értékeld az ötleteket:
- Felhasználói értéke (mennyire fontos Doorstar napi munkájához)
- Megvalósíthatóság (van-e backend, mennyi munka)
- Iparági relevancia (ez standard egy ERP-ben?)
Válaszd ki a TOP 3-at.

### 2. Web kutatás
A TOP 3 ötlethez keress webes mintákat:
- Hogyan oldják meg más ERP/MES rendszerek (pl. Odoo, SAP, Monday.com)?
- Van-e bevált UX pattern ehhez a funkcióhoz?
- Milyen adatmodell tipikus?
Használd a WebSearch tool-t: pl. 'manufacturing ERP work order mobile UX pattern'

### 3. Kimenet formátuma (stdout-ra írd — a szkript menti fájlba)

Pontosan ezt a struktúrát add vissza:

\`\`\`
---
created: $(date +%Y-%m-%d)
selected_by: sonnet
status: pending_debate
top_count: 3
---

# SpaceOS Planning — Kiválasztott fejlesztési irányok

## TOP 1: [Ötlet neve]
**Miért top:** ...
**Webes minták:** ...
**Javasolt megközelítés:** ...

## TOP 2: [Ötlet neve]
...

## TOP 3: [Ötlet neve]
...

## Elvetett ötletek (és miért)
- [Ötlet]: [ok]
\`\`\`"

# Kimenet befogása és fájlba írás (nem a claude Write tool-ja csinálja)
SELECT_OUTPUT=$(echo "$SELECT_PROMPT" | claude -p --model sonnet \
  --allowedTools "WebSearch" \
  2>/dev/null)

# Frontmatter blokkot kivonjuk (--- közötti rész + utána minden)
echo "$SELECT_OUTPUT" | sed -n '/^---$/,$ p' > "$SELECTED_DIR/pending.md"

echo "$TIMESTAMP Plan-select kész" >> "$LOG_DIR/pipeline.log"

# Ha sikerült → hot spot tracking + archive + debate
if [ -s "$SELECTED_DIR/pending.md" ]; then
  # ── Hot spot tracking + decay ──────────────────────────────────────────────
  #
  # 1. Decay: minden meglévő count *= 0.8 (időben csökkenő súlyok)
  # 2. Új találatok: +1 a releváns szegmensekhez
  #
  SCAN_STATE="$SPACEOS_ROOT/scripts/.plan-scan-state"
  OLD_HOTSPOTS=$(grep "^hotspots=" "$SCAN_STATE" 2>/dev/null | cut -d= -f2 || echo "")

  # Decay alkalmazása (count * 0.8, lefelé kerekítve)
  HOTSPOTS=""
  if [ -n "$OLD_HOTSPOTS" ]; then
    IFS=',' read -ra PAIRS <<< "$OLD_HOTSPOTS"
    for pair in "${PAIRS[@]}"; do
      SEG=$(echo "$pair" | cut -d: -f1)
      CNT=$(echo "$pair" | cut -d: -f2)
      # 80% decay (bash integer math: * 8 / 10)
      NEW_CNT=$(( CNT * 8 / 10 ))
      # Töröljük ha 0-ra csökkent
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
      # Meglévő: count + 1
      OLD_COUNT=$(echo "$HOTSPOTS" | tr ',' '\n' | grep "^${SEGMENT}:" | cut -d: -f2)
      NEW_COUNT=$(( OLD_COUNT + 1 ))
      HOTSPOTS=$(echo "$HOTSPOTS" | sed "s/${SEGMENT}:${OLD_COUNT}/${SEGMENT}:${NEW_COUNT}/")
    else
      # Új szegmens
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

  echo "$TIMESTAMP Hot spot frissítve (decay+új): $HOTSPOTS" >> "$LOG_DIR/pipeline.log"

  # Archive
  mkdir -p "$IDEAS_DIR/archive"
  for f in "$IDEAS_DIR"/*.md; do
    [ -f "$f" ] && mv "$f" "$IDEAS_DIR/archive/"
  done
  echo "$TIMESTAMP Ötletek archiválva, plan-debate indul" >> "$LOG_DIR/pipeline.log"
  bash "$SPACEOS_ROOT/scripts/plan-debate.sh" >> "$LOG_DIR/pipeline.log" 2>&1 &
else
  echo "$TIMESTAMP Plan-select HIBA: pending.md üres" >> "$LOG_DIR/pipeline.log"
  rm -f "$SELECTED_DIR/pending.md"
fi
