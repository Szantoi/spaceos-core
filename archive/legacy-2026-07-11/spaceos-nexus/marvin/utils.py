"""
Utility Functions for SpaceOS Marvin

Common helper functions used across planning, reviewing, and monitoring tasks.
"""

import re
from pathlib import Path
from typing import Dict, Any, Optional, Tuple
from datetime import datetime
from enum import Enum


# =============================================================================
# Frontmatter Parsing
# =============================================================================

class MessageType(str, Enum):
    """Inbox/Outbox message types"""
    TASK = "task"
    DONE = "done"
    BLOCKED = "blocked"
    QUESTION = "question"
    ESCALATION = "escalation"


def parse_frontmatter(file_path: str) -> Tuple[Dict[str, Any], str]:
    """
    Parse YAML frontmatter from inbox/outbox message file.
    
    Args:
        file_path: Path to markdown file with YAML frontmatter
        
    Returns:
        Tuple of (metadata dict, content string)
        
    Example frontmatter:
        ---
        id: MSG-FE-042
        from: conductor
        to: fe
        type: task
        priority: high
        status: UNREAD
        model: sonnet
        created: 2026-06-18
        ---
    """
    content = Path(file_path).read_text()
    
    # Match frontmatter between --- delimiters
    frontmatter_pattern = r'^---\s*\n(.*?)\n---\s*\n(.*)$'
    match = re.match(frontmatter_pattern, content, re.DOTALL)
    
    if not match:
        return {}, content
    
    frontmatter_text, body = match.groups()
    
    # Parse YAML-like frontmatter (simple key: value format)
    metadata = {}
    for line in frontmatter_text.strip().split('\n'):
        if ':' in line:
            key, value = line.split(':', 1)
            metadata[key.strip()] = value.strip()
    
    return metadata, body.strip()


def extract_message_id(file_path: str) -> Optional[str]:
    """Extract message ID from frontmatter"""
    metadata, _ = parse_frontmatter(file_path)
    return metadata.get('id')


def extract_terminal(file_path: str) -> Optional[str]:
    """Extract target terminal from frontmatter"""
    metadata, _ = parse_frontmatter(file_path)
    return metadata.get('to')


def extract_model(file_path: str, default: str = 'sonnet') -> str:
    """Extract model from frontmatter with fallback"""
    metadata, _ = parse_frontmatter(file_path)
    return metadata.get('model', default)


def is_unread(file_path: str) -> bool:
    """Check if message status is UNREAD"""
    metadata, _ = parse_frontmatter(file_path)
    return metadata.get('status') == 'UNREAD'


def is_done_message(file_path: str) -> bool:
    """Check if message type is 'done'"""
    metadata, _ = parse_frontmatter(file_path)
    return metadata.get('type') == 'done'


# =============================================================================
# Verdict Keyword Extraction
# =============================================================================

class Verdict(str, Enum):
    """Review verdict types"""
    APPROVE = "APPROVE"
    REJECT = "REJECT"
    UNCLEAR = "UNCLEAR"


def extract_verdict(
    review_text: str,
    approve_keywords: list = None,
    reject_keywords: list = None
) -> Verdict:
    """
    Extract verdict from review text using keyword detection.
    
    Args:
        review_text: Review output from Marvin Agent
        approve_keywords: List of keywords indicating approval
        reject_keywords: List of keywords indicating rejection
        
    Returns:
        Verdict enum (APPROVE, REJECT, or UNCLEAR)
        
    Example:
        >>> extract_verdict("APPROVE - All tests passed")
        Verdict.APPROVE
        
        >>> extract_verdict("REJECT - Missing documentation")
        Verdict.REJECT
    """
    if approve_keywords is None:
        approve_keywords = ['APPROVE', 'APPROVED', 'JÓVÁHAGYVA', '✅']
    
    if reject_keywords is None:
        reject_keywords = ['REJECT', 'REJECTED', 'VISSZADOBVA', '❌']
    
    # Normalize text (uppercase, strip)
    normalized = review_text.upper().strip()
    
    # Check for approve keywords
    for keyword in approve_keywords:
        if keyword.upper() in normalized:
            return Verdict.APPROVE
    
    # Check for reject keywords
    for keyword in reject_keywords:
        if keyword.upper() in normalized:
            return Verdict.REJECT
    
    # No clear verdict found
    return Verdict.UNCLEAR


