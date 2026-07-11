#!/bin/bash
# Knowledge Service Auto-Reindex Trigger
# Called by pipeline.sh after Librarian DONE approval

set -e

KNOWLEDGE_PATH="/opt/spaceos/docs/knowledge"
LAST_INDEX_FILE="/opt/spaceos/logs/dispatcher/.last-knowledge-index"
LOG="/opt/spaceos/logs/dispatcher/knowledge-reindex.log"

# Check if docs/knowledge/ has changed since last index
if [ -f "$LAST_INDEX_FILE" ]; then
  LAST_MTIME=$(stat -c %Y "$LAST_INDEX_FILE")
  CURRENT_MTIME=$(find "$KNOWLEDGE_PATH" -type f -name "*.md" -newer "$LAST_INDEX_FILE" | wc -l)

  if [ "$CURRENT_MTIME" -eq 0 ]; then
    echo "$(date): No changes in docs/knowledge/, skipping reindex" >> "$LOG"
    exit 0
  fi
fi

echo "$(date): Triggering Knowledge Service reindex..." >> "$LOG"

# Retry logic: 3 attempts, 10s delay
for attempt in 1 2 3; do
  if curl -X POST http://localhost:3456/api/knowledge/index \
       -H "Content-Type: application/json" \
       -d '{"source":"docs/knowledge"}' \
       --max-time 300 \
       --silent --show-error >> "$LOG" 2>&1; then

    echo "$(date): ✅ Reindex successful (attempt $attempt)" >> "$LOG"
    touch "$LAST_INDEX_FILE"

    # Telegram notification
    /opt/spaceos/scripts/spaceos-notify.sh "📚 Knowledge Service reindexed (Librarian trigger)"

    exit 0
  else
    echo "$(date): ⚠️ Reindex failed (attempt $attempt/3)" >> "$LOG"
    [ $attempt -lt 3 ] && sleep 10
  fi
done

echo "$(date): ❌ Reindex failed after 3 attempts" >> "$LOG"
/opt/spaceos/scripts/spaceos-notify.sh "⚠️ Knowledge reindex FAILED after 3 attempts"
exit 1
