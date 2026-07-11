#!/bin/bash
# Bulk Convert INJECTED → UNREAD
# Part of MSG-BACKEND-123 implementation

echo "=== Bulk Convert INJECTED → UNREAD ==="
echo ""

# Find all INJECTED messages
INJECTED_FILES=$(grep -rl "status: INJECTED" /opt/spaceos/terminals/*/inbox/ 2>/dev/null || true)

if [ -z "$INJECTED_FILES" ]; then
  echo "✓ No INJECTED messages found"
  exit 0
fi

COUNT=$(echo "$INJECTED_FILES" | wc -l)
echo "Found $COUNT INJECTED messages"
echo ""

CONVERTED=0
ARCHIVED=0
SKIPPED=0

for FILE in $INJECTED_FILES; do
  echo "Processing: $FILE"

  # Check if file has completed_at field
  if grep -q "^completed_at:" "$FILE"; then
    # Archive it
    TERMINAL=$(echo "$FILE" | sed 's|.*/terminals/\([^/]*\)/inbox/.*|\1|')
    ARCHIVE_DIR="/opt/spaceos/terminals/$TERMINAL/archive"

    mkdir -p "$ARCHIVE_DIR"
    BASENAME=$(basename "$FILE")

    mv "$FILE" "$ARCHIVE_DIR/$BASENAME"
    echo "  → ARCHIVED (had completed_at)"
    ((ARCHIVED++))

  else
    # Convert status: INJECTED → UNREAD
    # Remove injected: line
    sed -i 's/^status: INJECTED$/status: UNREAD/' "$FILE"
    sed -i '/^injected:/d' "$FILE"

    echo "  → CONVERTED (INJECTED → UNREAD)"
    ((CONVERTED++))
  fi

  echo ""
done

echo "=== Summary ==="
echo "Total: $COUNT"
echo "Converted: $CONVERTED"
echo "Archived: $ARCHIVED"
echo "Skipped: $SKIPPED"
echo ""
echo "✓ Bulk conversion complete"
