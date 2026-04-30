---
id: MSG-ARCH-019-RESPONSE
from: architect
to: root
type: response
priority: high
status: READ
ref: MSG-ARCH-016
created: 2026-04-29
---

# Portal World Architecture v4-final — Review eredmény

> Tervdok: `SpaceOS_Portal_World_Architecture_v4_final.md` (v4 FINAL, 153KB)
> README: `SpaceOS_Portal_World_Architecture_v4_README.md`
> 58 finding (4🔴/10🟠/34🟡/10🟢), 0 nyitott CRITICAL

---

## 1. Scope — mi Portal World v1, mi v2?

**A határ rögzített (D-01..D-09 LOCKED).**

| Feature | Státusz |
|---------|---------|
| 5 world: Home, Sales, Production, Shop Floor, Settings | ✅ v1 |
| `WorldShell` layout route (`chrome: standard/minimal/none`) | ✅ v1 |
| `worldCatalog.ts` + `tenantStore` + `LazyWorldRoute` HOC | ✅ v1 |
| Shop Floor: `/w/shopfloor` belül, kiosk-mode router szétválasztás (interim) | ✅ v1 |
| Brand skin aktiválás (Doorstar skin passzív, de kiváltható v2) | ❌ v2 |
| Shop Floor: külön Vite app entry-point (`apps/shopfloor`) | ❌ v2 (D-07 roadmap) |
| Refresh token httpOnly cookie (BFF-managed session) | ❌ v2 |
| SSE/WebSocket upgrade (polling → real-time) | ❌ v2 |
| PartnerTier `/bff/v1/partner/*` verziózott API | ❌ v2 |

**Doorstar Soft Launch szempontjából:** Sales + Production világ elegendő (cutting-plan + manufacturing FSM board). Shop Floor + Settings opcionális Soft Launch utánra.

---

## 2. Prereq-ok státusza

### Kernel EnabledModules (Migration 0025)

**✅ DEPLOYED** — `20260408100000_Migration_0025_TenantEnabledModules.cs` létezik a Kernel migrations-ban. A `TenantEnabledModules` aggregált state elérhető.

### Orchestrator `/bff/api/*` + `/bff/cutting/*`

**✅ DEPLOYED** — a Codebase_Status.md megerősíti, az Orchestrator routes-ban megtalálható `cutting.route.ts`, `doorOrder.route.ts`, `handshakes.route.ts`, `auth.route.ts`, `health.route.ts`.

### Manufacturing service (port 5007)

**⚠️ DEV COMPLETE, NEM DEPLOYED** — a `Codebase_Status.md` szerint státusz: "DEV COMPLETE, 250 tests". Nem production-candidate, nincs staging deploy.

**Hatás a Track F-re (R-17 kockázat):** a spec explicit kezeli ezt — `bff-f1-contract-freeze` session Day 1 checkpoint. Track F `/bff/manufacturing/*` proxy route-ok mock-first módban indulnak, valódi OpenAPI snapshot commit csak staging deploy után. **Ez nem blokkolja a Track A és Track B indítását.**

### ModuleSubscriptions — Manufacturing prereq ✅ MOST MÁR MEGVAN

⚠️ **Fontos pozitív finding:** az ARCH-017 Manufacturing Phase 1 review-ban blokkert azonosítottam (`module_subscriptions` tábla hiányzik a Kernel-ből). **2026-04-28-án elkészült:** `20260428152343_ModuleSubscriptions.cs` migration deployolva. A Manufacturing cross-module subscribe prereq most teljesítve.

---

## 3. Frontend stack — React 18/19? Turborepo? Standalone?

**Implementációs célpont: `spaceos-doorstar-portal/` (standalone Vite app)**

| Szempont | Tényleges állapot | Spec feltevés | Delta |
|----------|-------------------|---------------|-------|
| React | 18.3.1 ✅ | React 18 | Egyezik |
| Vite | 8.0.4 ✅ | Vite | Egyezik |
| TypeScript | 6.0.2 (strict) ✅ | strict | Egyezik |
| TanStack Query | v5 ✅ | v5 | Egyezik |
| **React Router DOM** | **v7.14.1** ⚠️ | v6 | **Spec v6-ot ír, repo v7!** |
| **Zustand** | **NINCS** ❌ | Kötelező | **Új dep** |
| **react-hook-form** | **NINCS** ❌ | Kötelező (FE-07) | **Új dep** |
| **size-limit** | **NINCS** ❌ | CI gate (FE-15) | **Új dep (A.5)** |
| oidc-client-ts | v3.5.0 ✅ | Kötelező | Egyezik |
| **react-oidc-context** | **v3.3.1** ⚠️ | Nincs a specben | **Meglévő wrapper** |
| MSW | v2.13.4 ✅ | Kötelező | Egyezik |
| Tailwind CSS | v3 ✅ | Kötelező | Egyezik |
| Vitest | v4.1.4 ✅ | Kötelező | Egyezik |
| **@spaceos/ui, @spaceos/domain** | **NINCS referencia a package.json-ban** ⚠️ | Kötelező | **Workspace link szükséges** |

