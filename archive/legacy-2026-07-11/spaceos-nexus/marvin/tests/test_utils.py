"""
Unit tests for utils.py

Tests for frontmatter parsing, verdict extraction, file path utilities, and message construction.
"""

import pytest
from pathlib import Path
from datetime import datetime
import tempfile
import os

import sys
sys.path.insert(0, str(Path(__file__).parent.parent))

from utils import (
    MessageType,
    Verdict,
    parse_frontmatter,
    extract_message_id,
    extract_terminal,
    extract_model,
    is_unread,
    is_done_message,
    extract_verdict,
    extract_verdict_with_confidence,
    get_terminal_from_path,
    construct_inbox_path,
    construct_outbox_path,
    format_timestamp,
    format_filename_timestamp,
    parse_filename_date,
    create_reject_inbox_message,
)


# =============================================================================
# Fixtures
# =============================================================================

@pytest.fixture
def sample_frontmatter_content():
    """Sample markdown file with frontmatter"""
    return """---
id: MSG-FE-042
from: conductor
to: fe
type: task
priority: high
status: UNREAD
model: sonnet
created: 2026-06-18
---

# Task Title

This is the task content.

## Details

More details here.
"""


@pytest.fixture
def sample_done_message():
    """Sample DONE message frontmatter"""
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

Task was completed successfully.
"""


@pytest.fixture
def temp_message_file(sample_frontmatter_content):
    """Create a temporary message file"""
    with tempfile.NamedTemporaryFile(mode='w', suffix='.md', delete=False) as f:
        f.write(sample_frontmatter_content)
        temp_path = f.name
    yield temp_path
    os.unlink(temp_path)


# =============================================================================
# Test Frontmatter Parsing
# =============================================================================

class TestFrontmatterParsing:
    """Test frontmatter parsing functions"""

    def test_parse_frontmatter_valid(self, temp_message_file):
        """Test parsing valid frontmatter"""
        metadata, body = parse_frontmatter(temp_message_file)

        assert metadata['id'] == 'MSG-FE-042'
        assert metadata['from'] == 'conductor'
        assert metadata['to'] == 'fe'
        assert metadata['type'] == 'task'
        assert metadata['priority'] == 'high'
        assert metadata['status'] == 'UNREAD'
        assert metadata['model'] == 'sonnet'
        assert metadata['created'] == '2026-06-18'

        assert '# Task Title' in body
        assert 'This is the task content.' in body

    def test_parse_frontmatter_no_frontmatter(self):
        """Test parsing file without frontmatter"""
        content = "# Just a title\n\nNo frontmatter here."
        with tempfile.NamedTemporaryFile(mode='w', suffix='.md', delete=False) as f:
            f.write(content)
            temp_path = f.name

        metadata, body = parse_frontmatter(temp_path)
        os.unlink(temp_path)

        assert metadata == {}
        assert body == content

    def test_extract_message_id(self, temp_message_file):
        """Test extracting message ID"""
        msg_id = extract_message_id(temp_message_file)
        assert msg_id == 'MSG-FE-042'

    def test_extract_terminal(self, temp_message_file):
        """Test extracting target terminal"""
        terminal = extract_terminal(temp_message_file)
        assert terminal == 'fe'

    def test_extract_model_default(self, temp_message_file):
        """Test extracting model with default"""
        model = extract_model(temp_message_file)
        assert model == 'sonnet'

    def test_extract_model_fallback(self):
        """Test model extraction with fallback"""
        content = """---
id: MSG-TEST-001
---

Test content.
"""
        with tempfile.NamedTemporaryFile(mode='w', suffix='.md', delete=False) as f:
            f.write(content)
            temp_path = f.name

        model = extract_model(temp_path, default='haiku')
        os.unlink(temp_path)

        assert model == 'haiku'

    def test_is_unread_true(self, temp_message_file):
        """Test UNREAD status detection"""
        assert is_unread(temp_message_file) is True

    def test_is_unread_false(self):
        """Test non-UNREAD status"""
        content = """---
status: READ
---

