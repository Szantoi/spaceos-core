# Nexus Infrastructure Audit — Task Audit & Formal Review Support

> **Dátum:** 2026-06-23
> **Cél:** Inventory of existing infrastructure in spaceos-nexus/knowledge-service that can be reused for Task Audit & Formal Review implementation

---

## 1. Meglévő Infrastruktúra Összefoglaló

### A. Express HTTP API Server

**Fájl:** `src/server.ts` (71,811 bytes)

**Már létező endpoint-ok:**

```typescript
// Knowledge & Search
POST /api/knowledge/search
POST /api/knowledge/index

// Mailbox Management
POST /api/mailbox/:terminal/inbox        // ✅ Inbox creation már van!
POST /api/mailbox/:terminal/outbox
POST /api/mailbox/:terminal/:box/:messageId/read
POST /api/mailbox/broadcast

// Session Management (MCP)
POST /api/session/start                  // ✅ Session indítás van
POST /api/session/inject
POST /api/session/wake
GET  /api/session/:terminal
GET  /api/session/all
GET  /api/session/logs                   // ✅ Audit log van

// Terminal Status
POST /api/terminal/status                // ✅ Status tracking van
POST /api/terminal/:terminal/status

// Memory Management (ADR-046 Track D)
POST /api/memories/save
POST /api/memories/:id/promote
POST /api/session/start-context
POST /api/session/end
GET  /api/session/history

// Daily Digest
POST /api/digest/generate                // ✅ Daily report infrastruktúra van
GET  /api/digest/:terminal/:date

// Graph API
GET  /api/graph/epics                    // ✅ Project tracking van
GET  /api/graph/critical-path/epic/:epic
GET  /api/graph/mermaid/:type/:id

// Auth
POST /api/auth/verify                    // ✅ Token verification van
```

**Újra használható API patterns:**
- ✅ POST /api/mailbox/:terminal/inbox — inbox creation logic
- ✅ POST /api/session/start — session management
- ✅ POST /api/auth/verify — token verification
- ✅ GET /api/session/logs — audit log query
- ✅ POST /api/digest/generate — daily report generation

**Hiányzik (új endpoint-ok kellenek):**
- ❌ POST /api/task/create — formal task creation with audit
- ❌ GET /api/task/audit?date=YYYY-MM-DD — audit log query
- ❌ POST /api/task/validate — formal review trigger

---

### B. Authentication & Authorization

**Fájl:** `src/server.ts` lines 1117-1133

```typescript
// Simple auth token (no database, just static token from env)
const DASHBOARD_TOKEN = process.env.DASHBOARD_AUTH_TOKEN || 'dev-token-spaceos-dashboard-2026';

function verifyAuthToken(req: Request, res: Response) {
  const authHeader = req.headers.authorization;
  const token = authHeader?.replace('Bearer ', '');

  if (token === DASHBOARD_TOKEN) {
    res.json({ valid: true });
  } else {
    res.status(401).json({ valid: false, message: 'Invalid token' });
  }
}

app.get('/api/auth/verify', verifyAuthToken);
app.post('/api/auth/verify', verifyAuthToken);
```

**Jelenleg:**
- ✅ Bearer token support
- ✅ Environment variable token (DASHBOARD_AUTH_TOKEN)
- ⚠️ Single global token (no role-based access)
- ❌ No token database
- ❌ No token scopes/permissions
- ❌ No token rotation/expiry

**Újra használható:**
- Bearer token extraction logic
- Authorization header parsing
- Simple token verification pattern

**Hiányzik (Task Audit Design szerint):**
- Token database/storage
- Role-based token scopes (`task:create:*`, `task:create:worker`)
- Token holder tracking (root, conductor, developer, operator)
- Token hash storage (SHA-256)

---

### C. Rate Limiting

**Fájl:** `src/server.ts` lines 50-100

```typescript
interface RateLimitEntry {
  count: number;
  resetAt: number;
}

const rateLimitStore = new Map<string, RateLimitEntry>();
const RATE_LIMIT_WINDOW = 60000; // 1 minute
const RATE_LIMIT_MAX = 500; // 500 requests per minute per IP

function rateLimit(req: Request, res: Response, next: NextFunction): void {
  // Get real IP from proxy headers
  const ip = (req.headers['x-real-ip'] as string) || ...;

  // Check rate limit
  if (entry.count >= RATE_LIMIT_MAX) {
    res.status(429).json({ error: 'Too many requests', retryAfter: ... });
    return;
  }

  entry.count++;
  next();
}
```