### ⚠️ React Router v7 vs. spec v6

A spec `createBrowserRouter`, `useBlocker`, `<Navigate>`, `useSearchParams`, `useParams` — ezek **React Router v7-ben is elérhetők**, az API backwards-compatible. Az implementáló agent-nek ezt jelezni kell Day 1-en. A spec kódpéldáit 1:1-ben alkalmazni lehet, nincs breaking change.

### ⚠️ `react-oidc-context` meglévő wrapper vs. spec direct `userManager`

A jelenlegi `spaceos-doorstar-portal` `react-oidc-context` `<AuthProvider>`-t használ. A spec `apps/joinerytech/src/auth/userManager.ts`-t direkt `UserManager` instanceként írja (SEC-FE-01 fix). A kettő kompatibilis — `react-oidc-context` belül `oidc-client-ts` `UserManager`-t használ, az `userStore = InMemoryWebStorage()` config átadható a `react-oidc-context` `<AuthProvider authority="..." userStore=...>` propján. **Nincs breaking change, de Day 1 verify szükséges.**

### Szükséges új függőségek

**`spaceos-doorstar-portal/package.json`-ba hozzáadandó:**
```
zustand ^5                   (per-world slice-ok + globális store-ok)
react-hook-form ^7           (FE-07 form stratégia)
zod ^3                       (URL filter parse + form schema — MSW-vel már valószínűleg van)
@radix-ui/react-dialog ^1    (FE-18 Dialog focus trap)
size-limit                   (FE-15 bundle budget CI gate, Track A.5)
vite-plugin-remove-console   (SEC-FE-16 production console strip)
```

**Orchestrator-ban szükséges:**
```
opossum ^8                   (BFF-11 circuit breaker — NEM szerepel jelenleg a package.json-ban)
@types/opossum               (TypeScript types)
```

`express-rate-limit` ✅ már megvan az Orchestrator-ban (BFF-07 könnyebb).

**`react-virtuoso`:** PagedTable virtualization audit (R-09, Track A.5) előtt nem szükséges — ha `@spaceos/ui` `PagedTable` belső virtualizációja megoldott, nem kell.

---

## 4. BFF route bővítés szükséges?

**Igen — 16 új route, middleware chain refactor, 1 új Orchestrator library.**

### Aktuális Orchestrator route-ok (`src/routes/`)

Van: `cutting.route.ts`, `doorOrder.route.ts`, `handshakes.route.ts`, `auth.route.ts`, `health.route.ts`, `joinery.route.ts`, `inventory.route.ts`, `procurement.route.ts`, `spatial.route.ts`, `proof.route.ts`, `snapshot.route.ts`, `stageDispatch.route.ts`.

**HIÁNYZIK** (mind új, Section 5.2):
- `/bff/api/me/session` + `/bff/api/me/home-state` — aggregátor
- `/bff/api/tenant`, `/bff/api/audit`, `/bff/api/users`
- `/bff/api/configurator/:productTemplateId/configure` (BFF-01 fix)
- `/bff/api/orders/:id/full` (BFF-03 aggregátor)
- `/bff/manufacturing/*` proxy (5007) + `/bff/manufacturing/tasks/:id/full`
- `/bff/cutting/plans/:id/full` (BFF-03 aggregátor)
- `/bff/shopfloor/pin/login|logout`, `/bff/shopfloor/tasks`, `/bff/shopfloor/task/:id/status`
- `/bff/health` bővítés Manufacturing ping-gel (BFF-05, health.route.ts bővítése)

### ⚠️ KRITIKUS FINDING: `/bff/abstractions/*` LÉTEZIK a codebase-ben

**A spec BFF-01 fix azt mondja: töröljük ezt a route-ot** (Abstractions NuGet-only, nincs 5003 HTTP service → BFF proxy sehova nem mutat).

**DE:** a tényleges codebase-ben `src/routes/abstractions.route.ts` EXISTS, és **`127.0.0.1:5003`-ra proxy-zik** — és az Abstractions service DEPLOYED állapotban van 5003-on (81 teszttel, Codebase_Status szerint).

