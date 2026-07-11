# CLAUDE.md — SpaceOS.Orchestrator (Root)

> Global rules. Every sub-folder's CLAUDE.md inherits these.

---

## JELENLEGI ÁLLAPOT (2026-04-17)

| | |
|---|---|
| **Terminál** | orchestrator · Port: **3000** (PM2) · Mailbox: `/opt/spaceos/docs/mailbox/orchestrator/` |
| **Repo** | `/opt/spaceos/spaceos-orchestrator` |
| **Aktuális commit** | `4e8926d` (ORCH-078: facility-first FlowEpic seed fix) |
| **Tesztek** | **218/218 pass** |
| **VPS** | deploy pending (INFRA-141) |

### Test reset endpoint
```
POST /bff/test/tenants/{tenantId}/reset?confirm=true
Headers: X-SpaceOS-Internal: true, X-SpaceOS-Brand: joinerytech
Body: { "seedProfile": "empty-v1" | "doorstar-smoke-v1" | "doorstar-cutting-ready-v1" }
```
⚠️ Metódus: **POST** (nem DELETE)

### Seed profilok
| Profil | Leírás |
|---|---|
| `empty-v1` | Törli az adatokat, nem seed |
| `doorstar-smoke-v1` | Facility + FlowEpic + DoorOrder |
| `doorstar-cutting-ready-v1` | Facility + FlowEpic + DoorOrder(Submitted) + CuttingSheet + 5 PanelStock + Supplier |

### FlowEpic létrehozás — KÖTELEZŐ sorrend
1. `POST /api/tenants/{tenantId}/facilities` → `facilityId`
2. `POST /api/facilities/{facilityId}/flow-epics` + `{ title }` → `epicId`

`POST /api/flow-epics` közvetlenül **NEM LÉTEZIK** a Kernelben — 404-et ad.

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/orchestrator.md`

**DONE előtt:** Frissítsd a memory fájlt!

---

## SESSION STARTUP/SHUTDOWN RITUAL

**Minden session elején:**
```bash
# 0. Datahaven státusz regisztráció — jelezd hogy dolgozol
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "orch",
    "status": "working",
    "currentTask": "Session started - checking inbox"
  }'

