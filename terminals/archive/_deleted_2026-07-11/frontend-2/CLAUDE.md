# CLAUDE.md — SpaceOS Frontend-2

> **Modell: `sonnet`**
>
> A Frontend terminál az összes React/TypeScript UI-t fejleszti:
> JoineryTech Portal, Datahaven Dashboard, és egyéb React alkalmazások.

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
| UI/UX logika kérdés | React, Datahaven, Portal | **→ Root/Conductor** |

### Hogyan küldj Nexus-nak?

```
mcp__spaceos-knowledge__create_task
  from: "frontend-2"
  to: "nexus"
  title: "MCP tool bug: session start timeout"
  description: "A session start 60+ másodpercig tart..."
  priority: "high"
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

> **ÚJ:** A frontend terminál párhuzamos workereket tud indítani domain-specifikus memóriával!

### Domain Memory Struktúra

```
terminals/frontend-2/knowledge/
├── portal.memory.md      ← Customer Portal (Quote forms, Tracking)
├── datahaven.memory.md   ← Datahaven Dashboard (Monitoring, Kanban)
├── industrial.memory.md  ← Industrial Flow Editor (Cytoscape, Drag-drop)
└── shared.memory.md      ← Cross-domain patterns (MINDIG betöltődik)
```

### Mikor töltődik be melyik memória?

| Feladat kulcsszavak | Betöltött memória |
|---------------------|-------------------|
| Portal, Quote, Form, Customer, Track | `portal.memory.md` |
| Dashboard, Datahaven, Terminal, Kanban, SSE | `datahaven.memory.md` |
| Industrial, Flow, Editor, Cytoscape, Graph, Workflow | `industrial.memory.md` |
| *Minden task* | `shared.memory.md` |

### Parallel Workers Használata

**Független komponensek párhuzamos fejlesztése:**

```bash
# 3 párhuzamos worker (Form, Timeline, API Service)
curl -X POST localhost:3456/api/mcp/spawn_parallel_workers \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "frontend-2",
    "tasks": [
      {"id": "form", "prompt": "Create PublicQuoteForm component"},
      {"id": "timeline", "prompt": "Create StatusTimeline component"},
      {"id": "service", "prompt": "Create API service", "depends_on": ["form"]}
    ]
  }'
```

**Raw workers (Best-of-N UI variáns):**

```bash
# 3 UI variáns, legjobb kiválasztása
curl -X POST localhost:3456/api/mcp/spawn_raw_workers \
  -H "Content-Type: application/json" \
  -d '{
    "terminal": "frontend-2",
    "task": "Design a responsive card component for terminal status",
    "count": 3,
    "model": "haiku",
    "criteria": "Best UX + mobile responsiveness"
  }'
```

### Költség Limitek

| Szint | Limit | Akció |
|-------|-------|-------|
| Soft | $3/hour | Log warning |
| Hard | $5/hour | Telegram alert |
| Critical | $10/hour | Auto-kill + escalate |

### Session Naming

| Session | Model | Purpose |
|---------|-------|---------|
| `spaceos-frontend` | Sonnet | Main work session |
| `spaceos-frontend-chat` | Haiku | Telegram, quick responses |
| `spaceos-frontend-work-001` | Sonnet | Parallel worker #1 |
| `spaceos-frontend-raw-001` | Haiku | Raw UI prototype #1 |

---

## ⚡ TELEGRAM VÁLASZ — KÖTELEZŐ

**Ha `[TG @user chat:CHATID]` formátumú üzenetet kapsz:**

1. **MINDIG** használd az MCP `telegram_reply` tool-t a válaszhoz
2. **MINDIG** add meg a `from_terminal: "frontend-2"` paramétert!
3. **NE** írj a konzolra/stdout-ra — az nem jut el a userhez!

```
mcp__spaceos-knowledge__telegram_reply
  chat_id: <CHATID a beérkező üzenetből>
  message: "A válaszod ide"
  from_terminal: "frontend-2"
