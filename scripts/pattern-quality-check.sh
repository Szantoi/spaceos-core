#!/bin/bash
# Knowledge Pattern Quality Checker
# Validates pattern documentation against SpaceOS quality standards
# Usage: ./pattern-quality-check.sh <pattern-file.md>

set -e

PATTERN_FILE="$1"

if [ -z "$PATTERN_FILE" ] || [ ! -f "$PATTERN_FILE" ]; then
  echo "Usage: $0 <pattern-file.md>"
  exit 1
fi

echo "🔍 Checking pattern quality: $(basename "$PATTERN_FILE")"
echo ""

# Quality checks
ERRORS=0
WARNINGS=0

# Check 1: Required sections
echo "📋 Required sections:"
REQUIRED_SECTIONS=(
  "# .* Pattern"
  "## Overview"
  "## Architecture"
  "## Integration Points"
  "## Best Practices"
  "## Monitoring & Debugging"
  "## Error Handling"
  "## Performance Characteristics"
  "## Related Patterns"
)

for section in "${REQUIRED_SECTIONS[@]}"; do
  if grep -qE "^$section" "$PATTERN_FILE"; then
    echo "  ✅ $section"
  else
    echo "  ❌ Missing: $section"
    ((ERRORS++))
  fi
done

echo ""

# Check 2: Metadata
echo "📝 Metadata:"
if grep -q "^\*\*Created:\*\*" "$PATTERN_FILE"; then
  echo "  ✅ Created date"
else
  echo "  ⚠️  Missing: Created date"
  ((WARNINGS++))
fi

if grep -q "^\*\*Author:\*\*" "$PATTERN_FILE"; then
  echo "  ✅ Author"
else
  echo "  ⚠️  Missing: Author"
  ((WARNINGS++))
fi

if grep -q "^\*\*Last Updated:\*\*" "$PATTERN_FILE"; then
  echo "  ✅ Last Updated"
else
  echo "  ⚠️  Missing: Last Updated"
  ((WARNINGS++))
fi

echo ""

# Check 3: Code examples
echo "💻 Code examples:"
CODE_BLOCKS=$(grep -c '```' "$PATTERN_FILE" || echo 0)
if [ "$CODE_BLOCKS" -ge 6 ]; then
  echo "  ✅ Code examples: $CODE_BLOCKS blocks"
else
  echo "  ⚠️  Only $CODE_BLOCKS code blocks (recommend ≥6)"
  ((WARNINGS++))
fi

echo ""

# Check 4: Length check
echo "📏 Documentation length:"
LINE_COUNT=$(wc -l < "$PATTERN_FILE")
if [ "$LINE_COUNT" -ge 400 ]; then
  echo "  ✅ $LINE_COUNT lines (comprehensive)"
elif [ "$LINE_COUNT" -ge 200 ]; then
  echo "  ⚠️  $LINE_COUNT lines (consider expanding)"
  ((WARNINGS++))
else
  echo "  ❌ $LINE_COUNT lines (too short, recommend ≥400)"
  ((ERRORS++))
fi

echo ""

# Check 5: Tables
echo "📊 Tables:"
TABLE_COUNT=$(grep -c '^|' "$PATTERN_FILE" || echo 0)
if [ "$TABLE_COUNT" -ge 3 ]; then
  echo "  ✅ $TABLE_COUNT table rows found"
else
  echo "  ⚠️  Only $TABLE_COUNT table rows (recommend ≥3 tables)"
  ((WARNINGS++))
fi

echo ""

# Check 6: Related patterns
echo "🔗 Related patterns:"
if grep -qE '\[.*\.md\].*—' "$PATTERN_FILE"; then
  RELATED_COUNT=$(grep -cE '\[.*\.md\].*—' "$PATTERN_FILE")
  echo "  ✅ $RELATED_COUNT related patterns linked"
else
  echo "  ⚠️  No related patterns linked"
  ((WARNINGS++))
fi

echo ""

# Summary
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
if [ $ERRORS -eq 0 ] && [ $WARNINGS -eq 0 ]; then
  echo "✅ EXCELLENT — Pattern meets all quality standards!"
  exit 0
elif [ $ERRORS -eq 0 ]; then
  echo "⚠️  GOOD — Pattern passes with $WARNINGS warning(s)"
  exit 0
else
  echo "❌ NEEDS WORK — $ERRORS error(s), $WARNINGS warning(s)"
  exit 1
fi
