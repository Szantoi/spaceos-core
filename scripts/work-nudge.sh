#!/bin/bash
# =============================================================================
# work-nudge.sh — Root terminál munkára ösztönzés + Nexus projekt figyelmeztetés
#
# Cron: */15 * * * * bash /opt/spaceos/scripts/work-nudge.sh
#
# Funkciók:
#   - Ha Root 15+ perce inaktív, emlékeztetés
#   - Ha Nexus inbox UNREAD és session nem fut, figyelmeztetés
#   - Blokkoló outbox-ok számlálása
#   - Planning queue állapot
# =============================================================================

source "$(dirname "$0")/common.sh"

LOG_FILE="$LOG_DIR/work-nudge.log"
STATE_FILE="$SPACEOS_ROOT/scripts/.work-nudge-state"

log() { echo "$(date '+%H:%M:%S') $1" >> "$LOG_FILE"; }

# ── Időablak ellenőrzés (csak 7:00-22:00) ───────────────────────────────────
HOUR=$(date +%H)
if [ "$HOUR" -lt 7 ] || [ "$HOUR" -ge 22 ]; then
  exit 0
fi

# ── Root session inaktivitás ────────────────────────────────────────────────

CONTENT=$(tmux_s capture-pane -t spaceos-root -p 2>/dev/null | tail -20)
NOW=$(date +%s)

LAST_ACTIVITY=$(stat -c %Y "$STATE_FILE" 2>/dev/null || echo "0")
ELAPSED=$(( NOW - LAST_ACTIVITY ))

# Ha van aktivitás a pane-ben (nem ugyanaz mint legutóbb)
CONTENT_HASH=$(echo "$CONTENT" | md5sum | cut -d' ' -f1)
PREV_HASH=$(cat "$STATE_FILE" 2>/dev/null || echo "")

NUDGE_ROOT=false
if [ "$CONTENT_HASH" = "$PREV_HASH" ] && [ "$ELAPSED" -gt 900 ]; then
  # 15+ perc inaktivitás
  NUDGE_ROOT=true
fi
echo "$CONTENT_HASH" > "$STATE_FILE"

# ── Blokkoló outbox üzenetek ────────────────────────────────────────────────

BLOCKED_COUNT=$(grep -rl "status: UNREAD" "$SPACEOS_ROOT/docs/mailbox/*/outbox/" 2>/dev/null \
  | xargs grep -l "type: blocked\|type: question" 2>/dev/null | wc -l)

# ── Planning queue státusz ──────────────────────────────────────────────────

QUEUE_COUNT=$(ls "$SPACEOS_ROOT/docs/planning/queue/"*.md 2>/dev/null | wc -l)
IDEAS_COUNT=$(ls "$SPACEOS_ROOT/docs/planning/ideas/"*.md 2>/dev/null | wc -l)

# ── Nexus projekt állapot ───────────────────────────────────────────────────

NEXUS_INBOX=$(grep -rl "status: UNREAD" "$SPACEOS_ROOT/docs/mailbox/nexus/inbox/" 2>/dev/null | wc -l)
NEXUS_SESSION_RUNNING=false
if tmux_s list-panes -t spaceos-nexus -F "#{pane_current_command}" 2>/dev/null | grep -q "claude"; then
  NEXUS_SESSION_RUNNING=true
fi

ROADMAP_TOTAL=$(grep -c "\[ \]\|\[x\]" "$SPACEOS_ROOT/docs/agent-infrastructure/ROADMAP.md" 2>/dev/null || echo 0)
ROADMAP_DONE=$(grep -c "\[x\]" "$SPACEOS_ROOT/docs/agent-infrastructure/ROADMAP.md" 2>/dev/null || echo 0)
ROADMAP_PENDING=$((ROADMAP_TOTAL - ROADMAP_DONE))

# ── Üzenet összeállítás ─────────────────────────────────────────────────────

NUDGE_MSG=""
ALERTS=()

if [ "$BLOCKED_COUNT" -gt 0 ]; then
  ALERTS+=("$BLOCKED_COUNT BLOCKED outbox vár döntésre")
fi

if [ "$QUEUE_COUNT" -gt 0 ]; then
  ALERTS+=("$QUEUE_COUNT terv vár a queue-ban — Conductor inbox-ra")
fi

if [ "$NEXUS_INBOX" -gt 0 ] && [ "$NEXUS_SESSION_RUNNING" = "false" ]; then
  ALERTS+=("Nexus: $NEXUS_INBOX UNREAD feladat, session NEM fut")
fi

if [ "$ROADMAP_PENDING" -gt 0 ] && [ "$ROADMAP_PENDING" -le 5 ]; then
  ALERTS+=("Nexus roadmap: $ROADMAP_PENDING checkpoint maradt Phase 1-ből")
fi

# Ha vannak alertek, összeállítás
if [ ${#ALERTS[@]} -gt 0 ]; then
  NUDGE_MSG="📋 Tennivalók:\n"
  for alert in "${ALERTS[@]}"; do
    NUDGE_MSG="${NUDGE_MSG}• ${alert}\n"
  done
  NUDGE_MSG="${NUDGE_MSG}\nFolyamatosan haladj a feladatokon."
fi

# ── Nudge küldés Root sessionbe (ha inaktív + van teendő) ───────────────────

if [ "$NUDGE_ROOT" = "true" ] && [ -n "$NUDGE_MSG" ]; then
  log "Root inaktív 15+ perce, nudge küldés"

  tmux_s send-keys -t spaceos-root "$(echo -e "$NUDGE_MSG")" 2>/dev/null
  sleep 0.5
  tmux_s send-keys -t spaceos-root Enter 2>/dev/null
  sleep 1
  tmux_s send-keys -t spaceos-root Enter 2>/dev/null
fi

# ── Telegram értesítés (ha sok a teendő) ────────────────────────────────────

TOTAL_ALERTS=${#ALERTS[@]}
if [ "$TOTAL_ALERTS" -ge 3 ]; then
  tg "🔔 *SpaceOS Nudge*

Több mint 3 feladat vár figyelemre:
$(printf '• %s\n' "${ALERTS[@]}")

Root session: $([ "$NUDGE_ROOT" = "true" ] && echo "inaktív" || echo "aktív")"

  log "Telegram nudge küldve ($TOTAL_ALERTS alert)"
fi

# ── Nexus session auto-indítás (ha inbox van és nem fut) ────────────────────

if [ "$NEXUS_INBOX" -gt 0 ] && [ "$NEXUS_SESSION_RUNNING" = "false" ]; then
  # Ellenőrzés: van-e egyáltalán spaceos-nexus session
  if tmux_s has-session -t spaceos-nexus 2>/dev/null; then
    log "Nexus inbox van ($NEXUS_INBOX), session indítás..."
    tmux_s send-keys -t spaceos-nexus "claude --model sonnet" 2>/dev/null
    sleep 0.5
    tmux_s send-keys -t spaceos-nexus Enter 2>/dev/null
  else
    log "Nexus session nem létezik, létrehozás szükséges"
    tg "⚠️ *Nexus session hiányzik*

$NEXUS_INBOX UNREAD feladat van a Nexus inbox-ban, de nincs tmux session.

Hozd létre manuálisan:
\`\`\`
tmux -S /tmp/spaceos-tmux.sock new-session -d -s spaceos-nexus -c /opt/spaceos/spaceos-nexus
\`\`\`"
  fi
fi

log "Check complete: $TOTAL_ALERTS alerts, Root=$([ "$NUDGE_ROOT" = "true" ] && echo "inactive" || echo "active"), Nexus=$NEXUS_INBOX inbox"
