#!/usr/bin/env python3
"""
Datahaven Telegram Bot

Main entry point for the Telegram gateway.
"""

import os
import sys
import logging
import asyncio
from pathlib import Path
from dotenv import load_dotenv

# Add src to path for imports
sys.path.insert(0, str(Path(__file__).parent))

from telegram import Update
from telegram.ext import (
    Application,
    CommandHandler,
    MessageHandler,
    filters,
)

# Load environment
env_path = Path(__file__).parent.parent / "config" / ".env"
if env_path.exists():
    load_dotenv(env_path)
else:
    # Try default locations
    load_dotenv()

# Setup logging
logging.basicConfig(
    format="%(asctime)s - %(name)s - %(levelname)s - %(message)s",
    level=getattr(logging, os.getenv("LOG_LEVEL", "INFO")),
)
logger = logging.getLogger(__name__)

# Import handlers
from handlers.commands import (
    start_command,
    help_command,
    ask_command,
    status_command,
    inbox_command,
    send_command,
    ack_command,
    tasks_command,
    daemons_command,
)


async def error_handler(update: Update, context) -> None:
    """Handle errors."""
    logger.error(f"Exception while handling an update: {context.error}")

    if update and update.effective_message:
        await update.effective_message.reply_text(
            "An error occurred. Please try again."
        )


async def unknown_command(update: Update, context) -> None:
    """Handle unknown commands."""
    await update.message.reply_text(
        "Unknown command. Use /help to see available commands."
    )


async def poll_inbox(context) -> None:
    """
    Background job to poll inbox and send notifications.

    Runs periodically to check for new messages and notify users.
    """
    from services.datahaven import DatahavenClient
    from services.formatter import format_message

    if "datahaven" not in context.bot_data:
        return

    client: DatahavenClient = context.bot_data["datahaven"]

    # Send heartbeat
    client.heartbeat()

    # Check for new messages
    try:
        messages = client.get_inbox(limit=5)

        # Get admin users for notifications
        admin_users = os.getenv("ADMIN_USERS", "").split(",")

        for msg in messages:
            priority = msg.get("priority", "medium")
            msg_type = msg.get("msg_type", "")

            # Determine if we should notify
            should_notify = False

            if priority == "critical" and os.getenv("NOTIFY_ON_CRITICAL", "true") == "true":
                should_notify = True
            elif msg_type == "done" and os.getenv("NOTIFY_ON_DONE", "true") == "true":
                should_notify = True
            elif msg_type == "blocked" and os.getenv("NOTIFY_ON_BLOCKED", "true") == "true":
                should_notify = True

            if should_notify:
                formatted = format_message(msg)
                for user_id in admin_users:
                    if user_id.strip():
                        try:
                            await context.bot.send_message(
                                chat_id=int(user_id.strip()),
                                text=formatted,
                                parse_mode="HTML",
                            )
                        except Exception as e:
                            logger.error(f"Failed to notify user {user_id}: {e}")

    except Exception as e:
        logger.error(f"Poll inbox error: {e}")


def main() -> None:
    """Start the bot."""
    token = os.getenv("TELEGRAM_BOT_TOKEN")

    if not token:
        logger.error("TELEGRAM_BOT_TOKEN not set")
        sys.exit(1)

    # Create application
    application = Application.builder().token(token).build()

    # Add command handlers
    application.add_handler(CommandHandler("start", start_command))
    application.add_handler(CommandHandler("help", help_command))
    application.add_handler(CommandHandler("ask", ask_command))
    application.add_handler(CommandHandler("status", status_command))
    application.add_handler(CommandHandler("inbox", inbox_command))
    application.add_handler(CommandHandler("send", send_command))
    application.add_handler(CommandHandler("ack", ack_command))
    application.add_handler(CommandHandler("tasks", tasks_command))
    application.add_handler(CommandHandler("daemons", daemons_command))

    # Handle unknown commands
    application.add_handler(MessageHandler(filters.COMMAND, unknown_command))

    # Error handler
    application.add_error_handler(error_handler)

    # Add periodic job for inbox polling
    poll_interval = int(os.getenv("POLL_INTERVAL", "30"))
    job_queue = application.job_queue
    job_queue.run_repeating(poll_inbox, interval=poll_interval, first=10)

    # Start bot
    logger.info("Starting Datahaven Telegram Bot...")
    logger.info(f"DATAHAVEN_HOME: {os.getenv('DATAHAVEN_HOME')}")
    logger.info(f"KNOWLEDGE_URL: {os.getenv('KNOWLEDGE_URL')}")
    logger.info(f"Poll interval: {poll_interval}s")

    application.run_polling(allowed_updates=Update.ALL_TYPES)


if __name__ == "__main__":
    main()
