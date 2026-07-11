# SpaceOS Scripts

> Újrafelhasználható szkriptek a SpaceOS agent infrastruktúrához.
> **Alapelv:** Ha 2× csinálod, szkripteld!

---

## Mailbox Scripts (`mailbox/`)

| Szkript | Leírás | Használat |
|---------|--------|-----------|
| `list-unread.sh` | UNREAD inbox üzenetek listázása | `./list-unread.sh [terminal\|all]` |
| `send-inbox.sh` | Inbox üzenet küldése | `./send-inbox.sh <terminal> <type> <priority> <model> <title> [content_file]` |
| `mark-read.sh` | Üzenet READ-ként jelölése | `./mark-read.sh <terminal> <message_file>` |

---

## Session Scripts (`session/`)

| Szkript | Leírás | Használat |
|---------|--------|-----------|
| `start-terminal.sh` | Terminál session indítása | `./start-terminal.sh <terminal> [model]` |
| `inject-prompt.sh` | Prompt küldése futó session-be | `./inject-prompt.sh <terminal> <prompt>` |
| `list-sessions.sh` | Összes session listázása | `./list-sessions.sh` |
| `capture-output.sh` | Session output mentése | `./capture-output.sh <terminal> [lines]` |

---

## Health Scripts (`health/`)

| Szkript | Leírás | Használat |
|---------|--------|-----------|
| `check-all.sh` | Teljes rendszer health check | `./check-all.sh` |
| `check-services.sh` | Service-ek ellenőrzése | `./check-services.sh` |
| `check-workers.sh` | ADR-049 Worker státusz | `./check-workers.sh [terminal]` |
| `check-sessions.sh` | Session health, stuck detection | `./check-sessions.sh` |

---

## Használat

```bash
# Minden szkript executable
chmod +x /opt/spaceos/scripts/*/*.sh

# Health check futtatás
/opt/spaceos/scripts/health/check-all.sh

# UNREAD inbox listázás
/opt/spaceos/scripts/mailbox/list-unread.sh all

# Session indítás
/opt/spaceos/scripts/session/start-terminal.sh backend sonnet
```

---

## Szkript Fejlesztési Szabályok

1. **Fájlnév:** `<action>-<target>.sh`
2. **Header:** leírás, használat, példa
3. **Strict mode:** `set -euo pipefail`
4. **Output:** strukturált, pipe-olható
5. **Regisztráció:** add hozzá ehhez a README-hez

---

## Token Megtakarítás

| Művelet | LLM költség | Script költség | Megtakarítás |
|---------|-------------|----------------|--------------|
| Inbox list | ~500 token | 0 | 100% |
| Health check | ~1000 token | 0 | 100% |
| Session start | ~800 token | 0 | 100% |

**Napi 50 ilyen művelet = ~130K token = ~$2-5 megtakarítás**

---

**Verzió:** 1.0.0
**Készült:** 2026-06-30
