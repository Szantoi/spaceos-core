# Datahaven Telegram

> Telegram bot gateway a Datahaven Core-hoz — daemon kommunikáció és RAG keresés chat felületen

## Áttekintés

A Datahaven Telegram egy Telegram bot, amely összeköti a felhasználókat a Datahaven ökoszisztémával:

- **Knowledge Search** — RAG-alapú keresés a tudásbázisban
- **Daemon Messaging** — Üzenetek küldése/fogadása daemonoknak
- **Real-time Monitoring** — Queue és daemon státusz figyelés
- **Push Notifications** — DONE/BLOCKED/Critical értesítések

## Architektúra

```
┌─────────────────────────────────────────────────────────────┐
│                      Telegram User                          │
└─────────────────────────┬───────────────────────────────────┘
                          │ Bot API
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                  datahaven-telegram                         │
│                                                             │
│  ┌──────────────────┐         ┌──────────────────┐         │
│  │  Command Handler │         │    Formatter     │         │
│  │                  │         │                  │         │
│  │  • /ask          │         │  • HTML escape   │         │
│  │  • /send         │         │  • Message fmt   │         │
│  │  • /inbox        │         │  • Status fmt    │         │
│  │  • /status       │         │  • Error fmt     │         │
│  │  • /tasks        │         │                  │         │
│  │  • /daemons      │         │                  │         │
│  └────────┬─────────┘         └────────┬─────────┘         │
│           │                            │                    │
│  ┌────────▼────────────────────────────▼─────────┐         │
│  │              DatahavenClient                   │         │
│  │                                                │         │
│  │  • MessageQueue (SQLite)                       │         │
│  │  • KnowledgeClient (HTTP)                      │         │
│  │  • Heartbeat                                   │         │
│  └────────────────────────────────────────────────┘         │
└───────────────┬─────────────────────┬───────────────────────┘
                │                     │
                ▼                     ▼
        ┌──────────────┐      ┌──────────────┐
        │  messages.db │      │  Knowledge   │
        │   (SQLite)   │      │   Service    │
        │              │      │  :3456/api   │
        └──────────────┘      └──────────────┘
```

## Gyors kezdés

### Előfeltételek

- Python 3.10+
- Datahaven Core telepítve (`/home/gabor/datahaven-core`)
- Telegram Bot Token (@BotFather-től)
- Knowledge Service futtatása (opcionális, RAG-hoz)

### Telepítés

```bash
# 1. Klónozás
cd /opt/spaceos
git clone https://github.com/user/datahaven-telegram.git
cd datahaven-telegram

# 2. Telepítés
bash scripts/install.sh

# 3. Konfiguráció
cp config/.env.example config/.env
nano config/.env
```

### Konfiguráció

```bash
# config/.env

# Telegram Bot Token (kötelező)
TELEGRAM_BOT_TOKEN=your-bot-token-from-botfather

# Datahaven paths
DATAHAVEN_HOME=/opt/spaceos/datahaven
DATAHAVEN_CORE=/home/gabor/datahaven-core

# Knowledge Service
KNOWLEDGE_URL=http://localhost:3456

# Bot identity
BOT_DAEMON_ID=telegram-bot

# Authorization - Telegram user ID-k (kötelező)
ALLOWED_USERS=123456789
ADMIN_USERS=123456789

# Polling és értesítések
POLL_INTERVAL=30
NOTIFY_ON_CRITICAL=true
NOTIFY_ON_DONE=true
NOTIFY_ON_BLOCKED=true

# Logging
LOG_LEVEL=INFO
```

### Indítás

**Manuális:**
```bash
source venv/bin/activate
python src/bot.py
```

**Systemd service:**
```bash
sudo bash scripts/systemd-install.sh
sudo systemctl enable datahaven-telegram
sudo systemctl start datahaven-telegram
```

## Parancsok

| Parancs | Leírás | Példa |
|---------|--------|-------|
| `/start` | Bot indítás, üdvözlés | `/start` |
| `/help` | Parancsok listája | `/help` |
| `/ask [kérdés]` | RAG keresés a tudásbázisban | `/ask Hogyan működik a JWT?` |
| `/status` | Queue és daemon állapot | `/status` |
| `/inbox` | Bejövő üzenetek listája | `/inbox` |
| `/send [daemon] [üzenet]` | Üzenet küldése daemonnak | `/send kernel Fix bug #123` |
| `/ack [id]` | Üzenet nyugtázása | `/ack 42` |
| `/tasks` | Aktív feladatok | `/tasks` |
| `/daemons` | Regisztrált daemonok | `/daemons` |

## Használati példák

