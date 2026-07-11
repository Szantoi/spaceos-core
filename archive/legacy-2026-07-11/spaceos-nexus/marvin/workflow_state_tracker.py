"""
WorkflowStateTracker — Session State Management for SpaceOS

Tracks terminal session lifecycle in SQLite database for stuck detection,
activity monitoring, and workflow state management.

Usage:
    tracker = WorkflowStateTracker()
    tracker.session_started("fe", "MSG-FE-042", "/path/to/inbox.md")
    tracker.update_activity("fe")
    stuck = tracker.get_stuck_sessions(threshold_minutes=10)
"""

import sqlite3
from pathlib import Path
from datetime import datetime, timedelta
from typing import List, Optional, Dict, Any
from enum import Enum
from dataclasses import dataclass


class SessionStatus(str, Enum):
    """Session lifecycle states"""
    STARTED = "STARTED"           # Session just started
    IN_PROGRESS = "IN_PROGRESS"   # Active work detected
    STUCK = "STUCK"               # No activity for threshold period
    DONE = "DONE"                 # DONE message detected
    BLOCKED = "BLOCKED"           # BLOCKED message detected
    ARCHIVED = "ARCHIVED"         # Session closed and archived


@dataclass
class SessionState:
    """Session state record"""
    session_id: str               # e.g., "spaceos-fe"
    terminal: str                 # e.g., "fe"
    status: SessionStatus
    started_at: datetime
    last_activity: datetime
    inbox_message_id: Optional[str] = None
    done_message_path: Optional[str] = None
    stuck_count: int = 0          # Times marked as stuck


