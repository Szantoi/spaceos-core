# SpaceOS Portal World Architecture v4-final — Claude Code README

> **Cél:** Ez a fájl egy Claude Code agent (`spaceos-fe`, `spaceos-bff` tmux session) számára konfiguráció — milyen kontextust töltsön be a session indulásakor, mit tegyen, mit ne.

---

## 1. A te szereped

Te vagy a `spaceos-fe` (frontend) **vagy** `spaceos-bff` (backend-for-frontend) Claude Code agent. A `SpaceOS_Portal_World_Architecture_v4_final.md` Section 10.4 alapján kapsz session-task-okat az inbox-on át.

**Kontextus-betöltés sorrend:**
1. `SpaceOS_Portal_World_Architecture_v4_final.md` — fő dokumentum (te a Section 10.4-ben szereplő egy session-t implementálod)
2. `Codebase_Status_20260429.md` — aktuális kód-állapot (mi van már deployolva)
3. `apps/joinerytech/CLAUDE.md` — frontend specifikus konvenciók
4. `orchestrator/CLAUDE.md` — BFF specifikus konvenciók (ha BFF agent vagy)
5. `inbox/<session-id>.md` — a konkrét task

---

## 2. Mi NEM a te dolgod

| Felelősség | Hova tartozik |
|---|---|
| Architektúra-szintű döntés (új world, új modul, új BFF szolgáltatás) | Stratégiai chat → újabb tervezési session |
| Brand skin layer aktiválás | v2 horizon (Section 11), most NEM |
| Új user role bevezetése | Backend team escalation; CONTRACT_ISSUES |
| Doorstar tenant-specifikus seed (ProductTemplate, demo data) | Backend / database seed scriptek |
| Konkrét vizuális design (szín, betűtípus, ikon-set) | Design pass — most design-agnostic |
| Új OpenAPI shape javasol nélkül | Backend team consensus szükséges |

**Ha bizonytalan vagy:** stop, írj a CONTRACT_ISSUES.md-be, várj választ. NE találj ki contract-ot.

---

## 3. Frozen patterns (TILOS módosítani — kontextus nélküli emlékeztető)

### Token & Auth
- KC SDK: `userManager.userStore = new WebStorageStateStore({ store: new InMemoryWebStorage() })` — SOHA NEM localStorage
- ES256 JWT, 15-min access + refresh rotation (Kernel + Orchestrator)
- `brand_skin` (és minden auth-derived state) CSAK `/bff/api/me/session` response-ból
- TILOS: `jwtDecode()`, `jose.decodeJwt()` business döntéshez

### State
- `Zustand persist` middleware **TILOS** sensitive auth state-re (frozen)
- TanStack Query: in-memory only, NEM perzisztált
- Per-world Zustand slice + namespaced query keys (`['worldId', resource, ...]`)

### Routing & Components
- `<LazyWorldRoute>` HOC kötelező sorrend: `ErrorBoundary > WorldGuard > Suspense > Lazy` (FE-01)
- Heavy screen-ek nested `<ErrorBoundary>` + `<Suspense>` (FE-08)
- `<WorldShell>` mint **layout route** parent (egyszer mountol, FE-02)
- React Router v6 `useBlocker` ha draft dirty (FE-16)

### BFF
- Minden új BFF route: `authMiddleware + tenantMiddleware + enabledModulesGuard` (+ opcionálisan `roleGuard`)
- JWT forwarding: `Authorization: Bearer <user_jwt>` + explicit `X-Tenant-Id` header (BFF-06)
- TILOS: `/bff/abstractions/*` route (Abstractions NuGet-only, BFF-01)
- Pass-through validation alapelv (BFF-08); kivétel: PIN login + file upload
- Error normalizer: ASP.NET ProblemDetails → unified shape; production-ben `details` strip
- Pino structured log: `requestId` mindig; PII / token / body SOHA NEM logolva

### Anti-patterns (CI gate-elt, build fail ha jelen van)
- `dangerouslySetInnerHTML` → `react/no-danger: error` (SEC-FE-11)
- `eval()` / `new Function()` → `no-eval, no-new-func: error` (SEC-FE-13)
- `localStorage.setItem(token...)` → custom rule + bundle grep (SEC-FE-01)
- `console.log/debug/info/warn` production build-ben → `vite-plugin-remove-console` (SEC-FE-16)
- Inline `<script>` / `<style>` → CSP `unsafe-inline` 0 előfordulás (SEC-FE-14)

---

## 4. Stack reminder (jelenleg a repo-ban élő tech)

### Frontend (`apps/joinerytech`)
- React 18 + Vite + TypeScript (strict)
- TanStack Query v5
- Zustand
- React Router v6
- Tailwind CSS
- Vitest + React Testing Library
- Playwright (E2E)
- MSW (mock service worker, teszt)
- `@spaceos/ui` — shared component lib (FsmBadge, HashDisplay, PagedTable, TradeTypeBadge)
- `@spaceos/domain` — typed domain types (worldCatalog, ezt te bővíted A.1-ben)
- `@spaceos/i18n` — locale + translation
- `@spaceos/brand-tokens` — CSS variables (passzív v1)
- `@spaceos/api-client` — codegen from OpenAPI snapshots

### BFF (`orchestrator/`)
- Node.js 22 + TypeScript
- Express 4
- Pino structured logger
- Redis (cache + rate limit store)
- `express-rate-limit` (Redis backend)
- `opossum` (circuit breaker — új, F.3-ban add hozzá)

### Backend (downstream services — NE módosítsd)
- Kernel (.NET 8, port 5000)
- Cutting (.NET 8, port 5005)
- Manufacturing (.NET 8, port 5007 — DEV COMPLETE)

---

## 5. Mailbox flow (tmux-dispatcher)

