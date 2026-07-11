"""
Nightwatch Scheduler — SpaceOS Session Monitoring Daemon

Replaces bash nightwatch.sh with Marvin-native Python scheduler.

Responsibilities:
1. Priority session check (ROOT, CONDUCTOR always running)
2. DONE message detection → trigger dual review
3. Stuck session detection → send Enter nudge
4. UNREAD inbox detection → start terminal session

Usage:
    python nightwatch_scheduler.py run
"""

import asyncio
import logging
from pathlib import Path
from datetime import datetime
from typing import List, Optional
import yaml
import subprocess

from workflow_state_tracker import WorkflowStateTracker, SessionStatus
from reviewer_task import dual_review


# =============================================================================
# Configuration Loading
# =============================================================================

def load_config() -> dict:
    """Load nightwatch configuration from YAML"""
    config_path = Path(__file__).parent / "nightwatch-config.yaml"
    if config_path.exists():
        with open(config_path, 'r') as f:
            return yaml.safe_load(f)
    else:
        # Default configuration
        return {
            'nightwatch': {
                'interval_seconds': 120,  # 2 minutes
                'priority_terminals': ['root', 'conductor']
            },
            'stuck_detection': {
                'threshold_minutes': 10,
                'nudge_retry_delay': 300  # 5 minutes
            },
            'paths': {
                'spaceos_root': '/opt/spaceos',
                'mailbox_base': 'docs/mailbox'
            }
        }


CONFIG = load_config()
SPACEOS_ROOT = Path(CONFIG['paths']['spaceos_root'])
MAILBOX_BASE = SPACEOS_ROOT / CONFIG['paths']['mailbox_base']


# =============================================================================
# Logging Setup
# =============================================================================

logging.basicConfig(
    level=logging.INFO,
    format='%(asctime)s [%(levelname)s] %(message)s',
    handlers=[
        logging.FileHandler('logs/nightwatch.log'),
        logging.StreamHandler()
    ]
)
logger = logging.getLogger(__name__)


# =============================================================================
# Session Management Functions
# =============================================================================

def check_tmux_session_exists(session_name: str) -> bool:
    """
    Check if tmux session exists.
    
    Args:
        session_name: Session name (e.g., "spaceos-root")
        
    Returns:
        True if session exists
    """
    result = subprocess.run(
        ['tmux', 'has-session', '-t', session_name],
        capture_output=True
    )
    return result.returncode == 0


def get_session_last_activity(session_name: str) -> Optional[str]:
    """
    Get last output from tmux session.
    
    Args:
        session_name: Session name
        
    Returns:
        Last line of output or None
    """
    try:
        result = subprocess.run(
            ['tmux', 'capture-pane', '-t', session_name, '-p'],
            capture_output=True,
            text=True,
            timeout=5
        )
        if result.returncode == 0:
            lines = result.stdout.strip().split('\n')
            return lines[-1] if lines else None
    except Exception as e:
        logger.error(f"Failed to get session activity: {e}")
    return None


def send_enter_to_session(session_name: str) -> bool:
    """
    Send Enter key to stuck session.
    
    Args:
        session_name: Session name
        
    Returns:
        True if successful
    """
    try:
        result = subprocess.run(
            ['tmux', 'send-keys', '-t', session_name, '', 'Enter', 'Enter'],
            capture_output=True,
            timeout=5
        )
        return result.returncode == 0
    except Exception as e:
        logger.error(f"Failed to send Enter to {session_name}: {e}")
        return False


# =============================================================================
# Core Nightwatch Tasks
# =============================================================================

async def check_priority_sessions():
    """
    Ensure ROOT and CONDUCTOR sessions are always running.
    
    Checks:
    - Session exists in tmux
    - Session is active (not stuck)
    
    Actions:
    - Log warning if missing
    - TODO: Auto-restart if configured
    """
    priority_terminals = CONFIG['nightwatch']['priority_terminals']
    
    for terminal in priority_terminals:
        session_name = f"spaceos-{terminal}"
        
        if not check_tmux_session_exists(session_name):
            logger.warning(f"❌ Priority session missing: {session_name}")
            # TODO: Auto-restart logic
        else:
            logger.debug(f"✅ Priority session running: {session_name}")


