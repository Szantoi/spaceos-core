# CLAUDE.md — SpaceOS Backend

> **Modell: `sonnet`**
>
> A Backend terminál az összes backend kódot fejleszti:
> .NET 8 modulok (Kernel, Joinery, Cutting, Identity, stb.) és Node.js Orchestrator.

---

## 🔧 NEXUS ROUTING — INFRASTRUKTÚRA HIBÁK (2026-07-10)

> **FONTOS:** Ha agent infrastruktúra problémát találsz, NE a Root-nak küldd!
> A **Nexus terminál** felelős minden knowledge-service, MCP, pipeline hibáért.

### Mikor küldj Nexus-nak?

| Probléma típus | Példa | Hova küldöd? |
|----------------|-------|--------------|
| MCP tool nem működik | `list_inbox` hibát dob | **→ Nexus** |
| Session management bug | Session nem indul, stuck | **→ Nexus** |
| Pipeline hiba | Nightwatch, watchDone nem fut | **→ Nexus** |
| Knowledge service crash | Port 3456 nem válaszol | **→ Nexus** |
| Új MCP tool igény | "Kellene egy tool ami..." | **→ Nexus** |
| Teljesítmény probléma | MCP lassú, timeout | **→ Nexus** |
| Üzleti logika kérdés | Joinery, Cutting, Doorstar | **→ Root/Conductor** |

### Hogyan küldj Nexus-nak?

```
mcp__spaceos-knowledge__create_task
  from: "backend"
  to: "nexus"
  title: "MCP tool bug: list_inbox timeout"
  description: "A list_inbox 30+ másodpercig fut nagy inbox esetén..."
  priority: "high"
```

### Bug Report Sablon

```markdown
## Probléma
[1 mondat leírás]

## Reprodukálás
1. [Lépés 1]
2. [Lépés 2]

## Elvárt viselkedés
[Mi lett volna a helyes?]

## Aktuális viselkedés
[Mi történt helyette?]

## Log/Error (ha van)
[Hibaüzenet]
```

---

## ⚡ TOKEN OPTIMIZATION — BEST PRACTICES

> **2026-07-02: MCP `list_inbox` optimalizálva** — Metadata only by default (10× token reduction)

### Inbox Listing

**✅ Recommended (lightweight):**
```
mcp__spaceos-knowledge__list_inbox
  terminal: "TERMINAL_NAME"
  status: "UNREAD"              ← Only unread (best performance)
  # include_content: false      ← Default: metadata only
```

**Token costs:**
- `status: "UNREAD"`: ~15-20 tokens/message (metadata only)
- `status: "all"`: All messages (10× more tokens)
- `include_content: true`: Full content (50× more tokens for large inboxes)

**Use `include_content: true` only when:**
- Debugging message parsing
- Manual audit required
- NOT for routine checks!

---
## 🚀 PARALLEL WORKERS & DOMAIN MEMORY (ADR-049 Phase 3)

> **ÚJ:** A backend terminál párhuzamos workereket tud indítani domain-specifikus memóriával!

### Domain Memory Struktúra

```
terminals/backend/knowledge/
├── kernel.memory.md      ← Kernel domain (Auth, RBAC, Audit, FSM)
├── joinery.memory.md     ← Joinery domain (Ajtó/ablak konfig, Doorstar)
├── cutting.memory.md     ← Cutting domain (Lapszabászat, Quote API)
├── orchestrator.memory.md ← Orchestrator (BFF, LLM Tools)
├── nexus.memory.md       ← Knowledge Service (MCP, Workers)
└── shared.memory.md      ← Cross-domain patterns (MINDIG betöltődik)
```

### Mikor töltődik be melyik memória?

| Feladat kulcsszavak | Betöltött memória |
|---------------------|-------------------|
| Kernel, Auth, RBAC, Tenant, FSM | `kernel.memory.md` |
| Joinery, Door, Ajtó, Doorstar | `joinery.memory.md` |
| Cutting, Quote, Szabászat, Nesting | `cutting.memory.md` |
| Orchestrator, BFF, Tool Calling | `orchestrator.memory.md` |
| Nexus, MCP, Knowledge, Worker | `nexus.memory.md` |
| *Minden task* | `shared.memory.md` |

### Parallel Workers Használata

**Független taskok párhuzamos végrehajtása:**

```bash
# 3 párhuzamos worker indítása (API, Tests, Docs)
curl -X POST localhost:3456/api/mcp/spawn_parallel_workers \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "backend",
    "tasks": [
      {"id": "api", "prompt": "Implement /api/cutting/materials endpoint"},
      {"id": "tests", "prompt": "Write unit tests", "depends_on": ["api"]},
      {"id": "docs", "prompt": "Update API docs"}
    ]
  }'
```

**Raw workers (Best-of-N prototípus):**

```bash
# 3 alternatív megoldás, legjobb kiválasztása
curl -X POST localhost:3456/api/mcp/spawn_raw_workers \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "backend",
    "task": "Implement a caching strategy for materials API",
    "count": 3,
    "model": "haiku",
    "criteria": "Performance + simplicity"
  }'
```

**Worker státusz lekérdezés:**

```bash
curl -s localhost:3456/api/mcp/get_worker_status?terminal=backend
```

### Költség Limitek

| Szint | Limit | Akció |
|-------|-------|-------|
| Soft | $3/hour | Log warning |
| Hard | $5/hour | Telegram alert |
| Critical | $10/hour | Auto-kill + escalate |

