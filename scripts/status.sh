#!/bin/bash
# =============================================================================
# status.sh — SpaceOS + Datahaven felügyeleti dashboard
#
# Használat: bash /opt/spaceos/scripts/status.sh
# =============================================================================

source "$(dirname "$0")/common.sh"

RESET='\033[0m'
BOLD='\033[1m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
CYAN='\033[0;36m'
GRAY='\033[0;37m'

echo ""
echo -e "${BOLD}╔══════════════════════════════════════════════════════╗${RESET}"
echo -e "${BOLD}║         SpaceOS + Datahaven — Status                ║${RESET}"
echo -e "${BOLD}║         $(date '+%Y-%m-%d %H:%M:%S')                    ║${RESET}"
echo -e "${BOLD}╚══════════════════════════════════════════════════════╝${RESET}"
echo ""

# ── 1. Futó sessionök ─────────────────────────────────────────────────────────
echo -e "${BOLD}▶ FUTÓ SESSIONÖK${RESET}"
tmux_s list-sessions 2>/dev/null | while IFS= read -r line; do
  session=$(echo "$line" | cut -d: -f1)
  if tmux_s list-panes -t "$session" -F "#{pane_current_command}" 2>/dev/null | grep -q "claude"; then
    echo -e "  ${GREEN}● $session${RESET} (claude fut)"
  else
    echo -e "  ${GRAY}○ $session${RESET} (idle)"
  fi
done
echo ""

# ── 2. BLOCKED üzenetek (emberi döntés kell) ─────────────────────────────────
BLOCKED=$(grep -rl "type: blocked" "$SPACEOS_ROOT/docs/mailbox/*/outbox/" 2>/dev/null \
  | xargs grep -l "status: UNREAD" 2>/dev/null)

if [ -n "$BLOCKED" ]; then
  echo -e "${BOLD}${RED}⚠ BLOCKED — EMBERI DÖNTÉS KELL${RESET}"
  echo "$BLOCKED" | while read f; do
    FROM=$(grep "^from:" "$f" | head -1 | cut -d: -f2 | tr -d ' ')
    SUBJ=$(grep "^# " "$f" | head -1 | sed 's/# //')
    echo -e "  ${RED}→ $FROM: $SUBJ${RESET}"
    echo -e "    ${GRAY}$f${RESET}"
  done
  echo ""
fi

# ── 3. Aktív inbox-ok (mi van folyamatban) ───────────────────────────────────
echo -e "${BOLD}▶ FOLYAMATBAN LÉVŐ FELADATOK${RESET}"
for mailbox in "$SPACEOS_ROOT/docs/mailbox"/*/inbox/; do
  terminal=$(basename $(dirname "$mailbox"))
  unread=$(grep -rl "status: UNREAD" "$mailbox" 2>/dev/null | wc -l)
  if [ "$unread" -gt 0 ]; then
    latest=$(grep -rl "status: UNREAD" "$mailbox" 2>/dev/null | sort | head -1)
    subj=$(grep "^# " "$latest" | head -1 | sed 's/# //' | cut -c1-50)
    echo -e "  ${CYAN}[$terminal]${RESET} $subj"
  fi
done
echo ""

# ── 4. Jóváhagyásra váró DONE-ok ─────────────────────────────────────────────
PENDING_DONE=$(grep -rl "type: done" "$SPACEOS_ROOT/docs/mailbox/*/outbox/" 2>/dev/null \
  | xargs grep -l "status: UNREAD" 2>/dev/null | wc -l)

if [ "$PENDING_DONE" -gt 0 ]; then
  echo -e "${BOLD}▶ REVIEWER FOLYAMATBAN${RESET}"
  grep -rl "type: done" "$SPACEOS_ROOT/docs/mailbox/*/outbox/" 2>/dev/null \
    | xargs grep -l "status: UNREAD" 2>/dev/null | while read f; do
    FROM=$(grep "^from:" "$f" | head -1 | cut -d: -f2 | tr -d ' ')
    echo -e "  ${YELLOW}⏳ $FROM → review folyamatban${RESET}"
  done
  echo ""
fi

# ── 5. Planning pipeline ──────────────────────────────────────────────────────
echo -e "${BOLD}▶ PLANNING PIPELINE${RESET}"
IDEAS=$(ls "$SPACEOS_ROOT/docs/planning/ideas/"*.md 2>/dev/null | wc -l)
LAST_SEG=$(grep "^last_segment=" "$SPACEOS_ROOT/scripts/.plan-scan-state" 2>/dev/null | cut -d= -f2)
SEGMENTS=("api-status" "fe-memory" "kernel-memory" "joinery-memory" "knowledge-adr" "knowledge-api" "knowledge-patterns" "infra-memory" "datahaven-resonance")
CURRENT_SEG="${SEGMENTS[$LAST_SEG]:-ismeretlen}"
PENDING_SELECT=$([ -f "$SPACEOS_ROOT/docs/planning/selected/pending.md" ] && echo "igen" || echo "nem")

echo -e "  Összegyűlt ötletek: ${YELLOW}$IDEAS / 5${RESET} (5-nél debate indul)"
echo -e "  Utolsó szegmens:    $CURRENT_SEG"
echo -e "  Pending debate:     $PENDING_SELECT"
echo ""

# ── 6. Datahaven / Nexus állapot ─────────────────────────────────────────────
echo -e "${BOLD}▶ DATAHAVEN / NEXUS${RESET}"
NEXUS_INBOX=$(grep -rl "status: UNREAD" "$SPACEOS_ROOT/docs/mailbox/nexus/inbox/" 2>/dev/null | wc -l)
ROADMAP_DONE=$(grep -c "\[x\]" "$SPACEOS_ROOT/docs/agent-infrastructure/ROADMAP.md" 2>/dev/null || echo 0)
ROADMAP_TOTAL=$(grep -c "\[ \]\|\[x\]" "$SPACEOS_ROOT/docs/agent-infrastructure/ROADMAP.md" 2>/dev/null || echo 0)
echo -e "  Nexus inbox:        ${NEXUS_INBOX} UNREAD feladat"
echo -e "  Roadmap:            ${ROADMAP_DONE}/${ROADMAP_TOTAL} kész"
echo ""

# ── 7. Legutóbbi pipeline aktivitás ──────────────────────────────────────────
echo -e "${BOLD}▶ LEGUTÓBBI AKTIVITÁS${RESET}"
tail -5 "$LOG_DIR/pipeline.log" 2>/dev/null | while IFS= read -r line; do
  echo -e "  ${GRAY}$line${RESET}"
done
echo ""

echo -e "${GRAY}Részletes log: tail -f $LOG_DIR/pipeline.log${RESET}"
echo -e "${GRAY}Telegram:      értesítés minden pipeline futás után${RESET}"
echo ""
