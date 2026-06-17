---
name: spaceos-frontend-arch-planner
description: >
  SpaceOS frontend architecture planning skill. Multi-review pipeline that produces
  a versioned, implementation-ready frontend architecture document through structured
  review passes (frontend → security → backend/BFF), culminating in a Claude Code–ready
  implementation plan with Definition of Done, performance budgets, and component inventory.

  ACTIVATE when the user asks for frontend/portal/UI architecture planning:
  "Portal tervezés", "frontend architektúra", "UI tervdokumentum", "portal shell tervezés",
  "world navigation tervezés", "frontend implementációs terv", "UI/UX architektúra",
  "portal world architecture", "BFF route tervezés", "frontend sprint terv".

  Do NOT activate for ad-hoc frontend questions — only for the full
  multi-review pipeline that produces a shippable implementation document.
---

# SpaceOS Frontend Architecture Planner

Frontend-specifikus multi-review pipeline. A backend `spaceos-arch-planner` párja.
Ugyanaz a struktúra és minőségi gate rendszer, frontend review lencsékkel.

---

## 1. Mikor használd

**IGEN:**
- Új frontend modul / world / page-csoport tervezése
- Portal shell / routing / navigation architektúra
- BFF (Backend-for-Frontend) route tervezés az Orchestrator-ban
- Design-to-code mapping (Claude Design → React implementáció)

**NEM:**
- Egyedi komponens kérdés → válaszolj közvetlenül
- Backend-only modul → `spaceos-arch-planner`
- Styling / CSS kérdés → `senior-frontend` skill

---

## 2. Pre-loaded SpaceOS Frontend context

### Stack (frozen)

| Réteg | Stack | Státusz |
|-------|-------|---------|
| L4 Portal | React 18, TypeScript 5, Vite 5, Turborepo | DEPLOYED |
| L4 UI | Tailwind CSS 3, `@spaceos/ui`, `@spaceos/brand-tokens` | DEPLOYED |
| L4 State | Zustand (auth, brand) + TanStack Query (server state) | DEPLOYED |
| L4 Auth | Keycloak PKCE, ES256 JWT, role-based | DEPLOYED |
| L4 i18n | `@spaceos/i18n`, hostname-based locale (HU/EN) | DEPLOYED |
| L3 BFF | Orchestrator Express `/bff/*` proxy | DEPLOYED |
| L4 Test | Vitest (unit) + Playwright (E2E) + MSW (mock) | SCAFFOLD |

### Frontend frozen decisions

- Frontend NEVER computes measurements (Data → Rules → Geometry axiom)
- Frontend NEVER calls Kernel directly — always through Orchestrator BFF
- JWT `brand_skin` read from Orchestrator response body — never decoded client-side
- Zustand `persist` middleware MUST NOT persist sensitive auth state to localStorage
- Lazy-loaded module routes require `<Suspense>` + `<ErrorBoundary>` wrappers
- OpenAPI codegen from committed snapshot — never runtime schema discovery
- `enabledModules` tenant configuration drives sidebar/route visibility

### Monorepo structure (Turborepo)

```
spaceos-fe/
├── apps/
│   └── joinerytech/          ← main portal app (portal.joinerytech.hu)
│       ├── src/
│       │   ├── App.tsx
│       │   ├── worlds/        ← world-based routing (production, sales, design, etc.)
│       │   ├── pages/         ← shared/legacy pages
│       │   ├── components/    ← app-specific components
│       │   └── stores/        ← Zustand stores (authStore, brandStore)
│       └── vite.config.ts
├── packages/
│   ├── @spaceos/domain/       ← TypeScript interfaces
│   ├── @spaceos/api-client/   ← BFF client (OpenAPI codegen)
│   ├── @spaceos/ui/           ← Shared components (FsmBadge, HashDisplay, PagedTable)
│   ├── @spaceos/brand-tokens/ ← Brand skin system (JoineryTech default, Doorstar override)
│   ├── @spaceos/i18n/         ← Hostname-based locale detection
│   └── @spaceos/joinery-ui/   ← Trade-specific joinery components
└── turbo.json
```

### Existing UI components (`@spaceos/ui`)

FsmBadge · HashDisplay · PagedTable · TradeTypeBadge · JsonIntentEditor

### BFF routes (existing)

| Route | Backend | Státusz |
|-------|---------|---------|
| `/bff/api/*` | Kernel :5000 | DEPLOYED |
| `/bff/joinery/*` | Joinery :5002 | DEPLOYED |
| `/bff/cutting/*` | Cutting :5005 | DEPLOYED |