**Max parallel workers:** 5 (hard cap)

### Session Naming

| Session | Model | Purpose |
|---------|-------|---------|
| `spaceos-backend` | Sonnet | Main work session |
| `spaceos-backend-chat` | Haiku | Telegram, quick responses |
| `spaceos-backend-work-001` | Sonnet | Parallel worker #1 |
| `spaceos-backend-raw-001` | Haiku | Raw prototype #1 |

---

## ⚡ TELEGRAM VÁLASZ — KÖTELEZŐ

**Ha `[TG @user chat:CHATID]` formátumú üzenetet kapsz:**

1. **MINDIG** használd az MCP `telegram_reply` tool-t a válaszhoz
2. **MINDIG** add meg a `from_terminal: "backend"` paramétert!
3. **NE** írj a konzolra/stdout-ra — az nem jut el a userhez!

```
mcp__spaceos-knowledge__telegram_reply
  chat_id: <CHATID a beérkező üzenetből>
  message: "A válaszod ide"
  from_terminal: "backend"
```

**Példa:**
- Beérkező: `[TG @Gábor chat:8426048796] Mi a státuszod?`
- Te: `mcp__spaceos-knowledge__telegram_reply(chat_id: 8426048796, message: "Assembly endpoint kész, tesztek futnak.", from_terminal: "backend")`

---

## ADR-053: CHECKPOINT-BASED TASK PROTOCOL (KÖTELEZŐ!)

> ⚠️ **KRITIKUS: Ez a protocol kötelező minden task feldolgozásnál!**
> A rendszer figyeli az MCP hívásokat — ha nem követed, alert generálódik.

### TASK LIFECYCLE (3 MCP hívás)

**1. TASK FOGADÁS — AZONNAL (5 percen belül!):**
```
mcp__spaceos-knowledge__ack_task
  terminal: "backend"
  message_id: "MSG-BACKEND-NNN"
```
→ Ha 5 percen belül nincs ACK, alert megy Root-nak!

**2. TASK LEKÉRÉS (opcionális, ha kell a tartalom):**
```
mcp__spaceos-knowledge__fetch_task
  terminal: "backend"
  message_id: "MSG-BACKEND-NNN"
```

**3. TASK BEFEJEZÉS — MINDIG:**
```
mcp__spaceos-knowledge__complete_task
  terminal: "backend"
  message_id: "MSG-BACKEND-NNN"
```

### MIÉRT FONTOS?

- **Subscription trigger** — a Conductor/Root feliratkozott a task állapotára
- **Audit trail** — minden lépés naplózva van
- **Checkpoint coordination** — az epic haladás automatikusan frissül
- **Telegram értesítés** — DONE esetén automatikus notification megy

---

## SESSION RITUAL — EPIC-AWARE TASK ROUTING (2026-06-24)

> ⚠️ **FONTOS: Csak a neked kiosztott taskot dolgozhatod fel!**
>
> A rendszer automatikusan injektálja a task ID-t a session indításakor.
> Nem férhetsz hozzá közvetlenül a mailbox-hoz — csak az MCP-n keresztül kérheted le a task tartalmát.

### 1. TASK FOGADÁSA

Amikor a session indul, egy `[TASK ASSIGNED]` üzenetet kapsz a task ID-val.

**KÖTELEZŐ: Task fogadásának megerősítése MCP-n:**
```
mcp__spaceos-knowledge__ack_task
  terminal: "backend"
  message_id: "MSG-BACKEND-NNN"
```

**Task tartalom lekérése MCP-n:**
```
mcp__spaceos-knowledge__fetch_task
  terminal: "backend"
  message_id: "MSG-BACKEND-NNN"
```

> ⚠️ **BIZTONSÁGI KORLÁT:** Csak az aktuálisan neked kiosztott taskot tudod lekérni!
> Más task ID-val próbálkozva `403 Forbidden` választ kapsz.

### 2. MUNKAVÉGZÉS

**Kód írás/javítás:**
- Read/Write/Edit toolok → kódbázis módosítás
- Bash tool → build, test, git
- Glob/Grep toolok → fájlkeresés

**Státusz regisztráció (opcionális):**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","status":"working","currentTask":"MSG-BACKEND-NNN"}'
```

### 3. TASK BEFEJEZÉSE

**Task completion jelzése:**
```bash
curl -X POST "http://localhost:3456/api/epic-router/task/backend/complete" \
  -H "Content-Type: application/json" \
  -d '{"messageId":"MSG-BACKEND-NNN"}'
```

Ez automatikusan:
1. Lezárja az aktuális taskot
2. Megkeresi a következő taskot az epic kontextusban
3. Ha van következő task, új injekció történik

**Idle regisztráció:**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","status":"idle"}'
```
---
## PROJEKT ÉS EPIC KONTEXTUS

> **Lásd a teljes képet!** A feladatod része egy nagyobb epic-nek és projektnek.
> Használd az MCP toolokat a kontextus lekérdezéséhez.

### Kontextus lekérdezés

**EPICS.yaml olvasása** (ajánlott, ~100 sor):
```bash
cat /opt/spaceos/docs/projects/EPICS.yaml
```

**Projekt státusz** (task completion):
```bash
mcp__spaceos-knowledge__get_project_status --project="spaceos"
```

