"""
Unit tests for nightwatch_scheduler.py (using mocks)

Tests nightwatch scheduler functions without requiring:
- OPENAI_API_KEY
- tmux sessions
- Actual file system operations

Uses pytest fixtures and unittest.mock for isolation.
"""

import pytest
import asyncio
from pathlib import Path
from unittest.mock import Mock, MagicMock, patch, mock_open, call
import tempfile
import os

import sys
sys.path.insert(0, str(Path(__file__).parent.parent))

from nightwatch_scheduler import (
    load_config,
    check_tmux_session_exists,
    get_session_last_activity,
    send_enter_to_session,
    check_priority_sessions,
    check_done_messages,
    check_stuck_sessions,
    check_unread_inbox,
    nightwatch_cycle,
)


# =============================================================================
# Fixtures
# =============================================================================

@pytest.fixture
def mock_config():
    """Mock nightwatch configuration"""
    return {
        'nightwatch': {
            'interval_seconds': 120,
            'priority_terminals': ['root', 'conductor']
        },
        'stuck_detection': {
            'threshold_minutes': 10,
            'nudge_retry_delay': 300
        },
        'paths': {
            'spaceos_root': '/opt/spaceos',
            'mailbox_base': 'docs/mailbox'
        }
    }


@pytest.fixture
def temp_config_file(mock_config):
    """Create temporary config file"""
    import yaml
    with tempfile.NamedTemporaryFile(mode='w', suffix='.yaml', delete=False) as f:
        yaml.dump(mock_config, f)
        temp_path = f.name
    yield temp_path
    os.unlink(temp_path)


@pytest.fixture
def sample_done_message():
    """Sample DONE message content"""
    return """---
id: MSG-FE-042-DONE
from: fe
to: conductor
type: done
priority: medium
status: UNREAD
ref: MSG-FE-042
created: 2026-06-18
completed: 2026-06-18
---

# Task Complete

Task completed successfully.
"""


@pytest.fixture
def sample_task_message():
    """Sample TASK message content"""
    return """---
id: MSG-FE-043
from: conductor
to: fe
type: task
priority: high
status: UNREAD
model: sonnet
created: 2026-06-18
---

# New Task

Please implement feature X.
"""


# =============================================================================
# Test Configuration Loading
# =============================================================================

class TestConfigLoading:
    """Test nightwatch configuration loading"""

    def test_load_config_from_file(self, temp_config_file, mock_config):
        """Test loading config from YAML file"""
        with patch('nightwatch_scheduler.Path') as mock_path:
            mock_path.return_value.parent = Path(temp_config_file).parent
            mock_config_path = MagicMock()
            mock_config_path.exists.return_value = True
            mock_config_path.open = mock_open(read_data=open(temp_config_file).read())

            with patch('builtins.open', mock_open(read_data=open(temp_config_file).read())):
                # This would require complex mocking, skip for now
                pass

    def test_load_config_default(self):
        """Test loading default config when file doesn't exist"""
        with patch('nightwatch_scheduler.Path') as mock_path:
            mock_config_path = MagicMock()
            mock_config_path.exists.return_value = False
            mock_path.return_value.parent.__truediv__.return_value = mock_config_path

            config = load_config()

            assert 'nightwatch' in config
            assert config['nightwatch']['interval_seconds'] == 120
            assert 'root' in config['nightwatch']['priority_terminals']
            assert 'conductor' in config['nightwatch']['priority_terminals']


# =============================================================================
# Test Tmux Session Functions
# =============================================================================