```

**Példa:**
- Beérkező: `[TG @Gábor chat:8426048796] Hol tartasz?`
- Te: `mcp__spaceos-knowledge__telegram_reply(chat_id: 8426048796, message: "Kanban UI 80% kész, drag-drop működik.", from_terminal: "frontend-2")`

---

## ADR-053: CHECKPOINT-BASED TASK PROTOCOL (KÖTELEZŐ!)

> ⚠️ **KRITIKUS: Ez a protocol kötelező minden task feldolgozásnál!**
> A rendszer figyeli az MCP hívásokat — ha nem követed, alert generálódik.

### TASK LIFECYCLE (3 MCP hívás)

**1. TASK FOGADÁS — AZONNAL (5 percen belül!):**
```
mcp__spaceos-knowledge__ack_task
  terminal: "frontend-2"
  message_id: "MSG-FRONTEND-NNN"
```
→ Ha 5 percen belül nincs ACK, alert megy Root-nak!

**2. TASK LEKÉRÉS (opcionális, ha kell a tartalom):**
```
mcp__spaceos-knowledge__fetch_task
  terminal: "frontend-2"
  message_id: "MSG-FRONTEND-NNN"
```

**3. TASK BEFEJEZÉS — MINDIG:**
```
mcp__spaceos-knowledge__complete_task
  terminal: "frontend-2"
  message_id: "MSG-FRONTEND-NNN"
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
  terminal: "frontend-2"
  message_id: "MSG-FRONTEND-NNN"
```

**Task tartalom lekérése MCP-n:**
```
mcp__spaceos-knowledge__fetch_task
  terminal: "frontend-2"
  message_id: "MSG-FRONTEND-NNN"
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
  -d '{"terminal":"frontend","status":"working","currentTask":"MSG-FRONTEND-NNN"}'
```

### 3. TASK BEFEJEZÉSE

**Task completion jelzése:**
```bash
curl -X POST "http://localhost:3456/api/epic-router/task/frontend/complete" \
  -H "Content-Type: application/json" \
  -d '{"messageId":"MSG-FRONTEND-NNN"}'
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
  -d '{"terminal":"frontend","status":"idle"}'
```
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
  -d '{"terminal":"frontend","status":"idle"}'
```
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

| Epic ID | Név | Státusz | Frontend releváns? |
|---|---|---|---|
| **EPIC-CUTTING-Q3** | Cutting Module Q3 | 🟢 active | ✅ (941 teszt) |
| **EPIC-PORTAL-V2** | Customer Portal v2 | ✅ done | ✅ (27/27 világ LIVE) |
| **EPIC-NEXUS-V1** | Nexus Agent Infrastructure | 🟢 active | ✅ (Datahaven Dashboard) |
| **EPIC-GRAPH-WORKFLOW** | Graph-Based Workflow (ADR-041) | 🟢 active | ✅ |

### Referencia Dokumentumok

| Dokumentum | Hol | Mikor olvasd |
|---|---|---|
| **EPICS.yaml** | `docs/projects/EPICS.yaml` | Epic dependency gráf |
| **Codebase_Status.md** | `docs/Codebase_Status.md` | FE teszt számok, sprint roadmap |
| **SpaceOS Vision** | `docs/vision/SpaceOS_Vision_Master.md` | Architektúra, 5 Golden Rule |
| **Knowledge Index** | `docs/knowledge/INDEX.md` | FE minták (drag-drop, KPI, stb.) |
| **FE Patterns** | `docs/knowledge/patterns/FRONTEND_*.md` | Drag-drop, offline-first, localStorage |

### Miért fontos a kontextus?

1. **Backend API dependency** — tudd melyik epic backend része kész
2. **Design system** — lásd a közös komponenseket és stílusokat
3. **UX consistency** — a Portal és Dashboard közös mintákat követ

---

## FRONTEND PROJEKTEK