### Knowledge keresés
```
/ask Mi az a Row Level Security?
```
Visszaad 5 releváns dokumentumot a tudásbázisból, relevancia score-ral.

### Daemon kommunikáció
```
/send conductor Review the latest PR
/send kernel Check authentication bug
```
Üzenetet küld a megadott daemon inbox-ába.

### Monitoring
```
/status
```
```
📊 Datahaven Status

Queue:
  Total: 3
  Pending: kernel(2), conductor(1)

Daemons: (4)
  🤖 telegram-bot (last seen: 2026-06-19 09:21)
  🤖 kernel (last seen: 2026-06-19 09:15)
  🤖 conductor (last seen: 2026-06-19 09:20)
  🤖 librarian (last seen: 2026-06-19 08:45)
```

## Értesítések

A bot automatikusan értesíti az ADMIN_USERS-ben lévő felhasználókat:

| Esemény | Konfig | Leírás |
|---------|--------|--------|
| Critical üzenet | `NOTIFY_ON_CRITICAL=true` | Kritikus prioritású bejövő üzenet |
| DONE státusz | `NOTIFY_ON_DONE=true` | Feladat befejezve |
| BLOCKED státusz | `NOTIFY_ON_BLOCKED=true` | Feladat elakadt |

Az értesítések a `POLL_INTERVAL` (default: 30s) gyakorisággal ellenőrződnek.

## Projekt struktúra

```
datahaven-telegram/
├── src/
│   ├── bot.py                 # Main entry point
│   ├── handlers/
│   │   ├── __init__.py
│   │   └── commands.py        # Összes command handler
│   ├── services/
│   │   ├── __init__.py
│   │   ├── datahaven.py       # DatahavenClient (MQ + Knowledge)
│   │   └── formatter.py       # Telegram HTML formatter
│   └── utils/
│       └── __init__.py
├── config/
│   ├── .env                   # Aktív konfiguráció (git-ignored)
│   └── .env.example           # Konfig template
├── scripts/
│   ├── install.sh             # Függőségek telepítése
│   └── systemd-install.sh     # Systemd service telepítés
├── logs/                      # Log fájlok
├── venv/                      # Python virtualenv
├── requirements.txt
├── .gitignore
└── README.md
```

## Systemd kezelés

```bash
# Státusz
sudo systemctl status datahaven-telegram

# Indítás/leállítás
sudo systemctl start datahaven-telegram
sudo systemctl stop datahaven-telegram
sudo systemctl restart datahaven-telegram

# Logok
journalctl -u datahaven-telegram -f

# Auto-start on boot
sudo systemctl enable datahaven-telegram
sudo systemctl disable datahaven-telegram
```

## Hibakeresés

### Bot nem válaszol
1. Ellenőrizd a service státuszt: `sudo systemctl status datahaven-telegram`
2. Nézd meg a logokat: `journalctl -u datahaven-telegram -n 50`
3. Ellenőrizd a `TELEGRAM_BOT_TOKEN`-t

### "Unauthorized" üzenet
- Add hozzá a Telegram user ID-dat az `ALLOWED_USERS` vagy `ADMIN_USERS` listához
- User ID lekérdezése: írd be `/start` a botnak, és kiírja

### Knowledge keresés nem működik
1. Ellenőrizd hogy fut-e a Knowledge Service: `curl http://localhost:3456/health`
2. Ellenőrizd a `KNOWLEDGE_URL` beállítást

### "Conflict: terminated by other getUpdates request"
- Csak egy bot instance futhat egyszerre
- Állítsd le a többi process-t: `pkill -f "bot.py"`

## Fejlesztés

### Új parancs hozzáadása

1. Hozd létre a handler-t a `src/handlers/commands.py`-ban:
```python
async def mycommand_command(update: Update, context: ContextTypes.DEFAULT_TYPE) -> None:
    if not is_authorized(update.effective_user.id):
        return

    # Logic here
    await update.message.reply_text("Response")
```

2. Regisztráld a `src/bot.py`-ban:
```python
application.add_handler(CommandHandler("mycommand", mycommand_command))
```

### Tesztelés

```bash
source venv/bin/activate
python -c "
from services.datahaven import DatahavenClient
client = DatahavenClient('/opt/spaceos/datahaven', 'http://localhost:3456', 'test')
print(client.mq.stats())
"
```

## Kapcsolódó projektek

- **[datahaven-core](../datahaven-core/)** — Core messaging és knowledge engine
- **[datahaven-kanban](../datahaven-kanban/)** — Web-based Kanban UI (planned)
- **[datahaven-web](../datahaven-web/)** — Dashboard és monitoring (planned)

## License

MIT
