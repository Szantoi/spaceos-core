# Task Audit & Formal Review — Design Document

> **Dátum:** 2026-06-23
> **Státusz:** DESIGN
> **Owner:** Root
> **Kapcsolódik:** REVIEWER_SECURITY_ARCHITECTURE.md, ADR-041 (Graph-based Workflow)

---

## Összefoglaló

Két kritikus fejlesztés a reviewer és task management rendszerhez:

1. **Formal Review** — automatizált ellenőrzés egyszerű taskoknál (gyorsabb, olcsóbb)
2. **Task Audit Trail** — task creation log + jogosultság + projekt tracking

---

## 1. Formal vs. Tartalmi Review

### Probléma

**Jelenleg**: Minden DONE → dual Haiku review (költséges, lassú)

Vannak egyszerű feladatok ahol elég automatizált check:
- "Frissítsd a README-t" → elég ha: ✅ file exists, ✅ frontmatter valid, ✅ git commit OK
- "Add hozzá config opciót" → elég ha: ✅ build success, ✅ tests pass
- "Javítsd a typo-t" → elég ha: ✅ lint pass

### Megoldás: `review_type` field

Inbox frontmatter új field:

```yaml
---
id: MSG-BACKEND-123
from: root
to: backend
type: task
priority: medium
status: UNREAD
model: sonnet
task_type: CODE
review_type: formal  # formal | content | manual
created: 2026-06-23
---
```

### Review Type definíciók

| Review Type | Mit jelent | Ellenőrzés | Idő | Költség |
|---|---|---|---|---|
| `formal` | Automatikus check elég | `scripts/formal-review.sh` | ~30 sec | $0 |
| `content` | Dual Haiku review kell | `reviewer.ts` (jelenlegi) | ~3 min | ~$0.02 |
| `manual` | Csak Root review | Skip automation → Root inbox | - | manual |

### Formal Review Script

**Lokáció:** `/opt/spaceos/scripts/formal-review.sh`

**Ellenőrzések:**

```bash
#!/bin/bash
# formal-review.sh - Automated formal checks (no LLM)

DONE_FILE=$1
TERMINAL=$2

# 1. Frontmatter validation
check_frontmatter() {
  grep -q "^status: DONE$" "$DONE_FILE" || return 1
  grep -q "^files_changed:" "$DONE_FILE" || return 1
  return 0
}

# 2. Git commit format
check_git_commit() {
  # Extract git_commit from DONE
  COMMIT=$(grep "^git_commit:" "$DONE_FILE" | cut -d' ' -f2)
  [ -n "$COMMIT" ] || return 1
  git rev-parse --verify "$COMMIT" &>/dev/null || return 1
  return 0
}

# 3. Build success (if code changes)
check_build() {
  # Check if CODE task
  TASK_TYPE=$(grep "^task_type:" "$DONE_FILE" | cut -d' ' -f2)
  if [ "$TASK_TYPE" = "CODE" ]; then
    # Run build based on terminal
    case $TERMINAL in
      backend)
        cd /opt/spaceos && dotnet build --no-restore || return 1
        ;;
      frontend)
        cd /opt/spaceos/datahaven-web/client && pnpm build || return 1
        ;;
    esac
  fi
  return 0
}

# 4. Test pass (if CODE task)
check_tests() {
  TASK_TYPE=$(grep "^task_type:" "$DONE_FILE" | cut -d' ' -f2)
  if [ "$TASK_TYPE" = "CODE" ]; then
    # Skip tests for DOCUMENTATION/COORDINATION
    return 0  # TODO: implement test runner
  fi
  return 0
}

# 5. Lint pass
check_lint() {
  # TODO: implement linter based on changed files
  return 0
}

# Run all checks
echo "[FormalReview] Checking $DONE_FILE"
check_frontmatter || { echo "❌ Frontmatter invalid"; exit 1; }
check_git_commit || { echo "❌ Git commit invalid"; exit 1; }
check_build || { echo "❌ Build failed"; exit 1; }
check_tests || { echo "❌ Tests failed"; exit 1; }
check_lint || { echo "❌ Lint failed"; exit 1; }

echo "✅ Formal review PASSED"
exit 0
```