| Projekt | Lokáció | Stack | URL |
|---|---|---|---|
| **JoineryTech Portal** | `/opt/spaceos/frontend/joinerytech-portal/` | React 18, TypeScript, Vite | https://portal.joinerytech.hu |
| **Datahaven Dashboard** | `/opt/spaceos/datahaven-web/client/` | React 19, TypeScript, Vite | https://datahaven.joinerytech.hu |

---

## TECH STACK

- **React 18/19** — Hooks, functional components
- **TypeScript 5** — Strict mode
- **Vite** — Build tool, HMR
- **Tailwind CSS 4** — Utility-first styling
- **React Router 7** — Routing
- **TanStack Query** — Server state management
- **Zustand** — Client state management
- **MSW** — Mock Service Worker for testing
- **Vitest** — Unit testing
- **Playwright** — E2E testing

---

## PROJEKT STRUKTÚRA

```
src/
├── components/       ← Újrafelhasználható komponensek
│   ├── ui/           ← Alap UI elemek (Button, Input, Card)
│   └── features/     ← Feature-specifikus komponensek
├── pages/            ← Route-hoz kötött oldalak
├── hooks/            ← Custom React hooks
├── services/         ← API hívások
├── stores/           ← Zustand stores
├── types/            ← TypeScript típusok
├── utils/            ← Utility függvények
└── mocks/            ← MSW mock handlers
```

---

## KÓDOLÁSI SZABÁLYOK

```typescript
// 1. Functional components + TypeScript interface
interface Props {
  title: string;
  onSubmit: (data: FormData) => void;
}

export const MyComponent: React.FC<Props> = ({ title, onSubmit }) => {
  // ...
};

// 2. Custom hooks elnevezése: use* prefix
export const useOrders = () => {
  return useQuery({ queryKey: ['orders'], queryFn: fetchOrders });
};

// 3. Error boundary minden page-en
<ErrorBoundary fallback={<ErrorFallback />}>
  <PageContent />
</ErrorBoundary>

// 4. Loading states kezelése
if (isLoading) return <Skeleton />;
if (error) return <ErrorMessage error={error} />;
```

---

## NAMING CONVENTIONS

| Scope | Convention |
|---|---|
| Komponensek | `PascalCase.tsx` |
| Hooks | `useCamelCase.ts` |
| Services | `camelCase.service.ts` |
| Types | `camelCase.types.ts` |
| Test fájlok | `*.test.tsx` |

---

## KÖTELEZŐ PIPELINE

```
INBOX READ → CODE → BUILD → TEST → LINT → OUTBOX
```

### BUILD
```bash
npm run build → 0 TypeScript error
```

### TEST
```bash
npm test → minden teszt zöld
```

### LINT
```bash
npm run lint → 0 error
```

---

## ACCESSIBILITY (A11Y) SZABÁLYOK

- [ ] Minden interaktív elem keyboard accessible
- [ ] ARIA labels ahol szükséges
- [ ] Color contrast WCAG 2.1 AA szint
- [ ] Focus visible minden interaktív elemen

---

## RESPONSIVE DESIGN

- Mobile first megközelítés
- Breakpoints: `sm: 640px`, `md: 768px`, `lg: 1024px`, `xl: 1280px`
- Touch targets minimum 44x44px

---

## DONE OUTBOX SABLON

```yaml
---
id: MSG-FRONTEND-NNN-DONE
from: frontend
to: conductor
type: done
status: UNREAD
ref: MSG-FRONTEND-NNN
created: YYYY-MM-DD
---

## Összefoglaló
[Mit implementáltál, mely komponensek változtak]

## Tesztek
[Hány teszt futott, mind zöld? Új tesztek száma?]

## Build
[Build sikerült? Bundle size változás?]

## Screenshots
[Ha UI változás, screenshot link]

## Kockázatok
[Ha van → status: BLOCKED]
```

---

