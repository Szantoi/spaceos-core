#!/bin/bash
# =============================================================================
# motivator.sh — SpaceOS terminál motivátor
# Streak tracker + milestone üzenetek + progress kalkuláció
# Hívható: bash /opt/spaceos/scripts/motivator.sh [--json]
# =============================================================================

BOLD='\033[1m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
MAGENTA='\033[0;35m'
BLUE='\033[0;34m'
DIM='\033[2m'
NC='\033[0m'

SPACEOS_ROOT="${SPACEOS_ROOT:-/opt/spaceos}"
STREAK_FILE="$SPACEOS_ROOT/scripts/.streak"
STATE_FILE="$SPACEOS_ROOT/scripts/.motivator-state"
JSON_MODE=false
[ "$1" = "--json" ] && JSON_MODE=true

# ── Streak számítás ────────────────────────────────────────────────────────────

TODAY=$(date +%Y-%m-%d)
YESTERDAY=$(date -d "yesterday" +%Y-%m-%d 2>/dev/null || date -v-1d +%Y-%m-%d 2>/dev/null)

if [ -f "$STREAK_FILE" ]; then
  LAST_DATE=$(head -1 "$STREAK_FILE" 2>/dev/null || echo "")
  STREAK=$(tail -1 "$STREAK_FILE" 2>/dev/null || echo "0")

  if [ "$LAST_DATE" = "$TODAY" ]; then
    : # Ma már volt aktivitás, streak nem változik
  elif [ "$LAST_DATE" = "$YESTERDAY" ]; then
    STREAK=$((STREAK + 1))
    printf "%s\n%d" "$TODAY" "$STREAK" > "$STREAK_FILE"
  else
    STREAK=1
    printf "%s\n%d" "$TODAY" "$STREAK" > "$STREAK_FILE"
  fi
else
  STREAK=1
  printf "%s\n%d" "$TODAY" "$STREAK" > "$STREAK_FILE"
fi

# ── Progress kalkuláció ────────────────────────────────────────────────────────

WORLDS_ALL=("tasks" "production" "mfgprep" "supervisor" "sales" "crm" "design" "interior"
            "procurement" "finance" "masterdata" "warehouse" "shopfloor" "shop" "trade"
            "projects" "logistics" "kontrolling" "service" "hr" "maintenance" "quality"
            "ehs" "docs" "attendance" "settings" "ai")