class WorkflowStateTracker:
    """SQLite-based session state tracker"""
    
    def __init__(self, db_path: str = "logs/workflow_state.db"):
        """
        Initialize tracker with database path.
        
        Args:
            db_path: Path to SQLite database (relative to marvin/ dir)
        """
        self.db_path = Path(__file__).parent / db_path
        self.db_path.parent.mkdir(parents=True, exist_ok=True)
        self._init_database()
    
    def _init_database(self):
        """Create database schema if not exists"""
        with sqlite3.connect(self.db_path) as conn:
            conn.execute("""
                CREATE TABLE IF NOT EXISTS session_state (
                    session_id TEXT PRIMARY KEY,
                    terminal TEXT NOT NULL,
                    status TEXT NOT NULL,
                    started_at TIMESTAMP NOT NULL,
                    last_activity TIMESTAMP NOT NULL,
                    inbox_message_id TEXT,
                    done_message_path TEXT,
                    stuck_count INTEGER DEFAULT 0,
                    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
                )
            """)
            
            # Index for common queries
            conn.execute("""
                CREATE INDEX IF NOT EXISTS idx_terminal_status 
                ON session_state(terminal, status)
            """)
            
            conn.execute("""
                CREATE INDEX IF NOT EXISTS idx_last_activity 
                ON session_state(last_activity)
            """)
    
    def session_started(
        self,
        terminal: str,
        message_id: str,
        inbox_path: str
    ) -> None:
        """
        Record session start.

        Args:
            terminal: Terminal name (e.g., "fe")
            message_id: Inbox message ID (e.g., "MSG-FE-042")
            inbox_path: Path to inbox message file
        """
        session_id = f"spaceos-{terminal}"
        now = datetime.now().isoformat()

        with sqlite3.connect(self.db_path) as conn:
            conn.execute("""
                INSERT OR REPLACE INTO session_state
                (session_id, terminal, status, started_at, last_activity,
                 inbox_message_id, updated_at)
                VALUES (?, ?, ?, ?, ?, ?, ?)
            """, (
                session_id,
                terminal,
                SessionStatus.STARTED.value,
                now,
                now,
                message_id,
                now
            ))
    
    def update_activity(self, terminal: str) -> None:
        """
        Update last activity timestamp.

        Args:
            terminal: Terminal name
        """
        session_id = f"spaceos-{terminal}"
        now = datetime.now().isoformat()

        with sqlite3.connect(self.db_path) as conn:
            # Update to IN_PROGRESS if currently STARTED
            conn.execute("""
                UPDATE session_state
                SET status = CASE
                    WHEN status = ? THEN ?
                    ELSE status
                END,
                last_activity = ?,
                updated_at = ?
                WHERE session_id = ?
            """, (
                SessionStatus.STARTED.value,
                SessionStatus.IN_PROGRESS.value,
                now,
                now,
                session_id
            ))
    
    def mark_stuck(self, terminal: str) -> None:
        """
        Mark session as stuck.

        Args:
            terminal: Terminal name
        """
        session_id = f"spaceos-{terminal}"
        now = datetime.now().isoformat()

        with sqlite3.connect(self.db_path) as conn:
            conn.execute("""
                UPDATE session_state
                SET status = ?,
                    stuck_count = stuck_count + 1,
                    updated_at = ?
                WHERE session_id = ?
            """, (SessionStatus.STUCK.value, now, session_id))
    
    def mark_done(self, terminal: str, done_message_path: str) -> None:
        """
        Mark session as DONE.

        Args:
            terminal: Terminal name
            done_message_path: Path to DONE outbox message
        """
        session_id = f"spaceos-{terminal}"
        now = datetime.now().isoformat()

        with sqlite3.connect(self.db_path) as conn:
            conn.execute("""
                UPDATE session_state
                SET status = ?,
                    done_message_path = ?,
                    updated_at = ?
                WHERE session_id = ?
            """, (SessionStatus.DONE.value, done_message_path, now, session_id))
    
    def get_stuck_sessions(self, threshold_minutes: int = 10) -> List[SessionState]:
        """
        Get sessions stuck for longer than threshold.

        Args:
            threshold_minutes: Inactivity threshold in minutes

        Returns:
            List of stuck SessionState objects
        """
        threshold = (datetime.now() - timedelta(minutes=threshold_minutes)).isoformat()

        with sqlite3.connect(self.db_path) as conn:
            conn.row_factory = sqlite3.Row
            cursor = conn.execute("""
                SELECT * FROM session_state
                WHERE status IN (?, ?)
                AND last_activity < ?
                ORDER BY last_activity ASC
            """, (
                SessionStatus.STARTED.value,
                SessionStatus.IN_PROGRESS.value,
                threshold
            ))

            return [self._row_to_state(row) for row in cursor.fetchall()]
    
    def get_session(self, terminal: str) -> Optional[SessionState]:
        """
        Get current session state for terminal.
        
        Args:
            terminal: Terminal name
            
        Returns:
            SessionState or None if not found
        """
        session_id = f"spaceos-{terminal}"
        
        with sqlite3.connect(self.db_path) as conn:
            conn.row_factory = sqlite3.Row
            cursor = conn.execute(
                "SELECT * FROM session_state WHERE session_id = ?",
                (session_id,)
            )
            row = cursor.fetchone()
            return self._row_to_state(row) if row else None
    
    def _row_to_state(self, row: sqlite3.Row) -> SessionState:
        """Convert database row to SessionState"""
        return SessionState(
            session_id=row['session_id'],
            terminal=row['terminal'],
            status=SessionStatus(row['status']),
            started_at=datetime.fromisoformat(row['started_at']),
            last_activity=datetime.fromisoformat(row['last_activity']),
            inbox_message_id=row['inbox_message_id'],
            done_message_path=row['done_message_path'],
            stuck_count=row['stuck_count']
        )


# Example usage
if __name__ == "__main__":
    tracker = WorkflowStateTracker()
    
    # Simulate session lifecycle
    tracker.session_started("fe", "MSG-FE-042", "/path/to/inbox.md")
    print("✅ Session started: fe")
    
    # Update activity
    tracker.update_activity("fe")
    print("✅ Activity updated: fe")
    
    # Check stuck sessions
    stuck = tracker.get_stuck_sessions(threshold_minutes=0)
    print(f"Stuck sessions: {len(stuck)}")
    
    # Get session state
    state = tracker.get_session("fe")
    if state:
        print(f"Session state: {state.status.value}")
        print(f"Last activity: {state.last_activity}")