## KOMMUNIKÁCIÓ

- **Mailbox:** `/opt/spaceos/terminals/frontend-2/inbox/` és `.../outbox/`
- **Terminál ID:** `frontend`

---

## NEXUS RENDSZER ÉS MCP INTEGRÁCIÓ

> ⚠️ **FONTOS:** Minden kommunikáció az MCP (Model Context Protocol) keresztül történik!

### Mi a Nexus?

A **Nexus** egy önálló termék, amely a **SpaceOS mellett fejlődik**. Célja:
- Agent infrastruktúra fejlesztési támogatás
- Terminal koordináció és monitoring
- MCP-alapú kommunikációs csatorna biztosítása

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
- useApi hook backend kommunikációhoz

### Hiányzó eszközök 🔧
- Nincs közvetlen MCP eszköz a bundle size elemzéséhez
- Hasznos lenne egy MCP tool a component dependency tree vizualizálásához
```

### MCP Eszközök a Frontend terminálhoz

Jelenleg elérhető MCP integrációk:
- **Datahaven Dashboard API** — terminal status, messages, kanban sync
- **Backend API-k** — Procurement, Joinery, Cutting, Inventory, Identity modulok
- **Memory API** — terminál memória kezelés

### Memória kezelés MCP-n keresztül

A Nexus tartalmaz egy **SQLite FTS5 alapú memória rendszert**. Használd ezt preferenciák, tanult minták és kontextus tárolására!

```bash
# Memória olvasás
curl -X POST http://localhost:3456/mcp -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/call","params":{"name":"read_memory","arguments":{"terminal":"frontend"}},"id":1}'

# Memóriához hozzáfűzés (AJÁNLOTT)
curl -X POST http://localhost:3456/mcp -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","method":"tools/call","params":{"name":"append_memory","arguments":{"terminal":"frontend","content":"## Tanult minta\n- xyz"}},"id":1}'
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
- **Ismeretlen komponens mintát keresel** — hogyan csináltuk korábban?
- **Chat history kutatás** — melyik session-ben volt szó erről a feature-ről?
- **Kódbázis feltérképezés** — hol van implementálva egy hook/komponens?
- **Tech stack kutatás** — mi a best practice React 18/19-ben?
- **UI pattern kutatás** — hogyan csinálják mások? (OSS React projektek)

**Inbox üzenet minta az Explorernek:**
```yaml
---
id: MSG-EXPLORER-NNN
from: frontend
to: explorer
type: task
priority: medium
status: UNREAD
model: haiku
created: YYYY-MM-DD
---

# Kutatási kérés: [Téma]

## Kontextus
[Milyen UI implementációhoz kell a kutatás]

## Kutatási kérdések
1. [Kérdés 1 — pl. "Hogyan implementáljuk a drag-drop-ot TanStack-kel?"]
2. [Kérdés 2]

## Elvárt output
- Kód példák a kódbázisból
- Best practice összefoglaló
- Komponens struktúra javaslat
```

### Mikor kérj segítséget a Librarian-tól?

A **Librarian** a SpaceOS tudásbázis kurátora. Használd ha:
- **Olvasólista kell** — milyen cikkeket olvassak el React patterns-ről?
- **Knowledge doc keresés** — hol van dokumentálva egy UI pattern?
- **Design system referencia** — milyen színek/spacing van definiálva?
- **Best practices összefoglaló** — mi a bevált megoldás frontend-en?

**Inbox üzenet minta a Librarian-nak:**
```yaml
---
id: MSG-LIBRARIAN-NNN
from: frontend
to: librarian
type: task
priority: medium
status: UNREAD
model: haiku
created: YYYY-MM-DD
---

# Tudás összegyűjtés: [Téma]

## Kontextus
[Milyen feladathoz kell a tudás — pl. "Form validáció pattern keresés"]

## Kérdések
1. [Mit szeretnél tudni? — pl. "Milyen form library-t használunk?"]
2. [Milyen összefoglalóra van szükség?]

## Elvárt output
- Releváns knowledge doc linkek
- Olvasólista ha külső forrás is releváns (React docs, blog cikkek)
```

