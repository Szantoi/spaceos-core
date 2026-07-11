"""
File Upload Handler for Datahaven Telegram Bot

Handles document uploads and saves them to the knowledge base.
Supports: .md, .txt, .pdf, .json, .yaml, .yml
"""

import os
import logging
import asyncio
import aiohttp
from pathlib import Path
from datetime import datetime
from telegram import Update
from telegram.ext import ContextTypes
from telegram.constants import ParseMode

from services.formatter import format_success, format_error, escape_html

logger = logging.getLogger(__name__)

# File storage paths
UPLOAD_BASE = Path(os.getenv("UPLOAD_PATH", "/opt/spaceos/docs/uploads"))
KNOWLEDGE_BASE = Path(os.getenv("KNOWLEDGE_PATH", "/opt/spaceos/docs/knowledge"))
KNOWLEDGE_URL = os.getenv("KNOWLEDGE_URL", "http://localhost:3456")

# Allowed file types
ALLOWED_EXTENSIONS = {'.md', '.txt', '.pdf', '.json', '.yaml', '.yml', '.csv'}
MAX_FILE_SIZE = 10 * 1024 * 1024  # 10 MB

# Target categories
CATEGORIES = {
    'knowledge': KNOWLEDGE_BASE,
    'upload': UPLOAD_BASE,
    'architecture': KNOWLEDGE_BASE / 'architecture',
    'deployment': KNOWLEDGE_BASE / 'deployment',
    'patterns': KNOWLEDGE_BASE / 'patterns',
    'security': KNOWLEDGE_BASE / 'security',
    'context': KNOWLEDGE_BASE / 'context',
}

# Authorized users (imported from commands.py pattern)
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


async def trigger_reindex():
    """Trigger knowledge service reindex."""
    try:
        async with aiohttp.ClientSession() as session:
            async with session.post(
                f"{KNOWLEDGE_URL}/api/knowledge/index",
                timeout=aiohttp.ClientTimeout(total=120)
            ) as response:
                if response.status == 200:
                    data = await response.json()
                    return True, data.get('indexed', 0)
                return False, 0
    except Exception as e:
        logger.error(f"Reindex trigger failed: {e}")
        return False, 0