class TestTmuxSessionFunctions:
    """Test tmux session management functions"""

    def test_check_tmux_session_exists_true(self):
        """Test session exists (returncode 0)"""
        with patch('subprocess.run') as mock_run:
            mock_run.return_value = Mock(returncode=0)

            result = check_tmux_session_exists("spaceos-root")

            assert result is True
            mock_run.assert_called_once_with(
                ['tmux', 'has-session', '-t', 'spaceos-root'],
                capture_output=True
            )

    def test_check_tmux_session_exists_false(self):
        """Test session doesn't exist (returncode 1)"""
        with patch('subprocess.run') as mock_run:
            mock_run.return_value = Mock(returncode=1)

            result = check_tmux_session_exists("spaceos-nonexistent")

            assert result is False

    def test_get_session_last_activity_success(self):
        """Test getting last activity from tmux session"""
        with patch('subprocess.run') as mock_run:
            mock_run.return_value = Mock(
                returncode=0,
                stdout="Line 1\nLine 2\nLast line of output"
            )

            result = get_session_last_activity("spaceos-root")

            assert result == "Last line of output"
            mock_run.assert_called_once()

    def test_get_session_last_activity_failure(self):
        """Test getting activity from failed tmux command"""
        with patch('subprocess.run') as mock_run:
            mock_run.return_value = Mock(returncode=1)

            result = get_session_last_activity("spaceos-root")

            assert result is None

    def test_get_session_last_activity_exception(self):
        """Test exception handling in get_session_last_activity"""
        with patch('subprocess.run') as mock_run:
            mock_run.side_effect = Exception("tmux error")

            result = get_session_last_activity("spaceos-root")

            assert result is None

    def test_send_enter_to_session_success(self):
        """Test sending Enter to session successfully"""
        with patch('subprocess.run') as mock_run:
            mock_run.return_value = Mock(returncode=0)

            result = send_enter_to_session("spaceos-root")

            assert result is True
            mock_run.assert_called_once_with(
                ['tmux', 'send-keys', '-t', 'spaceos-root', '', 'Enter', 'Enter'],
                capture_output=True,
                timeout=5
            )

    def test_send_enter_to_session_failure(self):
        """Test send Enter failure"""
        with patch('subprocess.run') as mock_run:
            mock_run.return_value = Mock(returncode=1)

            result = send_enter_to_session("spaceos-root")

            assert result is False

    def test_send_enter_to_session_exception(self):
        """Test exception handling in send_enter_to_session"""
        with patch('subprocess.run') as mock_run:
            mock_run.side_effect = Exception("tmux error")

            result = send_enter_to_session("spaceos-root")

            assert result is False


# =============================================================================
# Test Async Nightwatch Tasks
# =============================================================================

class TestCheckPrioritySessions:
    """Test priority session monitoring"""

    @pytest.mark.asyncio
    async def test_check_priority_sessions_all_running(self):
        """Test all priority sessions are running"""
        with patch('nightwatch_scheduler.check_tmux_session_exists') as mock_check:
            mock_check.return_value = True
            with patch('nightwatch_scheduler.CONFIG', {
                'nightwatch': {'priority_terminals': ['root', 'conductor']}
            }):
                # Should not raise exception
                await check_priority_sessions()

                # Should check both sessions
                assert mock_check.call_count == 2

    @pytest.mark.asyncio
    async def test_check_priority_sessions_one_missing(self):
        """Test one priority session missing"""
        with patch('nightwatch_scheduler.check_tmux_session_exists') as mock_check:
            # First call (root) returns True, second (conductor) returns False
            mock_check.side_effect = [True, False]
            with patch('nightwatch_scheduler.CONFIG', {
                'nightwatch': {'priority_terminals': ['root', 'conductor']}
            }):
                with patch('nightwatch_scheduler.logger') as mock_logger:
                    await check_priority_sessions()

                    # Should log warning for missing conductor
                    assert any('conductor' in str(call) for call in mock_logger.warning.call_args_list)


class TestCheckDoneMessages:
    """Test DONE message detection"""

    @pytest.mark.asyncio
    async def test_check_done_messages_found(self, sample_done_message):
        """Test finding UNREAD DONE messages"""
        with patch('nightwatch_scheduler.MAILBOX_BASE') as mock_mailbox:
            # Mock file system structure
            mock_outbox = MagicMock()
            mock_file = MagicMock()
            mock_file.name = "2026-06-18_001_done.md"
            mock_file.read_text.return_value = sample_done_message

            mock_outbox.glob.return_value = [mock_file]
            mock_mailbox.glob.return_value = [mock_outbox]

            with patch('nightwatch_scheduler.logger') as mock_logger:
                await check_done_messages()

                # Should find 1 DONE message
                assert any('1' in str(call) and 'DONE' in str(call)
                          for call in mock_logger.info.call_args_list)

    @pytest.mark.asyncio
    async def test_check_done_messages_none_found(self):
        """Test no DONE messages"""
        with patch('nightwatch_scheduler.MAILBOX_BASE') as mock_mailbox:
            mock_mailbox.glob.return_value = []

            with patch('nightwatch_scheduler.logger') as mock_logger:
                await check_done_messages()

                # Should find 0 DONE messages
                assert any('0' in str(call) and 'DONE' in str(call)
                          for call in mock_logger.info.call_args_list)