> ⚠️ **NE használd** a `get_project_context` MCP tool-t — túl nagy response (~10k token)!

### Aktív Epicek (2026-06-24)

| Epic ID | Név | Státusz | Backend releváns? |
|---|---|---|---|
| **EPIC-CUTTING-Q3** | Cutting Module Q3 | 🟢 active | ✅ (994 teszt) |
| **EPIC-NEXUS-V1** | Nexus Agent Infrastructure | 🟢 active | ✅ (knowledge-service) |
| **EPIC-GRAPH-WORKFLOW** | Graph-Based Workflow (ADR-041) | 🟢 active | ✅ |
| EPIC-KERNEL-STABLE | Kernel Stability | ✅ done | ✅ |
| EPIC-JOINERY-V2 | Joinery Module v2 | ✅ done | ✅ |
| EPIC-IDENTITY-V1 | Identity Module v1 | ✅ done | ✅ |
| EPIC-ORCH-V2 | Orchestrator BFF v2 | ✅ done | ✅ |

### Referencia Dokumentumok

| Dokumentum | Hol | Mikor olvasd |
|---|---|---|
| **EPICS.yaml** | `docs/projects/EPICS.yaml` | Epic dependency gráf |
| **Codebase_Status.md** | `docs/Codebase_Status.md` | Modul státuszok, teszt számok |
| **SpaceOS Vision** | `docs/vision/SpaceOS_Vision_Master.md` | Architektúra, 5 Golden Rule |
| **Knowledge Index** | `docs/knowledge/INDEX.md` | Minták, döntések, gotchák |

### Miért fontos a kontextus?

1. **Epic dependency** — ha a taskod epic-je blokkolva van, jelezd vissza
2. **Cross-modul koordináció** — lásd melyik epic függ a tied-től
3. **Roadmap alignment** — ne implementálj olyat ami nincs az aktív epic-ben

---

## BACKEND SZOLGÁLTATÁSOK

| Modul | Port | Stack | Leírás |
|---|---|---|---|
| **Kernel** | 5000 | .NET 8 | Auth, audit, FSM, tenant management |
| **Orchestrator** | 3000 | Node.js 22 | BFF, LLM routing, API gateway |
| **Joinery** | 5002 | .NET 8 | Ajtógyártás üzleti logika |
| **Cutting** | 5004 | .NET 8 | Lapszabászat, nesting |
| **Identity** | 5008 | .NET 8 | User management, Keycloak sync |
| **Inventory** | 5005 | .NET 8 | Készletkezelés |
| **Procurement** | 5006 | .NET 8 | Beszerzés |
| **Sales** | 5007 | .NET 8 | Értékesítés |
| **Abstractions** | 5003 | .NET 8 | Parametric engine |

---

## .NET PROJEKTEK — STRUKTÚRA

```
SpaceOS.Modules.{Module}/
├── Domain/           ← aggregates, VOs, domain events
├── Application/      ← CQRS handlers, validators, DTOs
├── Infrastructure/   ← EF Core + PostgreSQL
├── Api/              ← Minimal API endpoints
└── Tests/            ← xUnit v3, Moq
```

**Layer dependency rule:**
```
Domain ← Application ← Infrastructure ← Api
```

---

## KÓDOLÁSI SZABÁLYOK (.NET)

```csharp
// 1. ConfigureAwait(false) minden async callban
await _repository.GetByIdAsync(id, ct).ConfigureAwait(false);

// 2. CancellationToken neve mindig ct
public async Task<Result<T>> Handle(TRequest request, CancellationToken ct)

// 3. AsNoTracking() minden read-only lekérdezésnél
_db.Orders.AsNoTracking().Where(...)

// 4. Result<T> minden handler return type
public async Task<Result<OrderResponse>> Handle(...)

// 5. XML docs minden publikus típuson
/// <summary>...</summary>
```

---

## KÓDOLÁSI SZABÁLYOK (Node.js)

```typescript
// 1. Minden route handler: try/catch → next(err)
async (req, res, next) => { try { ... } catch (err) { next(err); } }

// 2. Zod validáció minden req.body-ra
const parsed = schema.safeParse(req.body);
if (!parsed.success) { res.status(422).json(parsed.error.flatten()); return; }

// 3. Env csak config/env.ts-ből
import { env } from '../config/env';
```

---

## APPROVED PACKAGES

**.NET:**
```
MediatR 12.4.1 · FluentValidation 12.1.1 · Ardalis.Result 10.1.0
Ardalis.Specification 8.0.0 · EF Core 8 · xUnit v3 · Moq
```

**Node.js:**
```
express · helmet · cors · @anthropic-ai/sdk · axios
jsonwebtoken · zod · vitest · typescript
```

---

## KÖTELEZŐ PIPELINE

```
INBOX READ → CODE → BUILD → TEST → SECURITY → OUTBOX
```

### BUILD
```bash
# .NET
dotnet build → 0 error, 0 warning

# Node.js
npm run build → 0 TypeScript error
```

### TEST
```bash
# .NET
dotnet test → minden teszt zöld

# Node.js
npm test → minden teszt zöld
```

### SECURITY CHECKLIST

- [ ] Input validation (FluentValidation / Zod)
- [ ] Authorization ([Authorize] / requireAuth middleware)
- [ ] RLS policy az érintett táblákon
- [ ] Paraméteres query (nincs string concat)
- [ ] Sensitive data nem kerül logba

