#!/bin/bash
# Knowledge Pattern Template Generator
# Usage: ./knowledge-pattern-template.sh <pattern-name> <category>
# Example: ./knowledge-pattern-template.sh "Authentication System" "security"

set -e

PATTERN_NAME="$1"
CATEGORY="${2:-patterns}"
DATE=$(date +%Y-%m-%d)

if [ -z "$PATTERN_NAME" ]; then
  echo "Usage: $0 <pattern-name> [category]"
  echo "Example: $0 'Authentication System' security"
  exit 1
fi

# Convert to filename (lowercase, hyphens)
FILENAME=$(echo "$PATTERN_NAME" | tr '[:upper:]' '[:lower:]' | tr ' ' '-')
FILEPATH="/opt/spaceos/docs/knowledge/${CATEGORY}/${FILENAME}-pattern.md"

if [ -f "$FILEPATH" ]; then
  echo "❌ File already exists: $FILEPATH"
  exit 1
fi

# Generate template
cat > "$FILEPATH" <<EOF
# ${PATTERN_NAME} Pattern

**Created:** ${DATE}
**Author:** Librarian
**Status:** Draft

---

## Overview

**${PATTERN_NAME}** is a [brief description of what this pattern solves].

### Problem Solved

**Before ${PATTERN_NAME}:**
- [Problem 1]
- [Problem 2]
- [Problem 3]

**After ${PATTERN_NAME}:**
- [Solution 1]
- [Solution 2]
- [Solution 3]

---

## Architecture

### Core Components

**[Number] Core [Tables/Components/Modules]:**

1. **\`component_1\`** — [Description]
2. **\`component_2\`** — [Description]
3. **\`component_3\`** — [Description]

### Workflow

\`\`\`
1. [Step 1] → [Action]
2. [Step 2] → [Action]
3. [Step 3] → [Action]
\`\`\`

---

## Configuration

### [Configuration Type]

| [Item] | [Value] | [Description] |
|--------|---------|---------------|
| **[Item 1]** | [Value] | [Description] |
| **[Item 2]** | [Value] | [Description] |

---

## Integration Points

### 1. [Integration Point 1]

**Description:**
\`\`\`typescript
// Code example
\`\`\`

### 2. [Integration Point 2]

**Description:**
\`\`\`typescript
// Code example
\`\`\`

---

## Best Practices

### For [Role 1]

**1. [Best Practice 1]:**
\`\`\`typescript
// Example
\`\`\`

**2. [Best Practice 2]:**
\`\`\`typescript
// Example
\`\`\`

### For [Role 2]

**1. [Best Practice 1]:**
\`\`\`typescript
// Example
\`\`\`

---

## Monitoring & Debugging

### [Monitoring Type]

**Check [something]:**
\`\`\`bash
# Command example
\`\`\`

**Output:**
\`\`\`
# Example output
\`\`\`

---

## Error Handling

### Common Errors

**1. [Error Type]:**
\`\`\`
Error: [error message]
\`\`\`
**Cause:** [Explanation]
**Fix:** [Solution]

**2. [Error Type]:**
\`\`\`
Error: [error message]
\`\`\`
**Cause:** [Explanation]
**Fix:** [Solution]

---

## Performance Characteristics

### Write Performance

| Operation | Time | Notes |
|-----------|------|-------|
| [Operation 1] | ~Xms | [Notes] |
| [Operation 2] | ~Xms | [Notes] |

### Read Performance

| Query | Time | Notes |
|-------|------|-------|
| [Query 1] | ~Xms | [Notes] |
| [Query 2] | ~Xms | [Notes] |

---

## Future Enhancements

**Planned:**
- [ ] [Enhancement 1]
- [ ] [Enhancement 2]
- [ ] [Enhancement 3]

**Under consideration:**
- [ ] [Idea 1]
- [ ] [Idea 2]

---

## Related Patterns

- [RELATED_PATTERN_1.md](RELATED_PATTERN_1.md) — [Brief description]
- [RELATED_PATTERN_2.md](RELATED_PATTERN_2.md) — [Brief description]

---

**Last Updated:** ${DATE}
**Source Code:** \`path/to/source/\`
**Tests:** \`path/to/tests.ts\` (TODO)
EOF

echo "✅ Pattern template created: $FILEPATH"
echo ""
echo "Next steps:"
echo "1. Fill in the template sections"
echo "2. Add real-world examples"
echo "3. Update docs/knowledge/INDEX.md with new pattern"
echo "4. Validate with: scripts/pattern-quality-check.sh $FILEPATH"
