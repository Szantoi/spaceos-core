# API Dokumentáció

## DatahavenClient

A `DatahavenClient` osztály egyesíti a Datahaven Core szolgáltatásait.

### Inicializálás

```python
from services.datahaven import DatahavenClient

client = DatahavenClient(
    datahaven_home="/opt/spaceos/datahaven",
    knowledge_url="http://localhost:3456",
    daemon_id="telegram-bot"
)
```

**Paraméterek:**
- `datahaven_home` — Datahaven instance könyvtár (messages.db helye)
- `knowledge_url` — Knowledge Service URL
- `daemon_id` — Bot daemon azonosítója

---

## Messaging API

### send_message()

Üzenet küldése egy daemonnak.

```python
msg_id = client.send_message(
    to_daemon="kernel",
    subject="Fix bug #123",
    msg_type="task",
    payload={"bug_id": 123},
    priority="high"
)
```

**Paraméterek:**
| Név | Típus | Kötelező | Leírás |
|-----|-------|----------|--------|
| `to_daemon` | str | igen | Cél daemon azonosítója |
| `subject` | str | igen | Üzenet tárgya |
| `msg_type` | str | nem | Típus: `task`, `question`, `done`, `blocked` (default: `task`) |
| `payload` | dict | nem | Extra adatok JSON formátumban |
| `priority` | str | nem | `low`, `medium`, `high`, `critical` (default: `medium`) |

**Visszatérés:** `int` — Üzenet ID

---

### get_inbox()

Bejövő üzenetek lekérése.

```python
messages = client.get_inbox(limit=10)
for msg in messages:
    print(f"{msg['id']}: {msg['subject']} from {msg['from_daemon']}")
```

**Paraméterek:**
| Név | Típus | Kötelező | Leírás |
|-----|-------|----------|--------|
| `limit` | int | nem | Maximum visszaadott üzenetek (default: 10) |

**Visszatérés:** `List[Dict]` — Üzenetek listája

**Üzenet struktúra:**
```python
{
    "id": 42,
    "from_daemon": "conductor",
    "to_daemon": "telegram-bot",
    "msg_type": "task",
    "subject": "Review PR #456",
    "payload": {},
    "priority": "medium",
    "status": "pending",
    "created_at": "2026-06-19T09:21:00",
    "correlation_id": None
}
```

---

### ack_message()

Üzenet nyugtázása (feldolgozottnak jelölés).

```python
client.ack_message(msg_id=42)
```

**Paraméterek:**
| Név | Típus | Kötelező | Leírás |
|-----|-------|----------|--------|
| `msg_id` | int | igen | Nyugtázandó üzenet ID |

---

### get_daemon_status()

Queue statisztikák és daemon lista lekérése.

```python
status = client.get_daemon_status()
print(f"Total messages: {status['stats']['total']}")
print(f"Daemons: {len(status['daemons'])}")
```

**Visszatérés:**
```python
{
    "stats": {
        "total": 15,
        "by_status": {"pending": 5, "acked": 10},
        "pending_by_daemon": {"kernel": 3, "conductor": 2}
    },
    "daemons": [
        {"id": "telegram-bot", "description": "Telegram Bot Gateway", "last_heartbeat": "2026-06-19T09:21:00"},
        {"id": "kernel", "description": "Backend kernel", "last_heartbeat": "2026-06-19T09:15:00"}
    ]
}
```

---

### query_daemon()

Szinkron kérdés küldése és válaszra várás.

```python
response = client.query_daemon(
    to_daemon="kernel",
    question="What is the current build status?",
    timeout_seconds=60
)
if response:
    print(f"Answer: {response['payload']}")
```

**Paraméterek:**
| Név | Típus | Kötelező | Leírás |
|-----|-------|----------|--------|
| `to_daemon` | str | igen | Cél daemon |
| `question` | str | igen | Kérdés szövege |
| `timeout_seconds` | int | nem | Várakozási idő (default: 60) |

**Visszatérés:** `Optional[Dict]` — Válasz üzenet vagy `None` timeout esetén

---

### heartbeat()

