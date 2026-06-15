#!/bin/bash
# =============================================================================
# fe-status.sh — SpaceOS FE terminál állapot + motivátor
# Futtatás: bash /opt/spaceos/scripts/fe-status.sh
# =============================================================================

BOLD='\033[1m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
BLUE='\033[0;34m'
DIM='\033[2m'
NC='\033[0m'

echo ""
echo -e "${BOLD}╔══════════════════════════════════════════════════════╗${NC}"
echo -e "${BOLD}║       SpaceOS FE Terminál — Állapot & Motivátor      ║${NC}"
echo -e "${BOLD}╚══════════════════════════════════════════════════════╝${NC}"
echo ""

# --- 1. tmux session-ök ---
echo -e "${CYAN}▸ Aktív tmux session-ök:${NC}"
SESSIONS=$(tmux ls 2>/dev/null | grep -c .)
if [ "$SESSIONS" -eq 0 ]; then
  echo -e "  ${RED}Nincs aktív tmux session!${NC}"
else
  tmux ls 2>/dev/null | while read line; do
    echo -e "  ${GREEN}✓${NC} $line"
  done
fi
echo ""

# --- 2. Claude process-ek ---
echo -e "${CYAN}▸ Claude process-ek:${NC}"
CLAUDE_COUNT=$(pgrep -f "claude" 2>/dev/null | wc -l)
if [ "$CLAUDE_COUNT" -gt 0 ]; then
  echo -e "  ${GREEN}✓ $CLAUDE_COUNT Claude process fut${NC}"
else
  echo -e "  ${RED}✗ Nincs futó Claude terminál${NC}"
  echo -e "  ${YELLOW}  → tmux new-s -s fe-a && claude${NC}"
fi
echo ""

# --- 3. Aktív task-ok ---
echo -e "${CYAN}▸ Aktív task-ok (docs/tasks/active/):${NC}"
ACTIVE_DIR="/opt/spaceos/docs/tasks/active"
if [ -d "$ACTIVE_DIR" ] && [ "$(ls -A $ACTIVE_DIR 2>/dev/null)" ]; then
  ls "$ACTIVE_DIR" | while read f; do
    echo -e "  ${YELLOW}⚡${NC} $f"
  done
else
  echo -e "  ${GREEN}✓ Nincs blokkoló aktív task${NC}"
fi
echo ""

# --- 4. Olvasatlan inbox-ok ---
echo -e "${CYAN}▸ UNREAD inbox üzenetek:${NC}"
UNREAD=$(grep -rl "status: UNREAD" /opt/spaceos/docs/mailbox/*/inbox/ 2>/dev/null)
if [ -z "$UNREAD" ]; then
  echo -e "  ${GREEN}✓ Minden olvasva${NC}"
else
  echo "$UNREAD" | while read f; do
    TERM=$(echo "$f" | sed 's|.*/mailbox/\([^/]*\)/.*|\1|')
    BASE=$(basename "$f")
    echo -e "  ${RED}✗ [$TERM] $BASE${NC}"
  done
fi
echo ""

# --- 5. Prototípus vs production: világ-megvalósítottság ---
echo -e "${CYAN}▸ JoineryTech Portal — Világ-implementáltság:${NC}"
echo ""

WORLDS_ALL=("tasks" "production" "mfgprep" "supervisor" "sales" "crm" "design" "interior" 
            "procurement" "finance" "masterdata" "warehouse" "shopfloor" "shop" "trade" 
            "projects" "logistics" "kontrolling" "service" "hr" "maintenance" "quality" 
            "ehs" "docs" "attendance" "settings" "ai")

# Melyik world-ök vannak a React app-ban (pages + komponensek alapján)
IMPLEMENTED=("production" "design" "procurement" "sales" "settings" "shopfloor" "warehouse")

# Részben kész (van page de hiányos)
PARTIAL=("finance" "projects")

DONE=0
PARTIAL_N=0
TODO=0

for world in "${WORLDS_ALL[@]}"; do
  is_done=false
  is_partial=false
  
  for impl in "${IMPLEMENTED[@]}"; do
    [ "$world" = "$impl" ] && is_done=true && break
  done
  
  for part in "${PARTIAL[@]}"; do
    [ "$world" = "$part" ] && is_partial=true && break
  done
  
  if $is_done; then
    echo -e "  ${GREEN}✅ $world${NC}"
    DONE=$((DONE+1))
  elif $is_partial; then
    echo -e "  ${YELLOW}🔨 $world ${DIM}(részleges)${NC}"
    PARTIAL_N=$((PARTIAL_N+1))
  else
    echo -e "  ${RED}⏳ $world${NC}"
    TODO=$((TODO+1))
  fi
done

TOTAL=${#WORLDS_ALL[@]}
echo ""
echo -e "  ${BOLD}Összesen: ${GREEN}$DONE kész${NC} · ${YELLOW}$PARTIAL_N részleges${NC} · ${RED}$TODO hiányzik${NC} / $TOTAL világ${NC}"
PERCENT=$(( (DONE * 100) / TOTAL ))
echo -e "  ${BOLD}Haladás: $PERCENT%${NC}"
echo ""

# --- 6. Következő lépések ---
echo -e "${CYAN}▸ Javasolt következő lépések:${NC}"
echo ""
echo -e "  ${BOLD}1. Pénzügy (finance) világ${NC}"
echo -e "     ${DIM}→ Számla lista, ki/bejövő, kifizetések${NC}"
echo -e "     ${DIM}Prototípus: page-finance.jsx + page-finance-2.jsx${NC}"
echo ""
echo -e "  ${BOLD}2. CRM világ${NC}"
echo -e "     ${DIM}→ Lead pipeline, ajánlat, ügyféladatlap${NC}"
echo -e "     ${DIM}Prototípus: page-crm.jsx + page-crm-2.jsx${NC}"
echo ""
echo -e "  ${BOLD}3. HR világ${NC}"
echo -e "     ${DIM}→ Dolgozók, kapacitás, távollét${NC}"
echo -e "     ${DIM}Prototípus: page-hr.jsx + page-hr-2.jsx${NC}"
echo ""
echo -e "  ${BOLD}4. Gyártás-előkészítés (mfgprep) világ${NC}"
echo -e "     ${DIM}→ Munkasor, nesting, előkészítés${NC}"
echo -e "     ${DIM}Prototípus: page-mfg-prep.jsx + page-mfg-prep-*.jsx${NC}"
echo ""

# --- 7. Prototípus link ---
echo -e "${CYAN}▸ Prototípus referencia:${NC}"
echo -e "  🌐 ${BOLD}https://joinerytech.hu/proto/${NC}"
echo -e "  ${DIM}(JoineryTech Portal.html — teljes UI spec)${NC}"
echo ""

echo -e "${DIM}── Legutóbbi deploy: $(stat -c %y /var/www/joinerytech/index.html 2>/dev/null | cut -d' ' -f1-2 | cut -d'.' -f1) ──${NC}"
echo -e "${DIM}── Script: /opt/spaceos/scripts/fe-status.sh ──${NC}"
echo ""