**Újra használható:**
- ✅ In-memory rate limit store (Map)
- ✅ IP-based tracking (X-Real-IP, X-Forwarded-For)
- ✅ Configurable window and max requests
- ✅ 429 Too Many Requests response

**Task Audit use case:**
- Rate limit POST /api/task/create endpoint (prevent spam)
- Per-token rate limiting (e.g., root: unlimited, conductor: 100/min)

---

### D. Pipeline Infrastructure

**Mappa:** `src/pipeline/` (54 files)

**Releváns modulok Task Audit & Formal Review-hoz:**

| Fájl | Méret | Funkcionalitás | Újra használható? |
|---|---|---|---|
| `hashUtils.ts` | 972 B | SHA-256 file hashing | ✅ **Már megvan** (létrehoztam) |
| `reviewLog.ts` | 2,650 B | JSONL review decision log | ✅ **Már megvan** (létrehoztam) |
| `reviewer.ts` | 21,151 B | Dual Haiku review + escalation | ✅ **Már módosítva** |
| `watchDone.ts` | 3,121 B | DONE outbox watcher | ✅ Formal review trigger |
| `common.ts` | 8,734 B | Pipeline utilities (log, paths) | ✅ Általános utils |
| `nightwatch.ts` | 4,053 B | Orchestrator loop | ✅ Pipeline coordination |
| `yamlValidator.ts` | 12,246 B | YAML schema validation | ✅ Task type config validation |
| `channelCoordinator.ts` | 11,336 B | Multi-channel notifications | ✅ Telegram/Slack notify |
| `telegramBot.ts` | 17,833 B | Telegram integration | ✅ Daily report notification |
| `projectDispatcher.ts` | 17,232 B | Project-based task routing | ✅ Project/epic tracking |
| `projectMatcher.ts` | 6,886 B | Match task to project | ✅ Auto-detect project |
| `memoryStore.ts` | 23,300 B | Terminal memory management | ⚠️ Session context |
| `hourlyDigest.ts` | 9,178 B | Hourly summary generation | ✅ **Daily report pattern** |
| `immediatePipeline.ts` | 7,911 B | Hybrid API immediate trigger | ✅ Real-time task creation |

**Újra használható patterns:**

1. **JSONL Log Pattern** (`reviewLog.ts`):
   ```typescript
   const line = JSON.stringify(decision) + '\n';
   await fs.appendFile(LOG_PATH, line, 'utf-8');
   ```
   → **Task creation log ugyanez a pattern**

2. **SHA-256 Hash Pattern** (`hashUtils.ts`):
   ```typescript
   const hash = createHash('sha256').update(content).digest('hex');
   return `sha256:${hash}`;
   ```
   → **Inbox file integrity verification**

3. **YAML Config Pattern** (`yamlValidator.ts` + task type configs):
   ```typescript
   const content = await fs.readFile(configPath, 'utf-8');
   return yaml.load(content) as TaskTypeConfig;
   ```
   → **Task type extensibility**

4. **Channel Notification Pattern** (`channelCoordinator.ts`):
   ```typescript
   await telegram(`🎯 Ma 8 task készült: 5 CODE, 2 COORD, 1 BUGFIX`);
   ```
   → **Daily report Telegram notification**

5. **Digest Generation Pattern** (`hourlyDigest.ts`):
   - Query logs by time window
   - Aggregate statistics
   - Generate Markdown report
   → **Daily task summary ugyanez**

---

### E. Database & Storage

**SQLite:**
```typescript
// package.json
"better-sqlite3": "^12.11.1"
```

**Jelenleg használat:**
- ❌ Nincs használva a knowledge-service-ben (only ChromaDB for embeddings)

**Potenciális használat Task Audit-hoz:**
- Token database (`tokens.db`)
- Task creation audit index (JSONL + SQLite index for fast query)
- Daily statistics cache

**Alternatíva:**
- ✅ JSONL append-only logs (immutable, git-tracked)
- ✅ In-memory cache (Map) + periodic flush
- ⚠️ SQLite later for query optimization

---

### F. Validation & Schema

**Zod dependency:**
```typescript
// package.json
"zod": "^4.4.3"
```

**Használat példa (server.ts):**
```typescript
const SearchBodySchema = z.object({
  q: z.string().min(1),
  topK: z.number().int().min(1).max(50).optional()
});

app.post('/api/knowledge/search', validate(SearchBodySchema), async (req, res) => {
  // ...
});
```