---

## KRITIKUS TECHNIKAI KONSTANSOK

### Kernel
- RLS: `IgnoreQueryFilters()` + explicit `WHERE tenantId = ...`
- FlowEpic létrehozás: Facility first, then FlowEpic

### Joinery
- `TenantGucKey = "app.tenant_id"` (NEM "app.current_tenant_id")
- CuttingList: SOHA nem cache-elhető (no-store header)
- MaxItems = 500 per order

### Orchestrator
- JWT: ES256 (ECDSA P-256 asymmetric)
- Tool names: `snake_case`

---

## DONE OUTBOX SABLON

```yaml
---
id: MSG-BACKEND-NNN-DONE
from: backend
to: conductor
type: done
status: UNREAD
ref: MSG-BACKEND-NNN
created: YYYY-MM-DD
---

## Összefoglaló
[Mit implementáltál, mely modulok/fájlok változtak]

## Tesztek
[Hány teszt futott, mind zöld? Új tesztek száma?]

## Security review
[Mely pontokat ellenőrizted]

## Kockázatok
[Ha van → status: BLOCKED]
```

---

## KOMMUNIKÁCIÓ

- **Terminál ID:** `backend`
- **Task API:** `http://localhost:3456/api/epic-router/fetch/backend/{taskId}`
- **Completion API:** `http://localhost:3456/api/epic-router/task/backend/complete`

> ⚠️ **Nincs közvetlen mailbox hozzáférés!** Minden task az API-n keresztül érkezik.

---

## NEXUS RENDSZER ÉS MCP INTEGRÁCIÓ

> ⚠️ **FONTOS:** Minden kommunikáció az MCP (Model Context Protocol) keresztül történik!

### Mi a Nexus?

A **Nexus** egy önálló termék, amely a **SpaceOS mellett fejlődik**. Célja:
- Agent infrastruktúra fejlesztési támogatás
- Terminal koordináció és monitoring
- MCP-alapú kommunikációs csatorna biztosítása
- Session Management API (terminál indítás/injection)

### MCP Session API Használata

```bash
# Session indítás
curl -X POST http://localhost:3456/api/session/start \
  -H "Content-Type: application/json" \
  -d '{"terminal":"architect","model":"opus","prompt":"...","fromTerminal":"backend"}'

# Prompt injection futó sessionbe
curl -X POST http://localhost:3456/api/session/inject \
  -H "Content-Type: application/json" \
  -d '{"terminal":"architect","prompt":"...","fromTerminal":"backend"}'

# Session státusz lekérdezés
curl -s http://localhost:3456/api/session/backend
curl -s http://localhost:3456/api/sessions/all
```

**Jogosultság:** A backend terminál csak saját magát irányíthatja.

### Miért használjam az MCP-t?

1. **Aktív fejlesztés alatt áll** — a Nexus termék a SpaceOS-sal párhuzamosan fejlődik
2. **Visszajelzés segít** — ha használod az MCP eszközöket, és visszajelzést gyűjtesz, segíted a Nexus fejlesztését
3. **Új eszközök** — ha hiányzik valamilyen eszköz a feladataidhoz, **jelezd vissza**!

### Hogyan gyűjts visszajelzést?

**Session végén vagy DONE outbox-ban jelezd:**
- Milyen MCP eszközre lett volna szükséged?
- Mely meglévő MCP eszköz működött jól?
- Mely workflow lépés volt körülményes MCP nélkül?

**Példa visszajelzés:**
```markdown
## MCP Visszajelzés

### Használt eszközök ✅
- Datahaven status API (működött)
- Session Management API terminál koordinációhoz

### Hiányzó eszközök 🔧
- Nincs közvetlen MCP eszköz a .NET build eredmény lekérdezéséhez
- Hasznos lenne egy MCP tool a teszt lefedettség összefoglalásához
```

### MCP Eszközök a Backend terminálhoz

Jelenleg elérhető MCP integrációk:
- **Datahaven Dashboard API** — terminal status, messages, kanban sync
- **Session Management API** — session start, inject, wake, status
- **Knowledge Service API** — knowledge search, mailbox tools
- **Memory API** — terminál memória kezelés

### Memória kezelés MCP-n keresztül

A Nexus tartalmaz egy **SQLite FTS5 alapú memória rendszert**. Használd ezt preferenciák, tanult minták és kontextus tárolására!

```bash
# Memória olvasás
curl -X POST http://localhost:3456/mcp -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/call","params":{"name":"read_memory","arguments":{"terminal":"backend"}},"id":1}'

# Memóriához hozzáfűzés (AJÁNLOTT)
curl -X POST http://localhost:3456/mcp -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/call","params":{"name":"append_memory","arguments":{"terminal":"backend","content":"## Tanult minta\n- xyz"}},"id":1}'
```

**Memória típusok:**
- `semantic` — preferenciák, tények, döntések
- `episodic` — beszélgetés összefoglalók, napi digest
- `procedural` — how-to tudás, minták

**TODO:** További MCP eszközök igény szerint (jelezd vissza!)

---

## EXPLORER ÉS LIBRARIAN SEGÍTSÉG

> **Az Explorer és Librarian terminálok támogatják a munkádat!**
> Kérj tőlük segítséget kutatáshoz, tudásbázis kereséshez, és best practices javaslatokhoz.

### Mikor kérj segítséget az Explorertől?

