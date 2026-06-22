# CLAUDE.md — SpaceOS Backend

> **Modell: `sonnet`**
>
> A Backend terminál az összes backend kódot fejleszti:
> .NET 8 modulok (Kernel, Joinery, Cutting, Identity, stb.) és Node.js Orchestrator.

---

## SESSION RITUAL

> **Használj Claude Code built-in toolokat:** Bash, Read, Write, Edit, Grep, Glob

### 1. SESSION START — Datahaven regisztráció

**Bash tool + curl:**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","status":"working","currentTask":"Session started"}'
```

**Inbox olvasás (Read tool):**
- Read minden UNREAD üzenetet: `/opt/spaceos/terminals/backend/inbox/*.md`

### 2. MUNKAVÉGZÉS

**Kód írás/javítás:**
- Read tool → kódbázis olvasás
- Edit tool → módosítások
- Write tool → új fájlok (csak ha szükséges!)
- Bash tool → build, test, git

**Keresés:**
- Glob tool → fájlminták (`**/*.cs`, `**/*.tsx`)
- Grep tool → tartalom keresés

### 3. SESSION END — DONE/BLOCKED outbox

**Write tool - outbox üzenet:**
```yaml
---
id: MSG-backend-OUT-NNN
from: backend
to: conductor
type: done|blocked
status: UNREAD
created: YYYY-MM-DD
---

# [Feladat címe]

## Elvégzett munka
- ...

## Tesztek
- Build: ✅/❌
- Tests: ✅/❌
```

**Datahaven idle (Bash + curl):**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"backend","status":"idle"}'
```
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

- **Mailbox:** `/opt/spaceos/terminals/backend/inbox/` és `.../outbox/`
- **Terminál ID:** `backend`

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
