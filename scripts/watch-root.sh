#!/bin/bash
# =============================================================================
# watch-root.sh — Root terminál ébresztő (20 percenként)
#
# Cron: */20 * * * * SPACEOS_ROOT=/opt/spaceos bash /opt/spaceos/scripts/watch-root.sh
#
# Felelőssége:
#   1. Ellenőrzi hogy a Root session fut-e
#   2. Ha fut → küld egy rövid státusz összefoglalót Enter-rel
#   3. Ha beragadt folyamat van → figyelmeztet
#
# =============================================================================

source "$(dirname "$0")/common.sh"

ROOT_SESSION="spaceos-root"

# Csak ha fut a session
if ! tmux_s has-session -t "$ROOT_SESSION" 2>/dev/null; then
  exit 0
fi

# ── Státusz összegyűjtése ────────────────────────────────────────────────────

# Queue állapot
QUEUE_COUNT=$(ls "$SPACEOS_ROOT/docs/planning/queue"/*.md 2>/dev/null | grep -v gitkeep | wc -l)

# Ideas állapot
IDEAS_COUNT=$(ls "$SPACEOS_ROOT/docs/planning/ideas"/*.md 2>/dev/null | wc -l)

# Conductor inbox (van-e feldolgozatlan?)
COND_UNREAD=$(grep -rl "status: UNREAD" "$SPACEOS_ROOT/docs/mailbox/conductor/inbox/" 2>/dev/null | wc -l)

# Terminál outboxok (DONE/BLOCKED)
TERMINAL_OUTBOX=$(grep -rl "status: UNREAD" "$SPACEOS_ROOT/docs/mailbox/*/outbox/" 2>/dev/null | wc -l)

# Stuck session-ök (>30 perc inaktív)
STUCK_COUNT=0
for sess in $(tmux_s list-sessions -F "#{session_name}" 2>/dev/null); do
  [ "$sess" = "$ROOT_SESSION" ] && continue
  LAST_ACTIVITY=$(tmux_s display-message -t "$sess" -p "#{session_activity}" 2>/dev/null || echo "0")
  if [ -n "$LAST_ACTIVITY" ] && [ "$LAST_ACTIVITY" -gt 0 ]; then
    IDLE=$(( NOW - LAST_ACTIVITY ))
    [ "$IDLE" -gt 1800 ] && STUCK_COUNT=$((STUCK_COUNT + 1))
  fi
done

# Utolsó pipeline log
LAST_PIPELINE=$(tail -1 "$LOG_DIR/pipeline.log" 2>/dev/null | cut -c1-80)

# ── Üzenet összeállítása ─────────────────────────────────────────────────────

MSG="
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
🔔 ROOT ÉBRESZTŐ — $(date +%H:%M)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📊 Folyamatok:
   Ideas: $IDEAS_COUNT | Queue: $QUEUE_COUNT | Conductor inbox: $COND_UNREAD
   Terminál outbox (DONE/BLOCKED): $TERMINAL_OUTBOX
   Stuck session-ök: $STUCK_COUNT

📝 Utolsó pipeline:
   $LAST_PIPELINE
"

# Figyelmeztetések
ALERTS=""
[ "$QUEUE_COUNT" -ge 3 ] && ALERTS="${ALERTS}⚠️  Queue tele ($QUEUE_COUNT) — Conductor feldolgozza?\n"
[ "$TERMINAL_OUTBOX" -gt 0 ] && ALERTS="${ALERTS}⚠️  Van feldolgozatlan outbox ($TERMINAL_OUTBOX)\n"
[ "$STUCK_COUNT" -gt 0 ] && ALERTS="${ALERTS}⚠️  Stuck session-ök: $STUCK_COUNT\n"
[ "$IDEAS_COUNT" -eq 0 ] && [ "$QUEUE_COUNT" -eq 0 ] && ALERTS="${ALERTS}ℹ️  Nincs ötlet és queue üres — scan fut?\n"

if [ -n "$ALERTS" ]; then
  MSG="${MSG}
🚨 Ellenőrizd:
$(echo -e "$ALERTS")"
fi

MSG="${MSG}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Parancsok: queue | outbox | stuck | scan
"

# ── Státusz mentése fájlba (nem küldjük a session-nek) ───────────────────────

# A státusz a log-ba megy, a session nem zavarjuk feleslegesen
# Ha beavatkozás kell, a watch-stuck.sh vagy manuális nudge intézi

echo "$TIMESTAMP Root ébresztő: ideas=$IDEAS_COUNT queue=$QUEUE_COUNT outbox=$TERMINAL_OUTBOX stuck=$STUCK_COUNT" >> "$LOG_DIR/nightwatch.log"

# Telegram csak 7:00-22:00 között (éjjel nem zavar)
HOUR=$(date +%H)
if [ -n "$ALERTS" ] && [ "$HOUR" -ge 7 ] && [ "$HOUR" -lt 22 ]; then
  tg "🔔 *Root ébresztő*
Ideas: $IDEAS_COUNT | Queue: $QUEUE_COUNT
$(echo -e "$ALERTS")"
fi