Az **Explorer** a SpaceOS tudásbányász terminál. Használd ha:
- **Ismeretlen mintát keresel** — hogyan oldottuk meg korábban egy hasonló problémát?
- **Chat history kutatás** — melyik session-ben volt szó egy bugról/feature-ről?
- **Kódbázis feltérképezés** — hol van implementálva egy interfész/pattern?
- **Tech stack kutatás** — mi a best practice .NET 8-ban? (EF Core, Minimal API, stb.)
- **Konkurens megoldások** — hogyan csinálják mások? (OSS projektek)

**Inbox üzenet minta az Explorernek:**
```yaml
---
id: MSG-EXPLORER-NNN
from: backend
to: explorer
type: task
priority: medium
status: UNREAD
model: haiku
created: YYYY-MM-DD
---

# Kutatási kérés: [Téma]

## Kontextus
[Milyen implementációs döntéshez kell a kutatás]

## Kutatási kérdések
1. [Kérdés 1 — pl. "Hogyan implementáljuk az event sourcing-ot EF Core-ral?"]
2. [Kérdés 2]

## Elvárt output
- Kód példák a kódbázisból vagy OSS-ből
- Best practice összefoglaló
- Gotchák amire figyeljek
```

### Mikor kérj segítséget a Librarian-tól?

A **Librarian** a SpaceOS tudásbázis kurátora. Használd ha:
- **Olvasólista kell** — milyen cikkeket olvassak el egy témában?
- **Knowledge doc keresés** — hol van dokumentálva egy pattern?
- **Tanult leckék** — milyen gotchák voltak korábban ebben a modulban?
- **Best practices összefoglaló** — mi a bevált megoldás?

**Inbox üzenet minta a Librarian-nak:**
```yaml
---
id: MSG-LIBRARIAN-NNN
from: backend
to: librarian
type: task
priority: medium
status: UNREAD
model: haiku
created: YYYY-MM-DD
---

# Tudás összegyűjtés: [Téma]

## Kontextus
[Milyen feladathoz kell a tudás — pl. "RLS implementáció a Procurement modulban"]

## Kérdések
1. [Mit szeretnél tudni? — pl. "Milyen RLS gotchák voltak korábban?"]
2. [Milyen összefoglalóra van szükség?]

## Elvárt output
- Releváns knowledge doc linkek (DATABASE_PATTERNS.md, KNOWN_GOTCHAS.md)
- Olvasólista ha külső forrás is releváns
```

### Tipikus Backend use-case-ek

| Probléma | Kihez fordulj | Mit kérj |
|---|---|---|
| Új modul scaffold | Explorer | "Hogyan néz ki a többi modul struktúrája?" |
| RLS implementáció | Librarian | "DATABASE_PATTERNS.md RLS szekció + gotchák" |
| EF Core migration hiba | Explorer | "Keress hasonló hibát a chat history-ban" |
| .NET 8 best practice | Librarian | "Olvasólista Minimal API-ról" |
| Integration test setup | Explorer | "Hogyan működik a Testcontainers setup?" |

---

## PARALLEL WORKERS (ADR-049 Phase 3)

> **Függetlenül futtatható feladatok párhuzamosítása** — Cost-aware worker management

### Mikor használd

- **Független modulok** — Több modul párhuzamos fejlesztése (pl. Kernel + Orch API-k)
- **Best-of-N testing** — Több teszt megközelítés közül a legjobb (pl. 3 migration stratégia tesztelése)
- **Security audit** — Párhuzamos code review több modul között

### MCP Tools

```bash
# Parallel tasks with dependencies
mcp__spaceos-knowledge__spawn_parallel_workers
  terminal: "backend"
  tasks: [
    {id: "api", prompt: "Create API endpoint"},
    {id: "test", prompt: "Write integration tests", depends_on: ["api"]}
  ]

# Best-of-N selection (2-5 workers)
mcp__spaceos-knowledge__spawn_raw_workers
  terminal: "backend"
  task: "Optimize RLS query performance"
  count: 3
  criteria: "fastest execution with maintained security"

# Worker status + cost tracking
mcp__spaceos-knowledge__get_worker_status
  terminal: "backend"
```

### Cost Limits

| Threshold | Action |
|-----------|--------|
| **Soft limit:** $3/hour | Warning logged |
| **Hard limit:** $5/hour | Alert sent to Root |
| **Critical:** $10/hour | Auto-kill all workers |
| **Max parallel:** 5 worker/terminal | Queue additional requests |

### Példa használat

**Scenario:** 3 modul API endpoint párhuzamos fejlesztése

```
1. spawn_parallel_workers tasks=[
     {id: "kernel-api", prompt: "Create Kernel endpoint"},
     {id: "orch-api", prompt: "Create Orch endpoint"},
     {id: "joinery-api", prompt: "Create Joinery endpoint"}
   ]
2. Workers párhuzamosan futnak
3. get_worker_status → progress check
```

**NE használd ha:**
- Migration (szekvenciális kell legyen)
- Shared state módosítás (pl. same DB table)
- Simple bugfix (túl egyszerű párhuzamosításhoz)

---

## CODE GENERATOR TOOLCHAIN (ADR-050)

> **Automatizált kódgenerálás** — API kliensek, komponensek, modulok
>
> A Backend terminál aktívan használja ezeket az eszközöket fejlesztéskor.

### Elérhető MCP Tools

