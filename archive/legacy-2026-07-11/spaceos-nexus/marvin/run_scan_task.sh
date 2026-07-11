#!/bin/bash
# =============================================================================
# run_scan_task.sh — Marvin scan_for_ideas() Task wrapper
#
# Replaces: plan-scan.sh (partially)
# Usage: ./run_scan_task.sh <segment_name> <segment_file>
# =============================================================================

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
MARVIN_DIR="$SCRIPT_DIR"
SPACEOS_ROOT="${SPACEOS_ROOT:-/opt/spaceos}"

# Load environment
if [ -f "$MARVIN_DIR/.env" ]; then
    export $(grep -v '^#' "$MARVIN_DIR/.env" | xargs)
fi

# Check API key
if [ -z "${OPENAI_API_KEY:-}" ]; then
    echo "ERROR: OPENAI_API_KEY not set" >&2
    echo "Create $MARVIN_DIR/.env with:" >&2
    echo "  OPENAI_API_KEY=sk-..." >&2
    exit 1
fi

# Arguments
SEGMENT_NAME="${1:-}"
SEGMENT_FILE="${2:-}"

if [ -z "$SEGMENT_NAME" ] || [ -z "$SEGMENT_FILE" ]; then
    echo "Usage: $0 <segment_name> <segment_file>" >&2
    exit 1
fi

if [ ! -f "$SEGMENT_FILE" ]; then
    echo "ERROR: Segment file not found: $SEGMENT_FILE" >&2
    exit 1
fi

# Read segment content
SEGMENT_CONTENT=$(cat "$SEGMENT_FILE")

# Read domain focus
DOMAIN_FOCUS_FILE="$SPACEOS_ROOT/docs/planning/domain-focus.md"
DOMAIN_FOCUS=$(cat "$DOMAIN_FOCUS_FILE" 2>/dev/null || echo "")

# Recent ideas (last 10)
IDEAS_DIR="$SPACEOS_ROOT/docs/planning/ideas"
RECENT_IDEAS=$(ls -t "$IDEAS_DIR"/*.md 2>/dev/null | head -10 | xargs -I{} basename {} | tr '\n' ', ' || echo "")

# Run Marvin task
cd "$MARVIN_DIR"
source venv/bin/activate

python3 << EOF
import asyncio
import json
from planning_tasks import scan_for_ideas, PlanningIdea

async def main():
    ideas = await scan_for_ideas(
        segment_name="$SEGMENT_NAME",
        segment_content="""$SEGMENT_CONTENT""",
        domain_focus="""$DOMAIN_FOCUS""",
        recent_ideas="$RECENT_IDEAS"
    )

    # Output as JSON
    for idea in ideas:
        print(json.dumps(idea.dict(), indent=2))

asyncio.run(main())
EOF
