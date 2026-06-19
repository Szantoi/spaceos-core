# External Dependencies

A datahaven-web dashboard külső függőségei és azok követelményei.

---

## Áttekintés

```
┌─────────────────────────────────────────────────────────────────────┐
│                        datahaven-web                                │
│                         (port 3457)                                 │
└───────────────────────────┬─────────────────────────────────────────┘
                            │
            ┌───────────────┴───────────────┐
            │                               │
            ▼                               ▼
┌───────────────────────┐       ┌───────────────────────┐
│     messages.db       │       │   knowledge-service   │
│      (SQLite)         │       │     (port 3456)       │
│                       │       │                       │
│  KÖTELEZŐ             │       │  OPCIONÁLIS           │
│  read-only hozzáférés │       │  HTTP API             │
└───────────────────────┘       └───────────────────────┘
```

---

## 1. SQLite Database (KÖTELEZŐ)

### Mi ez?

A Datahaven message queue adatbázisa. Tartalmazza az üzeneteket és a regisztrált daemon-okat.

### Követelmények

| Tulajdonság | Érték |
|-------------|-------|
| Típus | SQLite 3 |
| Hozzáférés | Read-only |
| Default path | `/opt/spaceos/datahaven/messages.db` |
| Env variable | `MESSAGES_DB` |

### Ellenőrzés

```bash
# Fájl létezik?
ls -la /opt/spaceos/datahaven/messages.db

# Táblák megvannak?
sqlite3 /opt/spaceos/datahaven/messages.db ".tables"
# Output: daemons  messages

# Adatok vannak?
sqlite3 /opt/spaceos/datahaven/messages.db "SELECT COUNT(*) FROM messages;"
sqlite3 /opt/spaceos/datahaven/messages.db "SELECT COUNT(*) FROM daemons;"
```

### Ha hiányzik

A dashboard "demo mode"-ban indul és üres adatokat ad vissza:

```json
{ "total": 0, "pending": 0, "acked": 0, "daemons": 0 }
```

### Séma

Lásd: [DATABASE_SCHEMA.md](./DATABASE_SCHEMA.md)

---

## 2. Knowledge Service (OPCIONÁLIS)

### Mi ez?

RAG (Retrieval Augmented Generation) alapú tudásbázis kereső szolgáltatás. ChromaDB vector store-t használ.

### Követelmények

| Tulajdonság | Érték |
|-------------|-------|
| Típus | HTTP REST API |
| Default URL | `http://localhost:3456` |
| Env variable | `KNOWLEDGE_URL` |
| Protocol | HTTP (nem HTTPS) |

### API Endpoints

#### Health Check

```http
GET /health
```

**Response:**
```json
{
  "status": "ok",
  "doc_count": 441,
  "chroma_status": "connected"
}
```

#### Search

```http
POST /api/knowledge/search
Content-Type: application/json

{
  "q": "search query",
  "limit": 5
}
```

**Response:**
```json
{
  "results": [
    {
      "source": "docs/file.md",
      "content": "Relevant text...",
      "score": 0.89
    }
  ],
  "query": "search query",
  "count": 1
}
```

### Ellenőrzés

```bash
# Service fut?
curl http://localhost:3456/health

# Keresés működik?
curl -X POST http://localhost:3456/api/knowledge/search \
  -H "Content-Type: application/json" \
  -d '{"q": "test", "limit": 3}'
```

### Ha nem elérhető

A dashboard Knowledge Search panel jelzi:
- Status: "Offline"
- Search: "Knowledge service unavailable" error

A többi funkció normálisan működik.

---

## 3. Konfiguráció

### Environment Variables

```bash
# Kötelező (de van default)
MESSAGES_DB=/opt/spaceos/datahaven/messages.db

# Opcionális
KNOWLEDGE_URL=http://localhost:3456

# Server config
PORT=3457
HOST=0.0.0.0

# Auth (opcionális)
AUTH_ENABLED=false
AUTH_TOKEN=your-secret-token

# Rate limiting
RATE_LIMIT_WINDOW_MS=60000
RATE_LIMIT_MAX=100
```

### .env file

```bash
cp .env.example .env
nano .env
```

---

## 4. Startup ellenőrzések

A server indításkor ellenőrzi:

1. **Database connection**
   - Sikeres: `[DATA] Connected to database: /path/to/messages.db`
   - Sikertelen: `[DATA] Running in demo mode without database`

2. **Knowledge service** (lazy check)
   - Első kereséskor ellenőrzi
   - Health endpoint-ot pollozza a frontend

### Startup log

```
═══════════════════════════════════════════════════════════════
  Datahaven Web Dashboard
═══════════════════════════════════════════════════════════════
  URL:        http://0.0.0.0:3457
  Database:   /opt/spaceos/datahaven/messages.db
  Knowledge:  http://localhost:3456
  Auth:       disabled
═══════════════════════════════════════════════════════════════
```

---

## 5. Troubleshooting

### Database nem található

```
[DATA] Failed to connect to database: SQLITE_CANTOPEN
[DATA] Running in demo mode without database
```

**Megoldás:**
```bash
# Ellenőrizd az útvonalat
ls -la /opt/spaceos/datahaven/messages.db

# Vagy állítsd be a helyes útvonalat
export MESSAGES_DB=/correct/path/to/messages.db
```

### Knowledge service nem elérhető

```json
{"error": "Knowledge service unavailable: fetch failed"}
```

**Megoldás:**
```bash
# Service fut?
systemctl status knowledge-service

# Manuális indítás
cd /opt/spaceos/datahaven-core/knowledge-service
npm start

# Vagy másik port?
export KNOWLEDGE_URL=http://localhost:3456
```

### Permission denied

```
[DATA] Failed to connect to database: SQLITE_READONLY
```

**Megoldás:**
```bash
# A dashboard read-only módban nyitja az adatbázist
# De a fájlnak létezni kell és olvashatónak kell lennie
chmod 644 /opt/spaceos/datahaven/messages.db
```

---

## 6. Összefoglaló táblázat

| Függőség | Típus | Kötelező | Port | Env Variable |
|----------|-------|----------|------|--------------|
| messages.db | SQLite | Igen* | - | `MESSAGES_DB` |
| knowledge-service | HTTP | Nem | 3456 | `KNOWLEDGE_URL` |

*\* Nélküle "demo mode"-ban indul, de éles használathoz kell.*