---

## 3. The pipeline — 3 review lenses

```
Phase 0 — Context load
  └─ Read: latest Codebase_Status, design bundle (if Claude Design output), existing portal v4 docs

Phase 1 — v1 Draft
  └─ Route map · Component tree · State architecture · BFF surface · i18n keys
     Output: {document}_v1.md

Phase 2 — Frontend review
  📂 Load: references/sub-senior-frontend.md
  └─ Findings → v2 delta table
     Focus: component decomposition, code splitting, state management,
            performance budget, a11y, responsive breakpoints, bundle size
     Output: {document}_v2.md

Phase 3 — Security review
  📂 Load: references/sub-senior-security.md
  └─ Findings → v3 delta table
     Focus: auth flow, token handling, CORS, XSS, CSP, role-based rendering,
            enabledModules trust boundary, Shop Floor PIN isolation
     Output: {document}_v3.md

Phase 4 — Backend/BFF review
  📂 Load: references/sub-senior-backend-bff.md
  └─ Findings → v4 delta table
     Focus: BFF route design, OpenAPI codegen, aggregation endpoints,
            error handling, retry/timeout, cache strategy
     Output: {document}_v4.md

Phase 5 — Final document assembly
  └─ Cumulative Finding Summary · DoD · Sprint plan · Component inventory
```

**Note:** `database-designer` és `database-schema-designer` lencsék NEM szerepelnek — frontend tervezésnél nincs új DB schema.

---

## 4. Document structure (every version)

```markdown
# {Project} — {Feature Name}
## Frontend Architecture

> Verzió: vN — {date}
> Státusz: {DRAFT | REVIEW | IMPLEMENTÁCIÓRA KÉSZ}
> Prereq: {backend modules that must be DEPLOYED}
> Design forrás: {Claude Design bundle link, ha van}
> Kumulált review: {lens list} → vN

---
## 1. Kumulált Finding Összesítő (v1 → vN)
## 2. Route architektúra
   ### Route map (React Router v6)
   ### Code splitting stratégia
   ### enabledModules → route visibility
## 3. Komponens architektúra
   ### Komponens fa (tree diagram)
   ### Új komponensek listája (shared vs. app-specific)
   ### Meglévő @spaceos/ui komponensek újrahasználata
## 4. State management
   ### Zustand store-ok (új / bővített)
   ### TanStack Query hook-ok (query key stratégia)
   ### Server state vs. client state szétválasztás
## 5. BFF API surface
   ### Új Orchestrator proxy route-ok
   ### Aggregáló endpoint-ok (ha kellenek)
   ### OpenAPI codegen bővítés
   ### Error handling + retry stratégia
## 6. i18n & Brand
   ### Új fordítási kulcsok (HU/EN)
   ### Brand token bővítés (ha kell)
## 7. Responsive & Accessibility
   ### Breakpoint stratégia (desktop / tablet / mobile)
   ### a11y követelmények (WCAG 2.1 AA minimum)
   ### Touch target minimumok (48px)
## 8. Test stratégia
   ### Vitest unit tesztek (komponens + hook)
   ### Playwright E2E szcenáriók
   ### MSW mock handler-ek
## 9. Definition of Done
   ### Frontend gates
   ### BFF gates
   ### Test gates
   ### Performance gates
   ### Összesített
## 10. Sprint terv (Claude Code implementáció)
   ### Végrehajtási sorrend (track-ek)
   ### Agent utasítás
   ### Kockázatok és mitigációk
## 11. Mi jön utána (roadmap)
```

---

## 5. Finding format

Ugyanaz mint a backend planner:

| ID | Súly | Terület | Probléma | vN javítás |
|----|------|---------|----------|------------|
| FE-01 | 🟠 | Route | ... | ... |
| SEC-FE-01 | 🔴 | Auth | ... | ... |
| BFF-01 | 🟡 | API | ... | ... |

Prefixek:
- `FE-NN` — Frontend review findings
- `SEC-FE-NN` — Security review findings (frontend-specifikus)
- `BFF-NN` — Backend/BFF review findings

Severity: 🔴 CRITICAL · 🟠 HIGH · 🟡 MEDIUM · 🟢 LOW

---

## 6. Frontend-specifikus DoD gates

Append to every DoD Összesített section:

