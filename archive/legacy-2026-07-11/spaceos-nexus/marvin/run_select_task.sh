#!/bin/bash
# =============================================================================
# run_select_task.sh — Marvin select_best_ideas() Task wrapper
#
# Replaces: plan-select.sh (partially)
# Usage: ./run_select_task.sh <ideas_dir> [top_n]
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
    exit 1
fi

# Arguments
IDEAS_DIR="${1:-$SPACEOS_ROOT/docs/planning/ideas}"
TOP_N="${2:-5}"

if [ ! -d "$IDEAS_DIR" ]; then
    echo "ERROR: Ideas directory not found: $IDEAS_DIR" >&2
    exit 1
fi

# Read domain focus
DOMAIN_FOCUS_FILE="$SPACEOS_ROOT/docs/planning/domain-focus.md"
DOMAIN_FOCUS=$(cat "$DOMAIN_FOCUS_FILE" 2>/dev/null || echo "")

# Run Marvin task
cd "$MARVIN_DIR"
source venv/bin/activate

python3 << EOF
import asyncio
import json
import os
from pathlib import Path
from planning_tasks import select_best_ideas, PlanningIdea

async def main():
    ideas_dir = Path("$IDEAS_DIR")

    # Load all ideas from JSON files (or parse markdown)
    ideas = []
    for idea_file in ideas_dir.glob("*.md"):
        # For now, create mock ideas
        # In production, parse the actual idea files
        pass

    # If no ideas loaded, create sample for testing
    if not ideas:
        print("No ideas found in $IDEAS_DIR", file=os.sys.stderr)
        return

    selected = await select_best_ideas(
        ideas=ideas,
        domain_focus="""$DOMAIN_FOCUS""",
        top_n=int("$TOP_N")
    )

    # Output as JSON
    for sel in selected:
        print(json.dumps(sel.dict(), indent=2))

asyncio.run(main())
EOF
