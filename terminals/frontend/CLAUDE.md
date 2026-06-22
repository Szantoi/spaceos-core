# CLAUDE.md — SpaceOS Frontend

> **Modell: `sonnet`**
>
> A Frontend terminál az összes React/TypeScript UI-t fejleszti:
> JoineryTech Portal, Datahaven Dashboard, és egyéb React alkalmazások.

---

## SESSION RITUAL

> **Használj Claude Code built-in toolokat:** Bash, Read, Write, Edit, Grep, Glob

### 1. SESSION START — Datahaven regisztráció

**Bash tool + curl:**
```bash
curl -X POST https://datahaven.joinerytech.hu/api/terminal/status \
  -H "Authorization: Bearer dev-token-spaceos-dashboard-2026" \
  -H "Content-Type: application/json" \
  -d '{"terminal":"frontend","status":"working","currentTask":"Session started"}'
```

**Inbox olvasás (Read tool):**
- Read minden UNREAD üzenetet: `/opt/spaceos/terminals/frontend/inbox/*.md`

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
id: MSG-frontend-OUT-NNN
from: frontend
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
  -d '{"terminal":"frontend","status":"idle"}'
```
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

- **Mailbox:** `/opt/spaceos/terminals/frontend/inbox/` és `.../outbox/`
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
