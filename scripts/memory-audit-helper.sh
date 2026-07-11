#!/bin/bash
# Memory Audit Helper Script
# Quick memory health check for all SpaceOS terminals
# Usage: ./memory-audit-helper.sh

set -e

TERMINALS_DIR="/opt/spaceos/terminals"
DATE=$(date +%Y-%m-%d)

echo "🔍 SpaceOS Memory Audit — $DATE"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""

# Terminal list
TERMINALS=(
  "root"
  "conductor"
  "architect"
  "librarian"
  "explorer"
  "backend"
  "frontend"
  "designer"
  "monitor"
)

# Summary stats
TOTAL_SIZE=0
TOTAL_LINES=0
LARGEST_TERMINAL=""
LARGEST_SIZE=0

echo "📊 Memory File Stats:"
echo ""
printf "%-12s %10s %10s %12s\n" "Terminal" "Lines" "Size (KB)" "Last Modified"
echo "────────────────────────────────────────────────────────"

for terminal in "${TERMINALS[@]}"; do
  MEMORY_FILE="$TERMINALS_DIR/$terminal/MEMORY.md"

  if [ ! -f "$MEMORY_FILE" ]; then
    printf "%-12s %10s %10s %12s\n" "$terminal" "N/A" "N/A" "Missing"
    continue
  fi

  # Get stats
  LINES=$(wc -l < "$MEMORY_FILE")
  SIZE=$(du -k "$MEMORY_FILE" | cut -f1)
  MODIFIED=$(stat -c %y "$MEMORY_FILE" 2>/dev/null | cut -d' ' -f1 || echo "Unknown")

  # Track totals
  TOTAL_SIZE=$((TOTAL_SIZE + SIZE))
  TOTAL_LINES=$((TOTAL_LINES + LINES))

  # Track largest
  if [ "$SIZE" -gt "$LARGEST_SIZE" ]; then
    LARGEST_SIZE=$SIZE
    LARGEST_TERMINAL=$terminal
  fi

  # Color code based on size
  if [ "$SIZE" -gt 1024 ]; then
    # >1 MB = red warning
    printf "%-12s %10d %10d %12s ⚠️\n" "$terminal" "$LINES" "$SIZE" "$MODIFIED"
  elif [ "$SIZE" -gt 512 ]; then
    # >512 KB = yellow caution
    printf "%-12s %10d %10d %12s 📊\n" "$terminal" "$LINES" "$SIZE" "$MODIFIED"
  else
    # <512 KB = green ok
    printf "%-12s %10d %10d %12s ✅\n" "$terminal" "$LINES" "$SIZE" "$MODIFIED"
  fi
done

echo "────────────────────────────────────────────────────────"
printf "%-12s %10d %10d\n" "TOTAL" "$TOTAL_LINES" "$TOTAL_SIZE"
echo ""

echo "📈 Summary:"
echo "  • Total memory: $TOTAL_SIZE KB (~$((TOTAL_SIZE / 1024)) MB)"
echo "  • Average size: $((TOTAL_SIZE / ${#TERMINALS[@]})) KB per terminal"
echo "  • Largest: $LARGEST_TERMINAL ($LARGEST_SIZE KB)"
echo ""

# Check for stale entries (>90 days old)
echo "🔍 Checking for stale entries (>90 days)..."
echo ""

STALE_COUNT=0
for terminal in "${TERMINALS[@]}"; do
  MEMORY_FILE="$TERMINALS_DIR/$terminal/MEMORY.md"

  if [ ! -f "$MEMORY_FILE" ]; then
    continue
  fi

  # Find dates in memory file (format: YYYY-MM-DD or ## YYYY-MM-DD)
  DATES=$(grep -oE '[0-9]{4}-[0-9]{2}-[0-9]{2}' "$MEMORY_FILE" | sort -u | head -5)

  for date in $DATES; do
    # Calculate days difference
    DAYS_AGO=$(( ($(date +%s) - $(date -d "$date" +%s)) / 86400 ))

    if [ "$DAYS_AGO" -gt 90 ]; then
      if [ "$STALE_COUNT" -eq 0 ]; then
        echo "⚠️  Found stale entries:"
      fi
      echo "  • $terminal: Entry from $date (${DAYS_AGO} days ago)"
      ((STALE_COUNT++))
    fi
  done
done

if [ "$STALE_COUNT" -eq 0 ]; then
  echo "✅ No stale entries found (all entries <90 days)"
fi
echo ""

# Check for project-specific memories
echo "📁 Project-Specific Memories:"
echo ""

for terminal in "${TERMINALS[@]}"; do
  KNOWLEDGE_DIR="$TERMINALS_DIR/$terminal/knowledge"

  if [ ! -d "$KNOWLEDGE_DIR" ]; then
    continue
  fi

  PROJECT_COUNT=$(find "$KNOWLEDGE_DIR" -name "*.memory.md" 2>/dev/null | wc -l)

  if [ "$PROJECT_COUNT" -gt 0 ]; then
    echo "  • $terminal: $PROJECT_COUNT project memory file(s)"
    find "$KNOWLEDGE_DIR" -name "*.memory.md" -exec basename {} \; | sed 's/^/    - /'
  fi
done
echo ""

# Health status
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
if [ "$TOTAL_SIZE" -lt 512 ]; then
  echo "✅ EXCELLENT — Memory health is optimal (<512 KB total)"
elif [ "$TOTAL_SIZE" -lt 1024 ]; then
  echo "✅ GOOD — Memory health is acceptable (<1 MB total)"
elif [ "$TOTAL_SIZE" -lt 2048 ]; then
  echo "⚠️  CAUTION — Memory growing, consider archival (1-2 MB)"
else
  echo "🚨 ACTION NEEDED — Memory exceeds 2 MB, archival required"
fi
echo ""

# Recommendations
echo "💡 Recommendations:"
if [ "$STALE_COUNT" -gt 0 ]; then
  echo "  • Archive entries >90 days old ($STALE_COUNT found)"
fi

if [ "$LARGEST_SIZE" -gt 1024 ]; then
  echo "  • Review $LARGEST_TERMINAL memory ($LARGEST_SIZE KB) for archival opportunities"
fi

echo "  • Next audit: $(date -d '+30 days' +%Y-%m-%d) (monthly cadence)"
echo "  • Detailed audit: Run MSG-LIBRARIAN-008 workflow"
echo ""