### Reviewer.ts módosítás

```typescript
// reviewer.ts - review type routing

async function runReview(donePath: string, terminal: string) {
  const inboxPath = findInboxForDone(donePath);
  const inboxContent = await fs.readFile(inboxPath, 'utf-8');

  // Extract review_type from inbox
  const reviewTypeMatch = inboxContent.match(/^review_type:\s*(.+)$/m);
  const reviewType = reviewTypeMatch ? reviewTypeMatch[1].trim() : 'content';  // default

  switch (reviewType) {
    case 'formal':
      return await runFormalReview(donePath, terminal);
    case 'content':
      return await runDualReview(donePath, terminal);
    case 'manual':
      return await escalateToRoot(donePath, terminal, 'MANUAL_REVIEW_REQUIRED');
    default:
      await log(`[Reviewer] Unknown review_type: ${reviewType}, defaulting to content`);
      return await runDualReview(donePath, terminal);
  }
}

async function runFormalReview(donePath: string, terminal: string) {
  const { exec } = require('child_process');
  return new Promise((resolve, reject) => {
    exec(`/opt/spaceos/scripts/formal-review.sh "${donePath}" "${terminal}"`,
      (error, stdout, stderr) => {
        if (error) {
          resolve({ approved: false, reason: 'Formal checks failed', stdout, stderr });
        } else {
          resolve({ approved: true, reason: 'Formal checks passed', stdout });
        }
      }
    );
  });
}
```

### Előnyök

- 🚀 **Gyorsabb**: formal check ~30 sec vs. dual review ~3 min
- 💰 **Olcsóbb**: no LLM cost ($0 vs. ~$0.02/review)
- 🎯 **Pontosabb**: egyszerű taskokhoz felesleges LLM
- ⚡ **Skálázható**: több egyidejű formal review lehetséges

---

## 2. Task Audit Trail — Creation Log

### Probléma

**Jelenleg hiányzik:**
- ❌ Ki hozta létre a taskot? (`from:` field van, de nincs authentication)
- ❌ Token alapú jogosultság ellenőrzés
- ❌ Projekt/epic/task hierarchia tracking
- ❌ "Mit csináltunk ma?" report

### Megoldás: Task Creation JSONL Log

**Lokáció:** `/opt/spaceos/logs/tasks/creation.jsonl`

**Formátum:**

```json
{
  "timestamp": "2026-06-23T05:30:00Z",
  "task_id": "MSG-BACKEND-123",
  "created_by": "root",
  "created_by_token_hash": "sha256:abc123...",
  "assigned_to": "backend",
  "project": "EPIC-CUTTING-Q3",
  "epic": "Track-A-Customer-Portal",
  "task_type": "CODE",
  "review_type": "content",
  "priority": "high",
  "inbox_path": "terminals/backend/inbox/2026-06-23_123_cutting-ui.md",
  "inbox_hash": "sha256:def456...",
  "metadata": {
    "estimated_workdays": 2,
    "dependencies": ["MSG-KERNEL-045"],
    "blocking": []
  }
}
```

### API Endpoint (knowledge-service)

**Endpoint:** `POST /api/task/create`

**Headers:**
```
Authorization: Bearer <token>
Content-Type: application/json
```

**Request Body:**
```json
{
  "assigned_to": "backend",
  "task_type": "CODE",
  "review_type": "content",
  "priority": "high",
  "project": "EPIC-CUTTING-Q3",
  "epic": "Track-A-Customer-Portal",
  "title": "Implement cutting plan UI",
  "content": "...",
  "metadata": {
    "estimated_workdays": 2,
    "dependencies": ["MSG-KERNEL-045"]
  }
}
```

