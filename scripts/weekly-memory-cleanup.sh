#!/bin/bash
# Weekly Memory Cleanup Check
# Runs: Every Sunday at 02:00 AM
# Purpose: Automatically detect oversized MEMORY.md files and create Librarian cleanup tasks

LOG_FILE="/opt/spaceos/logs/dispatcher/memory-hygiene.log"
THRESHOLD_KB=50

echo "[$(date)] Weekly memory cleanup check started" >> "$LOG_FILE"

# Check all terminal MEMORY.md sizes
for terminal in root conductor architect librarian explorer backend frontend designer monitor; do
  memory_file="/opt/spaceos/terminals/${terminal}/MEMORY.md"

  if [ -f "$memory_file" ]; then
    size_kb=$(du -k "$memory_file" | cut -f1)

    if [ "$size_kb" -gt "$THRESHOLD_KB" ]; then
      echo "[$(date)] ⚠️ ${terminal}: ${size_kb}KB (threshold: ${THRESHOLD_KB}KB)" >> "$LOG_FILE"

      # Create Librarian cleanup task
      next_id=$(ls /opt/spaceos/terminals/librarian/inbox/ | grep -E '^2026' | sort | tail -1 | sed 's/.*_\([0-9]\{3\}\)_.*/\1/' | awk '{printf "%03d", $1+1}')
      task_file="/opt/spaceos/terminals/librarian/inbox/$(date +%Y-%m-%d)_${next_id}_weekly-memory-cleanup-${terminal}.md"

      cat > "$task_file" <<EOF
---
id: MSG-LIBRARIAN-${next_id}
from: system
to: librarian
type: task
priority: medium
status: UNREAD
model: sonnet
created: $(date +%Y-%m-%d)
---

# Weekly Memory Cleanup: ${terminal}

MEMORY.md size: ${size_kb}KB (threshold: ${THRESHOLD_KB}KB)

Archive content >2 weeks old, promote patterns to MCP server memory.
EOF

      echo "[$(date)] ✅ Created MSG-LIBRARIAN-${next_id} for ${terminal}" >> "$LOG_FILE"
    else
      echo "[$(date)] ✅ ${terminal}: ${size_kb}KB (OK)" >> "$LOG_FILE"
    fi
  fi
done

echo "[$(date)] Weekly memory cleanup check complete" >> "$LOG_FILE"