Bot online státusz jelzése.

```python
client.heartbeat()
```

Automatikusan hívódik a polling job-ban.

---

## Knowledge API

### search_knowledge()

RAG keresés a tudásbázisban.

```python
import asyncio

async def search():
    results = await client.search_knowledge(
        query="How does JWT authentication work?",
        limit=5
    )
    for doc in results.get("results", []):
        print(f"{doc['source']}: {doc['score']}")

asyncio.run(search())
```

**Paraméterek:**
| Név | Típus | Kötelező | Leírás |
|-----|-------|----------|--------|
| `query` | str | igen | Keresési kérdés |
| `limit` | int | nem | Maximum találatok (default: 5) |

**Visszatérés:**
```python
{
    "results": [
        {
            "source": "docs/security/JWT_GUIDE.md",
            "content": "JWT (JSON Web Token) is...",
            "score": 0.89
        }
    ],
    "query": "How does JWT authentication work?",
    "count": 5
}
```

---

### get_knowledge_health()

Knowledge Service állapot ellenőrzése.

```python
async def check_health():
    health = await client.get_knowledge_health()
    if health.get("status") == "ok":
        print(f"Documents indexed: {health.get('doc_count')}")

asyncio.run(check_health())
```

**Visszatérés:**
```python
{
    "status": "ok",
    "doc_count": 441,
    "chroma_status": "connected"
}
```

---

## Formatter API

HTML formázás Telegram üzenetekhez.

### escape_html()

HTML karakterek escape-elése.

```python
from services.formatter import escape_html

safe = escape_html("Use <b>bold</b> & 'quotes'")
# "Use &lt;b&gt;bold&lt;/b&gt; &amp; 'quotes'"
```

---

### format_message()

Egyetlen üzenet formázása.

```python
from services.formatter import format_message

msg = {"id": 1, "subject": "Test", "from_daemon": "kernel", "priority": "high"}
html = format_message(msg)
```

---

### format_message_list()

Üzenet lista formázása.

```python
from services.formatter import format_message_list

messages = [...]
html = format_message_list(messages, title="Inbox")
```

---

### format_knowledge_results()

RAG eredmények formázása.

```python
from services.formatter import format_knowledge_results

results = {"results": [...], "count": 5}
html = format_knowledge_results(results)
```

---

### format_status()

Daemon státusz formázása.

```python
from services.formatter import format_status

status = client.get_daemon_status()
html = format_status(status)
```

---

### format_success() / format_error()

Sikeres/hiba üzenet formázása.

```python
from services.formatter import format_success, format_error

html_ok = format_success("Message sent!")
html_err = format_error("Connection failed")
```

---

## Telegram Command Handlers

### Authorization

```python
from handlers.commands import is_authorized, is_admin

if is_authorized(user_id):
    # Allowed user
    pass

if is_admin(user_id):
    # Admin user
    pass
```

### Handler minta

```python
async def mycommand_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    """Handle /mycommand command."""
    if not is_authorized(update.effective_user.id):
        return

    client = get_client(context)

    try:
        # Business logic
        result = client.some_operation()
        response = format_success(f"Result: {result}")
    except Exception as e:
        response = format_error(str(e))

    await update.message.reply_html(response)
```

---

## Környezeti változók

| Változó | Leírás | Default |
|---------|--------|---------|
| `TELEGRAM_BOT_TOKEN` | Bot token | kötelező |
| `DATAHAVEN_HOME` | Datahaven instance path | `/opt/spaceos/datahaven` |
| `DATAHAVEN_CORE` | Datahaven core path | `/home/gabor/datahaven-core` |
| `KNOWLEDGE_URL` | Knowledge Service URL | `http://localhost:3456` |
| `BOT_DAEMON_ID` | Bot daemon azonosító | `telegram-bot` |
| `ALLOWED_USERS` | Engedélyezett user ID-k | kötelező |
| `ADMIN_USERS` | Admin user ID-k | kötelező |
| `POLL_INTERVAL` | Polling intervallum (sec) | `30` |
| `LOG_LEVEL` | Log szint | `INFO` |