**Response:**
```json
{
  "success": true,
  "task_id": "MSG-BACKEND-123",
  "inbox_path": "terminals/backend/inbox/2026-06-23_123_cutting-ui.md",
  "creation_log_id": "TCREATE-2026-06-23-1687512600-001"
}
```

### Jogosultság Mátrix

| Token holder | Hozhat-e létre taskot? | Kinek? | Token scope |
|---|---|---|---|
| **root** | ✅ | MINDENKI (8 terminál) | `task:create:*` |
| **conductor** | ✅ | architect, librarian, explorer, backend, frontend, designer | `task:create:worker` |
| **developer** | ❌ | - | - |
| **operator** | ❌ | - | - |
| **external** | ❌ | - | - |

### Token Verification

```typescript
// taskCreation.ts

import { createHash } from 'crypto';

interface TaskCreationRequest {
  assigned_to: string;
  task_type: string;
  review_type: string;
  priority: string;
  project?: string;
  epic?: string;
  title: string;
  content: string;
  metadata?: {
    estimated_workdays?: number;
    dependencies?: string[];
  };
}

async function verifyToken(token: string, requiredScope: string): Promise<boolean> {
  // Token hash verification
  const tokenHash = createHash('sha256').update(token).digest('hex');

  // TODO: implement token database lookup
  // For now: hardcoded root/conductor tokens
  const validTokens = {
    'dev-token-root-2026': { holder: 'root', scopes: ['task:create:*'] },
    'dev-token-conductor-2026': { holder: 'conductor', scopes: ['task:create:worker'] }
  };

  const tokenData = validTokens[token];
  if (!tokenData) return false;

  // Check scope
  if (tokenData.scopes.includes(requiredScope) || tokenData.scopes.includes('task:create:*')) {
    return true;
  }

  return false;
}

async function createTask(req: TaskCreationRequest, token: string): Promise<any> {
  // 1. Verify token
  if (!await verifyToken(token, 'task:create:*')) {
    throw new Error('Unauthorized: invalid token or insufficient scope');
  }

  // 2. Generate task_id
  const nextNum = await getNextInboxNumber(req.assigned_to);
  const taskId = `MSG-${req.assigned_to.toUpperCase()}-${String(nextNum).padStart(3, '0')}`;

  // 3. Create inbox file
  const date = new Date().toISOString().split('T')[0];
  const inboxPath = path.join(
    SPACEOS_ROOT,
    `terminals/${req.assigned_to}/inbox/${date}_${String(nextNum).padStart(3, '0')}_${slugify(req.title)}.md`
  );

  const frontmatter = `---
id: ${taskId}
from: ${getTokenHolder(token)}
to: ${req.assigned_to}
type: task
priority: ${req.priority}
status: UNREAD
model: sonnet
task_type: ${req.task_type}
review_type: ${req.review_type}
project: ${req.project || ''}
epic: ${req.epic || ''}
created: ${date}
---

${req.content}
`;

  await fs.writeFile(inboxPath, frontmatter);

  // 4. Compute hash
  const inboxHash = await sha256File(inboxPath);

  // 5. Log to creation.jsonl
  const creationLog = {
    timestamp: new Date().toISOString(),
    task_id: taskId,
    created_by: getTokenHolder(token),
    created_by_token_hash: createHash('sha256').update(token).digest('hex'),
    assigned_to: req.assigned_to,
    project: req.project,
    epic: req.epic,
    task_type: req.task_type,
    review_type: req.review_type,
    priority: req.priority,
    inbox_path: inboxPath,
    inbox_hash: inboxHash,
    metadata: req.metadata || {}
  };

  await appendTaskCreationLog(creationLog);

  // 6. Git commit
  await exec(`git add "${inboxPath}" && git commit -m "task: ${taskId} created by ${getTokenHolder(token)}"`);

  return {
    success: true,
    task_id: taskId,
    inbox_path: inboxPath,
    creation_log_id: `TCREATE-${date}-${Date.now()}-${String(Math.floor(Math.random()*1000)).padStart(3,'0')}`
  };
}
```