**Újra használható Task Audit-hoz:**
```typescript
const TaskCreationSchema = z.object({
  assigned_to: z.enum(['backend', 'frontend', 'designer', 'architect', ...]),
  task_type: z.enum(['CODE', 'COORDINATION', 'BUGFIX', ...]),
  review_type: z.enum(['formal', 'content', 'manual']),
  priority: z.enum(['critical', 'high', 'medium', 'low']),
  project: z.string().optional(),
  epic: z.string().optional(),
  title: z.string().min(1).max(200),
  content: z.string().min(1),
  metadata: z.object({
    estimated_workdays: z.number().optional(),
    dependencies: z.array(z.string()).optional()
  }).optional()
});
```

---

### G. File Watching & Auto-trigger

**Chokidar dependency:**
```typescript
// package.json
"chokidar": "^5.0.0"
```

**Használat:** `inboxWatcher.ts`, `watchDone.ts`, stb.

**Pattern:**
```typescript
const watcher = chokidar.watch('terminals/*/inbox/*.md', {
  persistent: true,
  ignoreInitial: true
});

watcher.on('add', async (filePath) => {
  // Auto-trigger on new inbox
});
```

**Formal Review use case:**
- Watch `terminals/*/outbox/*-done.md`
- Extract `review_type` from inbox
- If `formal` → trigger `formal-review.sh`
- If `content` → trigger dual Haiku review

---

### H. Telegram Integration

**Fájl:** `src/pipeline/telegramBot.ts` (17,833 bytes)

**Functionality:**
- ✅ Bot initialization
- ✅ Webhook setup
- ✅ Message sending
- ✅ Markdown formatting
- ✅ Channel notifications

**Daily Report use case:**
```typescript
await telegram(`
📊 *SpaceOS Daily Task Report — ${date}*

*Total tasks created:* 8

*By task type:*
• CODE: 5
• COORDINATION: 2
• BUGFIX: 1

*By project:*
• EPIC-CUTTING-Q3: 4
• EPIC-PORTAL-V2: 3
• INFRA-KC01: 1

*By creator:*
• root: 6
• conductor: 2
`);
```

---

## 2. Hiányzó Komponensek (Implementálni kell)

### Phase 1: Formal Review

| Komponens | Fájl | Státusz | Idő |
|---|---|---|---|
| Formal review script | `scripts/formal-review.sh` | ❌ | 30 min |
| Review routing | `src/pipeline/reviewer.ts` | ⚠️ Módosítás kell | 20 min |
| Review type extraction | `src/pipeline/reviewer.ts` | ⚠️ Módosítás kell | 10 min |
| Formal review log | `logs/reviews/formal.jsonl` | ❌ | 10 min |
| Task type configs update | `config/task-types/*.yaml` | ⚠️ `review_type` field | 10 min |

**Összesen:** ~1.5 óra

### Phase 2: Task Creation Audit

| Komponens | Fájl | Státusz | Idő |
|---|---|---|---|
| Task creation module | `src/taskCreation.ts` | ❌ | 1 óra |
| API endpoint | `src/server.ts` | ⚠️ Új route | 30 min |
| Token database | `config/tokens.yaml` vagy `data/tokens.db` | ❌ | 30 min |
| Token verification | `src/auth.ts` | ❌ | 30 min |
| Creation log | `logs/tasks/creation.jsonl` | ❌ | 20 min |
| Git auto-commit | `src/taskCreation.ts` | ❌ | 20 min |
| Zod schema | `src/schemas/taskCreation.ts` | ❌ | 20 min |

**Összesen:** ~3.5 óra

### Phase 3: Daily Report + Datahaven

| Komponens | Fájl | Státusz | Idő |
|---|---|---|---|
| Daily report script | `scripts/daily-report.sh` | ❌ | 30 min |
| Cron job setup | `/etc/cron.d/spaceos-daily-report` | ❌ | 10 min |
| API endpoint | `src/server.ts` | ⚠️ Új route | 20 min |
| Query function | `src/taskAudit.ts` | ❌ | 30 min |
| Datahaven widget | `datahaven-web/client/src/components/TaskSummary.tsx` | ❌ | 1 óra |
| SSE integration | Datahaven client | ✅ Már megvan | - |
| Telegram notification | `src/pipeline/telegramBot.ts` | ⚠️ Új message template | 20 min |

**Összesen:** ~2.5 óra

---

## 3. Technológiai Stack Összefoglaló

| Technológia | Verzió | Használat | Task Audit support |
|---|---|---|---|
| **Node.js** | 22+ | Runtime | ✅ |
| **TypeScript** | 5.9.3 | Language | ✅ |
| **Express** | 5.2.1 | HTTP API | ✅ |
| **better-sqlite3** | 12.11.1 | Database (optional) | ⚠️ Later |
| **js-yaml** | 5.0.0 | YAML parsing | ✅ Task type configs |
| **zod** | 4.4.3 | Validation | ✅ Request schema |
| **gray-matter** | 4.0.3 | Frontmatter parsing | ✅ Inbox parsing |
| **chokidar** | 5.0.0 | File watching | ✅ Auto-trigger |
| **crypto** | (Node.js) | SHA-256 hashing | ✅ hashUtils.ts |

