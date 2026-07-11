"""
Datahaven Client — interface a core messaging és knowledge service-hez
"""

import os
import sys
import json
import aiohttp
from typing import Optional, Dict, Any, List
from pathlib import Path

# Add datahaven-core to path
DATAHAVEN_CORE = os.getenv("DATAHAVEN_CORE", "/home/gabor/datahaven-core")
sys.path.insert(0, DATAHAVEN_CORE)

from messaging.queue import MessageQueue


class DatahavenClient:
    """
    Unified client for Datahaven Core services.

    Provides access to:
    - Message Queue (SQLite)
    - Knowledge Service (HTTP API)
    """

    def __init__(
        self,
        datahaven_home: str,
        knowledge_url: str = "http://localhost:3456",
        daemon_id: str = "telegram-bot",
    ):
        self.datahaven_home = Path(datahaven_home)
        self.knowledge_url = knowledge_url.rstrip("/")
        self.daemon_id = daemon_id

        # Initialize message queue
        db_path = self.datahaven_home / "messages.db"
        self.mq = MessageQueue(str(db_path))

        # Register as daemon
        self.mq.register_daemon(daemon_id, "Telegram Bot Gateway")

    # =========================================================================
    # Messaging
    # =========================================================================

    def send_message(
        self,
        to_daemon: str,
        subject: str,
        msg_type: str = "task",
        payload: Optional[Dict[str, Any]] = None,
        priority: str = "medium",
    ) -> int:
        """Send a message to a daemon."""
        return self.mq.send(
            from_daemon=self.daemon_id,
            to_daemon=to_daemon,
            msg_type=msg_type,
            subject=subject,
            payload=payload,
            priority=priority,
        )

    def get_inbox(self, limit: int = 10) -> List[Dict[str, Any]]:
        """Get pending messages for this bot."""
        return self.mq.receive(self.daemon_id, limit=limit)

    def ack_message(self, msg_id: int) -> None:
        """Acknowledge a message."""
        self.mq.ack(msg_id)

    def get_daemon_status(self) -> Dict[str, Any]:
        """Get queue statistics and daemon list."""
        return {
            "stats": self.mq.stats(),
            "daemons": self.mq.list_daemons(),
        }

    def query_daemon(
        self,
        to_daemon: str,
        question: str,
        timeout_seconds: int = 60,
    ) -> Optional[Dict[str, Any]]:
        """Send a query and wait for response."""
        corr_id = self.mq.query(
            from_daemon=self.daemon_id,
            to_daemon=to_daemon,
            question=question,
        )
        return self.mq.wait_response(corr_id, timeout_seconds=timeout_seconds)

    def heartbeat(self) -> None:
        """Send heartbeat to mark bot as online."""
        self.mq.heartbeat(self.daemon_id)

    # =========================================================================
    # Knowledge Service
    # =========================================================================

    async def search_knowledge(
        self,
        query: str,
        limit: int = 5,
    ) -> Dict[str, Any]:
        """Search the knowledge base via RAG."""
        async with aiohttp.ClientSession() as session:
            try:
                async with session.post(
                    f"{self.knowledge_url}/api/knowledge/search",
                    json={"q": query, "limit": limit},
                    timeout=aiohttp.ClientTimeout(total=30),
                ) as response:
                    if response.status == 200:
                        return await response.json()
                    else:
                        return {"error": f"HTTP {response.status}"}
            except aiohttp.ClientError as e:
                return {"error": str(e)}

    async def get_knowledge_health(self) -> Dict[str, Any]:
        """Check knowledge service health."""
        async with aiohttp.ClientSession() as session:
            try:
                async with session.get(
                    f"{self.knowledge_url}/health",
                    timeout=aiohttp.ClientTimeout(total=10),
                ) as response:
                    if response.status == 200:
                        return await response.json()
                    else:
                        return {"status": "error", "code": response.status}
            except aiohttp.ClientError as e:
                return {"status": "error", "message": str(e)}

    # =========================================================================
    # Task helpers
    # =========================================================================

    def get_active_tasks(self, daemon: Optional[str] = None) -> List[Dict[str, Any]]:
        """
        Get active tasks from the queue.

        If daemon is specified, get tasks for that daemon.
        Otherwise, get all pending tasks.
        """
        # This is a simplified version - in production you'd query
        # the tasks from the appropriate source
        if daemon:
            messages = self.mq.receive(daemon, limit=50, msg_types=["task"])
        else:
            # Get stats to show overview
            stats = self.mq.stats()
            return [{"stats": stats}]

        return [
            {
                "id": m["id"],
                "subject": m["subject"],
                "from": m["from_daemon"],
                "priority": m["priority"],
                "created": m["created_at"],
            }
            for m in messages
        ]
