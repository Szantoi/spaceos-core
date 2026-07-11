#!/bin/bash
# watch-monitor.sh — Trigger monitor terminal for health check
# Called by nightwatch.sh every 10 minutes
#
# Mode: Creates UNREAD inbox message → inboxWatcher starts session
# Session mode: cold (session terminates after check)

set -e

TERMINALS_DIR="/opt/spaceos/terminals"
MONITOR_INBOX="$TERMINALS_DIR/monitor/inbox"
DATE=$(date +%Y-%m-%d)
TIMESTAMP=$(date +%H:%M:%S)

# Skip if monitor session already running
if tmux has-session -t spaceos-monitor 2>/dev/null; then
  echo "[Monitor] Session already running, skipping"
  exit 0
fi

# Skip if last check was <5 minutes ago (prevent spam)
LAST_CHECK=$(ls -t "$MONITOR_INBOX"/*.md 2>/dev/null | head -1)
if [ -n "$LAST_CHECK" ]; then
  LAST_MTIME=$(stat -c %Y "$LAST_CHECK" 2>/dev/null || echo 0)
  NOW=$(date +%s)
  DIFF=$((NOW - LAST_MTIME))
  if [ "$DIFF" -lt 300 ]; then
    echo "[Monitor] Last check was ${DIFF}s ago, skipping (min 300s)"
    exit 0
  fi
fi

# Get next message number
NUM=$(ls "$MONITOR_INBOX" 2>/dev/null | grep "^$DATE" | wc -l)
NUM=$((NUM + 1))
FILENAME="${DATE}_$(printf '%03d' $NUM)_scheduled-health-check.md"

# Create inbox trigger message
cat > "$MONITOR_INBOX/$FILENAME" << EOF
---
id: MSG-MONITOR-$(printf '%03d' $NUM)
from: cron
to: monitor
type: task
priority: low
status: UNREAD
model: haiku
created: $DATE
---

# Scheduled Health Check — $TIMESTAMP

Futtasd le a teljes rendszer ellenőrzést:

1. **Terminálok:** tmux sessions, melyik fut/idle
2. **Inbox:** UNREAD üzenetek száma terminálonként
3. **Outbox:** BLOCKED üzenetek (kritikus!)
4. **Services:** Knowledge (3456), Datahaven (3457)
5. **Logs:** Pipeline/nightwatch hibák

**Output:** Írj outbox összefoglalót. Ha probléma van (BLOCKED, service DOWN, >5 UNREAD), küldj Root inbox-ot.

**Session mode:** Cold — fejezd be a session-t DONE után.
EOF

echo "[Monitor] Health check triggered: $FILENAME"