# 1. Inbox ellenőrzés
ls /opt/spaceos/docs/mailbox/orch/inbox/
grep -l "status: UNREAD" /opt/spaceos/docs/mailbox/orch/inbox/*.md 2>/dev/null
```

**Session végén (DONE/BLOCKED outbox után):**
```bash
# Datahaven státusz regisztráció — jelezd hogy befejeztél
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"orch","status":"idle"}'
```

**Datahaven Dashboard:** https://datahaven.joinerytech.hu (token: `dev-token-spaceos-dashboard-2026`)
- Dashboard (`/`) — Orch státusz (WORKING/IDLE), inbox/outbox metrikák
- Kanban (`/kanban`) — Orch swimlane a Delivery track-en
- Teljes API: `docs/WORKFLOW.md` — "Datahaven Dashboard" szakasz

---

## PROJEKT VÍZIÓ — MIÉRT ÉPÜL EZ A RENDSZER

**SpaceOS** = a magyar faipar digitális gerince. Iparspecifikus SaaS platform, amely ajtógyártókat,
szekrénygyártókat, lapszabászokat, kereskedőket és beszerelőket köt össze egyetlen ökoszisztémában.

**A probléma:** A faiparos KKV-k 90%+ ma Viber + Excel + telefon alapon dolgozik. Nincs rájuk
szabott, megfizethető digitális megoldás — a SpaceOS ezt az űrt tölti be.

**Első éles ügyfél:** Doorstar Kft. (ajtógyártó) — Soft Launch: **2026 Q2**

### Az Orchestrator szerepe ebben

Az Orchestrator (L3 BFF) az AI-natív réteg: az LLM Tool Calling itt fut, a natural language
rendelésfelvétel és konfiguráció-segítség innen érkezik. Közvetíti a Portal (L4) és a Kernel (L2)
között a kéréseket, és védi a Kernel API-t az internet felé.

### 5 Golden Rule (minden fejlesztési döntésnél mérvadó)

| # | Szabály | Orchestrator-hatás |
|---|---|---|
| 1 | **Data → Rules → Geometry** — frontend rajzol, C# számol, LLM paramétereket ad | LLM soha nem küld geometriát, csak JSON tool call-t |
| 2 | **Modular Monolith** — Kernel `IParametricProduct`-on dolgozik | Orchestrator nem tud ajtóról/szekrényről — csak proxy |
| 3 | **Immutability & Trust** — SHA-256 hash, audit event minden változáson | JWT-t forwardoljuk, nem módosítjuk |
| 4 | **Need-to-Know RBAC** — megrendelő nem látja a gyártó anyaglistáját | `requireAuth` minden védett route-on kötelező |
| 5 | **Walking Skeleton First** — E2E pipeline előbb, matematika utóbb | Új route = teszt is, különben nem mehet be |

> Teljes üzleti vízió: `/opt/spaceos/docs/SpaceOS_Vision_Results_20260413.md`
> Technikai master: `/opt/spaceos/docs/vision/SpaceOS_Vision_Master.md`

---

## PROJECT SNAPSHOT

**Project:** `spaceos-orchestrator` — Node.js 22, TypeScript 5, Express 4
**Role:** Layer 3 BFF — LLM routing, agentic loop, Kernel proxy
**Build:** `npm run build` → 0 TypeScript errors | **Tests:** 183 passing (Vitest) | **Auth:** ES256 (ECDSA P-256 asymmetric JWT)

```
src/
  config/       ← Zod-validated env + JWT key loader (CLAUDE.md inside)
  types/        ← ILlmProvider + C# DTO mirrors (CLAUDE.md inside)
  llm/          ← Provider adapters (CLAUDE.md inside)
  interpreter/  ← Agentic loop + tool registry (CLAUDE.md inside)
  proxy/        ← Kernel passthrough + federation proxy (CLAUDE.md inside)
  middleware/   ← Auth (ES256) + error handling (CLAUDE.md inside)
  routes/       ← Express route handlers (CLAUDE.md inside)
  index.ts      ← Bootstrap only — no business logic here
```

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/orchestrator.md`

**DONE előtt:** Frissítsd a memory fájlt!

---
---

## LAYER DEPENDENCY RULE (HARD CONSTRAINT)

```
routes → interpreter → llm (via ILlmProvider)
routes → proxy → Kernel (via http-proxy-middleware)
interpreter → kernel.action (via axios — ONLY HERE)
middleware → config/env
ALL → types
```

`index.ts` wires everything — it must not contain logic, only registrations.

---

## APPROVED PACKAGES

```
express · helmet · cors · morgan · express-rate-limit
@anthropic-ai/sdk · axios · http-proxy-middleware
jsonwebtoken · zod · dotenv
tsx · typescript · vitest · @types/*
```

Anything outside this list → discuss before adding.

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/orchestrator.md`

**DONE előtt:** Frissítsd a memory fájlt!

---
---

## NAMING CONVENTIONS

| Scope | Convention |
|---|---|
| Files | `kebab-case.ts` |
| Classes / Interfaces | `PascalCase` |
| Functions / variables | `camelCase` |
| Test files | `*.test.ts` next to source file |
| Tool names | `snake_case` (matches C# endpoint action) |

---

## UNIVERSAL CODE RULES

```typescript
// 1. Every route handler: try/catch → next(err)
async (req, res, next) => { try { ... } catch (err) { next(err); } }

// 2. Always parse req.body with Zod before use
const parsed = schema.safeParse(req.body);
if (!parsed.success) { res.status(422).json(parsed.error.flatten()); return; }

// 3. Env — always from config/env.ts, never process.env directly
import { env } from '../config/env';

// 4. No TODO/FIXME in committed code
```

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/orchestrator.md`

**DONE előtt:** Frissítsd a memory fájlt!

---
---

## OUTPUT FORMAT

- **File path** as first line in every code block:
  ```typescript
  // src/routes/chat.route.ts
  ```
- **Diff preferred** — full file only if entirely rewritten
- **After implementation:** summary table only

  | File | Change | Reason |
  |---|---|---|

- **No TODO/FIXME** in committed code
- **Every new service/route** → companion `*.test.ts`, no exceptions
- **Next steps:** only if explicitly asked

---

## BEHAVIORAL RULES

| Situation | Action |
|---|---|
| Uncertain about existing code | Read the file first — never guess |
| Request violates a Golden Rule | Warn once, then execute |
| New env var needed | Add to `env.ts` schema AND `.env.example` |
| New tool needed | Add to `tool-registry.ts` AND `kernel.action.ts` dispatch table |
| Breaking change | **Stop. Confirm before proceeding.** |

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/orchestrator.md`

**DONE előtt:** Frissítsd a memory fájlt!

---
---

## CURRENT SESSION GOAL

Context: SpaceOS.Orchestrator — lezárt sprintek
Goal: ORCH-058 + ORCH-059 DONE · VPS live · várakozás következő inbox üzenetre
Legutóbbi változások (2026-04-13):
  - ORCH-058: requireAuth a chatLimiter előtt (/bff/chat) → no-auth kérés 401 (nem 429)
  - ORCH-059: /bff/abstractions/* → http-proxy-middleware → 127.0.0.1:5003/api/*
  - Commit: 9d02196 + 4a96e3c · 183/183 teszt zöld · VPS deployed
Következő: várakozás root inbox üzenetre (E2E-34 unblokkolva)

---

## KÖTELEZŐ PIPELINE — MINDEN FELADATRA

⚠️ Minden lépés kötelező. Kihagyni TILOS. Lásd teljes leírás: `/opt/spaceos/docs/WORKFLOW.md`

```
INBOX READ → CODE → BUILD → TEST → REVIEW → SECURITY → OUTBOX
```

### 1. INBOX READ
- `ls ./mailbox/inbox/` → legfrissebb UNREAD üzenet elolvasása
- Frontmatter: `status: UNREAD` → `status: READ`

### 2. CODE
- Implementálj a feladat szerint

### 3. BUILD
- `npm run build` → **0 TypeScript error** — ha nem: javítsd, ne lépj tovább

### 4. TEST
- `npm test` → **minden teszt zöld** — ha nem: javítsd, ne lépj tovább
- Új kódhoz új tesztet írj (`*.test.ts` a forrás mellé)

### 5. REVIEW (önellenőrzés)
- Layer dependency rule betartva? (`routes → proxy → Kernel`, stb.)
- Env csak `config/env.ts`-ből olvasva?
- Minden route handler try/catch → `next(err)`?
- Zod validáció minden `req.body` parsolásnál?
- Nincs `TODO`/`FIXME` a kódban?

### 6. SECURITY ⚠️
- **Input validation**: Zod schema minden határon?
- **Authorization**: JWT middleware minden védett route-on?
- **Header injection**: proxy headers sanitizálva?
- **Rate limiting**: új route-okon `proxyLimiter` alkalmezva?
- **Sensitive data**: token/secret nem kerül logba?
- **OWASP Top 10**: nincs nyilvánvaló sebezhetőség?

### 7. OUTBOX ⚠️ SOHA NEM HAGYHATÓ KI
Minden befejezett feladat után kötelező outbox üzenetet írni.
Fájlnév: `YYYY-MM-DD_NNN_[slug]-done.md` → `./mailbox/outbox/`

```markdown
## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/orchestrator.md`

**DONE előtt:** Frissítsd a memory fájlt!

---
---
id: MSG-OXXX-DONE
from: orchestrator
to: conductor
type: done
status: UNREAD
---

## Összefoglaló
[Mit implementáltál, mely fájlok változtak]

## Tesztek
[Hány teszt futott, mind zöld? Új tesztek száma?]

## Security review
[Mely pontokat ellenőrizted (Zod, auth middleware, rate limit, stb.)]

## Kockázatok / kérdések
[Ha van → status: BLOCKED és leírás]
```

**Ha elakadtál:** `status: BLOCKED` outbox üzenettel jelezz — ne folytasd találgatással.

## Memory (hideg indításhoz)

**Első lépés:** `cat /opt/spaceos/docs/memory/orchestrator.md`

**DONE előtt:** Frissítsd a memory fájlt!

---
---

## Közös erőforrások

- **Inbox**: `./mailbox/inbox/`
- **Outbox**: `./mailbox/outbox/`
- **Codebase_Status.md**: `./docs/Codebase_Status.md` — frissítsd minden sprint után
- **WORKFLOW.md**: `/opt/spaceos/docs/WORKFLOW.md` — teljes munka módszertan
- **Projekt vízió (üzleti)**: `/opt/spaceos/docs/SpaceOS_Vision_Results_20260413.md` — miért épül a rendszer, Doorstar first customer, célpiac
- **Technikai master overview**: `/opt/spaceos/docs/vision/SpaceOS_Vision_Master.md` — 4 réteg, 5 Golden Rule, domain modell, döntési fa
- **`/spaceos-terminal` skill**: inbox olvasás + build/test gate + DONE/BLOCKED outbox protokoll — `/opt/spaceos/.claude/skills/spaceos-terminal/`