### Daily Report

**Lokáció:** `/opt/spaceos/scripts/daily-report.sh`

```bash
#!/bin/bash
# daily-report.sh - Generate daily task creation report

DATE=${1:-$(date +%Y-%m-%d)}
LOG_FILE="/opt/spaceos/logs/tasks/creation.jsonl"

echo "# Daily Task Report — $DATE"
echo ""

# Total tasks created
TOTAL=$(grep "\"timestamp\":\"$DATE" "$LOG_FILE" | wc -l)
echo "**Total tasks created:** $TOTAL"
echo ""

# By task type
echo "## By Task Type"
jq -r "select(.timestamp | startswith(\"$DATE\")) | .task_type" "$LOG_FILE" | sort | uniq -c | while read count type; do
  echo "- $type: $count"
done
echo ""

# By project
echo "## By Project"
jq -r "select(.timestamp | startswith(\"$DATE\")) | .project // \"No Project\"" "$LOG_FILE" | sort | uniq -c | while read count proj; do
  echo "- $proj: $count"
done
echo ""

# By creator
echo "## By Creator"
jq -r "select(.timestamp | startswith(\"$DATE\")) | .created_by" "$LOG_FILE" | sort | uniq -c | while read count creator; do
  echo "- $creator: $count"
done
echo ""

# By assignee
echo "## By Assignee"
jq -r "select(.timestamp | startswith(\"$DATE\")) | .assigned_to" "$LOG_FILE" | sort | uniq -c | while read count assignee; do
  echo "- $assignee: $count"
done
```

**Output példa:**

```markdown
# Daily Task Report — 2026-06-23

**Total tasks created:** 8

## By Task Type
- CODE: 5
- COORDINATION: 2
- BUGFIX: 1

## By Project
- EPIC-CUTTING-Q3: 4
- EPIC-PORTAL-V2: 3
- INFRA-KC01: 1

## By Creator
- root: 6
- conductor: 2

## By Assignee
- backend: 5
- frontend: 2
- infra: 1
```

### Datahaven Integration

**Projects oldal fejlesztés:**
- Task creation timeline (horizontal timeline)
- Filter by project/epic/creator/assignee
- "Mit csináltunk ma?" widget (live SSE update)

**API endpoint:**
```bash
GET /api/tasks/daily-summary?date=2026-06-23
```

**Response:**
```json
{
  "date": "2026-06-23",
  "total": 8,
  "by_task_type": {
    "CODE": 5,
    "COORDINATION": 2,
    "BUGFIX": 1
  },
  "by_project": {
    "EPIC-CUTTING-Q3": 4,
    "EPIC-PORTAL-V2": 3,
    "INFRA-KC01": 1
  },
  "by_creator": {
    "root": 6,
    "conductor": 2
  },
  "by_assignee": {
    "backend": 5,
    "frontend": 2,
    "infra": 1
  },
  "tasks": [
    {
      "task_id": "MSG-BACKEND-123",
      "timestamp": "2026-06-23T05:30:00Z",
      "created_by": "root",
      "assigned_to": "backend",
      "project": "EPIC-CUTTING-Q3",
      "epic": "Track-A-Customer-Portal",
      "task_type": "CODE",
      "priority": "high"
    }
    // ... more tasks
  ]
}
```

---

## 3. Implementációs Terv

### Phase 1: Formal Review (gyors win)

**Scope:** Automatizált review egyszerű taskokhoz

**Lépések:**
1. `scripts/formal-review.sh` létrehozása
   - Frontmatter validation
   - Git commit format check
   - Build success (CODE taskok)
   - Lint check (optional)
