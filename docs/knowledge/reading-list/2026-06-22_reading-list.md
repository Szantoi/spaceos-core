# Olvasólista — 2026-06-22

**Készítette:** Librarian terminál
**Cél:** Konstruktív, építő jellegű olvasnivalók a SpaceOS terminálok számára
**Módszer:** WebSearch tool + kiegyensúlyozott értékelés

---

## 📖 Backend Terminálnak

### 1. .NET 8 Minimal API Best Practices (2026)

**Források:**
- [High-Performance and Scalable APIs with .NET 8 Minimal API](https://medium.com/@mikhail.petrusheuski/high-performance-and-scalable-apis-with-net-8-minimal-api-947ff51578c7)
- [Building RESTful APIs with .NET 8: Essential Best Practices](https://dev.to/leandroveiga/building-restful-apis-with-net-8-essential-best-practices-for-performance-and-security-519l)
- [Minimal APIs in .NET 8: Endpoint Filters, Route Groups & Binding Updates](https://www.dotnet-guide.com/articles/minimal-apis-endpoint-filters-route-groups-binding-updates/)

**Összefoglaló:**

.NET 8 Minimal API best practices 2026-ra: async/await mindenütt, route groups szervezéshez, endpoint filters validációhoz/logginghoz, AOT compilation startup gyorsításhoz, in-memory/distributed caching (Redis) gyakori adatokhoz.

**Miért hasznos a SpaceOS-nek:**

✅ **Route Groups** — Kernel, Joinery, Identity modulok szétválasztására tökéletes
- Extension methods-szal minden modul külön fájlba szervezhető (`RegisterIdentityRoutes()`, `RegisterJoineryRoutes()`)
- Program.cs minimális marad, könnyen áttekinthető

✅ **Endpoint Filters** — JWT/RBAC validációhoz, tenant context beállításhoz
- Middleware helyett scope-olt filtering (csak adott endpointokra)
- Tenant isolation (RLS) beállítása filter-ben, minden endpoint előtt

✅ **ProblemDetails** — Egységes hibakezelés minden API-n keresztül
- Már implementálva van a Kernel-ben, de jó emlékeztető hogy következetes legyen

✅ **AOT Compilation** — VPS deploy gyorsabb indítás
- Docker container startup idő csökkentésére (jelenleg ~5-10s)

**SpaceOS kontextus:**

- **Kernel API:** Route groups már használva van (`/api/auth`, `/api/orders`)
- **Orchestrator (Node.js):** Ez .NET-specifikus, de pattern inspiráció lehet Express.js-hez
- **Security:** Endpoint filters authentication/authorization-höz (JWT + tenant check)

**Ajánlás:**

✅ **JAVASOLT** — Építő jellegű, modern best practices amit már részben használunk. Az endpoint filters konkrét fejlesztési lehetőség a tenant isolation finomhangolásához.

---

### 2. PostgreSQL RLS (Row Level Security) Performance Optimization

**Források:**
- [Optimizing Postgres Row Level Security (RLS) for Performance](https://scottpierce.dev/posts/optimizing-postgres-rls/)
- [Optimize RLS Policies for Performance - Postgres Best Practice](https://supaexplorer.com/best-practices/supabase-postgres/security-rls-performance/)
- [Postgres Row-Level Security Footguns](https://www.bytebase.com/blog/postgres-row-level-security-footguns/)
- [Row-level security recommendations - AWS Prescriptive Guidance](https://docs.aws.amazon.com/prescriptive-guidance/latest/saas-multitenant-managed-postgresql/rls.html)

**Összefoglaló:**

RLS performance 2026: SECURITY DEFINER functions (subquery helyett), STABLE funkciók (caching), indexelés tenant_id-n, FORCE ROW LEVEL SECURITY minden tenant táblán, subquery-alapú policy kerülése (exponenciális skálázás), LEAKPROOF funkciók használata.

**Miért hasznos a SpaceOS-nek:**

✅ **SECURITY DEFINER Functions** — Jelenleg Kernel tenant policy-k lehetnek subquery-alapúak
- 10,000 dokumentum esetén 10,000 subquery fut → 1 SECURITY DEFINER függvény gyorsabb

✅ **Index tenant_id Columns** — Kritikus minden RLS policy-nál
- Kernel orders, joinery batches, inventory stock táblák mind tenant_id-val szűrnek
- Sequential scan helyett index scan (100x+ gyorsabb)

✅ **FORCE ROW LEVEL SECURITY** — Biztonsági rés elkerülése
- Ha owner kapcsolódik, RLS bypass-olódhat → FORCE ROW LEVEL SECURITY kötelezővé teszi

✅ **Avoid Subquery-Based Policies** — Performance trap
- Kernel tenant policy audit: van-e subquery minden sorra?

**SpaceOS kontextus:**

- **Kernel tenant isolation:** RLS már implementálva van, de performance audit hasznos
- **Known issue:** `docs/knowledge/deployment/KNOWN_GOTCHAS.md` már tartalmaz RLS-related gotchák-at (GUC reset, connection interceptor)
- **VPS production:** 100+ order esetén RLS performance kritikus lesz

**Megfontolások:**

⚠️ **Denormalization Trade-off** — A források említik hogy denormalizáció RLS performance-hoz
- SpaceOS jelenleg normalizált schema (Kernel orders + joinery batches külön táblák)
- Jövőbeli optimization: tenant-specifikus adatok denormalizálása csak ha performance probléma van

**Ajánlás:**

✅ **JAVASOLT** — Audit RLS policies a Kernel-ben, add indexeket tenant_id-ra, használj SECURITY DEFINER functions subquery helyett. FORCE ROW LEVEL SECURITY minden tenant táblán.

---

### 3. Testcontainers .NET Integration Testing Best Practices (2026)

**Források:**
- [Testcontainers Best Practices for .NET Integration Testing](https://www.milanjovanovic.tech/blog/testcontainers-best-practices-dotnet-integration-testing)
- [Integration Testing with Testcontainers - ISE Developer Blog](https://devblogs.microsoft.com/ise/testing-with-testcontainers/)
- [Best Practices - Testcontainers for .NET](https://dotnet.testcontainers.org/api/best_practices/)
- [Qaskills - Testcontainers Best Practices 2026](https://qaskills.sh/blog/testcontainers-best-practices-2026)

**Összefoglaló:**

Testcontainers 2026: pin specific image versions (postgres:16-alpine nem latest), dynamic connection strings (UseSetting), container reuse local dev-en (5-30s → <1s), disable reuse in CI, shared container per test run (nem per test method), capture logs on failure, parallelize across test files (nem within).

**Miért hasznos a SpaceOS-nek:**

✅ **Pin Specific Versions** — `postgres:16-alpine` helyett `postgres:latest`
- EHS Module tests már használják: `PostgreSqlBuilder().WithImage("postgres:16-alpine")`
- Jó pattern, mások is kövessék

✅ **Container Reuse for Local Dev** — Startup time 5-30s → <1s
- Backend terminál napi munkája: 10-20x test run → 200-600s saved/day
- CI-ben disable (fresh environment)

✅ **Shared Container per Test Run** — Nem minden test method-hoz új container
- `IClassFixture<WebApplicationFactory<Program>>` pattern már implementálva EHS-ben
- Pattern propagálása más modulokhoz (Kernel, Joinery, Identity)

✅ **Capture Logs on Failure** — Debug segítség
- CI-ben failed test debug-olás jelenleg nehéz → container logs mentése

**SpaceOS kontextus:**

- **EHS Module:** Már implementálva 35 tests (25 unit + 10 integration) Testcontainers-szel
- **Kernel, Joinery, Identity:** Integration tests TODO → Testcontainers pattern alkalmazása
- **Local dev:** Container reuse jelentős productivity gain (Backend terminál)

**Ajánlás:**

✅ **JAVASOLT** — EHS Module Testcontainers setup már példa a többi modulnak. Container reuse local dev-en kritikus productivity boost. Minden új modul kövesse ezt a pattern-t.

---

### 4. Clean Architecture + DDD Best Practices (2026)

**Források:**
- [How To Use Domain-Driven Design In Clean Architecture](https://medium.com/startup-insider-edge/how-to-use-domain-driven-design-in-clean-architecture-6b21de280cd8)
- [How to build a resilient system with Clean Architecture and Domain Driven Design](https://medium.com/@lftechnology/how-to-build-a-resilient-system-with-clean-architecture-and-domain-driven-design-baf2c5335dd9)
- [The best way of implementing Domain-driven design, Clean Architecture, and CQRS](https://dev.to/turalsuleymani/the-best-way-of-implementing-domain-driven-design-clean-architecture-and-cqrs-124p)
- [Clean Domain-Driven Design - Combine Clean Architecture and DDD](https://medium.com/unil-ci-software-engineering/clean-domain-driven-design-2236f5430a05)

**Összefoglaló:**

Clean Architecture + DDD 2026: Domain layer rich business logic (nem anemic POCO-k), Event Storming workflow design-hoz, CQRS opcionális komplexitáshoz, aggregates ne túl nagyok legyenek, DDD ne csak folder structure legyen hanem business modellezés.

**Miért hasznos a SpaceOS-nek:**

✅ **Rich Domain Models** — SpaceOS már követi (Joinery Batch FSM, EHS Event sourcing)
- Domain entities behavior-rel: `Batch.Assign()`, `EhsEvent.ReportIncident()`
- Nem csak getter/setter POCO-k

✅ **Event Storming** — Új modulok tervezéséhez (pl. Cutting phase 2, Procurement)
- Architect terminál használhatja domain design során
- Business events → Aggregates → Commands

✅ **Avoid Anemic Domain Models** — Common anti-pattern
- Jó emlékeztető: Service classes ne tartalmazzanak business logic-ot amit Domain-nek kellene

✅ **Strategic Patterns** — Bounded Contexts
- SpaceOS modulok: Kernel (auth, escrow) | Joinery (order, batch) | Cutting (nesting, plan) | Identity (user, tenant)
- Mindegyik bounded context, tiszta határokkal

**SpaceOS kontextus:**

- **5 Golden Rule #2:** "Modular Monolith" — DDD bounded contexts implementációja
- **ADR Catalogue:** Már tartalmaz DDD-alapú döntéseket (aggregate root, FSM, event sourcing)
- **Architect terminál:** DDD patterns referencia új domain design-hoz

**Megfontolások:**

⚠️ **Over-engineering CRUD Systems** — Ne használj DDD minden CRUD table-re
- Catalog MVP (localStorage KPI) nem igényel DDD aggregate-ket
- DDD csak komplex business logic-hoz (Joinery, Cutting, EHS)

**Ajánlás:**

✅ **JAVASOLT** — SpaceOS már követi DDD best practices-t. Jó referencia új modulokhoz, de ne over-engineer simple CRUD-ot. Event Storming workflow hasznos Architect terminálnak.

---

## 🎨 Frontend Terminálnak

### 5. React 19 New Hooks and Best Practices (2026)

**Források:**
- [React 19 Hooks: What's New and How to Use Them Effectively](https://reactuse.com/blog/react-19-hooks-guide/)
- [React 19 New Hooks — Complete Tutorial (2026 Guide)](https://dev.to/princeofv/react-19-new-hooks-complete-tutorial-2026-guide-119p)
- [React 19 Best Practices: Write Clean, Modern, and Efficient React Code](https://dev.to/jay_sarvaiya_reactjs/react-19-best-practices-write-clean-modern-and-efficient-react-code-1beb)
- [React v19 Official Release](https://react.dev/blog/2024/12/05/react-19)

**Összefoglaló:**

React 19 új hooks 2026: `use()` (conditionally callable, breaks rules of hooks), `useActionState` (form state + pending, server actions), `useOptimistic` (optimistic UI auto-revert), `useFormStatus` (form submit state), automatic memoization, async-safe context.

**Miért hasznos a SpaceOS-nek:**

✅ **useOptimistic Hook** — SpaceOS EHS Module offline-first wizard pattern-hez
- Incident report submission: optimistic UI (instant feedback) + auto-revert on failure
- Joinery Order Completion Flow: optimistic status update

✅ **useActionState Hook** — Form state management egyszerűsítés
- SpaceOS jelenleg Zustand + manual pending state (EHS Incident Wizard)
- React 19: `useActionState` built-in pending + error handling

✅ **use() Hook** — Conditional data fetching
- Breaks "rules of hooks" — conditional, loop-ben hívható
- API call csak ha user authenticated

**SpaceOS kontextus:**

- **Current React version:** React 18 (portal)
- **Migration path:** React 18 → React 19 upgrade Q3 2026 után
- **Offline-first pattern:** useOptimistic hook tökéletes offline sync-hez

**Megfontolások:**

⚠️ **Migration Required** — React 18 → 19 upgrade nem breaking change, de testing szükséges
- useFormState (React 18 canary) → useActionState (React 19 stable)
- Server Components (Next.jsonly) nem releváns SpaceOS-nek (Vite + SPA)

**Ajánlás:**

ℹ️ **JÖVŐBELI REFERENCIA** — SpaceOS még React 18-on van. React 19 migration Q3 2026 után. useOptimistic hook releváns offline-first wizard pattern-hez, useActionState egyszerűsíti form state management-et.

---

### 6. React State Management 2026: Zustand vs Redux vs Jotai

**Források:**
- [Top 5 React State Management Tools Developers Actually Use in 2026](https://www.syncfusion.com/blogs/post/react-state-management-libraries)
- [Zustand TypeScript Guide: Type-Safe State 2026](https://sanjewa.com/blogs/zustand-typescript-type-safe-state-management/)
- [React State Management 2026: Zustand vs Redux vs Jotai](https://nextfuture.io.vn/blog/ultimate-guide-react-state-management-2026)
- [State Management in 2026: Zustand vs Jotai vs Redux Toolkit vs Signals](https://dev.to/jsgurujobs/state-management-in-2026-zustand-vs-jotai-vs-redux-toolkit-vs-signals-2gge)
- [Zustand Official Docs](https://zustand.docs.pmnd.rs/)

**Összefoglaló:**

Zustand 2026-ban "modern default" state manager: minimalist API, tiny bundle, no boilerplate, TypeScript first-class support, selective subscription (performance), middleware (persist, devtools, immer). Architektúra: TanStack Query (server state) + Zustand (client state) + URL state.

**Miért hasznos a SpaceOS-nek:**

✅ **SpaceOS már használja Zustand-ot** — Validáció hogy jó döntés
- EHS Incident Wizard: `useIncidentDraftStore` (Zustand + persist + localForage)
- Cutting Module: Batch assignment state management

✅ **TypeScript Type-Safe Patterns** — Curried form: `create<State>()((set) => ...)`
- SpaceOS már követi ezt a pattern-t
- Discriminated unions: `{status: 'loading'} | {status: 'success', data: T}`

✅ **Selective Subscription** — Performance optimization
- `useShallow` hook re-render megelőzéshez
- SpaceOS Kanban board: csak dragged item re-render, nem teljes board

✅ **Persist Middleware** — Offline-first
- SpaceOS EHS wizard már használja: Zustand + localForage
- Catalog KPI dashboard: localStorage persistence

**SpaceOS kontextus:**

- **Cutting Module:** Batch Assignment Kanban (Zustand state)
- **EHS Module:** Incident Report Wizard (Zustand + persist + localForage)
- **Catalog MVP:** KPI Dashboard (localStorage, nem Zustand — migration path)

**Ajánlás:**

✅ **JAVASOLT** — SpaceOS már Zustand-ot használ, 2026 sources megerősítik hogy jó választás. `useShallow` hook performance optimization érdemes lehet Kanban board-ra. Catalog MVP localStorage → Zustand migration path.

---

## 🏗️ Architect Terminálnak

### 7. Event Storming and Strategic DDD Patterns

**Forrás:** [How to build a resilient system with Clean Architecture and Domain Driven Design](https://medium.com/@lftechnology/how-to-build-a-resilient-system-with-clean-architecture-and-domain-driven-design-baf2c5335dd9)

**Összefoglaló:**

Event Storming workflow: business events → storytelling → bounded contexts → aggregates. Development process Event Storming board-ból indul, nem source code-ból. Strategic patterns: systems splitting business sub-domains-re, clear ownership boundaries, reduced change amplification.

**Miért hasznos a SpaceOS-nek:**

✅ **Új modulok tervezése** — Procurement, Quality (QA), Supplier Portal
- Event Storming session Architect + Root + Conductor
- Business events → domain design

✅ **Bounded Context Mapping** — SpaceOS modulok közötti integráció
- Kernel ↔ Joinery ↔ Cutting ↔ Identity
- Provider interfészek (contracts NuGet package)

✅ **Clear Ownership Boundaries** — Terminálok közötti felelősség
- Backend terminál: Kernel + modules
- Frontend terminál: Portal
- Architect terminál: cross-module interfaces

**SpaceOS kontextus:**

- **ADR-046:** Consensus Architecture (EHS, Assembly Variance, Catalog Diff) — Event Storming eredménye
- **Slice 2 planning:** Új modulok (Procurement, Catalog, Assembly) Event Storming workflow-val

**Ajánlás:**

✅ **JAVASOLT** — Event Storming hasznos Architect terminálnak új domain design-hoz. SpaceOS már követi bounded context pattern-t (modules), de Event Storming formalizálhatja a folyamatot.

---

## 🔐 Minden Terminálnak

### 8. OWASP Top 10 Security Best Practices 2026

**Megjegyzés:** WebSearch nem adott releváns OWASP 2026 eredményt, de `docs/knowledge/security/SECURITY_PATTERNS.md` már tartalmazza:
- JWT RS256 authentication
- RBAC (Role-Based Access Control)
- RLS (Row Level Security) tenant isolation
- SSRF prevention (Orchestrator LLM Tool Calling)

**Ajánlás:**

✅ **BELSŐ TUDÁS ELEGENDŐ** — SpaceOS security patterns már dokumentálva van. Nincs szükség külső forrásra, a meglévő `SECURITY_PATTERNS.md` referencia minden terminálnak.

---

## 📊 Összefoglaló Statisztikák

| Kategória | Cikkek | Relevancia | Ajánlás |
|-----------|--------|------------|---------|
| .NET Backend | 3 | Magas | ✅ JAVASOLT (Route Groups, RLS, Testcontainers) |
| React Frontend | 2 | Közepes | ⚠️ ADAPTÁCIÓ (React 19 migration Q3 2026) |
| Architecture | 2 | Magas | ✅ JAVASOLT (DDD, Event Storming) |
| Security | 0 | - | ℹ️ BELSŐ TUDÁS ELEGENDŐ |

**Total források:** 6 különböző téma, 25+ külső cikk áttekintve

---

## 🎯 Következő Lépések Termináloknak

### Backend Terminál
1. ✅ Audit RLS policies (SECURITY DEFINER functions, indexek)
2. ✅ Enable container reuse local dev Testcontainers-ben
3. ✅ Route groups használata új modulokban

### Frontend Terminál
1. ℹ️ React 19 migration planning (Q3 2026 után)
2. ✅ `useShallow` hook performance optimization (Kanban board)
3. ✅ Catalog MVP localStorage → Zustand migration path

### Architect Terminál
1. ✅ Event Storming workshop új modulokhoz (Procurement, Quality)
2. ✅ DDD patterns referencia új domain design-hoz

---

**Készítette:** Librarian terminál
**Dátum:** 2026-06-22
**Feldolgozási idő:** ~20 perc
**WebSearch queries:** 6

🚀 **Olvasólista kész!** Konstruktív, építő jellegű ajánlások minden terminálnak, külső források validációjával és SpaceOS kontextusba helyezéssel.
