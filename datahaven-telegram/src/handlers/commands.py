"""
Telegram Command Handlers
"""

import os
import logging
from telegram import Update
from telegram.ext import ContextTypes
from telegram.constants import ParseMode

from services.datahaven import DatahavenClient
from services.formatter import (
    format_message_list,
    format_knowledge_results,
    format_status,
    format_success,
    format_error,
    escape_html,
)

logger = logging.getLogger(__name__)

# Authorized users
ALLOWED_USERS = set(
    int(uid.strip())
    for uid in os.getenv("ALLOWED_USERS", "").split(",")
    if uid.strip()
)

ADMIN_USERS = set(
    int(uid.strip())
    for uid in os.getenv("ADMIN_USERS", "").split(",")
    if uid.strip()
)


def is_authorized(user_id: int) -> bool:
    """Check if user is authorized."""
    return user_id in ALLOWED_USERS or user_id in ADMIN_USERS


def is_admin(user_id: int) -> bool:
    """Check if user is admin."""
    return user_id in ADMIN_USERS


def get_client(context: ContextTypes.DEFAULT_TYPE) -> DatahavenClient:
    """Get or create DatahavenClient from context."""
    if "datahaven" not in context.bot_data:
        context.bot_data["datahaven"] = DatahavenClient(
            datahaven_home=os.getenv("DATAHAVEN_HOME", "/opt/spaceos/datahaven"),
            knowledge_url=os.getenv("KNOWLEDGE_URL", "http://localhost:3456"),
            daemon_id=os.getenv("BOT_DAEMON_ID", "telegram-bot"),
        )
    return context.bot_data["datahaven"]


# =============================================================================
# Command Handlers
# =============================================================================


async def start_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    """Handle /start command."""
    user = update.effective_user
    user_id = user.id

    if not is_authorized(user_id):
        await update.message.reply_text(
            f"Unauthorized. Your user ID: {user_id}\n"
            "Contact admin to get access.",
        )
        logger.warning(f"Unauthorized access attempt from {user_id} ({user.username})")
        return

    await update.message.reply_html(
        f"Welcome <b>{escape_html(user.first_name)}</b>!\n\n"
        "I'm the Datahaven Telegram Gateway.\n\n"
        "<b>Commands:</b>\n"
        "/help - Show commands\n"
        "/ask [question] - Search knowledge base\n"
        "/status - Queue and daemon status\n"
        "/inbox - Check your messages\n"
        "/send [daemon] [message] - Send message\n"
        "/tasks - List active tasks\n"
    )


async def help_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    """Handle /help command."""
    if not is_authorized(update.effective_user.id):
        return

    await update.message.reply_html(
        "<b>Datahaven Telegram Bot</b>\n\n"
        "<b>Knowledge:</b>\n"
        "/ask [question] - Search the knowledge base\n\n"
        "<b>Messaging:</b>\n"
        "/inbox - Check incoming messages\n"
        "/send [daemon] [subject] - Send message\n"
        "/ack [id] - Acknowledge message\n\n"
        "<b>Status:</b>\n"
        "/status - Queue and daemon overview\n"
        "/tasks - Active tasks\n"
        "/daemons - Online daemons\n\n"
        "<b>Examples:</b>\n"
        "<code>/ask How does JWT auth work?</code>\n"
        "<code>/send kernel Check bug #123</code>\n"
    )


async def ask_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    """Handle /ask command - search knowledge base."""
    if not is_authorized(update.effective_user.id):
        return

    if not context.args:
        await update.message.reply_text("Usage: /ask <question>")
        return

    question = " ".join(context.args)
    client = get_client(context)

    # Send "searching" indicator
    searching_msg = await update.message.reply_text("Searching...")

    try:
        results = await client.search_knowledge(question, limit=3)
        response = format_knowledge_results(results)
        await searching_msg.edit_text(response, parse_mode=ParseMode.HTML)
    except Exception as e:
        logger.error(f"Knowledge search error: {e}")
        await searching_msg.edit_text(format_error(str(e)), parse_mode=ParseMode.HTML)


