#!/bin/bash
##
# Federation Watcher - Cabinet → Root automatic notification
#
# Monitors cabinet-bridge inbox for new federation messages.
# Wakes Root session automatically when Cabinet responds.
#
# Cron: */5 * * * * (every 5 minutes)
##

set -euo pipefail

CABINET_INBOX="/opt/spaceos/terminals/cabinet-bridge/inbox"
LOG_FILE="/opt/spaceos/logs/cron/federation-watcher.log"
MCP_API="http://localhost:3456/api/session/wake"

# Ensure log directory exists
mkdir -p "$(dirname "$LOG_FILE")"

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $*" | tee -a "$LOG_FILE"
}

# Check for UNREAD messages in cabinet-bridge inbox
check_federation_messages() {
    if [ ! -d "$CABINET_INBOX" ]; then
        log "WARNING: cabinet-bridge inbox not found: $CABINET_INBOX"
        return 1
    fi

    # Find UNREAD messages
    local unread_count=0
    local unread_files=()

    while IFS= read -r -d '' file; do
        if grep -q "^status: UNREAD" "$file" 2>/dev/null; then
            unread_count=$((unread_count + 1))
            unread_files+=("$(basename "$file")")
        fi
    done < <(find "$CABINET_INBOX" -name "*.md" -type f -print0)

    if [ "$unread_count" -eq 0 ]; then
        log "No UNREAD federation messages (idle)"
        return 0
    fi

    log "ALERT: $unread_count UNREAD federation message(s) detected!"
    for msg in "${unread_files[@]}"; do
        log "  - $msg"
    done

    # Wake Root session
    wake_root_session "$unread_count"
}

# Wake Root session via MCP API
wake_root_session() {
    local count=$1

    log "Waking Root session (federation messages: $count)..."

    local response
    response=$(curl -s -X POST "$MCP_API" \
        -H "Content-Type: application/json" \
        -d "{\"terminal\":\"root\",\"fromTerminal\":\"monitor\"}" 2>&1)

    local exit_code=$?

    if [ $exit_code -eq 0 ]; then
        log "Root session wake SUCCESS"
        log "Response: $response"
    else
        log "ERROR: Failed to wake Root session (exit code: $exit_code)"
        log "Response: $response"
    fi
}

# Main execution
log "=== Federation Watcher START ==="
check_federation_messages
log "=== Federation Watcher END ==="
