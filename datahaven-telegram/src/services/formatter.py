"""
Telegram Formatter — Markdown formázás Telegram üzenetekhez
"""

from typing import Dict, Any, List, Optional
import html


def escape_markdown(text: str) -> str:
    """Escape special characters for Telegram MarkdownV2."""
    # Characters that need escaping in MarkdownV2
    special_chars = ['_', '*', '[', ']', '(', ')', '~', '`', '>', '#', '+', '-', '=', '|', '{', '}', '.', '!']
    for char in special_chars:
        text = text.replace(char, f'\\{char}')
    return text


def escape_html(text: str) -> str:
    """Escape HTML special characters."""
    return html.escape(text)


def format_message(msg: Dict[str, Any]) -> str:
    """Format a queue message for Telegram display (HTML mode)."""
    msg_type = msg.get("msg_type", "unknown")

    # Type emoji
    type_emoji = {
        "task": "\U0001F4CB",       # clipboard
        "query": "\U00002753",      # question mark
        "response": "\U0001F4AC",   # speech bubble
        "done": "\U00002705",       # green check
        "blocked": "\U0001F6AB",    # no entry
        "broadcast": "\U0001F4E2",  # loudspeaker
    }.get(msg_type, "\U0001F4E7")   # envelope

    # Priority indicator
    priority = msg.get("priority", "medium")
    priority_indicator = {
        "critical": "\U0001F534",   # red circle
        "high": "\U0001F7E0",       # orange circle
        "medium": "\U0001F7E1",     # yellow circle
        "low": "\U0001F7E2",        # green circle
    }.get(priority, "")

    subject = escape_html(msg.get("subject", "No subject"))
    from_daemon = escape_html(msg.get("from_daemon", "unknown"))
    created = msg.get("created_at", "")[:16]  # Truncate to minutes

    lines = [
        f"{type_emoji} <b>[{msg_type.upper()}]</b> {priority_indicator}",
        f"<b>From:</b> {from_daemon}",
        f"<b>Subject:</b> {subject}",
    ]

    # Add payload preview if exists
    payload = msg.get("payload")
    if payload:
        if isinstance(payload, dict):
            preview = ", ".join(f"{k}={v}" for k, v in list(payload.items())[:3])
            if len(payload) > 3:
                preview += "..."
            lines.append(f"<b>Payload:</b> <code>{escape_html(preview)}</code>")

    lines.append(f"<i>{created}</i>")

    return "\n".join(lines)


def format_message_list(messages: List[Dict[str, Any]], title: str = "Messages") -> str:
    """Format a list of messages."""
    if not messages:
        return f"\U0001F4ED <i>No {title.lower()}</i>"

    lines = [f"\U0001F4EC <b>{title}</b> ({len(messages)})\n"]

    for i, msg in enumerate(messages, 1):
        msg_type = msg.get("msg_type", "?")
        subject = escape_html(msg.get("subject", "No subject")[:50])
        from_daemon = msg.get("from_daemon", "?")
        msg_id = msg.get("id", "?")

        lines.append(f"{i}. <b>#{msg_id}</b> [{msg_type}] {subject}")
        lines.append(f"   <i>from: {from_daemon}</i>\n")

    return "\n".join(lines)


def format_knowledge_results(results: Dict[str, Any]) -> str:
    """Format knowledge search results for Telegram."""
    if "error" in results:
        return f"\U0000274C <b>Error:</b> {escape_html(results['error'])}"

    items = results.get("results", [])
    if not items:
        return "\U0001F50D <i>No results found</i>"

    lines = [f"\U0001F4DA <b>Knowledge Results</b> ({len(items)})\n"]

    for i, item in enumerate(items, 1):
        text = item.get("text", "")[:200]
        source = item.get("metadata", {}).get("source", "unknown")
        score = item.get("score", 0)

        lines.append(f"<b>{i}. {escape_html(source)}</b> (score: {score:.2f})")
        lines.append(f"<code>{escape_html(text)}...</code>\n")

    return "\n".join(lines)


def format_status(status: Dict[str, Any]) -> str:
    """Format daemon/queue status for Telegram."""
    lines = ["\U0001F4CA <b>Datahaven Status</b>\n"]

    # Queue stats
    stats = status.get("stats", {})
    total = stats.get("total", 0)
    by_status = stats.get("by_status", {})

    lines.append("<b>Queue:</b>")
    lines.append(f"  Total: {total}")
    for s, count in by_status.items():
        emoji = {
            "pending": "\U0001F7E1",
            "delivered": "\U0001F535",
            "processed": "\U00002705",
        }.get(s, "\U00002B1C")
        lines.append(f"  {emoji} {s}: {count}")

    # Daemons
    daemons = status.get("daemons", [])
    lines.append(f"\n<b>Daemons:</b> ({len(daemons)})")
    for d in daemons:
        daemon_id = d.get("id", "?")
        last_hb = d.get("last_heartbeat", "")[:16]
        lines.append(f"  \U0001F916 {daemon_id}")
        lines.append(f"     <i>last seen: {last_hb}</i>")

    return "\n".join(lines)


def format_error(error: str) -> str:
    """Format an error message."""
    return f"\U0000274C <b>Error:</b> {escape_html(error)}"


def format_success(message: str) -> str:
    """Format a success message."""
    return f"\U00002705 {escape_html(message)}"