async def handle_document(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    """
    Handle uploaded documents.

    Usage: Send a file with optional caption:
    - No caption: saves to uploads/
    - Caption "knowledge": saves to knowledge base root
    - Caption "architecture": saves to knowledge/architecture/
    - etc.
    """
    if not is_authorized(update.effective_user.id):
        await update.message.reply_text("Unauthorized")
        return

    document = update.message.document
    if not document:
        return

    # Check file size
    if document.file_size > MAX_FILE_SIZE:
        await update.message.reply_html(
            format_error(f"File too large. Max size: {MAX_FILE_SIZE // 1024 // 1024} MB")
        )
        return

    # Check file extension
    file_name = document.file_name or "unnamed"
    file_ext = Path(file_name).suffix.lower()

    if file_ext not in ALLOWED_EXTENSIONS:
        await update.message.reply_html(
            format_error(f"Unsupported file type: {file_ext}\n"
                        f"Allowed: {', '.join(ALLOWED_EXTENSIONS)}")
        )
        return

    # Determine target category from caption
    caption = (update.message.caption or "").strip().lower()

    if caption in CATEGORIES:
        target_dir = CATEGORIES[caption]
        category = caption
    else:
        target_dir = UPLOAD_BASE
        category = "upload"

    # Create directory if needed
    target_dir.mkdir(parents=True, exist_ok=True)

    # Generate unique filename if exists
    target_path = target_dir / file_name
    if target_path.exists():
        stem = Path(file_name).stem
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        target_path = target_dir / f"{stem}_{timestamp}{file_ext}"

    # Send progress message
    progress_msg = await update.message.reply_text(
        f"Uploading {file_name} to {category}/..."
    )

    try:
        # Download file
        file = await document.get_file()
        await file.download_to_drive(target_path)

        # Set permissions for knowledge service
        os.chmod(target_path, 0o644)

        # Success message
        relative_path = str(target_path).replace("/opt/spaceos/", "")

        response_lines = [
            f"File uploaded successfully!",
            f"",
            f"<b>Name:</b> {escape_html(file_name)}",
            f"<b>Path:</b> <code>{relative_path}</code>",
            f"<b>Size:</b> {document.file_size:,} bytes",
            f"<b>Category:</b> {category}",
        ]

        # Trigger reindex if saved to knowledge
        if category != "upload":
            response_lines.append("")
            response_lines.append("Triggering knowledge reindex...")

            await progress_msg.edit_text("\n".join(response_lines), parse_mode=ParseMode.HTML)

            success, count = await trigger_reindex()
            if success:
                response_lines[-1] = f"Knowledge base reindexed ({count} docs)"
            else:
                response_lines[-1] = "Reindex scheduled (may take a moment)"

        await progress_msg.edit_text("\n".join(response_lines), parse_mode=ParseMode.HTML)

    except Exception as e:
        logger.error(f"Upload error: {e}")
        await progress_msg.edit_text(
            format_error(f"Upload failed: {str(e)}"),
            parse_mode=ParseMode.HTML
        )


async def upload_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    """
    Handle /upload command - show upload instructions.
    """
    if not is_authorized(update.effective_user.id):
        return

    categories_list = "\n".join([f"  <code>{cat}</code>" for cat in CATEGORIES.keys()])

    await update.message.reply_html(
        "<b>File Upload</b>\n\n"
        "Send a file to upload it to the knowledge base.\n\n"
        "<b>How to use:</b>\n"
        "1. Drag & drop or attach a file\n"
        "2. Add caption to specify category:\n"
        f"{categories_list}\n\n"
        "<b>Supported types:</b>\n"
        f"  {', '.join(ALLOWED_EXTENSIONS)}\n\n"
        "<b>Examples:</b>\n"
        "  • Send file with caption <code>architecture</code>\n"
        "    → saves to knowledge/architecture/\n"
        "  • Send file with caption <code>knowledge</code>\n"
        "    → saves to knowledge base root\n"
        "  • Send file without caption\n"
        "    → saves to uploads/\n\n"
        "<b>Max file size:</b> 10 MB"
    )


async def files_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    """
    Handle /files command - list recent uploads.
    """
    if not is_authorized(update.effective_user.id):
        return

    # List recent files from upload directory
    UPLOAD_BASE.mkdir(parents=True, exist_ok=True)

    files = sorted(
        UPLOAD_BASE.glob("*"),
        key=lambda f: f.stat().st_mtime,
        reverse=True
    )[:10]

    if not files:
        await update.message.reply_text("No files in upload folder")
        return

    lines = ["<b>Recent Uploads</b>\n"]

    for f in files:
        if f.is_file():
            size = f.stat().st_size
            mtime = datetime.fromtimestamp(f.stat().st_mtime)
            size_str = f"{size:,}" if size < 1024 else f"{size // 1024}K"
            lines.append(
                f"<code>{f.name}</code>\n"
                f"  {size_str} • {mtime.strftime('%Y-%m-%d %H:%M')}"
            )

    await update.message.reply_html("\n".join(lines))


async def reindex_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    """
    Handle /reindex command - trigger knowledge base reindex.
    """
    if not is_authorized(update.effective_user.id):
        return

    progress_msg = await update.message.reply_text("Triggering reindex...")

    success, count = await trigger_reindex()

    if success:
        await progress_msg.edit_text(
            f"Knowledge base reindexed successfully!\n"
            f"Total documents: {count}"
        )
    else:
        await progress_msg.edit_text(
            "Reindex failed or timed out.\n"
            "Check knowledge service status."
        )