### Tipikus Frontend use-case-ek

| Probléma | Kihez fordulj | Mit kérj |
|---|---|---|
| Új oldal scaffold | Explorer | "Hogyan néz ki a többi page struktúrája?" |
| State management | Librarian | "Zustand vs Context összehasonlítás" |
| API integration | Explorer | "Hogyan működik a useApi hook?" |
| React 19 features | Librarian | "Olvasólista Server Components-ről" |
| Accessibility pattern | Explorer | "Hogyan kezeljük a focus management-et?" |
| Tailwind customization | Librarian | "Design system token-ök dokumentációja" |

---

## PARALLEL WORKERS (ADR-049 Phase 3)

> **Függetlenül futtatható feladatok párhuzamosítása** — Cost-aware worker management

### Mikor használd

- **Component development** — Több independent komponens egyidejű fejlesztése
- **Best-of-N implementation** — 2-3 megközelítés közül választás (pl. state management)
- **Multi-page features** — Több page párhuzamos implementáció

### MCP Tools

```bash
# Parallel tasks with dependencies
mcp__spaceos-knowledge__spawn_parallel_workers
  terminal: "frontend-2"
  tasks: [
    {id: "api-hook", prompt: "Create useQuoteRequest hook"},
    {id: "ui", prompt: "Build QuoteForm component", depends_on: ["api-hook"]}
  ]

# Best-of-N selection (2-5 workers)
mcp__spaceos-knowledge__spawn_raw_workers
  terminal: "frontend-2"
  task: "Optimize dashboard performance"
  count: 3
  criteria: "fastest load time with best UX"

# Worker status + cost tracking
mcp__spaceos-knowledge__get_worker_status
  terminal: "frontend-2"
```

### Cost Limits

| Threshold | Action |
|-----------|--------|
| **Soft limit:** $3/hour | Warning logged |
| **Hard limit:** $5/hour | Alert sent to Root |
| **Critical:** $10/hour | Auto-kill all workers |
| **Max parallel:** 5 worker/terminal | Queue additional requests |

### Példa használat

**Scenario:** 3 page párhuzamos fejlesztés

```
1. spawn_parallel_workers tasks=[
     {id: "catalog-page", prompt: "Create CatalogPage"},
     {id: "supplier-page", prompt: "Create SupplierPage"},
     {id: "quote-page", prompt: "Create QuoteRequestPage"}
   ]
2. Párhuzamos fejlesztés = gyorsabb sprint
3. Integration test utána
```

**NE használd ha:**
- Shared component (pl. ugyanaz a form több helyen)
- Complex state dependency (egyik page függ a másiktól)
- Single component (túl egyszerű)

---

## CODE GENERATOR TOOLCHAIN (ADR-050)

> **Automatizált kódgenerálás** — API kliensek, komponensek, modulok
>
> A Frontend terminál aktívan használja ezeket az eszközöket fejlesztéskor.

### Elérhető MCP Tools

| Tool | Leírás |
|------|--------|
| `generate_api_client` | Orval (Portal) API kliens generálás — React Query hooks |
| `generate_component` | React komponens SpaceOS mintákkal |
| `generate_hook` | React hook generálás (query/mutation/state/effect) |
| `generate_module` | .NET modul scaffold (Backend) |
| `get_codegen_status` | Generátor konfiguráció és fájlok státusza |

### CLI Használat

```bash
# Státusz ellenőrzés
spaceos status

# Portal API kliens generálása (Kernel változás után)
spaceos generate api-client portal

# React komponens scaffold
spaceos generate component FlowEpicCard --category feature --with-test --with-story
```

### React Komponens Generálás

**Új komponens scaffold:**
```bash
spaceos generate component NestingViewer --category feature --with-test --with-story
```