```markdown
### Frontend gates
- [ ] Minden lazy-loaded world `<Suspense>` + `<ErrorBoundary>` wrapper-ben
- [ ] `enabledModules` ellenőrzés: nem-engedélyezett world route → redirect Home
- [ ] Minden form input label-lel + aria attribútummal
- [ ] Responsive: 3 breakpoint tesztelve (desktop 1280px, tablet 768px, mobile 375px)
- [ ] Touch target minimum 48px minden interaktív elemen
- [ ] Sötét/világos sidebar konzisztens brand token-ökkel

### BFF gates
- [ ] Minden új BFF route: auth middleware + tenant isolation
- [ ] OpenAPI snapshot frissítve + codegen lefuttatva
- [ ] Error response format konzisztens (Result<T> pattern)
- [ ] Timeout: 10s default, 30s aggregáló endpoint-okra

### Test gates
- [ ] Meglévő {N} FE teszt zöld (Vitest + Playwright)
- [ ] Új Vitest unit tesztek: >= {N} db
- [ ] Új Playwright E2E flow-k: >= {N} db
- [ ] MSW handler minden új BFF route-hoz

### Performance gates
- [ ] Lighthouse Performance >= 90 (desktop)
- [ ] Lighthouse Accessibility >= 95
- [ ] Bundle size: app entry < 200KB gzip
- [ ] Lazy-loaded world chunk: < 80KB gzip per world
- [ ] First Contentful Paint < 1.5s (desktop, prod build)
- [ ] No layout shift (CLS < 0.1)
```

---

## 7. Component inventory format

Minden v1 draft-ban kötelező:

```markdown
### Új komponensek

| Komponens | Csomag | Típus | Props | Leírás |
|-----------|--------|-------|-------|--------|
| `WorldCard` | app | Presentational | `world, accent, badge, onClick` | Home screen világ kártya |
| `WorldShell` | app | Layout | `world, screen, children` | Világ shell sidebar + topbar |
| `Sparkline` | @spaceos/ui | Shared | `data, color, width, height` | Mini trend chart |

### Meglévő komponensek újrahasználata

| Meglévő | Hol használjuk | Módosítás kell? |
|---------|---------------|-----------------|
| `FsmBadge` | Gyártás → végrehajtás státusz | Nem |
| `PagedTable` | Rendelések, Készlet lista | Nem |
| `HashDisplay` | Beállítások → Napló | Nem |
```

---

## 8. Design-to-code mapping section

Ha Claude Design bundle elérhető:

```markdown
### Design → Code mapping

| Design fájl | React komponens | Csomag | Megjegyzés |
|-------------|----------------|--------|------------|
| `page-home.jsx` | `HomeScreen.tsx` | app | World kártya grid |
| `page-shopfloor.jsx` | `ShopFloorWorld.tsx` | app | Teljes képernyő, dark mode |
| `ui.jsx → StatusPill` | `StatusPill.tsx` | @spaceos/ui | Merge FsmBadge-dzsel? |
| `data-worlds.js` | `worldConfig.ts` | @spaceos/domain | enabledModules mapping |
```

---

## 9. Sub-skill betöltési utasítások

| Review fázis | Betöltendő fájl | Mikor |
|---|---|---|
| v2 — Frontend review | `references/sub-senior-frontend.md` | v1 draft kész |
| v3 — Security review | `references/sub-senior-security.md` | v2 lezárva |
| v4 — BFF review | `references/sub-senior-backend-bff.md` | v3 lezárva |

**Betöltési instrukció:**
```
Olvasd el a [fájlnév] tartalmát.
Alkalmazd a frontend / security / BFF analitikai keretrendszert a tervdokumentumra.
Minden findinget a SpaceOS finding formátumban dokumentálj: [ID] · [súly] · [terület] · [probléma] · [javítás]
```

---

## 10. Naming convention

```
SpaceOS_{FeatureName}_Frontend_Architecture_v{N}.md
```

Examples:
- `SpaceOS_Portal_World_Architecture_v1.md`
- `SpaceOS_ShopFloor_Terminal_Frontend_v1.md`
- `SpaceOS_DesignEditor_Frontend_v1.md`

---

## 11. Workflow

1. User kéri a frontend tervezést (pl. "Portal world architecture tervezés")
2. Load: latest Codebase_Status, design bundle, existing portal v4 docs
3. Ha van nyitott döntés → táblázatban kérd be (D-01, D-02, ... egy mondatos válasz)
4. Run pipeline: v1 draft → frontend review → security review → BFF review → final
5. "Implementációs terv" kérésre → Section 10 (Claude Code sprint plan) kitöltése
6. Output: `.md` fájl a naming convention szerint

---

*SpaceOS Frontend Architecture Planner · v1.0 · 2026-04-29*
