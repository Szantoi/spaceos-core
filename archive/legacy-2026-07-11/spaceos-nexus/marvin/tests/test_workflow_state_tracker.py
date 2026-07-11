"""
Unit tests for WorkflowStateTracker

Tests session state management, FSM transitions, and stuck detection.
"""

import pytest
import tempfile
from pathlib import Path
from datetime import datetime, timedelta
import sys

# Add parent directory to path for imports
sys.path.insert(0, str(Path(__file__).parent.parent))

from workflow_state_tracker import (
    WorkflowStateTracker,
    SessionStatus,
    SessionState
)


@pytest.fixture
def tracker():
    """Create tracker with temporary database"""
    with tempfile.NamedTemporaryFile(suffix='.db', delete=False) as f:
        db_path = f.name
    
    tracker = WorkflowStateTracker(db_path=db_path)
    yield tracker
    
    # Cleanup
    Path(db_path).unlink(missing_ok=True)


class TestSessionLifecycle:
    """Test session state transitions"""
    
    def test_session_started(self, tracker):
        """Test session start creates record"""
        tracker.session_started("fe", "MSG-FE-001", "/path/to/inbox.md")
        
        state = tracker.get_session("fe")
        assert state is not None
        assert state.terminal == "fe"
        assert state.status == SessionStatus.STARTED
        assert state.inbox_message_id == "MSG-FE-001"
        assert state.stuck_count == 0
    
    def test_update_activity_transitions_to_in_progress(self, tracker):
        """Test activity update changes STARTED → IN_PROGRESS"""
        tracker.session_started("fe", "MSG-FE-001", "/path/to/inbox.md")
        tracker.update_activity("fe")
        
        state = tracker.get_session("fe")
        assert state.status == SessionStatus.IN_PROGRESS
    
    def test_update_activity_updates_timestamp(self, tracker):
        """Test activity update refreshes last_activity"""
        tracker.session_started("fe", "MSG-FE-001", "/path/to/inbox.md")
        
        state1 = tracker.get_session("fe")
        original_time = state1.last_activity
        
        # Update activity
        tracker.update_activity("fe")
        
        state2 = tracker.get_session("fe")
        assert state2.last_activity > original_time
    
    def test_mark_stuck_increments_counter(self, tracker):
        """Test stuck marking increments stuck_count"""
        tracker.session_started("fe", "MSG-FE-001", "/path/to/inbox.md")
        
        tracker.mark_stuck("fe")
        state1 = tracker.get_session("fe")
        assert state1.status == SessionStatus.STUCK
        assert state1.stuck_count == 1
        
        tracker.mark_stuck("fe")
        state2 = tracker.get_session("fe")
        assert state2.stuck_count == 2
    
    def test_mark_done(self, tracker):
        """Test marking session as DONE"""
        tracker.session_started("fe", "MSG-FE-001", "/path/to/inbox.md")
        tracker.mark_done("fe", "/path/to/outbox/done.md")
        
        state = tracker.get_session("fe")
        assert state.status == SessionStatus.DONE
        assert state.done_message_path == "/path/to/outbox/done.md"


class TestStuckDetection:
    """Test stuck session detection logic"""
    
    def test_get_stuck_sessions_empty(self, tracker):
        """Test no stuck sessions initially"""
        stuck = tracker.get_stuck_sessions(threshold_minutes=10)
        assert len(stuck) == 0
    
    def test_get_stuck_sessions_active_not_stuck(self, tracker):
        """Test active session not marked as stuck"""
        tracker.session_started("fe", "MSG-FE-001", "/path/to/inbox.md")
        tracker.update_activity("fe")
        
        stuck = tracker.get_stuck_sessions(threshold_minutes=10)
        assert len(stuck) == 0
    
    def test_get_stuck_sessions_detects_inactive(self, tracker):
        """Test stuck detection for inactive session"""
        # This test would require mocking time or using a very short threshold
        # For now, we test the query logic works
        tracker.session_started("fe", "MSG-FE-001", "/path/to/inbox.md")
        
        # Query with 0 minute threshold (everything is stuck)
        stuck = tracker.get_stuck_sessions(threshold_minutes=0)
        assert len(stuck) == 1
        assert stuck[0].terminal == "fe"
    
    def test_done_sessions_not_stuck(self, tracker):
        """Test DONE sessions not returned as stuck"""
        tracker.session_started("fe", "MSG-FE-001", "/path/to/inbox.md")
        tracker.mark_done("fe", "/path/to/done.md")
        
        stuck = tracker.get_stuck_sessions(threshold_minutes=0)
        assert len(stuck) == 0


class TestMultipleSessions:
    """Test concurrent session tracking"""
    
    def test_multiple_sessions_independent(self, tracker):
        """Test multiple terminals tracked independently"""
        tracker.session_started("fe", "MSG-FE-001", "/inbox1.md")
        tracker.session_started("kernel", "MSG-KERNEL-001", "/inbox2.md")
        tracker.session_started("orch", "MSG-ORCH-001", "/inbox3.md")
        
        fe_state = tracker.get_session("fe")
        kernel_state = tracker.get_session("kernel")
        orch_state = tracker.get_session("orch")
        
        assert fe_state.inbox_message_id == "MSG-FE-001"
        assert kernel_state.inbox_message_id == "MSG-KERNEL-001"
        assert orch_state.inbox_message_id == "MSG-ORCH-001"
    
    def test_stuck_detection_multiple_sessions(self, tracker):
        """Test stuck detection across multiple sessions"""
        tracker.session_started("fe", "MSG-FE-001", "/inbox1.md")
        tracker.session_started("kernel", "MSG-KERNEL-001", "/inbox2.md")
        
        # Mark kernel as DONE (not stuck)
        tracker.mark_done("kernel", "/done.md")
        
        # Both sessions inactive, but only fe should be stuck
        stuck = tracker.get_stuck_sessions(threshold_minutes=0)
        assert len(stuck) == 1
        assert stuck[0].terminal == "fe"


class TestEdgeCases:
    """Test edge cases and error handling"""
    
    def test_get_session_nonexistent(self, tracker):
        """Test querying non-existent session returns None"""
        state = tracker.get_session("nonexistent")
        assert state is None
    
    def test_update_activity_nonexistent_session(self, tracker):
        """Test updating non-existent session (should not crash)"""
        # This should not raise an exception
        tracker.update_activity("nonexistent")
        
        # Session should still not exist
        state = tracker.get_session("nonexistent")
        assert state is None
    
    def test_session_id_format(self, tracker):
        """Test session_id format is correct"""
        tracker.session_started("fe", "MSG-FE-001", "/inbox.md")
        
        state = tracker.get_session("fe")
        assert state.session_id == "spaceos-fe"


# Integration test
def test_full_workflow_integration(tracker):
    """Test complete session lifecycle"""
    # 1. Session starts
    tracker.session_started("fe", "MSG-FE-042", "/inbox/042.md")
    
    state = tracker.get_session("fe")
    assert state.status == SessionStatus.STARTED
    
    # 2. Work begins
    tracker.update_activity("fe")
    state = tracker.get_session("fe")
    assert state.status == SessionStatus.IN_PROGRESS
    
    # 3. More activity
    tracker.update_activity("fe")
    
    # 4. Session completes
    tracker.mark_done("fe", "/outbox/042-done.md")
    state = tracker.get_session("fe")
    assert state.status == SessionStatus.DONE
    assert state.done_message_path == "/outbox/042-done.md"
    
    # 5. Not stuck anymore
    stuck = tracker.get_stuck_sessions(threshold_minutes=0)
    assert len(stuck) == 0
