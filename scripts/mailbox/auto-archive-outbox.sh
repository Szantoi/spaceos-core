#!/bin/bash
# auto-archive-outbox.sh - Archives old outbox messages automatically
# Run from cron: 0 3 * * * /opt/spaceos/scripts/mailbox/auto-archive-outbox.sh
#
# Archives outbox messages older than 3 days to prevent accumulation

set -euo pipefail

SPACEOS_ROOT="${SPACEOS_ROOT:-/opt/spaceos}"
LOG_FILE="$SPACEOS_ROOT/logs/dispatcher/auto-archive.log"
ARCHIVE_AFTER_DAYS=3

log() {
    echo "[$(date '+%Y-%m-%d %H:%M:%S')] $1" >> "$LOG_FILE"
}

log "Auto-archive started"

total_archived=0

for terminal_dir in "$SPACEOS_ROOT"/terminals/*/; do
    terminal=$(basename "$terminal_dir")
    outbox_dir="$terminal_dir/outbox"
    archive_dir="$terminal_dir/archive/$(date +%Y-%m)-auto"

    [ -d "$outbox_dir" ] || continue

    # Create archive directory
    mkdir -p "$archive_dir"

    # Find files older than N days
    count=0
    while IFS= read -r -d '' file; do
        # Mark as READ and move to archive
        sed -i 's/status: UNREAD/status: READ/' "$file" 2>/dev/null || true
        mv "$file" "$archive_dir/" 2>/dev/null || true
        ((count++)) || true
    done < <(find "$outbox_dir" -name "*.md" -type f -mtime +$ARCHIVE_AFTER_DAYS -print0 2>/dev/null)

    if [ "$count" -gt 0 ]; then
        log "$terminal: archived $count old messages"
        total_archived=$((total_archived + count))
    fi
done

log "Auto-archive complete: $total_archived messages archived"