**Opciók:**
| Flag | Leírás |
|------|--------|
| `--category <feature\|ui\|layout>` | Komponens kategória |
| `--with-test` | Jest/Vitest teszt fájl |
| `--with-story` | Storybook story |

**Generált struktúra:**
```
src/components/features/NestingViewer/
├── NestingViewer.tsx       ← Fő komponens
├── NestingViewer.module.css ← Scoped stílusok
├── NestingViewer.test.tsx  ← Tesztek (opcionális)
├── NestingViewer.stories.tsx ← Storybook (opcionális)
└── index.ts                ← Export
```

### React Hook Generálás

**Új hook scaffold:**
```bash
spaceos generate hook Orders --type query --with-cache --endpoint /api/orders
```

**4 hook típus:**
| Típus | Leírás | Példa |
|-------|--------|-------|
| `query` | Adat lekérdezés | `useOrders`, `useQuote` |
| `mutation` | Adat módosítás | `useCreateOrder`, `useSubmitQuote` |
| `state` | Lokális állapot kezelés | `useAuth`, `useModal` |
| `effect` | Side effect kezelés | `useWindowResize`, `useDebounce` |

**Opciók:**
| Flag | Leírás |
|------|--------|
| `--type <query\|mutation\|state\|effect>` | Hook típus |
| `--with-test` | Vitest teszt fájl |
| `--with-cache` | TanStack Query integráció (query/mutation) |
| `--endpoint <path>` | API endpoint path |

**Generált hook (--with-cache):**
```typescript
// useOrders.ts - TanStack Query verzió
import { useQuery } from '@tanstack/react-query';

export const useOrders = (options = {}) => {
  return useQuery({
    queryKey: ['orders'],
    queryFn: fetchOrders,
    staleTime: 5 * 60 * 1000,  // 5 perc cache
    gcTime: 30 * 60 * 1000,    // 30 perc garbage collection
  });
};
```

### Orval API Kliens

A `spaceos generate api-client portal` parancs futtatásakor:

**Generált output:** `datahaven-web/client/src/api/generated/kernel/`

**Használat komponensben:**
```typescript
import { useGetOrders, useCreateOrder } from '@/api/generated/kernel';

const OrderList = () => {
  const { data, isLoading, error } = useGetOrders();
  const createOrder = useCreateOrder();

  // TanStack Query hooks automatikus cache + retry kezeléssel
};
```

### Frontend Workflow

**1. Kernel API változás értesítés után:**
```bash
# API kliens újragenerálása
spaceos generate api-client portal

# Build ellenőrzés
npm run build
```

**2. Új feature komponens:**
```bash
# Scaffold
spaceos generate component QuoteForm --category feature --with-test

# Testreszabás
# src/components/features/QuoteForm/QuoteForm.tsx
```

**3. Státusz ellenőrzés:**
```bash
spaceos status
# Portal API: ✓ 21 files
# Orval config: ✓ Found
```

### Fontos szabályok

- **Kernel API változás után mindig futtasd** a `generate api-client portal` parancsot
- A `src/api/generated/` mappa tartalma **NE módosítandó kézzel**
- Generált hookokat az `import { ... } from '@/api/generated/kernel'` path-ról érd el

---

## 🧠 CONTEXT PERSISTENCE — MCP TOOLS

> **Goal Drift Prevention** — Context window kezelés és fókusz megőrzés.

### Session Start (KÖTELEZŐ)

```
mcp__spaceos-knowledge__build_session_start_context
  terminal: "frontend-2"

mcp__spaceos-knowledge__get_context_saturation
  terminal: "frontend-2"
```

### Context Saturation Thresholds

| Turn Count | Teendő |
|------------|--------|
| **<30** | Normál működés |
| **30-50** | ⚠️ Fókuszálj a fő célra! |
| **>50** | 🚨 Kérj új session-t Monitor-tól! |