async def status_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    """Handle /status command."""
    if not is_authorized(update.effective_user.id):
        return

    client = get_client(context)

    try:
        status = client.get_daemon_status()
        response = format_status(status)
        await update.message.reply_html(response)
    except Exception as e:
        logger.error(f"Status error: {e}")
        await update.message.reply_html(format_error(str(e)))


async def inbox_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    """Handle /inbox command."""
    if not is_authorized(update.effective_user.id):
        return

    client = get_client(context)

    try:
        messages = client.get_inbox(limit=10)
        response = format_message_list(messages, "Inbox")
        await update.message.reply_html(response)
    except Exception as e:
        logger.error(f"Inbox error: {e}")
        await update.message.reply_html(format_error(str(e)))


async def send_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    """Handle /send command - send message to daemon."""
    if not is_authorized(update.effective_user.id):
        return

    if len(context.args) < 2:
        await update.message.reply_text("Usage: /send <daemon> <message>")
        return

    to_daemon = context.args[0]
    subject = " ".join(context.args[1:])

    client = get_client(context)

    try:
        msg_id = client.send_message(
            to_daemon=to_daemon,
            subject=subject,
            msg_type="task",
            priority="medium",
        )
        response = format_success(f"Message #{msg_id} sent to {to_daemon}")
        await update.message.reply_html(response)
    except Exception as e:
        logger.error(f"Send error: {e}")
        await update.message.reply_html(format_error(str(e)))


async def ack_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    """Handle /ack command - acknowledge message."""
    if not is_authorized(update.effective_user.id):
        return

    if not context.args:
        await update.message.reply_text("Usage: /ack <message_id>")
        return

    try:
        msg_id = int(context.args[0])
    except ValueError:
        await update.message.reply_text("Invalid message ID")
        return

    client = get_client(context)

    try:
        client.ack_message(msg_id)
        response = format_success(f"Message #{msg_id} acknowledged")
        await update.message.reply_html(response)
    except Exception as e:
        logger.error(f"Ack error: {e}")
        await update.message.reply_html(format_error(str(e)))


async def tasks_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    """Handle /tasks command - list active tasks."""
    if not is_authorized(update.effective_user.id):
        return

    client = get_client(context)

    try:
        tasks = client.get_active_tasks()

        if tasks and "stats" in tasks[0]:
            # Overview mode
            stats = tasks[0]["stats"]
            await update.message.reply_html(
                f"\U0001F4CB <b>Task Overview</b>\n\n"
                f"Total messages: {stats.get('total', 0)}\n"
                f"Pending by daemon: {stats.get('pending_by_daemon', {})}"
            )
        else:
            response = format_message_list(tasks, "Active Tasks")
            await update.message.reply_html(response)
    except Exception as e:
        logger.error(f"Tasks error: {e}")
        await update.message.reply_html(format_error(str(e)))


async def daemons_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    """Handle /daemons command - list online daemons."""
    if not is_authorized(update.effective_user.id):
        return

    client = get_client(context)

    try:
        status = client.get_daemon_status()
        daemons = status.get("daemons", [])

        if not daemons:
            await update.message.reply_text("No daemons registered")
            return

        lines = ["\U0001F916 <b>Registered Daemons</b>\n"]
        for d in daemons:
            daemon_id = d.get("id", "?")
            desc = d.get("description", "")
            last_hb = d.get("last_heartbeat", "")[:16]
            lines.append(f"<b>{daemon_id}</b>")
            lines.append(f"  {desc}")
            lines.append(f"  <i>last seen: {last_hb}</i>\n")

        await update.message.reply_html("\n".join(lines))
    except Exception as e:
        logger.error(f"Daemons error: {e}")
        await update.message.reply_html(format_error(str(e)))