def extract_verdict_with_confidence(
    review_text: str,
    approve_keywords: list = None,
    reject_keywords: list = None
) -> Tuple[Verdict, float]:
    """
    Extract verdict with confidence score.

    Returns:
        Tuple of (Verdict, confidence_score)

    Confidence:
        1.0 - Keyword found at start of text
        0.8 - Keyword found in first line
        0.6 - Keyword found in first paragraph
        0.4 - Keyword found in text
        0.0 - No keyword found (UNCLEAR)
    """
    # Set defaults if None
    if approve_keywords is None:
        approve_keywords = ['APPROVE', 'APPROVED', 'JÓVÁHAGYVA', '✅']

    if reject_keywords is None:
        reject_keywords = ['REJECT', 'REJECTED', 'VISSZADOBVA', '❌']

    verdict = extract_verdict(review_text, approve_keywords, reject_keywords)
    
    if verdict == Verdict.UNCLEAR:
        return verdict, 0.0
    
    # Determine confidence based on keyword position
    normalized = review_text.upper().strip()
    keywords = approve_keywords if verdict == Verdict.APPROVE else reject_keywords
    
    for keyword in keywords:
        keyword_upper = keyword.upper()
        
        # Check position
        if normalized.startswith(keyword_upper):
            return verdict, 1.0
        
        first_line = normalized.split('\n')[0]
        if keyword_upper in first_line:
            return verdict, 0.8
        
        first_para = normalized.split('\n\n')[0]
        if keyword_upper in first_para:
            return verdict, 0.6
        
        if keyword_upper in normalized:
            return verdict, 0.4
    
    return verdict, 0.0


# =============================================================================
# File Path Utilities
# =============================================================================

def get_terminal_from_path(file_path: str) -> Optional[str]:
    """
    Extract terminal name from mailbox path.
    
    Example:
        /opt/spaceos/docs/mailbox/fe/inbox/msg.md → "fe"
    """
    path = Path(file_path)
    parts = path.parts
    
    try:
        mailbox_index = parts.index('mailbox')
        return parts[mailbox_index + 1]
    except (ValueError, IndexError):
        return None


def construct_inbox_path(terminal: str, filename: str) -> Path:
    """Construct inbox path for terminal"""
    return Path(f"/opt/spaceos/docs/mailbox/{terminal}/inbox/{filename}")


def construct_outbox_path(terminal: str, filename: str) -> Path:
    """Construct outbox path for terminal"""
    return Path(f"/opt/spaceos/docs/mailbox/{terminal}/outbox/{filename}")


# =============================================================================
# Timestamp Utilities
# =============================================================================

def format_timestamp(dt: datetime = None) -> str:
    """Format datetime for message creation"""
    if dt is None:
        dt = datetime.now()
    return dt.strftime('%Y-%m-%d')


def format_filename_timestamp(dt: datetime = None) -> str:
    """Format datetime for filename (YYYY-MM-DD_HHmm)"""
    if dt is None:
        dt = datetime.now()
    return dt.strftime('%Y-%m-%d_%H%M')


def parse_filename_date(filename: str) -> Optional[datetime]:
    """
    Parse date from filename.
    
    Example:
        2026-06-18_001_description.md → datetime(2026, 6, 18)
    """
    pattern = r'(\d{4}-\d{2}-\d{2})'
    match = re.match(pattern, filename)
    
    if match:
        date_str = match.group(1)
        return datetime.strptime(date_str, '%Y-%m-%d')
    
    return None


# =============================================================================
# Message Construction
# =============================================================================

def create_reject_inbox_message(
    original_id: str,
    terminal: str,
    rejection_reason: str,
    priority: str = "high",
    model: str = "sonnet"
) -> str:
    """
    Create rejection inbox message content.
    
    Args:
        original_id: Original DONE message ID
        terminal: Target terminal
        rejection_reason: Why the review was rejected
        priority: Message priority
        model: Model to use for rework
        
    Returns:
        Complete markdown message with frontmatter
    """
    created = format_timestamp()
    
    return f"""---
id: {original_id}-REJECT
from: reviewer
to: {terminal}
type: task
priority: {priority}
status: UNREAD
model: {model}
ref: {original_id}
created: {created}
---

# Review Failed — REJECTED

**Original Task:** {original_id}

## Rejection Reason

{rejection_reason}

## Required Changes

Please address the issues above and resubmit.

---

**Reviewer:** Marvin Dual Review System
**Date:** {created}
"""


# Example usage
if __name__ == "__main__":
    # Test frontmatter parsing
    test_content = """---
id: MSG-FE-042
from: conductor
to: fe
type: task
priority: high
status: UNREAD
---

# Test Message

This is test content.
"""
    
    Path("/tmp/test_message.md").write_text(test_content)
    metadata, body = parse_frontmatter("/tmp/test_message.md")
    
    print("Metadata:", metadata)
    print("Body:", body[:50])
    
    # Test verdict extraction
    review1 = "APPROVE - All tests passing, code looks good."
    review2 = "REJECT - Missing error handling in line 42."
    
    print(f"\nVerdict 1: {extract_verdict(review1)}")
    print(f"Verdict 2: {extract_verdict(review2)}")
    
    verdict, confidence = extract_verdict_with_confidence(review1)
    print(f"Verdict with confidence: {verdict} ({confidence})")