| Tool | Leírás |
|------|--------|
| `generate_api_client` | Orval (Portal) / NSwag (Orchestrator) API kliens generálás |
| `generate_component` | React komponens SpaceOS mintákkal |
| `generate_module` | .NET DDD modul scaffold |
| `get_codegen_status` | Generátor konfiguráció és fájlok státusza |

### CLI Használat

```bash
# Státusz ellenőrzés
spaceos status

# API kliensek generálása (Kernel változás után)
spaceos generate api-client all

# Orchestrator-only kliens
spaceos generate api-client orchestrator
```

### .NET Modul Generálás

**Új modul scaffold:**
```bash
spaceos generate module Pricing --aggregate Quote --states Draft,Submitted,Approved,Rejected --with-api
```

**Generált struktúra:**
```
spaceos-modules-pricing/
├── src/
│   ├── Domain/Aggregates/      ← Quote.cs, QuoteState.cs
│   ├── Domain/Events/          ← QuoteCreatedEvent.cs, stb.
│   ├── Application/Commands/   ← CreateQuoteCommand.cs
│   ├── Infrastructure/         ← EF Core persistence
│   └── Api/                    ← QuoteEndpoints.cs (Minimal API)
└── tests/
    └── QuoteTests.cs           ← xUnit tesztek
```

### Backend Workflow

**1. Kernel API változás után:**
```bash
# Portal és Orchestrator kliensek újragenerálása
spaceos generate api-client all
```

**2. Új modul fejlesztés:**
```bash
# Modul scaffold
spaceos generate module <Name> --states <...> --with-api

# Majd testreszabás
cd backend/spaceos-modules/spaceos-modules-<name>
# Domain logic, handlers, validators implementálása
```

**3. Státusz ellenőrzés:**
```bash
spaceos status
# Kernel API: ● Running
# Portal API: ✓ 21 files
# Orchestrator API: ✓ generated
```

### Fontos szabályok

- **Kernel API változás után mindig futtasd** a `generate api-client all` parancsot
- A generált fájlokat **NE módosítsd kézzel** — a következő generálás felülírja
- Az `--with-api` opció Minimal API endpointokat is generál (testreszabható)

---
## 🧠 CONTEXT PERSISTENCE — MCP TOOLS (2026-07-07)

> **Új MCP eszközök a Goal Drift Prevention támogatására!**
> Használd őket a context window kezelésére és a fókusz megőrzésére.

---

### MIÉRT HOZTUK LÉTRE? — Elméleti Alap

**Probléma:** Long-running agent sessionök során **goal drift** lép fel — a terminál "elfelejti" az eredeti célt.

**5 Failure Mode azonosítva:**

1. **Subtask Overfocus** — Részletbe merülés, fő cél elhanyagolása
2. **Context Dilution** — Túl sok információ, elvész a fő cél
3. **Inherited Drift** — DONE outbox-ok eltérítik az irányt
4. **Long Horizon Loss** — Hosszú epic-eknél elvész az end-state látképe
5. **Milestone Blindness** — Nem ismeri fel mikor van kész

**6 Solution Pattern implementálva:**

1. **STATUS.md** — Current state snapshot (system_status, current_focus, recent_actions, next_steps)
2. **.session-state.json** — Cross-session goal recovery (epicId, progress, checkpoints, last task)
3. **.turn-count** — Context saturation tracking (WARNING >30, CRITICAL >50 turn)
4. **CHECKPOINTS.md** — Milestone tracking (GO/NO-GO decision points)
5. **Goal Re-Anchoring** — Session start context loading
6. **Dense Milestone Feedback** — Epic progress explicit frissítés

**Context Saturation Thresholds:**
- **0-29 turn:** ✅ OK — Normál működés
- **30-49 turn:** ⚠️ WARNING — Goal drift veszély, fókuszálj!
- **≥50 turn:** 🚨 CRITICAL — Auto re-anchor vagy session reset kötelező!

**Implementáció:** 13 új MCP tool a context persistence file-ok kezelésére.

**Referencia:**
- `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md`
- `docs/knowledge/patterns/TERMINAL_CONTEXT_PERSISTENCE_FILES.md`
- `spaceos-nexus/knowledge-service/src/contextPersistence.ts`

---

### SESSION START RITUAL (KÖTELEZŐ!)

**Minden session elején (első 3-5 percben):**

```typescript
// 1. Session context betöltése (automatikus goal re-anchoring)
mcp__spaceos-knowledge__build_session_start_context
  terminal: "<terminal-neve>"

// 2. Context saturation ellenőrzés
mcp__spaceos-knowledge__get_context_saturation
  terminal: "<terminal-neve>"
```

**Mit kapsz:**
- **STATUS.md snapshot** — Mi volt az utolsó állapot? Mi a current focus?
- **Session state** — Melyik epic, melyik checkpoint, mennyi a progress?
- **Turn count + threshold** — Hány turn volt, milyen közel vagy a WARNING/CRITICAL-hoz?

**Példa output:**
```json
{
  "terminal": "conductor",
  "turnCount": 13,
  "status": "ok",  // "ok" | "warning" | "critical"
  "thresholds": {
    "warning": 30,
    "critical": 50
  },
  "sessionState": {
    "epicId": "EPIC-CUTTING-Q3",
    "epicProgress": 25,
    "nextCheckpointId": "CP-KERNEL-FSM",
    "lastActiveTask": "MSG-BACKEND-045"
  },
  "statusMd": {
    "system_status": "in_progress",
    "current_focus": "Kernel FSM implementation",
    "recent_actions": ["..."],
    "next_steps": ["..."]
  }
}
```

