# Datahaven Database Schema

A dashboard a `messages.db` SQLite adatbázist használja read-only módban.

## Elérési út

```bash
# Default
/opt/spaceos/datahaven/messages.db

# Konfigurálható
MESSAGES_DB=/path/to/messages.db
```

---

## Táblák

### `daemons`

Regisztrált daemon-ok nyilvántartása.

```sql
CREATE TABLE daemons (
    id TEXT PRIMARY KEY,           -- Daemon azonosító (pl. "kernel", "conductor")
    description TEXT,              -- Ember-olvasható leírás
    last_heartbeat TEXT            -- ISO 8601 timestamp, UTC
);
```

| Mező | Típus | Kötelező | Leírás |
|------|-------|----------|--------|
| `id` | TEXT | igen | Egyedi daemon azonosító |
| `description` | TEXT | nem | Daemon leírása |
| `last_heartbeat` | TEXT | nem | Utolsó életjel (ISO 8601) |

**Példa:**
```sql
INSERT INTO daemons VALUES
  ('kernel', 'Backend kernel daemon', '2026-06-19T12:00:00Z'),
  ('conductor', 'Orchestration daemon', '2026-06-19T11:58:00Z'),
  ('telegram-bot', 'Telegram bot gateway', '2026-06-19T11:50:00Z');
```

---

### `messages`

Message queue üzenetek.

```sql
CREATE TABLE messages (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    from_daemon TEXT NOT NULL,      -- Küldő daemon ID
    to_daemon TEXT NOT NULL,        -- Címzett daemon ID
    msg_type TEXT DEFAULT 'task',   -- Üzenet típusa
    subject TEXT,                   -- Tárgy/összefoglaló
    payload TEXT,                   -- JSON payload
    priority TEXT DEFAULT 'medium', -- Prioritás szint
    status TEXT DEFAULT 'pending',  -- Feldolgozási státusz
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    correlation_id TEXT             -- Összetartozó üzenetek azonosítója
);
```

| Mező | Típus | Kötelező | Default | Leírás |
|------|-------|----------|---------|--------|
| `id` | INTEGER | auto | - | Egyedi üzenet ID |
| `from_daemon` | TEXT | igen | - | Küldő daemon |
| `to_daemon` | TEXT | igen | - | Címzett daemon |
| `msg_type` | TEXT | nem | `task` | Üzenet típus |
| `subject` | TEXT | nem | - | Rövid leírás |
| `payload` | TEXT | nem | - | JSON extra adat |
| `priority` | TEXT | nem | `medium` | Prioritás |
| `status` | TEXT | nem | `pending` | Státusz |
| `created_at` | TEXT | nem | CURRENT_TIMESTAMP | Létrehozás |
| `correlation_id` | TEXT | nem | - | Üzenet lánc ID |

**Példa:**
```sql
INSERT INTO messages (from_daemon, to_daemon, msg_type, subject, payload, priority, status)
VALUES
  ('conductor', 'kernel', 'task', 'Build project', '{"action":"build"}', 'high', 'pending'),
  ('kernel', 'conductor', 'done', 'Build completed', '{"result":"success"}', 'medium', 'acked');
```

---

## Enum értékek

### `msg_type`

| Érték | Jelentés |
|-------|----------|
| `task` | Feladat kiadás |
| `question` | Kérdés, döntést igényel |
| `done` | Feladat kész |
| `blocked` | Feladat blokkolva |

### `priority`

| Érték | Jelentés | UI szín |
|-------|----------|---------|
| `critical` | Azonnali | Piros (#f4212e) |
| `high` | Sürgős | Sárga (#ffd400) |
| `medium` | Normál | Kék (#1d9bf0) |
| `low` | Alacsony | Szürke (#2f3336) |

### `status`

| Érték | Jelentés | UI szín |
|-------|----------|---------|
| `pending` | Feldolgozásra vár | Sárga (#ffd400) |
| `acked` | Feldolgozva | Zöld (#00ba7c) |

---

## Indexek

A dashboard teljesítményéhez ajánlott indexek:

```sql
CREATE INDEX idx_messages_status ON messages(status);
CREATE INDEX idx_messages_to_daemon ON messages(to_daemon);
CREATE INDEX idx_messages_from_daemon ON messages(from_daemon);
CREATE INDEX idx_messages_created ON messages(created_at DESC);
CREATE INDEX idx_messages_priority ON messages(priority);
```

---

## Dashboard Query-k

A dashboard az alábbi lekérdezéseket használja (read-only):

### Statisztikák

```sql
-- Összesített stats
SELECT
  COUNT(*) as total,
  SUM(CASE WHEN status = 'pending' THEN 1 ELSE 0 END) as pending,
  SUM(CASE WHEN status = 'acked' THEN 1 ELSE 0 END) as acked
FROM messages;

-- Daemon szám
SELECT COUNT(*) as count FROM daemons;
```

### Daemon lista

```sql
SELECT
  d.id,
  d.description,
  d.last_heartbeat,
  (SELECT COUNT(*) FROM messages
   WHERE to_daemon = d.id AND status = 'pending') as pending_count
FROM daemons d
ORDER BY d.last_heartbeat DESC;
```

### Üzenet lista (szűréssel)

```sql
SELECT * FROM messages
WHERE 1=1
  AND status = ?           -- opcionális
  AND (from_daemon = ? OR to_daemon = ?)  -- opcionális
  AND msg_type = ?         -- opcionális
ORDER BY created_at DESC
LIMIT ? OFFSET ?;
```

### Inbox (daemon pending üzenetei)

```sql
SELECT * FROM messages
WHERE to_daemon = ? AND status = 'pending'
ORDER BY
  CASE priority
    WHEN 'critical' THEN 1
    WHEN 'high' THEN 2
    WHEN 'medium' THEN 3
    ELSE 4
  END,
  created_at ASC;
```

### Pending by daemon

```sql
SELECT to_daemon, COUNT(*) as count
FROM messages
WHERE status = 'pending'
GROUP BY to_daemon;
```

---

## Online státusz számítás

Egy daemon "online" ha az utolsó heartbeat 5 percen belül volt:

```javascript
const ONLINE_THRESHOLD_MS = 5 * 60 * 1000; // 5 perc
const isOnline = (Date.now() - new Date(last_heartbeat).getTime()) < ONLINE_THRESHOLD_MS;
```

---

## Adatbázis inicializálás

Új adatbázis létrehozása (teszt/dev):

```bash
sqlite3 /opt/spaceos/datahaven/messages.db < init.sql
```

**init.sql:**
```sql
CREATE TABLE IF NOT EXISTS daemons (
    id TEXT PRIMARY KEY,
    description TEXT,
    last_heartbeat TEXT
);

CREATE TABLE IF NOT EXISTS messages (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    from_daemon TEXT NOT NULL,
    to_daemon TEXT NOT NULL,
    msg_type TEXT DEFAULT 'task',
    subject TEXT,
    payload TEXT,
    priority TEXT DEFAULT 'medium',
    status TEXT DEFAULT 'pending',
    created_at TEXT DEFAULT CURRENT_TIMESTAMP,
    correlation_id TEXT
);

CREATE INDEX IF NOT EXISTS idx_messages_status ON messages(status);
CREATE INDEX IF NOT EXISTS idx_messages_to_daemon ON messages(to_daemon);
CREATE INDEX IF NOT EXISTS idx_messages_created ON messages(created_at DESC);
```