---

## 4. Fájl Struktúra (Javasolt)

```
spaceos-nexus/knowledge-service/
├── src/
│   ├── server.ts                      ← Új route: POST /api/task/create
│   ├── taskCreation.ts                ← ❌ ÚJ modul
│   ├── taskAudit.ts                   ← ❌ ÚJ modul (query, report)
│   ├── auth.ts                        ← ❌ ÚJ modul (token verification)
│   ├── schemas/
│   │   └── taskCreation.ts            ← ❌ ÚJ Zod schema
│   └── pipeline/
│       ├── hashUtils.ts               ← ✅ Megvan
│       ├── reviewLog.ts               ← ✅ Megvan
│       ├── reviewer.ts                ← ⚠️ Módosítás kell (review_type routing)
│       ├── watchDone.ts               ← ⚠️ Módosítás kell (formal review trigger)
│       └── formalReview.ts            ← ❌ ÚJ modul (formal review logic)
│
├── scripts/
│   ├── formal-review.sh               ← ❌ ÚJ script
│   └── daily-report.sh                ← ❌ ÚJ script
│
├── config/
│   ├── task-types/                    ← ✅ Megvan (6 YAML file)
│   │   ├── CODE.yaml                  ← ⚠️ review_type field hozzá
│   │   └── ...
│   └── tokens.yaml                    ← ❌ ÚJ config
│
└── logs/
    ├── reviews/
    │   ├── decisions.jsonl            ← ✅ Megvan (content review)
    │   └── formal.jsonl               ← ❌ ÚJ log (formal review)
    └── tasks/
        └── creation.jsonl             ← ❌ ÚJ log (task audit)
```

---

## 5. Ajánlás: Implementációs Sorrend

### Option A: Phase 1 First (Gyors Win)

**Előny:**
- ✅ Azonnal költségmegtakarítás (formal review $0 vs. dual review $0.02)
- ✅ Gyorsabb review ciklus (30 sec vs. 3 min)
- ✅ Kisebb scope (~1.5 óra)
- ✅ Immediate value

**Hátrány:**
- ⚠️ Nincs audit trail task creation-re
- ⚠️ Token-based auth később jön

**Javasolt ha:**
- Költség/sebesség prioritás
- Gyorsan kell eredmény

---

### Option B: Phase 2 First (Foundation)

**Előny:**
- ✅ Alapvető audit infrastruktúra megvan
- ✅ Token-based auth működik
- ✅ "Mit csináltunk ma?" report később könnyen épül rá
- ✅ Formal review later is használhatja az audit log-ot

**Hátrány:**
- ⚠️ Nagyobb scope (~3.5 óra)
- ⚠️ Később lesz költségmegtakarítás

**Javasolt ha:**
- Audit trail prioritás
- Hosszútávú foundation építés

---

### Option C: Hybrid (Parallel)

**Phase 1a: Formal Review Script** (30 min)
- `scripts/formal-review.sh` létrehozása

**Phase 2a: Task Creation API** (2 óra)
- `src/taskCreation.ts` + API endpoint
- Token verification (basic)
- JSONL audit log

**Phase 1b: Review Routing** (30 min)
- `reviewer.ts` módosítás (review_type routing)

**Phase 3: Daily Report** (később)

**Előny:**
- ✅ Mindkét irány halad párhuzamosan
- ✅ 3 óra alatt mindkettő működik

**Hátrány:**
- ⚠️ Komplexebb koordináció

---

## 6. Következő Lépés: Root Döntés

**Kérdések:**

1. **Melyik implementációs sorrendet választod?**
   - Option A: Phase 1 First (formal review)
   - Option B: Phase 2 First (audit foundation)
   - Option C: Hybrid (parallel)

2. **Token storage:**
   - `config/tokens.yaml` (egyszerű, git-tracked)
   - `data/tokens.db` (SQLite, query-elhető)
   - Environment variables (jelenlegi pattern)

3. **Formal review criteria:**
   - Minimal: frontmatter + git commit
   - Standard: + build success
   - Full: + tests + lint

4. **Daily report output:**
   - `docs/reports/daily/` + git commit
   - Csak Telegram notification
   - Csak Datahaven API (query on demand)

**Mit preferálsz?**
