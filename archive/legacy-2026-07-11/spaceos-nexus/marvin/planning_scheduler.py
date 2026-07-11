"""
SpaceOS Marvin Planning Scheduler
Phase 2.5: Replaces bash cron with Marvin-native scheduling

Runs planning pipeline automatically:
- Scan segments for ideas (*/10 minutes)
- Select best ideas (when threshold reached)
- Debate selected ideas (parallel execution)
"""

import os
import asyncio
import yaml
from datetime import datetime
from pathlib import Path
from typing import List, Dict, Any

# Marvin imports
from marvin import Agent

# Local imports
from planning_tasks import (
    scan_for_ideas,
    select_best_ideas,
    run_parallel_debate,
    PlanningIdea,
)
from marvin_tools import knowledge_search, build_discovery_context


# ─── Configuration ────────────────────────────────────────────────────────────


def load_config() -> Dict[str, Any]:
    """Load configuration from config.yaml"""
    config_path = Path(__file__).parent / "config.yaml"
    if not config_path.exists():
        raise FileNotFoundError(f"Config file not found: {config_path}")

    with open(config_path, 'r') as f:
        return yaml.safe_load(f)


CONFIG = load_config()

SPACEOS_ROOT = Path(os.getenv("SPACEOS_ROOT", "/opt/spaceos"))
MEMORY_DIR = Path(CONFIG['paths']['memory_dir'])
IDEAS_DIR = Path(CONFIG['paths']['ideas_dir'])
QUEUE_DIR = Path(CONFIG['paths']['queue_dir'])
PLANNING_DIR = SPACEOS_ROOT / "docs" / "planning"
SELECTED_DIR = PLANNING_DIR / "selected"
DOMAIN_FOCUS_FILE = PLANNING_DIR / "domain-focus.md"

# Thresholds from config
IDEAS_THRESHOLD = CONFIG['planning']['limits']['ideas_max']
QUEUE_THRESHOLD = CONFIG['planning']['limits']['queue_max']

# 9 segments from config (MSG-NEXUS-015)
SEGMENTS = [
    (seg, f"docs/memory/{seg}.md")
    for seg in CONFIG['planning']['segments']
]


# ─── Scheduler State ──────────────────────────────────────────────────────────


class PlanningState:
    """Track planning pipeline state"""

    def __init__(self):
        self.scan_counter = 0
        self.last_segment_index = -1
        self.ideas_generated = 0
        self.consensus_generated = 0

    def next_segment(self) -> int:
        """Rotating segment selection"""
        self.last_segment_index = (self.last_segment_index + 1) % len(SEGMENTS)
        return self.last_segment_index


state = PlanningState()


# ─── Helper Functions ─────────────────────────────────────────────────────────


def read_domain_focus() -> str:
    """Read current domain focus"""
    try:
        return DOMAIN_FOCUS_FILE.read_text()
    except FileNotFoundError:
        return "All domains"


def count_ideas() -> int:
    """Count ideas in IDEAS_DIR"""
    if not IDEAS_DIR.exists():
        IDEAS_DIR.mkdir(parents=True, exist_ok=True)
        return 0
    return len(list(IDEAS_DIR.glob("*.md")))


def count_queue() -> int:
    """Count consensus in QUEUE_DIR"""
    if not QUEUE_DIR.exists():
        QUEUE_DIR.mkdir(parents=True, exist_ok=True)
        return 0
    return len(list(QUEUE_DIR.glob("*.md")))


def save_idea_to_file(idea: PlanningIdea):
    """Save idea to IDEAS_DIR as markdown"""
    IDEAS_DIR.mkdir(parents=True, exist_ok=True)

    filename = f"{datetime.now().strftime('%Y-%m-%d_%H%M')}_{idea.segment}_{idea.title[:30].replace(' ', '-').lower()}.md"
    filepath = IDEAS_DIR / filename

    content = f"""---
title: {idea.title}
segment: {idea.segment}
priority: {idea.priority}
confidence: {idea.confidence:.2f}
created: {datetime.now().isoformat()}
---

# {idea.title}

**Segment:** {idea.segment}
**Priority:** {idea.priority}
**Confidence:** {idea.confidence:.2f}

## Description

{idea.description}

## Rationale

{idea.rationale}

## Knowledge Context

{idea.context if idea.context else "No additional context"}
"""

    filepath.write_text(content)
    print(f"  💾 Saved idea: {filename}")


def submit_artifact(content: str, artifact_type: str, metadata: Dict[str, Any] = None):
    """
    Submit artifact (idea or consensus) to file system.

    MCP tool equivalent of submitArtifact from MSG-NEXUS-015 spec.

    Args:
        content: Markdown content
        artifact_type: 'idea' or 'consensus'
        metadata: Optional metadata dict (title, segment, etc.)
    """
    metadata = metadata or {}
    timestamp = datetime.now().strftime('%Y-%m-%d_%H%M')
    slug = metadata.get('title', 'artifact')[:30].replace(' ', '-').lower()

    if artifact_type == "idea":
        target_dir = IDEAS_DIR
        filename = f"{timestamp}_{metadata.get('segment', 'unknown')}_{slug}.md"
    elif artifact_type == "consensus":
        target_dir = QUEUE_DIR
        filename = f"{timestamp}_{slug}.md"
    else:
        raise ValueError(f"Unknown artifact type: {artifact_type}")

    target_dir.mkdir(parents=True, exist_ok=True)
    filepath = target_dir / filename
    filepath.write_text(content)

    print(f"  📦 Submitted {artifact_type}: {filename}")
    return str(filepath)