2. `reviewer.ts` módosítás
   - `review_type` extraction from inbox
   - Route to `runFormalReview()` if `review_type: formal`
3. Inbox template frissítés
   - `review_type:` field hozzáadása minden task type config-hoz
4. Teszt
   - Egyszerű DOCUMENTATION task → formal review
   - Ellenőrzés: JSONL log tartalmazza formal review eredményt

**Idő:** 1-2 óra
**Érték:** Gyorsabb review, olcsóbb működés

### Phase 2: Task Creation Log (audit foundation)

**Scope:** Immutable task creation audit trail + jogosultság

**Lépések:**
1. `taskCreation.ts` modul létrehozása
   - `createTask()` function
   - `verifyToken()` function
   - `appendTaskCreationLog()` function
2. API endpoint (knowledge-service)
   - `POST /api/task/create`
   - Token verification middleware
   - Response: task_id, inbox_path, creation_log_id
3. Token database
   - Hardcoded root/conductor tokens (kezdetben)
   - Later: token management UI
4. Git auto-commit
   - Minden inbox creation → git commit
5. Teszt
   - Root token → create task backend-nek
   - Invalid token → 401 Unauthorized
   - JSONL log check

**Idő:** 3-4 óra
**Érték:** Audit trail, jogosultság, integrity

### Phase 3: Daily Report + Datahaven Integration

**Scope:** Daily summary + Datahaven visualization

**Lépések:**
1. `scripts/daily-report.sh` létrehozása
   - JSONL query by date
   - Markdown output
2. Cron job setup
   - Daily 23:59 → generate report
   - Save to `docs/reports/daily/YYYY-MM-DD.md`
3. Datahaven API endpoint
   - `GET /api/tasks/daily-summary`
   - Query creation.jsonl by date
   - Return JSON summary
4. Datahaven Projects oldal widget
   - "Mit csináltunk ma?" card
   - SSE real-time update
   - Filter by project/epic/creator
5. Telegram notification
   - Daily summary at 23:59
   - Format: "🎯 Ma 8 task készült: 5 CODE, 2 COORD, 1 BUGFIX"

**Idő:** 2-3 óra
**Érték:** Transparency, reporting, tracking

---

## 4. Nyitott Kérdések

1. **Implementálási sorrend**: Melyik phase-zel kezdjük? (Phase 1 vagy Phase 2?)

2. **Token rendszer**:
   - Hol tároljuk a token-eket? (környezeti változó, config file, database?)
   - Token rotation policy?
   - Token revocation mechanism?

3. **Formal review criteria**:
   - Milyen checkek kellenek? (build, test, lint, git commit format?)
   - Kell-e különböző formal check profil task type-onként?

4. **Daily report**:
   - Hova kerüljön? (`docs/reports/daily/`, Telegram, Datahaven API?)
   - Kell-e weekly/monthly summary is?

5. **Projekt/epic tracking**:
   - Honnan jön a projekt/epic információ? (manual inbox frontmatter vagy auto-detect?)
   - Kell-e PROJECTS.yaml szinkronizálás?

6. **Backward compatibility**:
   - Mi történik a meglévő inbox üzenetekkel amiknek nincs `review_type` field?
   - Default: `content` review?

---

## 5. Kapcsolódó Dokumentumok

- `REVIEWER_SECURITY_ARCHITECTURE.md` — Review decision log, SHA-256 hashing
- `ADR-041` — Graph-based workflow (EPICS.yaml)
- `WORKFLOW.md` — Pipeline definitions
- `docs/knowledge/architecture/ADR_CATALOGUE.md` — Architectural decisions

---

## 6. Verzió Történet

| Dátum | Verzió | Változás |
|---|---|---|
| 2026-06-23 | 1.0.0 | Initial design (formal review + task audit) |

---

**Next steps:** Root döntés melyik phase-zel induljon az implementáció.