### Session End (KÖTELEZŐ)

```
mcp__spaceos-knowledge__write_session_state
  terminal: "frontend-2"
  epic_id: "EPIC-ID"
  epic_progress: 35
  last_active_task: "MSG-ID"

mcp__spaceos-knowledge__write_terminal_status_md
  terminal: "frontend-2"
  system_status: "operational"
  current_focus: "..."
  recent_actions: [...]
  next_steps: [...]
```

**Részletes dokumentáció:** `docs/knowledge/patterns/GOAL_PERSISTENCE_PATTERNS.md`

---

## MCP TOOLS — FRONTEND WORKFLOW

> **Phase 1 Infrastructure Tool** — Component Scaffold (Available from 2026-07-07)
> **Full Documentation:** `docs/knowledge/patterns/MCP_TOOLS_CATALOGUE.md`

### Component Scaffolding

**Tool:** `scaffold_component`

**Purpose:** Generate React hooks, components, or API clients with boilerplate code.

**Usage:**
```typescript
// Generate React Query hook from OpenAPI
const result = await mcp__spaceos_knowledge__scaffold_component({
  component_type: "react_hook",      // "react_hook" | "react_component" | "api_client"
  name: "useCostBudget",
  api_spec: "openapi.yaml#/components/schemas/CostBudget",  // Optional
  output_dir: "client/src/hooks/",
  with_tests: true,                  // Default: true
  with_storybook: false              // Default: false (components only)
});

console.log("Created files:", result.filesCreated);
// ["client/src/hooks/useCostBudget.ts", "client/src/hooks/__tests__/useCostBudget.test.ts"]

console.log("Next steps:", result.nextSteps);
// ["Review generated hook", "Add business logic", "Run tests: npm test useCostBudget"]
```

### Use Cases

**1. React Hook Generation:**
```typescript
await mcp__spaceos_knowledge__scaffold_component({
  component_type: "react_hook",
  name: "useQuoteComparison",
  output_dir: "client/src/hooks/",
  description: "Quote comparison and filtering logic"
});

// Generates:
// - hooks/useQuoteComparison.ts (useState, useCallback, useEffect pattern)
// - hooks/__tests__/useQuoteComparison.test.ts
```

**2. React Component Generation:**
```typescript
await mcp__spaceos_knowledge__scaffold_component({
  component_type: "react_component",
  name: "CostBudgetWidget",
  output_dir: "client/src/components/",
  description: "Cost budget tracking widget",
  with_tests: true,
  with_storybook: true
});

// Generates:
// - components/CostBudgetWidget.tsx (Functional component + TypeScript)
// - components/CostBudgetWidget.module.css (CSS modules)
// - components/__tests__/CostBudgetWidget.test.tsx
// - components/CostBudgetWidget.stories.tsx (if with_storybook: true)
```

**3. API Client Generation:**
```typescript
await mcp__spaceos_knowledge__scaffold_component({
  component_type: "api_client",
  name: "costBudgetClient",
  api_spec: "openapi.yaml#/paths/~1api~1costs",
  output_dir: "client/src/api/"
});

// Generates:
// - api/costBudgetClient.ts (Axios instance, typed methods, error handling)
```

### ROI

- **Time Saved:** 2-3 hours/week (eliminates boilerplate writing)
- **Consistency:** All components follow SpaceOS patterns
- **Test Coverage:** Automatic test file generation

### Quick Reference

| Component Type | Generated Files | Pattern |
|---------------|-----------------|---------|
| `react_hook` | Hook + Test | useState, useCallback, useEffect |
| `react_component` | Component + CSS + Test | Functional component, CSS modules |
| `api_client` | Client file | Axios, typed methods, error handling |

**Example After Generation:**
```bash
# Review generated code
cat client/src/hooks/useCostBudget.ts

# Add business logic (customize as needed)
# Run tests
npm test useCostBudget

# Integrate into component
```

---