TOTAL_WORLDS=${#WORLDS_ALL[@]}

# Valós fájlrendszer ellenőrzés
DONE_WORLDS=0
PAGES_DIR="$SPACEOS_ROOT/frontend/joinerytech-portal/src/pages"

declare -A WORLD_PAGE_MAP=(
  [production]="ProductionPage"
  [design]="DesignPage"
  [procurement]="ProcurementPage"
  [sales]="SalesPage"
  [settings]="SettingsPage"
  [shopfloor]="ShopFloorPage"
  [warehouse]="WarehousePage"
  [crm]="CrmPage"
  [finance]="FinancePage"
  [hr]="HrPage"
  [kontrolling]="ControllingPage"
  [projects]="ProjectsPage"
  [logistics]="LogisticsPage"
  [masterdata]="MasterdataPage"
  [interior]="InteriorPage"
  [mfgprep]="MfgPrepPage"
  [supervisor]="SupervisorPage"
  [maintenance]="MaintenancePage"
  [quality]="QualityPage"
  [ehs]="EhsPage"
  [docs]="DocsPage"
  [attendance]="AttendancePage"
  [ai]="AiPage"
  [tasks]="TasksPage"
  [shop]="ShopPage"
  [trade]="TradePage"
)

DONE_LIST=()
TODO_LIST=()

for world in "${WORLDS_ALL[@]}"; do
  page="${WORLD_PAGE_MAP[$world]:-}"
  found=false
  if [ -n "$page" ]; then
    # Pontos fájl keresés (.tsx, .jsx) VAGY mappa (pl. warehouse/, production/)
    if ls "$PAGES_DIR/${page}.tsx" "$PAGES_DIR/${page}.jsx" 2>/dev/null | grep -q .; then
      found=true
    elif [ -d "$PAGES_DIR/${world}" ]; then
      found=true
    fi
  fi
  if $found; then
    DONE_LIST+=("$world")
    DONE_WORLDS=$((DONE_WORLDS + 1))
  else
    TODO_LIST+=("$world")
  fi
done

PERCENT=$(( (DONE_WORLDS * 100) / TOTAL_WORLDS ))

# ── Frontend teszt szám ────────────────────────────────────────────────────────

TEST_COUNT=0
if [ -f "$SPACEOS_ROOT/frontend/joinerytech-portal/package.json" ]; then
  # Vitest test fájlok száma × átlag tesztek (vagy spec snapshot)
  TEST_FILES=$(find "$SPACEOS_ROOT/frontend/joinerytech-portal/src" -name "*.test.*" 2>/dev/null | wc -l)
  # Utolsó ismert teszt szám (cache)
  if [ -f "$STATE_FILE" ]; then
    TEST_COUNT=$(grep "^TESTS=" "$STATE_FILE" 2>/dev/null | cut -d= -f2 || echo "391")
  else
    TEST_COUNT=391
  fi
fi

# ── Backend teszt szám (összesített) ──────────────────────────────────────────

BACKEND_TESTS=3902  # Utolsó ismert érték — frissítsd deploy után
if [ -f "$STATE_FILE" ]; then
  BT=$(grep "^BACKEND_TESTS=" "$STATE_FILE" 2>/dev/null | cut -d= -f2)
  [ -n "$BT" ] && BACKEND_TESTS=$BT
fi

# ── Milestone detektálás ───────────────────────────────────────────────────────

PREV_WORLDS=0
if [ -f "$STATE_FILE" ]; then
  PREV_WORLDS=$(grep "^WORLDS=" "$STATE_FILE" 2>/dev/null | cut -d= -f2 || echo "0")
fi

# State mentés
{
  echo "WORLDS=$DONE_WORLDS"
  echo "TESTS=$TEST_COUNT"
  echo "BACKEND_TESTS=$BACKEND_TESTS"
  echo "PERCENT=$PERCENT"
  echo "DATE=$TODAY"
} > "$STATE_FILE"

MILESTONE_MSG=""
if [ "$DONE_WORLDS" -gt "$PREV_WORLDS" ]; then
  NEW_WORLDS=$((DONE_WORLDS - PREV_WORLDS))
  MILESTONE_MSG="🎉 +${NEW_WORLDS} új világ implementálva ma!"
fi

if [ "$PERCENT" -ge 50 ] && [ "$PREV_WORLDS" -lt 14 ] && [ "$DONE_WORLDS" -ge 14 ]; then
  MILESTONE_MSG="🏆 FÉLÚTON! 50% elérve — ${DONE_WORLDS}/27 világ kész!"
fi

if [ "$DONE_WORLDS" -eq "$TOTAL_WORLDS" ]; then
  MILESTONE_MSG="🚀 MINDEN VILÁG IMPLEMENTÁLVA! A portal teljes!"
fi

# ── Motiváló üzenet kiválasztás ────────────────────────────────────────────────

get_message() {
  local pct=$1
  local streak=$2

  if [ "$pct" -lt 30 ]; then
    MESSAGES=(
      "Az első lépések a legnehezebbek. Minden egyes világ közelebb visz a teljes platformhoz."
      "Alapozás zajlik. A JoineryTech portal épül, és minden commit számít."
      "A Doorstar ügyfeleid hamarosan egy teljes platformot fognak látni. Csináld tovább!"
    )
  elif [ "$pct" -lt 50 ]; then
    MESSAGES=(
      "Szép lendület! A platform már elkezdett formát ölteni."
      "Az üzleti logika megvan, most az UI réteg töltődik fel tartalommal."
      "Minden implementált világ egy újabb érték a Doorstar (és a jövő ügyfelek) számára."
    )
  elif [ "$pct" -lt 75 ]; then
    MESSAGES=(
      "Félúton túl! A JoineryTech portal már valódi termékkönynek tűnik."
      "A pékség vertikál is vár — minél több világ kész, annál gyorsabb a következő iparág."
      "A prototípus a design spec. A kódod a valóság. Tartsd szinten a kettőt!"
    )
  else
    MESSAGES=(
      "Szinte kész! Minden hiányzó világ egy feature kérés amit még nem kellett megindokolni."
      "A befejező sprint a legizgalmasabb. Hajrá!"
      "Doorstar live, proto live, és a portal teli lesz. Ez a sprint dönti el a Q3-at."
    )
  fi

  IDX=$((streak % ${#MESSAGES[@]}))
  echo "${MESSAGES[$IDX]}"
}

MOTIVATIONAL_MSG=$(get_message "$PERCENT" "$STREAK")

# ── JSON output (email script hívja) ──────────────────────────────────────────

if $JSON_MODE; then
  NEXT_WORLD="${TODO_LIST[0]:-}"
  printf '{"percent":%d,"done":%d,"total":%d,"streak":%d,"fe_tests":%d,"backend_tests":%d,"milestone":"%s","message":"%s","next_world":"%s","date":"%s"}\n' \
    "$PERCENT" "$DONE_WORLDS" "$TOTAL_WORLDS" "$STREAK" "$TEST_COUNT" "$BACKEND_TESTS" \
    "$MILESTONE_MSG" "$MOTIVATIONAL_MSG" "$NEXT_WORLD" "$TODAY"
  exit 0
fi

# ── Terminal output ────────────────────────────────────────────────────────────

echo ""
echo -e "${BOLD}${MAGENTA}╔══════════════════════════════════════════════════════════╗${NC}"
echo -e "${BOLD}${MAGENTA}║              SpaceOS — Terminál Motivátor                ║${NC}"
echo -e "${BOLD}${MAGENTA}╚══════════════════════════════════════════════════════════╝${NC}"
echo ""

# Streak
if [ "$STREAK" -ge 7 ]; then
  STREAK_COLOR="${MAGENTA}"
  STREAK_ICON="🔥"
elif [ "$STREAK" -ge 3 ]; then
  STREAK_COLOR="${YELLOW}"
  STREAK_ICON="⚡"
else
  STREAK_COLOR="${CYAN}"
  STREAK_ICON="✨"
fi
echo -e "  ${STREAK_COLOR}${STREAK_ICON} Streak: ${BOLD}${STREAK} nap${NC}  ${DIM}(${TODAY})${NC}"
echo ""

# Progress bar
BAR_FILLED=$(( PERCENT / 4 ))
BAR_EMPTY=$(( 25 - BAR_FILLED ))
BAR="${GREEN}"
for ((i=0; i<BAR_FILLED; i++)); do BAR="${BAR}█"; done
BAR="${BAR}${DIM}"
for ((i=0; i<BAR_EMPTY; i++)); do BAR="${BAR}░"; done
BAR="${BAR}${NC}"

echo -e "  ${BOLD}Világ-haladás:${NC} ${BAR} ${BOLD}${PERCENT}%${NC}"
echo -e "  ${GREEN}✅ ${DONE_WORLDS} kész${NC}  ·  ${RED}⏳ ${#TODO_LIST[@]} hiányzik${NC}  /  ${TOTAL_WORLDS} világ"
echo ""

# Tesztek
echo -e "  ${CYAN}Tesztek:${NC}  🖥  BE ${BOLD}${BACKEND_TESTS}${NC}  ·  🌐 FE ${BOLD}${TEST_COUNT}${NC}  ·  Összes ${BOLD}$((BACKEND_TESTS + TEST_COUNT))${NC}"
echo ""

# Milestone
if [ -n "$MILESTONE_MSG" ]; then
  echo -e "  ${BOLD}${YELLOW}$MILESTONE_MSG${NC}"
  echo ""
fi

# Következő világ
if [ ${#TODO_LIST[@]} -gt 0 ]; then
  echo -e "  ${CYAN}▸ Következő:${NC} ${BOLD}${TODO_LIST[0]}${NC}"
  echo -e "  ${DIM}  Prototípus: https://joinerytech.hu/proto/ → ${TODO_LIST[0]} világ${NC}"
  echo ""
fi

# Motiváló üzenet
echo -e "  ${BOLD}\"${MOTIVATIONAL_MSG}\"${NC}"
echo ""
echo -e "${DIM}── $TODAY · sd --status · bash /opt/spaceos/scripts/fe-status.sh ──${NC}"
echo ""