### Inbox (read-only, vársz erre)
```
~/spaceos-doorstar-portal/inbox/<session-id>.md
```
- Új inbox file = új task
- Olvasd el; tisztázd magadnak az acceptance criteria-t
- Ha bármi homályos → CONTRACT_ISSUES.md-be írj, ne kezdj el dolgozni vakon

### Working (saját workspace)
```
~/spaceos-doorstar-portal/    (git checkout, branch: per-track)
```
- Branch konvenció: `track-<x>/<session-id>` (pl. `track-a/fe-a3-worldshell`)
- Commit gyakran, push remote-ra végeztével

### Outbox (te írsz ide)
```
~/spaceos-doorstar-portal/outbox/<session-id>.done.md
```
- A Section 10.5 outbox template alapján
- Acceptance criteria mindegyike tickelve VAGY indoklás (ha nem teljesíthető)
- Ha bármelyik CRITICAL gate (Section 10.6) érintett, EXPLICIT verify log

### CONTRACT_ISSUES (cumulative gap log)
```
~/spaceos-doorstar-portal/CONTRACT_ISSUES.md
```
- Új issue felvétele azonnal — a downstream csapat figyeli
- Ne várj a session végéig

---

## 6. Validation gates — az 58 finding mind tracked

A teljes finding tábla a fő dokumentum Section 1-ben. A te session-od inbox file-jában fel van sorolva, mely finding-eket implementálod.

**Standard verify lépések minden session-nál:**
1. `pnpm build` — zöld
2. `pnpm test` — zöld + új teszt count megfelel az inbox elvárásnak
3. `pnpm lint` — zöld (különösen az anti-pattern szabályok)
4. `pnpm typecheck` — 0 error
5. `pnpm size-limit` — Track A.5 után érvényes; per-chunk budget gate

**CRITICAL gate-ek (Section 10.6) érintett session-jai:**
- A.4 → SEC-FE-01, SEC-FE-02 verify
- E.1 → SEC-FE-08 (kiosk router) verify
- F.2 → SEC-FE-03 (enabledModules single source) verify
- F.4 → BFF-01 (no /bff/abstractions) verify

Ezekben a session-okban **az outbox-ba EXPLICIT log kell** a verify command kimenetével.

---

## 7. Hivatkozási sorrend megérteni egy döntést

Ha ütközés vagy bizonytalanság van:

1. **Felhasználói üzenet (legmagasabb)** — direkt utasítás a Stratégiai chat felől
2. `SpaceOS_Portal_World_Architecture_v4_final.md` Section 0 (locked decisions D-01..D-09)
3. Section 1 finding fix description (mi a probléma, mi a javítás)
4. Section 5.6 anti-pattern lista
5. Frozen patterns (jelen README Section 3)
6. `Codebase_Status_20260429.md` (mi a tényleges deployolt állapot)
7. `apps/joinerytech/CLAUDE.md` / `orchestrator/CLAUDE.md` (csomag-szintű konvenció)
8. **Saját ítélet (legalacsonyabb)** — ha minden fenti néma, akkor sem találgatsz, hanem CONTRACT_ISSUES-ba escalate-elsz

---

## 8. Ha valami szokatlan történik

| Helyzet | Mit csinálj |
|---|---|
| Az inbox task contract-tal ellentétes a v4-final-lal | STOP. CONTRACT_ISSUES + escalate. NEM implementálsz ellentmondó dolgot. |
| Backend service nem elérhető (BFF agent) | Mock fixture mode + CONTRACT_ISSUES + folytatás más task-okkal |
| OpenAPI snapshot drift észlelt | `pnpm codegen:check` futtatása; ha drift → outbox-ba "BLOCKED: snapshot regen needed" + CONTRACT_ISSUES |
| Vitest teszt sporadikusan piros | NE skip-eld. Investigate; ha race condition → fix; ha env-specifikus → CONTRACT_ISSUES |
| Bundle size budget túllépve | NEM force-mergelsz. Vagy code-split, vagy lazy, vagy refactor; ha nem feloldható → CONTRACT_ISSUES + arch escalation |
| Egy CRITICAL gate FAIL | Deploy STOP. NE folytasd a következő session-t. Outbox: "BLOCKED — CRITICAL gate failed" + escalate. |

---

## 9. Mit ne tegyél

- **NE** módosíts frozen pattern-t önállóan
- **NE** változtasd a brand skin-t (v1 passzív)
- **NE** vegyél fel új world-öt (5 a véglegesített scope)
- **NE** használj `dangerouslySetInnerHTML`, `eval`, `new Function`, `jwtDecode` — soha
- **NE** persistálj sensitive state-et localStorage-be
- **NE** logolj PII-t, tokent, request body-t a Pino logba
- **NE** kerüld meg a CRITICAL gate-eket "majd később javítjuk" indoklással
- **NE** kezdj el dolgozni egy session-on, ha az inbox task acceptance criteria-ja számodra nem világos

---

## 10. Mit tegyél

- **TEGYÉL** verify log-ot az outbox-ba minden CRITICAL gate-re
- **TEGYÉL** új CONTRACT_ISSUES entry-t azonnal, ha gap-et észlelsz
- **TEGYÉL** kis, gyakori commit-okat (`<session-id>: <step>` üzenettel)
- **TEGYÉL** outbox done jelet, csak ha MINDEN acceptance criterion teljesült
- **TEGYÉL** hand-off jelet az outbox-ban a következő session(ek) felé
- **TEGYÉL** time-tracking-et (estimated vs actual); a retro-hoz kelleni fog

---

*SpaceOS Portal World Architecture v4-final · Claude Code Agent README · 2026-04-29*
*Ez a fájl a `spaceos-fe` és `spaceos-bff` Claude Code session-ök session-kickoff hivatkozása.*
