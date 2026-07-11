#!/bin/bash
# =============================================================================
# run_debate_task.sh — Marvin parallel debate Task wrapper
#
# Replaces: plan-debate.sh (partially)
# Usage: ./run_debate_task.sh <idea_json>
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
IDEA_JSON="${1:-}"

if [ -z "$IDEA_JSON" ]; then
    echo "Usage: $0 <idea_json>" >&2
    echo "  idea_json: JSON string or file with PlanningIdea" >&2
    exit 1
fi

# Read codebase status
CODEBASE_STATUS_FILE="$SPACEOS_ROOT/docs/Codebase_Status.md"
CODEBASE_STATUS=$(head -30 "$CODEBASE_STATUS_FILE" 2>/dev/null || echo "")

# Read domain focus
DOMAIN_FOCUS_FILE="$SPACEOS_ROOT/docs/planning/domain-focus.md"
DOMAIN_FOCUS=$(cat "$DOMAIN_FOCUS_FILE" 2>/dev/null || echo "")

# Run Marvin task
cd "$MARVIN_DIR"
source venv/bin/activate

python3 << EOF
import asyncio
import json
from planning_tasks import run_parallel_debate, PlanningIdea

async def main():
    # Parse idea JSON
    idea_data = json.loads('''$IDEA_JSON''')
    idea = PlanningIdea(**idea_data)

    # Run parallel debate
    consensus = await run_parallel_debate(
        idea=idea,
        codebase_status="""$CODEBASE_STATUS""",
        domain_focus="""$DOMAIN_FOCUS"""
    )

    # Output consensus as JSON
    print(json.dumps(consensus.dict(), indent=2))

asyncio.run(main())
EOF