Test.
"""
        with tempfile.NamedTemporaryFile(mode='w', suffix='.md', delete=False) as f:
            f.write(content)
            temp_path = f.name

        result = is_unread(temp_path)
        os.unlink(temp_path)

        assert result is False

    def test_is_done_message_true(self):
        """Test DONE message detection"""
        content = """---
type: done
---

Done.
"""
        with tempfile.NamedTemporaryFile(mode='w', suffix='.md', delete=False) as f:
            f.write(content)
            temp_path = f.name

        result = is_done_message(temp_path)
        os.unlink(temp_path)

        assert result is True

    def test_is_done_message_false(self, temp_message_file):
        """Test non-DONE message"""
        assert is_done_message(temp_message_file) is False


# =============================================================================
# Test Verdict Extraction
# =============================================================================

class TestVerdictExtraction:
    """Test verdict keyword extraction"""

    def test_extract_verdict_approve(self):
        """Test APPROVE verdict extraction"""
        text = "APPROVE - All tests passed, code looks good."
        verdict = extract_verdict(text)
        assert verdict == Verdict.APPROVE

    def test_extract_verdict_reject(self):
        """Test REJECT verdict extraction"""
        text = "REJECT - Missing error handling in line 42."
        verdict = extract_verdict(text)
        assert verdict == Verdict.REJECT

    def test_extract_verdict_approve_hungarian(self):
        """Test Hungarian APPROVE keyword"""
        text = "JÓVÁHAGYVA - Minden rendben."
        verdict = extract_verdict(text)
        assert verdict == Verdict.APPROVE

    def test_extract_verdict_reject_hungarian(self):
        """Test Hungarian REJECT keyword"""
        text = "VISSZADOBVA - Hiányos dokumentáció."
        verdict = extract_verdict(text)
        assert verdict == Verdict.REJECT

    def test_extract_verdict_emoji_approve(self):
        """Test emoji APPROVE"""
        text = "✅ Everything looks great!"
        verdict = extract_verdict(text)
        assert verdict == Verdict.APPROVE

    def test_extract_verdict_emoji_reject(self):
        """Test emoji REJECT"""
        text = "❌ Critical issues found."
        verdict = extract_verdict(text)
        assert verdict == Verdict.REJECT

    def test_extract_verdict_unclear(self):
        """Test UNCLEAR verdict (no keywords)"""
        text = "This text has no clear verdict keywords."
        verdict = extract_verdict(text)
        assert verdict == Verdict.UNCLEAR

    def test_extract_verdict_custom_keywords(self):
        """Test custom keyword list"""
        text = "PASS - All good."
        verdict = extract_verdict(text, approve_keywords=['PASS'], reject_keywords=['FAIL'])
        assert verdict == Verdict.APPROVE

    def test_extract_verdict_case_insensitive(self):
        """Test case-insensitive matching"""
        text = "approve - looks good"
        verdict = extract_verdict(text)
        assert verdict == Verdict.APPROVE


class TestVerdictWithConfidence:
    """Test verdict extraction with confidence scoring"""

    def test_confidence_start_of_text(self):
        """Test confidence 1.0 - keyword at start"""
        text = "APPROVE - All tests passed."
        verdict, confidence = extract_verdict_with_confidence(text)
        assert verdict == Verdict.APPROVE
        assert confidence == 1.0

    def test_confidence_first_line(self):
        """Test confidence 0.8 - keyword in first line"""
        text = "Review result: APPROVE\n\nAll tests passed."
        verdict, confidence = extract_verdict_with_confidence(text)
        assert verdict == Verdict.APPROVE
        assert confidence == 0.8

    def test_confidence_first_paragraph(self):
        """Test confidence 0.6 - keyword in first paragraph"""
        text = "Review complete.\nDecision: APPROVE\n\nDetails here."
        verdict, confidence = extract_verdict_with_confidence(text)
        assert verdict == Verdict.APPROVE
        assert confidence == 0.6

    def test_confidence_in_text(self):
        """Test confidence 0.4 - keyword somewhere in text"""
        text = "Long review.\n\nMany details.\n\nFinal decision: APPROVE"
        verdict, confidence = extract_verdict_with_confidence(text)
        assert verdict == Verdict.APPROVE
        assert confidence == 0.4

    def test_confidence_unclear(self):
        """Test confidence 0.0 - UNCLEAR verdict"""
        text = "No clear decision here."
        verdict, confidence = extract_verdict_with_confidence(text)
        assert verdict == Verdict.UNCLEAR
        assert confidence == 0.0

    def test_confidence_reject_start(self):
        """Test REJECT with confidence 1.0"""
        text = "REJECT - Critical bugs found."
        verdict, confidence = extract_verdict_with_confidence(text)
        assert verdict == Verdict.REJECT
        assert confidence == 1.0


# =============================================================================
# Test File Path Utilities
# =============================================================================

class TestFilePathUtilities:
    """Test file path helper functions"""

    def test_get_terminal_from_path_inbox(self):
        """Test extracting terminal from inbox path"""
        path = "/opt/spaceos/docs/mailbox/fe/inbox/2026-06-18_001_test.md"
        terminal = get_terminal_from_path(path)
        assert terminal == "fe"

    def test_get_terminal_from_path_outbox(self):
        """Test extracting terminal from outbox path"""
        path = "/opt/spaceos/docs/mailbox/conductor/outbox/msg.md"
        terminal = get_terminal_from_path(path)
        assert terminal == "conductor"

    def test_get_terminal_from_path_invalid(self):
        """Test invalid path (no mailbox)"""
        path = "/some/random/path/file.md"
        terminal = get_terminal_from_path(path)
        assert terminal is None

    def test_construct_inbox_path(self):
        """Test constructing inbox path"""
        path = construct_inbox_path("fe", "test.md")
        assert str(path) == "/opt/spaceos/docs/mailbox/fe/inbox/test.md"

    def test_construct_outbox_path(self):
        """Test constructing outbox path"""
        path = construct_outbox_path("conductor", "done.md")
        assert str(path) == "/opt/spaceos/docs/mailbox/conductor/outbox/done.md"


# =============================================================================
# Test Timestamp Utilities
# =============================================================================

class TestTimestampUtilities:
    """Test timestamp formatting and parsing"""

    def test_format_timestamp(self):
        """Test timestamp formatting"""
        dt = datetime(2026, 6, 18, 14, 30, 0)
        formatted = format_timestamp(dt)
        assert formatted == "2026-06-18"

    def test_format_timestamp_default(self):
        """Test timestamp with default (now)"""
        formatted = format_timestamp()
        assert len(formatted) == 10  # YYYY-MM-DD
        assert formatted[4] == '-'
        assert formatted[7] == '-'

    def test_format_filename_timestamp(self):
        """Test filename timestamp formatting"""
        dt = datetime(2026, 6, 18, 14, 30, 0)
        formatted = format_filename_timestamp(dt)
        assert formatted == "2026-06-18_1430"

    def test_parse_filename_date(self):
        """Test parsing date from filename"""
        filename = "2026-06-18_001_test.md"
        dt = parse_filename_date(filename)
        assert dt.year == 2026
        assert dt.month == 6
        assert dt.day == 18

    def test_parse_filename_date_invalid(self):
        """Test parsing invalid filename"""
        filename = "invalid_filename.md"
        dt = parse_filename_date(filename)
        assert dt is None


# =============================================================================
# Test Message Construction
# =============================================================================

class TestMessageConstruction:
    """Test message template generation"""

    def test_create_reject_inbox_message(self):
        """Test rejection message creation"""
        message = create_reject_inbox_message(
            original_id="MSG-FE-042",
            terminal="fe",
            rejection_reason="Missing unit tests for new feature.",
            priority="high",
            model="sonnet"
        )

        assert "MSG-FE-042-REJECT" in message
        assert "from: reviewer" in message
        assert "to: fe" in message
        assert "type: task" in message
        assert "priority: high" in message
        assert "status: UNREAD" in message
        assert "model: sonnet" in message
        assert "ref: MSG-FE-042" in message
        assert "Missing unit tests for new feature." in message
        assert "# Review Failed — REJECTED" in message

    def test_create_reject_message_defaults(self):
        """Test rejection message with defaults"""
        message = create_reject_inbox_message(
            original_id="MSG-FE-042",
            terminal="fe",
            rejection_reason="Test failure."
        )

        assert "priority: high" in message  # default
        assert "model: sonnet" in message  # default


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