Ez azt jelenti:
- Az Abstractions service NEM csak NuGet — van futó HTTP service is (5003)
- A `/bff/abstractions/*` route jelenleg élő backend felé mutat
- **BFF-01 fix alkalmazása előtt** a Track F F.1 contract-freeze checkpoint-ban: **audit szükséges, melyik frontend kód hív `/bff/abstractions/*`**
- Ha jelenleg a `ProductConfiguratorScreen` vagy más screen ezen a route-on keresztül hívja a Graph engine-t, a spec-beli Kernel rerouting (`/bff/api/configurator/:id/configure`) NEM alkalmazható anélkül, hogy a Kernel oldalon az endpoint meglegyen

**Az implementáló agentnek ezt Day 1 Contract Review-ba (F.1) be kell vennie és CONTRACT_ISSUES.md-be írni.**

---

## 5. 4 CRITICAL finding — mi és hogyan?

| ID | Súly | Probléma | v4 fix | Állapot |
|----|------|----------|--------|---------|
| **BFF-01** | 🔴 | `/bff/abstractions/*` antipattern — nem NuGet-only service BFF route-ja | Configurator → `/bff/api/configurator/:id/configure` (Kernel) | ⚠️ LÁSD FENT: route létezik és élő service-re mutat → Day 1 audit |
| **SEC-FE-01** | 🔴 | KC SDK default localStorage-be ír (token leak) | `userManager.userStore = InMemoryWebStorage()` + CI grep gate | ✅ Absorbed — `oidc-client-ts` v3 támogatja |
| **SEC-FE-02** | 🔴 | `jwtDecode()` business döntéshez (trust boundary) | ESLint `no-restricted-imports: jwt-decode, jose`; minden auth state `/me/session`-ből | ✅ Absorbed |
| **SEC-FE-03** | 🔴 | `enabledModules` URL bypass (FE guard ≠ BFF enforce) | Single source: Orchestrator `enabledModulesService.getForTenant()` — azonos hívás mind `/me/session` mind middleware-nek | ✅ Absorbed — Orchestrator már kapcsolódik Kernel-hez |

**Megjegyzés SEC-FE-11:** a finding tablában 🔴-ként jelölt (`dangerouslySetInnerHTML` XSS), de a spec header 4 CRITICAL-t mond (3 SEC + 1 BFF). A spec számolásban nem szerepel az összesítőben 🔴-ként, DE az ESLint gate ugyanolyan szigorúan alkalmazandó.

**Track F F.2-ben kötelező critical gate verify:** `pnpm lint` + `pnpm typecheck` + CI grep-ek (token, eval, dangerouslySetInnerHTML) — Section 10.6 alapján.

---

## 6. Implementációs sorrend — track-ok

**7 track, 2 parallel kezdés, critical path ~14-16 nap.**

```
Track A — Foundation (3 nap)   ─────┐
Track F — BFF routes (4 nap)   ─────┼──► Track B — Home+Settings (2 nap) ─┐
  (parallel Day 1-4)                │   Track C — Sales (5 nap)           ├──► Track G — Integration (2 nap)
                                    └──► Track D — Production (5 nap)     │
                                         Track E — Shop Floor (4 nap)     ┘
                                         (B/C/D/E parallel egymással)
```

| Track | Tartalom | Nap | Függőség |
|-------|----------|-----|----------|
| **A Foundation** | worldCatalog.ts, WorldShell, WorldGuard, LazyWorldRoute HOC, tenantStore, auth hardening (SEC-FE-01..06), size-limit CI, Zustand install | ~3 | — |
| **F BFF routes** | F.1 contract-freeze (Day 1!), middleware lánc, error normalizer, circuit breaker (opossum), rate limit, 16 új route, openAPI snapshot | ~4 | Backend services DEPLOYED (Manufacturing: mock-first) |
| **B Home+Settings** | WorldHome + auto-redirect, WorldCard, TenantInfo, AuditLog, UserList | ~2 | A, F |
| **C Sales** | OrdersList, OrderDetail, ProductConfigurator, B2bHandshakes, draft store | ~5 | A, F |
| **D Production** | CuttingPlanList/Detail, NestingVisualizer (heavy), ManufacturingFsmBoard (heavy), TaskDetail | ~5 | A, F |
| **E Shop Floor** | ShopFloorPinGate, PinEntry, TaskList, TaskDetail, kiosk router | ~4 | A, F |
| **G Integration** | E2E full flow, Lighthouse ≥90, bundle budget audit, CONTRACT_ISSUES drain | ~2 | B, C, D, E |