**Ha WARNING vagy CRITICAL:**
```typescript
// Újraolvassuk a fő célt
mcp__spaceos-knowledge__read_session_state
  terminal: "<terminal-neve>"

// STATUS.md explicit check: mi volt a focus?
mcp__spaceos-knowledge__read_terminal_status_md
  terminal: "<terminal-neve>"
```

---

### DURING WORK — FÓKUSZ TRACKING

**Minden 10-15 turn után (vagy major milestone után):**

```typescript
// Turn count increment (manuális vagy automatikus)
mcp__spaceos-knowledge__increment_turn_count
  terminal: "<terminal-neve>"
  amount: 1

// Context saturation check
mcp__spaceos-knowledge__get_context_saturation
  terminal: "<terminal-neve>"
```

**Threshold Action Table:**

| Turn Count | Status | Teendő |
|------------|--------|--------|
| **0-29** | ✅ OK | Normál működés |
| **30-49** | ⚠️ WARNING | **FÓKUSZÁLJ!** Térj vissza az epic fő céljához. Ne merülj új részletekbe. Olvasd újra a STATUS.md-t! |
| **≥50** | 🚨 CRITICAL | **STOP!** Session re-anchor kérése Monitor-tól vagy summary mentés + új session indítás. |

**WARNING esetén (30-49 turn):**
```typescript
// 1. Mi volt a fő cél? (session state)
const state = await mcp__spaceos-knowledge__read_session_state
  terminal: "<terminal-neve>"

// 2. Mi volt az utolsó focus? (STATUS.md)
const status = await mcp__spaceos-knowledge__read_terminal_status_md
  terminal: "<terminal-neve>"

// 3. Következő checkpoint? (milestone tracking)
const checkpoints = await mcp__spaceos-knowledge__read_checkpoints_md
  terminal: "<terminal-neve>"

// → Térj vissza a fő célhoz! Ne merülj részletekbe!
```

**CRITICAL esetén (≥50 turn):**
```typescript
// 1. Session state mentés (cross-session recovery)
mcp__spaceos-knowledge__write_session_state
  terminal: "<terminal-neve>"
  epic_id: "EPIC-CUTTING-Q3"
  epic_progress: 35
  next_checkpoint_id: "CP-INTEGRATION-TEST"
  last_active_task: "MSG-BACKEND-045"
  completed_checkpoints: ["CP-KERNEL-FSM"]

// 2. STATUS.md snapshot mentés
mcp__spaceos-knowledge__write_terminal_status_md
  terminal: "<terminal-neve>"
  system_status: "in_progress"
  current_focus: "Kernel FSM integration testing"
  recent_actions: ["Completed FSM implementation", "Started integration tests"]
  next_steps: ["Complete test suite", "Frontend integration"]

// 3. Monitor-nak escalation
mcp__spaceos-knowledge__send_message
  to: "monitor"
  type: "info"
  content: "Context saturation CRITICAL (≥50 turn). Re-anchoring vagy új session kérése."
  priority: "high"

// 4. Turn count reset (ha új session)
mcp__spaceos-knowledge__reset_turn_count
  terminal: "<terminal-neve>"
```

---

### MAJOR DECISION ELŐTT — STATUS CHECK

**Mielőtt:**
- Új epic-hez kezdesz
- Terminálnak task-ot adsz ki
- Strategic döntést hozol
- Cross-terminal koordinációt indítasz

**Ellenőrizd:**

```typescript
// 1. Current focus mi volt?
mcp__spaceos-knowledge__read_terminal_status_md
  terminal: "<terminal-neve>"

// 2. Session state aktív?
mcp__spaceos-knowledge__read_session_state
  terminal: "<terminal-neve>"

// 3. Checkpoint-ok állapota
mcp__spaceos-knowledge__read_checkpoints_md
  terminal: "<terminal-neve>"

// 4. Context saturation check
mcp__spaceos-knowledge__get_context_saturation
  terminal: "<terminal-neve>"
```

**Miért fontos?**
- **Goal Drift Prevention** — Ne térj el az aktív epic-től!
- **Subtask Overfocus** — Ne optimalizálj túl részfeladatokat!
- **Context Dilution** — Ne veszítsd el a fő célt!
- **Milestone Awareness** — Tudd hol tartasz!

---

### SESSION END — STATE PERSISTENCE (KÖTELEZŐ!)

**Session lezárás előtt (utolsó 5 percben):**

```typescript
// 1. STATUS.md snapshot frissítés
mcp__spaceos-knowledge__write_terminal_status_md
  terminal: "<terminal-neve>"
  system_status: "operational"      // operational | in_progress | paused | blocked
  current_focus: "MSG-BACKEND-045: Kernel FSM implementation"
  recent_actions: [
    "Completed Kernel FSM state machine",
    "Started integration tests",
    "Blocked on mock API endpoint"
  ]
  next_steps: [
    "Wait for Frontend mock API completion",
    "Continue integration test suite",
    "Review FSM edge cases"
  ]

// 2. Session state mentés (cross-session recovery)
mcp__spaceos-knowledge__write_session_state
  terminal: "<terminal-neve>"
  epic_id: "EPIC-CUTTING-Q3"
  epic_progress: 35                  // % progress
  next_checkpoint_id: "CP-INTEGRATION-TEST"
  last_active_task: "MSG-BACKEND-045"
  completed_checkpoints: ["CP-KERNEL-FSM", "CP-DOMAIN-MODEL"]

// 3. Turn count reset (ha új session következik)
mcp__spaceos-knowledge__reset_turn_count
  terminal: "<terminal-neve>"
```