# ─── Planning Pipeline Tasks ──────────────────────────────────────────────────


async def scan_task():
    """
    Scan one segment for ideas (rotating).
    Runs every 10 minutes (or on-demand).
    """
    state.scan_counter += 1
    print(f"\n{'='*60}")
    print(f"SCAN TASK #{state.scan_counter} — {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print(f"{'='*60}")

    # Check throttling
    idea_count = count_ideas()
    if idea_count >= IDEAS_THRESHOLD:
        print(f"⏸️  Throttled: {idea_count} ideas (threshold: {IDEAS_THRESHOLD})")
        print("   Triggering select_task...")
        await select_task()
        return

    # Select segment
    segment_index = state.next_segment()
    segment_name, segment_path = SEGMENTS[segment_index]
    full_path = SPACEOS_ROOT / segment_path

    print(f"📂 Segment: {segment_name} ({segment_path})")

    if not full_path.exists():
        print(f"⚠️  File not found: {full_path}")
        return

    # Read segment content
    segment_content = full_path.read_text()
    domain_focus = read_domain_focus()

    print(f"📄 Content: {len(segment_content)} chars")
    print(f"🎯 Domain: {domain_focus[:50]}...")

    # Scan for ideas
    print("🔍 Scanning for ideas...")
    ideas = await scan_for_ideas(
        segment_name=segment_name,
        segment_content=segment_content,
        domain_focus=domain_focus,
        recent_ideas=""  # TODO: Load recent ideas
    )

    print(f"✅ Found {len(ideas)} ideas")

    # Save ideas
    for idea in ideas:
        print(f"  - [{idea.priority}] {idea.title} (confidence: {idea.confidence:.2f})")
        save_idea_to_file(idea)
        state.ideas_generated += 1

    print(f"\n💡 Total ideas in pipeline: {count_ideas()}")


async def select_task():
    """
    Select best ideas for debate.
    Runs when ideas reach threshold.
    """
    print(f"\n{'='*60}")
    print(f"SELECT TASK — {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print(f"{'='*60}")

    idea_count = count_ideas()
    print(f"💡 Ideas available: {idea_count}")

    if idea_count < IDEAS_THRESHOLD:
        print("⏸️  Not enough ideas for selection")
        return

    # TODO: Load ideas from FILES_DIR (currently mock)
    # For now, create mock ideas for testing
    print("⚠️  TODO: Load actual ideas from IDEAS_DIR")
    print("   Skipping selection (mock implementation)")

    # When implemented:
    # ideas = load_ideas_from_dir(IDEAS_DIR)
    # selected = await select_best_ideas(ideas, domain_focus=read_domain_focus(), top_n=5)
    # save_selected_to_file(selected)


async def debate_task():
    """
    Debate selected ideas (parallel Planner-A vs Planner-B).
    Runs when selected ideas are ready.
    """
    print(f"\n{'='*60}")
    print(f"DEBATE TASK — {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print(f"{'='*60}")

    # TODO: Load selected ideas and run debate
    print("⚠️  TODO: Implement debate from SELECTED_DIR")
    print("   Skipping debate (mock implementation)")

    # When implemented:
    # selected_file = SELECTED_DIR / "pending.md"
    # if not selected_file.exists():
    #     return
    # selected = load_selected_ideas(selected_file)
    # for idea in selected:
    #     consensus = await run_parallel_debate(idea, codebase_status="...", domain_focus="...")
    #     save_consensus_to_queue(consensus)


# ─── Scheduler Main Loop ──────────────────────────────────────────────────────


async def planning_scheduler(interval_seconds: int = 600):
    """
    Main scheduler loop.
    Runs scan task every `interval_seconds`.

    Args:
        interval_seconds: Scan interval (default: 600 = 10 minutes)
    """
    print(f"""
╔══════════════════════════════════════════════════════════════╗
║  SpaceOS Marvin Planning Scheduler                           ║
║  Scan interval: {interval_seconds}s ({interval_seconds//60} minutes)                          ║
║  Segments: {len(SEGMENTS)}                                               ║
╚══════════════════════════════════════════════════════════════╝
""")

    print("📋 Scheduler started. Press Ctrl+C to stop.\n")

    try:
        while True:
            await scan_task()

            # Sleep until next scan
            print(f"\n💤 Next scan in {interval_seconds}s...")
            await asyncio.sleep(interval_seconds)

    except KeyboardInterrupt:
        print("\n\n⏹️  Scheduler stopped by user")
        print(f"📊 Stats: {state.scan_counter} scans, {state.ideas_generated} ideas generated")


# ─── CLI Entry Point ──────────────────────────────────────────────────────────


async def main():
    """CLI entry point"""
    import sys

    if len(sys.argv) > 1:
        command = sys.argv[1]

        if command == "scan":
            # Manual scan
            await scan_task()

        elif command == "select":
            # Manual select
            await select_task()

        elif command == "debate":
            # Manual debate
            await debate_task()

        elif command == "run":
            # Run scheduler
            interval = int(sys.argv[2]) if len(sys.argv) > 2 else 600
            await planning_scheduler(interval_seconds=interval)

        else:
            print(f"Unknown command: {command}")
            print("Usage:")
            print("  python planning_scheduler.py scan      # Manual scan")
            print("  python planning_scheduler.py select    # Manual select")
            print("  python planning_scheduler.py debate    # Manual debate")
            print("  python planning_scheduler.py run [interval_seconds]")
            sys.exit(1)
    else:
        # Default: run scheduler
        await planning_scheduler(interval_seconds=600)


if __name__ == "__main__":
    asyncio.run(main())