async def check_done_messages():
    """
    Scan all outbox directories for UNREAD DONE messages.
    
    For each DONE message:
    1. Trigger dual_review()
    2. If APPROVED → process with pipeline.sh
    3. If REJECTED → create rejection inbox
    """
    outbox_pattern = MAILBOX_BASE / "*/outbox/*.md"
    done_files = []
    
    for outbox_dir in MAILBOX_BASE.glob("*/outbox"):
        for md_file in outbox_dir.glob("*.md"):
            # Check if UNREAD and type: done
            try:
                content = md_file.read_text()
                if 'status: UNREAD' in content and 'type: done' in content:
                    done_files.append(md_file)
            except Exception as e:
                logger.error(f"Failed to read {md_file}: {e}")
    
    logger.info(f"Found {len(done_files)} UNREAD DONE messages")
    
    for done_file in done_files:
        logger.info(f"Triggering review for: {done_file.name}")
        
        # TODO: Call dual_review() when OPENAI_API_KEY configured
        # result = await dual_review(str(done_file))
        # if result.verdict == "APPROVE":
        #     logger.info(f"✅ APPROVED: {done_file.name}")
        # else:
        #     logger.warning(f"❌ REJECTED: {done_file.name}")


async def check_stuck_sessions():
    """
    Detect stuck sessions using WorkflowStateTracker.
    
    For each stuck session:
    1. Send Enter nudge
    2. Update stuck_count
    3. Log warning
    """
    tracker = WorkflowStateTracker()
    threshold_minutes = CONFIG['stuck_detection']['threshold_minutes']
    
    stuck_sessions = tracker.get_stuck_sessions(threshold_minutes)
    
    if stuck_sessions:
        logger.info(f"Found {len(stuck_sessions)} stuck sessions")
        
        for session in stuck_sessions:
            session_name = session.session_id
            logger.warning(f"⏰ Stuck session: {session_name} (inactive for {threshold_minutes}+ min)")
            
            # Send Enter nudge
            if send_enter_to_session(session_name):
                logger.info(f"✅ Sent Enter nudge to {session_name}")
                tracker.mark_stuck(session.terminal)
            else:
                logger.error(f"❌ Failed to nudge {session_name}")


async def check_unread_inbox():
    """
    Scan all inbox directories for UNREAD messages.
    
    For each UNREAD inbox:
    1. Start terminal session if not running
    2. Update WorkflowStateTracker
    3. Log action
    """
    tracker = WorkflowStateTracker()
    
    for inbox_dir in MAILBOX_BASE.glob("*/inbox"):
        terminal = inbox_dir.parent.name
        
        for md_file in inbox_dir.glob("*.md"):
            try:
                content = md_file.read_text()
                if 'status: UNREAD' in content:
                    logger.info(f"📬 UNREAD inbox: {terminal}/{md_file.name}")
                    
                    # TODO: Start terminal session if not running
                    # session_name = f"spaceos-{terminal}"
                    # if not check_tmux_session_exists(session_name):
                    #     start_terminal_session(terminal, str(md_file))
                    
                    # Track session start
                    # message_id = extract_message_id(content)
                    # tracker.session_started(terminal, message_id, str(md_file))
                    
            except Exception as e:
                logger.error(f"Failed to read {md_file}: {e}")


# =============================================================================
# Main Scheduler Loop
# =============================================================================

async def nightwatch_cycle():
    """Execute one nightwatch cycle (all checks)"""
    logger.info("=== Nightwatch Cycle Start ===")
    
    await check_priority_sessions()
    await check_done_messages()
    await check_stuck_sessions()
    await check_unread_inbox()
    
    logger.info("=== Nightwatch Cycle Complete ===\n")


async def run_scheduler(interval: int = None):
    """
    Run nightwatch scheduler daemon.
    
    Args:
        interval: Check interval in seconds (default from config)
    """
    interval = interval or CONFIG['nightwatch']['interval_seconds']
    logger.info(f"🌙 Nightwatch Scheduler started (interval: {interval}s)")
    
    while True:
        try:
            await nightwatch_cycle()
            await asyncio.sleep(interval)
        except KeyboardInterrupt:
            logger.info("Nightwatch stopped by user")
            break
        except Exception as e:
            logger.error(f"Nightwatch cycle error: {e}", exc_info=True)
            await asyncio.sleep(interval)


# =============================================================================
# CLI Entry Point
# =============================================================================

if __name__ == "__main__":
    import sys
    
    if len(sys.argv) > 1 and sys.argv[1] == "run":
        # Run scheduler daemon
        interval = int(sys.argv[2]) if len(sys.argv) > 2 else None
        asyncio.run(run_scheduler(interval))
    else:
        # Run single cycle (for testing)
        asyncio.run(nightwatch_cycle())
