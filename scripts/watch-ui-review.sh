#!/bin/bash
# watch-ui-review.sh - UI Review Loop automation
# ⚠️  DEPRECATED 2026-07-11 - Replaced by MCP-based solution
#
# This script has been replaced by watchFrontendUiDone() in:
#   spaceos-nexus/knowledge-service/src/pipeline/watchDone.ts
#
# The new solution is:
# - Type-safe (TypeScript enums, no grep fragility)
# - DB-based state tracking (not file-based)
# - Integrated into Nightwatch pipeline (runs every 2 minutes)
# - Audited via messageRegistry
#
# To remove this script from cron:
#   crontab -l | grep -v watch-ui-review | crontab -
#
# See: MSG-NEXUS-024 (MCP-based UI Review Loop implementation)
# ---

set -euo pipefail

# Emit deprecation warning
echo "[DEPRECATED] watch-ui-review.sh is no longer used. Use Nightwatch pipeline instead." >&2
exit 0

# ─── OLD IMPLEMENTATION (kept for reference) ───

SPACEOS_ROOT="/opt/spaceos"
FRONTEND_OUTBOX="$SPACEOS_ROOT/terminals/frontend/outbox"
DESIGNER_INBOX="$SPACEOS_ROOT/terminals/designer/inbox"
STATE_FILE="$SPACEOS_ROOT/scripts/.ui-review-state"
PAUSE_FILE="$SPACEOS_ROOT/scripts/.ui-review-paused"
LOG_FILE="$SPACEOS_ROOT/logs/dispatcher/ui-review.log"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" >> "$LOG_FILE"
}

# Check pause flag - Conductor can control this
if [ -f "$PAUSE_FILE" ]; then
    log "PAUSED (touch $PAUSE_FILE exists) - skipping cycle"
    exit 0
fi

# Create state file if not exists
touch "$STATE_FILE"

# Find Frontend DONE messages that haven't been reviewed yet
for outbox_file in "$FRONTEND_OUTBOX"/*.md; do
    [ -f "$outbox_file" ] || continue

    # Check if it's a DONE message with UI-related content
    if grep -q "type: done" "$outbox_file" 2>/dev/null; then
        filename=$(basename "$outbox_file")

        # Skip if already processed
        if grep -q "$filename" "$STATE_FILE" 2>/dev/null; then
            continue
        fi

        # Check if UI-related (JoineryTech, frontend, component, page, etc.)
        if grep -qiE "(joinerytech|component|page|ui|dashboard|crm|ehs|hr|kontrolling|maintenance|qa|dms)" "$outbox_file" 2>/dev/null; then
            log "UI DONE detected: $filename"

            # Get message ID
            msg_id=$(grep -oP 'id: \K[A-Z0-9-]+' "$outbox_file" | head -1)

            # Create Designer review task
            next_num=$(ls "$DESIGNER_INBOX"/*.md 2>/dev/null | wc -l)
            next_num=$((next_num + 1))
            task_file="$DESIGNER_INBOX/$(date +%Y-%m-%d)_$(printf '%03d' $next_num)_ui-review-${msg_id}.md"

            cat > "$task_file" << EOF
---
id: MSG-DESIGNER-REVIEW-${next_num}
from: system
to: designer
type: task
priority: high
status: UNREAD
model: sonnet
ref: ${msg_id}
created: $(date +%Y-%m-%d)
---

# UI Review Required

Frontend completed: **${msg_id}**

## Review Checklist

1. **Capture Screenshots**
   \`\`\`bash
   npx playwright screenshot --wait-for-timeout=5000 "http://localhost:5173/dashboard/crm/leads" /tmp/review-crm.png
   npx playwright screenshot --wait-for-timeout=5000 "http://localhost:5173/dashboard/ehs" /tmp/review-ehs.png
   \`\`\`

2. **Compare with Prototypes**
   - Check \`docs/tasks/new/joinerytech/\` for design specs
   - Verify: Layout, colors, spacing, typography

3. **Known Issues to Verify**
   - Mock API integration (Loading states)
   - SSE connection ("Kapcsolat megszakadt")
   - Hungarian localization

4. **Action Required**
   - If issues: Create feedback task for Frontend
   - If OK: DONE outbox with APPROVED

Use skill: \`ui-review-loop\`
EOF

            # Mark as processed
            echo "$filename" >> "$STATE_FILE"
            log "Created Designer review task: $task_file"

            # Optional: Telegram notification
            if [ -f "$SPACEOS_ROOT/scripts/tg-reply.sh" ]; then
                "$SPACEOS_ROOT/scripts/tg-reply.sh" "designer" "UI Review task created for $msg_id" 2>/dev/null || true
            fi
        fi
    fi
done

log "UI Review watch cycle complete"