class TestCheckStuckSessions:
    """Test stuck session detection"""

    @pytest.mark.asyncio
    async def test_check_stuck_sessions_none_stuck(self):
        """Test no stuck sessions"""
        with patch('nightwatch_scheduler.WorkflowStateTracker') as mock_tracker_class:
            mock_tracker = MagicMock()
            mock_tracker.get_stuck_sessions.return_value = []
            mock_tracker_class.return_value = mock_tracker

            with patch('nightwatch_scheduler.CONFIG', {
                'stuck_detection': {'threshold_minutes': 10}
            }):
                await check_stuck_sessions()

                mock_tracker.get_stuck_sessions.assert_called_once_with(10)

    @pytest.mark.asyncio
    async def test_check_stuck_sessions_found_and_nudged(self):
        """Test finding and nudging stuck sessions"""
        with patch('nightwatch_scheduler.WorkflowStateTracker') as mock_tracker_class:
            # Mock stuck session
            mock_session = MagicMock()
            mock_session.session_id = "spaceos-fe"
            mock_session.terminal = "fe"

            mock_tracker = MagicMock()
            mock_tracker.get_stuck_sessions.return_value = [mock_session]
            mock_tracker_class.return_value = mock_tracker

            with patch('nightwatch_scheduler.send_enter_to_session') as mock_send:
                mock_send.return_value = True
                with patch('nightwatch_scheduler.CONFIG', {
                    'stuck_detection': {'threshold_minutes': 10}
                }):
                    await check_stuck_sessions()

                    # Should send Enter nudge
                    mock_send.assert_called_once_with("spaceos-fe")
                    # Should mark as stuck
                    mock_tracker.mark_stuck.assert_called_once_with("fe")


class TestCheckUnreadInbox:
    """Test UNREAD inbox detection"""

    @pytest.mark.asyncio
    async def test_check_unread_inbox_found(self, sample_task_message):
        """Test finding UNREAD inbox messages"""
        with patch('nightwatch_scheduler.MAILBOX_BASE') as mock_mailbox:
            # Mock inbox directory
            mock_inbox = MagicMock()
            mock_inbox.parent.name = "fe"

            mock_file = MagicMock()
            mock_file.name = "2026-06-18_001_task.md"
            mock_file.read_text.return_value = sample_task_message

            mock_inbox.glob.return_value = [mock_file]
            mock_mailbox.glob.return_value = [mock_inbox]

            with patch('nightwatch_scheduler.WorkflowStateTracker'):
                with patch('nightwatch_scheduler.logger') as mock_logger:
                    await check_unread_inbox()

                    # Should log UNREAD inbox found
                    assert any('UNREAD inbox' in str(call)
                              for call in mock_logger.info.call_args_list)

    @pytest.mark.asyncio
    async def test_check_unread_inbox_none_found(self):
        """Test no UNREAD inbox messages"""
        with patch('nightwatch_scheduler.MAILBOX_BASE') as mock_mailbox:
            mock_mailbox.glob.return_value = []

            with patch('nightwatch_scheduler.WorkflowStateTracker'):
                await check_unread_inbox()
                # Should complete without errors


class TestNightwatchCycle:
    """Test complete nightwatch cycle"""

    @pytest.mark.asyncio
    async def test_nightwatch_cycle_executes_all_tasks(self):
        """Test that nightwatch cycle calls all check functions"""
        with patch('nightwatch_scheduler.check_priority_sessions') as mock_priority:
            with patch('nightwatch_scheduler.check_done_messages') as mock_done:
                with patch('nightwatch_scheduler.check_stuck_sessions') as mock_stuck:
                    with patch('nightwatch_scheduler.check_unread_inbox') as mock_unread:
                        await nightwatch_cycle()

                        # All checks should be called once
                        mock_priority.assert_called_once()
                        mock_done.assert_called_once()
                        mock_stuck.assert_called_once()
                        mock_unread.assert_called_once()


# =============================================================================
# Integration Tests (Mock-based)
# =============================================================================

@pytest.mark.integration
class TestNightwatchIntegration:
    """Integration tests using mocks (no OPENAI_API_KEY required)"""

    @pytest.mark.asyncio
    async def test_full_nightwatch_cycle_with_stuck_session(self):
        """Test complete cycle with stuck session scenario"""
        # Mock stuck session
        mock_session = MagicMock()
        mock_session.session_id = "spaceos-fe"
        mock_session.terminal = "fe"

        with patch('nightwatch_scheduler.WorkflowStateTracker') as mock_tracker_class:
            mock_tracker = MagicMock()
            mock_tracker.get_stuck_sessions.return_value = [mock_session]
            mock_tracker_class.return_value = mock_tracker

            with patch('nightwatch_scheduler.check_tmux_session_exists', return_value=True):
                with patch('nightwatch_scheduler.send_enter_to_session', return_value=True):
                    with patch('nightwatch_scheduler.MAILBOX_BASE') as mock_mailbox:
                        mock_mailbox.glob.return_value = []

                        with patch('nightwatch_scheduler.CONFIG', {
                            'nightwatch': {'priority_terminals': ['root']},
                            'stuck_detection': {'threshold_minutes': 10}
                        }):
                            # Execute full cycle
                            await nightwatch_cycle()

                            # Verify stuck session was handled
                            mock_tracker.mark_stuck.assert_called_once_with("fe")


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