### Track A kritikus belépési feltételek

1. **Új dep-ek installálása Day 1 előtt:** `zustand`, `react-hook-form`, `zod`, `@radix-ui/react-dialog`, `size-limit`, `vite-plugin-remove-console`
2. **`@spaceos/ui` + `@spaceos/domain` workspace link** — ellenőrizni package.json-ban; ha nincs, hozzáadni
3. **React Router v7 compat verify** — `useBlocker` API test (spec v6 kódpéldák direktben alkalmazhatók)

### Track F kritikus belépési feltételek

1. **F.1 CONTRACT_ISSUES audit Day 1:** `/bff/abstractions/*` frontend callers lista → döntés a Kernel route migration sebességéről
2. **opossum install** az Orchestrator-ban
3. **Manufacturing staging deploy ETA** bekérése a backend csapattól

---

## 7. Effort validálás

**~24 nap szoros, de reális. ~14-16 nap critical path párhuzamosítással.**

| Komponens | Spec becslés | Validálás |
|-----------|-------------|-----------|
| Track A Foundation | ~3 nap | Indokolt: 5 store, 10+ komponens, auth hardening, ESLint config, CI setup |
| Track F BFF | ~4 nap | Indokolt: 16 új route + middleware + circuit breaker + openAPI |
| B+C+D+E parallel | ~13 nap total | Indokolt: 30+ screen, heavy screen-ek, tesztek |
| Track G | ~2 nap | Tight: Lighthouse + E2E + bundle audit egyszerre |

**Doorstar Soft Launch szempontjából:** A+F+B+C+D = ~19 nap sequential; párhuzamosítva ~11-12 nap. Shop Floor (E) post-Soft Launch opcionális. Ez illeszkedik a 2026 Q2 Soft Launch gate-hez.

**Teszt cél:** 99 (jelenlegi) + 115 (Vitest) = ~214; E2E: 277 + 39 = ~316.

---

## 8. Cross-module dependency

| Dependency | Állapot | Track hatás |
|-----------|---------|-------------|
| Kernel `EnabledModules` (0025) | ✅ DEPLOYED | A, F prereq teljesítve |
| Kernel `ModuleSubscriptions` migration | ✅ DEPLOYED (2026-04-28) | Manufacturing prereq teljesítve |
| Orchestrator `/bff/api/*`, `/bff/cutting/*` | ✅ DEPLOYED | Track F bővítési alap megvan |
| Manufacturing service (5007) | ⚠️ DEV COMPLETE, nem deployed | Track F mock-first; F.4 snapshot commit késik |
| `express-rate-limit` (Orchestrator) | ✅ MÁR MEGVAN | BFF-07 könnyebb |
| `opossum` (Orchestrator) | ❌ NINCS | Installálni kell Track F előtt |
| `/bff/abstractions/*` route | ⚠️ LÉTEZIK, 5003 proxyzik | F.1 audit kötelező |

---

## Definition of Done — ellenőrzőlista

- [x] Tervdok elolvasva és validálva (58 finding, 0 nyitott CRITICAL a specben)
- [x] Scope határ: 5 world v1, brand skin/Shop Floor separate app/httpOnly cookie v2
- [x] Prereqek: Kernel 0025 ✅, BFF deployed ✅, Manufacturing DEV COMPLETE (nem deployed → mock-first)
- [x] Frontend stack: React 18 + Vite + RR v7 (compat v6), Zustand/react-hook-form/radix-ui új dep-ek kellenek
- [x] BFF bővítés: 16 új route, middleware chain, opossum new dep
- [x] 4 CRITICAL finding: 3 absorbed (SEC-FE-01..03), BFF-01 → `/bff/abstractions/*` frontend audit Day 1
- [x] Implementációs sorrend: 7 track, A+F parallel, B/C/D/E parallel
- [x] Effort: ~24 nap sequential, ~14-16 nap critical path
- [x] **DEPENDENCY GAP:** Zustand, react-hook-form, zod, @radix-ui/react-dialog, size-limit, vite-plugin-remove-console — mind installálni kell Day 1
- [x] **ROUTER VERSION MISMATCH:** react-router-dom v7 (spec v6 kódpéldák) — backwards-compatible, Day 1 verify
- [x] **ABSTRACTIONS ROUTE EXISTS:** `/bff/abstractions/*` → élő 5003 service proxy — F.1 CONTRACT_ISSUES audit kötelező BFF-01 alkalmazása előtt