**Miért kötelező?**
- **Cross-session goal recovery** — A következő session tudja folytatni!
- **Goal re-anchoring** — Nem vész el az epic fókusz!
- **Progress tracking** — Milestone visibility!

---

### CHECKPOINT MANAGEMENT

**Új checkpoint hozzáadása (Conductor/Root):**

```typescript
mcp__spaceos-knowledge__append_checkpoint_to_md
  terminal: "<terminal-neve>"
  date: "2026-07-10"
  name: "Kernel FSM Complete"
  decision: "GO/NO-GO"
  evaluation_criteria: [
    "All FSM states implemented",
    "Unit tests pass (>95%)",
    "Integration with Orchestrator ready"
  ]
  go_actions: ["Proceed to Orchestrator integration"]
  no_go_actions: ["Fix FSM edge cases", "Add missing transitions"]
  refs: ["MSG-BACKEND-045", "EPIC-CUTTING-Q3"]
```

**Checkpoint státusz check:**

```typescript
mcp__spaceos-knowledge__read_checkpoints_md
  terminal: "<terminal-neve>"
```

**Checkpoint-ok célja:**
- **Milestone tracking** — Hol tartunk az epic-ben?
- **GO/NO-GO decision points** — Mehetünk tovább vagy vissza kell lépni?
- **Progress visibility** — Explicit haladás követés!

---

### DIAGNOSTIC — ÖSSZES TERMINÁL OVERVIEW

**Root/Monitor használja:**

```typescript
// Minden terminál context files státusza
mcp__spaceos-knowledge__get_all_context_files_status

// Output:
[
  {
    "terminal": "conductor",
    "hasStatus": true,
    "hasSessionState": true,
    "hasTurnCount": true,
    "hasCheckpoints": true,
    "turnCount": 13,
    "sessionState": {
      "epicId": "EPIC-CUTTING-Q3",
      "epicProgress": 25,
      "nextCheckpointId": "CP-KERNEL-FSM"
    }
  },
  {
    "terminal": "backend",
    "hasStatus": false,
    "hasSessionState": false,
    "turnCount": 0
  }
]
```

**Use case:**
- Melyik terminál van **goal drift** veszélyben? (turnCount >30)
- Melyik terminálnak nincs session state? (hasSessionState: false)
- Melyik terminál session-je túl hosszú? (turnCount >50 → re-anchor!)

---

### BEST PRACTICES

1. **Session start: MINDIG** `build_session_start_context` — Ne kezdj munkát goal re-anchoring nélkül!
2. **Every 10-15 turns: CHECK** `get_context_saturation` — Ne várd meg a CRITICAL-t!
3. **Before major decision: READ** `read_session_state` + `read_terminal_status_md` — Ellenőrizd a fókuszt!
4. **Session end: WRITE** `write_session_state` + `write_terminal_status_md` — A következő session hálás lesz!
5. **Checkpoint milestones: APPEND** `append_checkpoint_to_md` — Track progress explicitly!

---

### ANTI-PATTERNS (NE CSINÁLD!)

❌ **Session start goal re-anchoring nélkül** — Goal drift garantált!
❌ **Turn count ignorálás** — >50 turn után már minden context diluted.
❌ **Session state mentés nélküli lezárás** — A következő session elveszett.
❌ **STATUS.md nem frissítése** — "Mi volt a fókusz?" → senki nem tudja.
❌ **Checkpoint-ok nélküli epic** — Progress tracking lehetetlen.
❌ **WARNING threshold ignorálás** — 30-49 turn = goal drift veszély!

---

### MCP TOOL REFERENCE

| Tool | Használat | Mikor |
|------|-----------|-------|
| `build_session_start_context` | Session start context | **Session start (első 3 perc)** |
| `get_context_saturation` | Turn count + threshold | **Every 10-15 turns** |
| `read_session_state` | Epic + progress + checkpoints | **Session start, decision előtt** |
| `write_session_state` | Session state save | **Session end, CRITICAL** |
| `read_terminal_status_md` | Current focus snapshot | **Session start, decision előtt** |
| `write_terminal_status_md` | STATUS.md update | **Session end, milestone** |
| `increment_turn_count` | Turn tracking | **Every 10-15 turns** |
| `reset_turn_count` | Turn reset | **Session end (ha új session)** |
| `read_checkpoints_md` | Checkpoint list | **Session start, progress check** |
| `append_checkpoint_to_md` | Add new checkpoint | **Milestone planning** |
| `get_context_files_status` | Single terminal overview | **Diagnostic** |
| `get_all_context_files_status` | All terminals overview | **Root/Monitor diagnostic** |

---

**Referencia:**
- `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md` — 5 failure mode, 6 solution pattern
- `docs/knowledge/patterns/TERMINAL_CONTEXT_PERSISTENCE_FILES.md` — File structure, theory
- `spaceos-nexus/knowledge-service/src/contextPersistence.ts` — Implementation

---
