#!/bin/bash
# Mailbox Health Monitor
# Purpose: Daily automated health check for inbox/outbox queue depths, UNREAD tracking, archival candidates
# Impact: HIGH (prevents queue overflow, identifies stuck terminals)
# Difficulty: LOW
# Author: Explorer Task Research (2026-07-07)
# Usage: Run via cron daily at 08:00 or on-demand

set -euo pipefail

# Configuration
TERMINALS=("root" "conductor" "architect" "librarian" "explorer" "backend" "frontend" "designer" "monitor")
TERMINAL_BASE="/opt/spaceos/terminals"
UNREAD_THRESHOLD=5
INBOX_DEPTH_THRESHOLD=50
ARCHIVE_AGE_DAYS=7
OUTPUT_JSON="/opt/spaceos/logs/mailbox-health.json"

# Colors
RED='\033[0;31m'
YELLOW='\033[1;33m'
GREEN='\033[0;32m'
NC='\033[0m' # No Color

# Initialize JSON output
echo "{" > "$OUTPUT_JSON"
echo '  "timestamp": "'$(date -u +%Y-%m-%dT%H:%M:%SZ)'",' >> "$OUTPUT_JSON"
echo '  "terminals": [' >> "$OUTPUT_JSON"

# Health check function
check_terminal_health() {
  local terminal=$1
  local inbox_path="$TERMINAL_BASE/$terminal/inbox"
  local outbox_path="$TERMINAL_BASE/$terminal/outbox"

  # Skip if terminal doesn't exist
  if [ ! -d "$inbox_path" ]; then
    echo "    {\"terminal\": \"$terminal\", \"status\": \"not_found\"}," >> "$OUTPUT_JSON"
    return
  fi

  # Count metrics
  local unread_count=$(grep -l "status: UNREAD" "$inbox_path"/*.md 2>/dev/null | wc -l)
  local inbox_depth=$(ls -1 "$inbox_path"/*.md 2>/dev/null | wc -l)
  local outbox_done=$(ls -1 "$outbox_path"/*DONE*.md 2>/dev/null | wc -l)
  local outbox_blocked=$(ls -1 "$outbox_path"/*BLOCKED*.md 2>/dev/null | wc -l)

  # Archive candidates (>7 days old, READ status)
  local archive_candidates=$(find "$inbox_path" -name "*.md" -mtime +$ARCHIVE_AGE_DAYS 2>/dev/null | xargs grep -l "status: READ" 2>/dev/null | wc -l)

  # JSON output
  echo "    {" >> "$OUTPUT_JSON"
  echo "      \"terminal\": \"$terminal\"," >> "$OUTPUT_JSON"
  echo "      \"unread_count\": $unread_count," >> "$OUTPUT_JSON"
  echo "      \"inbox_depth\": $inbox_depth," >> "$OUTPUT_JSON"
  echo "      \"outbox_done\": $outbox_done," >> "$OUTPUT_JSON"
  echo "      \"outbox_blocked\": $outbox_blocked," >> "$OUTPUT_JSON"
  echo "      \"archive_candidates\": $archive_candidates," >> "$OUTPUT_JSON"

  # Health status
  local status="healthy"
  local alerts=()

  if [ $unread_count -gt $UNREAD_THRESHOLD ]; then
    status="warning"
    alerts+=("\"unread_overflow\"")
    echo -e "${YELLOW}⚠️  $terminal: $unread_count UNREAD messages (threshold: $UNREAD_THRESHOLD)${NC}"
  fi

  if [ $inbox_depth -gt $INBOX_DEPTH_THRESHOLD ]; then
    status="warning"
    alerts+=("\"inbox_overflow\"")
    echo -e "${YELLOW}⚠️  $terminal: inbox queue depth $inbox_depth (threshold: $INBOX_DEPTH_THRESHOLD)${NC}"
  fi

  if [ $outbox_blocked -gt 0 ]; then
    status="attention"
    alerts+=("\"blocked_messages\"")
    echo -e "${YELLOW}⚠️  $terminal: $outbox_blocked BLOCKED messages${NC}"
  fi

  if [ $archive_candidates -gt 20 ]; then
    status="attention"
    alerts+=("\"archival_needed\"")
    echo "ℹ️  $terminal: $archive_candidates messages ready for archival (>$ARCHIVE_AGE_DAYS days old)"
  fi

  echo "      \"status\": \"$status\"," >> "$OUTPUT_JSON"
  echo "      \"alerts\": [$(IFS=,; echo "${alerts[*]}")]" >> "$OUTPUT_JSON"
  echo "    }," >> "$OUTPUT_JSON"

  # Console summary
  if [ "$status" == "healthy" ]; then
    echo -e "${GREEN}✅ $terminal: healthy (UNREAD: $unread_count, inbox: $inbox_depth, DONE: $outbox_done)${NC}"
  fi
}

# Run health check for all terminals
echo "🔍 SpaceOS Mailbox Health Check - $(date +%Y-%m-%d\ %H:%M:%S)"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"

for terminal in "${TERMINALS[@]}"; do
  check_terminal_health "$terminal"
done

# Close JSON
sed -i '$ s/,$//' "$OUTPUT_JSON"  # Remove trailing comma
echo '  ]' >> "$OUTPUT_JSON"
echo '}' >> "$OUTPUT_JSON"

echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "📊 Health report saved to: $OUTPUT_JSON"

# Datahaven integration (optional)
if command -v curl &> /dev/null; then
  echo "📡 Sending metrics to Datahaven..."
  curl -X POST "https://datahaven.joinerytech.hu/api/mailbox/health" \
    -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
    -H "Content-Type: application/json" \
    -d @"$OUTPUT_JSON" \
    --silent --show-error || echo "⚠️  Datahaven upload failed (non-critical)"
fi

echo "✅ Mailbox health check complete"
