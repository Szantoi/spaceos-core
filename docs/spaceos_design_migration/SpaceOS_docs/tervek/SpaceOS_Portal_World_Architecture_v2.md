# SpaceOS — Portal World Architecture
## Frontend Architecture v2 (Frontend Review Pass)

> **Verzió:** v2 — 2026-04-29
> **Státusz:** REVIEW — security review (v3) → BFF review (v4) hátravan
> **Prereq:** Kernel `EnabledModules` (Migration 0025) DEPLOYED · Orchestrator `/bff/api/*` + `/bff/cutting/*` DEPLOYED · Manufacturing service DEV COMPLETE (port 5007)
> **Design forrás:** `https://api.anthropic.com/v1/design/h/Gf2t9hUM4KFNw5f9Kr8igw` (404 — design-agnostic, mapping → v3+)
> **Kumulált review:** v1 → **v2 (frontend, sub-senior-frontend.md)**
> **Nyitott döntések:** D-01..D-09 LOCKED (lásd Section 0)
> **Δ v1 → v2:** 25 finding (2🟠 + 16🟡 + 7🟢, 0🔴) — lásd Section 1

---

## 0. Locked decisions (D-01..D-09)

| ID | Döntés | Lock |
|---|---|---|
| D-01 | Scope — 5 world v1-ben | Home, Sales, Production, Shop Floor, Settings |
| D-02 | Cél portál app | Egy közös moduláris `apps/joinerytech` build, **nincs aktív brand skin layer** v1-ben (default skin passzívan) |
| D-03 | Home pattern | World card grid + auto-redirect single-module/single-role esetén |
| D-04 | WorldShell layout | Egy közös `<WorldShell>`, `chrome: 'standard' \| 'minimal' \| 'none'` prop |
| D-05 | URL topology | `/w/{world}/{screen}` |
| D-06 | World visibility | Statikus `worldCatalog.ts` (FE) szűrve `enabledModules` alapján; **BFF route guard authoritative server-side** |
| D-07 | Shop Floor | v1: `/w/shopfloor` route `chrome='none'`-szal a portálon belül; v2: külön app entry-point |
| D-08 | State izoláció | Globális (auth, brand, tenant) + per-world Zustand slice; React Query keys `[worldId, ...]` namespace |
| D-09 | Code splitting | Per-world chunk + nested Suspense heavy screen-ekre |

---

## 1. Kumulált Finding Összesítő (v1 → v2)

**v2 frontend review — 25 finding** (2🟠 HIGH · 16🟡 MEDIUM · 7🟢 LOW · 0🔴 CRITICAL)

### Route & Navigation

| ID | Súly | Terület | Probléma | v2 javítás |
|---|---|---|---|---|
| **FE-01** | 🟠 HIGH | Route guard ordering | A v1 Section 2.4 példájában a `<WorldGuard>` a `<Suspense>` BELSEJÉBEN van — emiatt a nem-engedélyezett world chunk-ja letöltődik még a redirect előtt. Bandwidth-pazarlás + chunk URL-ből kikövetkeztethető a world létezése (info leak) | Wrapper sorrend: `<ErrorBoundary> > <WorldGuard> > <Suspense> > <LazyWorld>`. Ha a guard `<Navigate>`-et ad vissza, a `<Suspense>` soha nem trigger-el, a lazy import nem indul. Section 2.4 + új `<LazyWorldRoute>` HOC. |
| **FE-02** | 🟠 HIGH | Layout pattern | A v1 minden world komponens belül saját `<WorldShell>`-t renderelt → world-váltáskor (pl. `/w/sales` → `/w/production`) a sidebar+topbar **unmount + remount**, query-k újraindulnak, sidebar collapse state elvész, perceptible flicker. | React Router v6 layout route: `<WorldShell>` mint **parent route element**, `<Outlet>` a content. Csak a content-area lazy chunk cserélődik. Sidebar/topbar stabil marad. Section 2.1 + 3.1 átszerkesztve. |
| FE-03 | 🟡 MEDIUM | Deep linking | Filter/sort state (orders list, plans list, audit log) URL-ben NINCS reflektálva — bookmark, share, browser back nem hozza vissza a szűrt nézetet | `useSearchParams` minden filter/sort komponenshez; URL → store init at mount; standardizált query string konvenció (`?status=...&from=...&to=...&page=...`) |
| FE-04 | 🟡 MEDIUM | Browser history policy | World switch / modal open / guard redirect — push vagy replace? Nem specifikálva → Back gomb viselkedés inkonzisztens | Policy: world-switch = push; guard-redirect = replace; modal-open = NO history change (state in sub-route opcionális v3-ra); auth-redirect = replace |
| FE-05 | 🟡 MEDIUM | 404 strategy granularity | A v1 csak top-level `*` route 404-et kezel. "World létezik, de erőforrás nem" (pl. `/w/sales/orders/9999`) eset nem specifikus → globális 404 oldalra dob, kontextus-vesztés | World-level `<EntityNotFound>` komponens; query 404 → `<EntityNotFound>` a world content-areán belül (sidebar+topbar megmarad); csak ismeretlen world-id → globális 404 |
| FE-06 | 🟡 MEDIUM | Lazy wrapper enforcement | A v1 csak példában mutatja a wrapper-szettet — nincs reusable HOC, ami minden lazy world-re kötelezi a sorrendet | Bevezetni `<LazyWorldRoute worldId chunkLoader>` HOC-ot; `worldRoutes.tsx`-ben central registry; minden új world ezzel kerül be |

### Component Architecture

| ID | Súly | Terület | Probléma | v2 javítás |
|---|---|---|---|---|
| FE-07 | 🟡 MEDIUM | Form stratégia | Configurator, PIN entry, settings forms — controlled vs uncontrolled, validáció lib nem specifikálva | Standard: react-hook-form + Zod schema; controlled forms; minden form ugyanazt a pattern-t használja; common error rendering Section 3.5-ben |
| FE-08 | 🟡 MEDIUM | Heavy screen ErrorBoundary | NestingVisualizer/FsmBoard összeomlás → world-szintű ErrorBoundary kapja el → user az egész world-ből kiesik | Heavy screen saját ErrorBoundary + saját Suspense pár (nested). World-szintű boundary csak a fő world-shell hibát kapja |
| FE-13 | 🟡 MEDIUM | Memoization stratégia | Heavy lista (PagedTable rows N>100), board cells (FsmBoard) memoization nincs specifikálva | Konvenció: `<PagedTable>` row child komponens `React.memo`-val; FsmBoard cell komponens `React.memo` + stabil callback (`useCallback`); selectors Zustand-on shallow compare |
| FE-18 | 🟡 MEDIUM | Modal focus trap | Dialog/modal focus trap a v1-ben nem specifikus → kbd nav kiszökhet | `<Dialog>` standard komponens (radix-ui adoption javasolt) focus trap + Esc + ARIA dialog role + initial focus + return focus on close |
| FE-19 | 🟢 LOW | Component placement rationale | App-local vs `@spaceos/ui` indoklás csak implicit | 1 mondatos rationale új komponensenként (Section 3.2 oszlop) |
| FE-20 | 🟢 LOW | List key konvenció | Lista key stratégia nem dokumentált | Konvenció: entity ID használata, NEVER index; ha nincs ID (in-memory draft items), `crypto.randomUUID()` mount-kor |

### State Management

| ID | Súly | Terület | Probléma | v2 javítás |
|---|---|---|---|---|
| FE-09 | 🟡 MEDIUM | Live data refresh | Shop Floor task feed `staleTime: 5s` van, de `refetchInterval` nincs → nem auto-update, csak manuális/route-revisit refetch | Explicit `refetchInterval: 10_000` Shop Floor task list + `refetchIntervalInBackground: false` (battery). v3 backlog: SSE/WS upgrade |
| FE-10 | 🟡 MEDIUM | Mutation feedback policy | Optimistic update vs invalidation policy nem specifikálva mutáció-típusonként | Policy táblázat (Section 4.5): status update (Shop Floor) = optimistic + rollback; order save = invalidation; configurator = optimistic + rollback; admin user op = invalidation |
| FE-11 | 🟡 MEDIUM | Toast / notification | Mutáció eredmény visszacsatolás nincs rendszerezve | `<Toaster>` komponens (`@spaceos/ui` candidate); `useToast` hook; ARIA live region; success/error/warning/info variants; auto-dismiss kivéve kritikus error |
| FE-12 | 🟡 MEDIUM | Loading state taxonomy | Csak chunk-load loading specifikálva; query loading + mutation pending nem rendszerezve | Taxonomy (Section 4.6): world-load = full skeleton screen; query-load = component-level skeleton/spinner; mutation-pending = button disabled + inline spinner |
| FE-16 | 🟡 MEDIUM | Configurator draft persistence | `useSalesOrderDraftStore` world-unmount → reset; user accidentally navigál → unsaved changes elveszik | `useBlocker` (React Router v6) confirm dialog ha draft dirty; `beforeunload` browser-szinten; opcionális auto-save sessionStorage-be (NEM localStorage — security) |
| FE-17 | 🟡 MEDIUM | Auth refresh edge case | KC silent renew failure mid-world → mit lát a user? Nincs spec → blank screen / loop kockázat | `onSilentRenewError` handler: graceful redirect `/login?returnTo=<currentPath>`; post-login restore `returnTo` path; toast "Munkamenet lejárt, jelentkezz be újra" |

### Performance

| ID | Súly | Terület | Probléma | v2 javítás |
|---|---|---|---|---|
| FE-14 | 🟡 MEDIUM | Virtualization | PagedTable virtualizál-e? Audit log + Orders list 1000+ row potenciálisan; v1-ben nem specifikus | DoD: PagedTable internal virtualization audit a Track A elején; ha nem virtualizál + N>200 → `react-virtuoso` integráció Audit Log + Orders list-re (ezekből a többi `PagedTable` használat <100 row jellemzően) |
| FE-15 | 🟡 MEDIUM | Bundle budget granularity | Production world "override OK if nested heavy" homályos | Granuláris budget: world-shell-content < 50KB gzip; minden heavy screen chunk < 100KB gzip; CI-kapu PR-szinten (`size-limit` lib); regression alert |

### Accessibility & Responsive

| ID | Súly | Terület | Probléma | v2 javítás |
|---|---|---|---|---|
| FE-21 | 🟢 LOW | Asset optimization | SVG inline vs sprite, font preconnect/swap nem specifikálva | SVG icons: `lucide-react` (tree-shakeable); brand SVG inline; raszter képek `loading="lazy"`; `font-display: swap`; preconnect Google Fonts/CDN; subset Inter/Manrope |
| FE-22 | 🟢 LOW | Semantic landmarks + ARIA | `<nav>`, `<main>`, `<header>`, `<aside>` használat + icon-only aria-label nem explicit | WorldShell: `<header>` topbar, `<nav aria-label="Sidebar">` sidebar, `<main>` content; minden world `<h1>`; minden icon-only `aria-label` (sidebar collapse, user menu); icon next-to-text `aria-hidden="true"` |
| FE-23 | 🟢 LOW | Empty state taxonomy | Mikor `<EmptyState>` (zero result, error, forbidden) — nem specifikus | 3 variant az `<EmptyState>`-ben: `zero` (info icon + "Nincs még…" + opcionális CTA); `error` (warning icon + retry button); `forbidden` (lock icon + "Hozzáférés megtagadva" + admin contact CTA) |
| FE-24 | 🟢 LOW | Locale hot-swap | Mid-session locale switch nem specifikus (page reload?) | `i18nStore.setLocale()` hot-swap (no page reload); React-friendly bundle split namespace per locale; preferre user-saved locale `/me/session`-ből, fallback hostname-based |
| FE-25 | 🟢 LOW | i18n date/number format | Lokalizáció csak fordítási kulcs szinten — szám/dátum formázás nem specifikus | `Intl.DateTimeFormat` + `Intl.NumberFormat` minden render-en; `useLocaleFormat` hook (`@spaceos/i18n` candidate) |

### Cumulative — érvényben marad v1-ből

A v1 11 finding-je nem volt — first review pass.

---

## 2. Route architektúra

### 2.1 Route map (React Router v6 layout routes)

**Δ FE-02:** A `<WorldShell>` mostantól **layout route element** — egyszer mountolódik, world-váltáskor csak a content cserélődik. Sidebar/topbar stabil, query refetch nincs (a tenant/session query-k cache-elve maradnak).

```
/                                        → ProtectedRoute → /w/home (auth check)
/login                                   → KeycloakLoginRedirect
/callback                                → KeycloakCallback
/silent-callback                         → KeycloakSilentRenew
/logout                                  → KeycloakLogout

# ─── Layout route: <WorldShell> renderelődik egyszer, <Outlet> a content ───

/w                                       → <WorldShell chrome="standard">
   ''                                      → redirect /w/home
   home                                    → <WorldHome /> (card grid OR auto-redirect)
   sales/*                                 → <LazyWorldRoute worldId="sales" /> [content-only chunk]
     /                                       → SalesIndex (default: orders)
     orders                                  → OrdersListScreen
     orders/:orderId                         → OrderDetailScreen
     orders/:orderId/items/:lineId/configure → ProductConfiguratorScreen [nested Suspense + EB]
     handshakes                              → B2bHandshakesScreen
   production/*                            → <LazyWorldRoute worldId="production" /> [content-only chunk]
     /                                       → ProductionIndex (default: cutting-plan)
     cutting-plan                            → CuttingPlanListScreen
     cutting-plan/:planId                    → CuttingPlanDetailScreen
     cutting-plan/:planId/nesting            → NestingVisualizerScreen [nested Suspense + EB]
     manufacturing                           → ManufacturingFsmBoardScreen [nested Suspense + EB]
     manufacturing/:taskId                   → ManufacturingTaskDetailScreen
   settings/*                              → <LazyWorldRoute worldId="settings" /> [content-only chunk]
     /                                       → SettingsIndex (default: tenant)
     tenant                                  → TenantInfoScreen
     users                                   → UserListScreen [role: admin]
     audit                                   → AuditLogScreen (HashDisplay reuse)

# ─── Külön layout route: <WorldShell chrome="none"> Shop Floor-nak ───

/w/shopfloor                             → <WorldShell chrome="none"> + <ShopFloorPinGate>
   /                                       → ShopFloorIndex
   pin                                     → PinEntryScreen
   tasks                                   → ShopFloorTaskListScreen
   task/:taskId                            → ShopFloorTaskDetailScreen

# ─── Globális 404 + entity-szintű 404 (FE-05) ───

*                                        → NotFound404 (globális — ismeretlen world-id)
                                          (entity 404 → <EntityNotFound> a world content-ben)
```

**Δ FE-01 (wrapper order):** `<LazyWorldRoute>` HOC bevezetése — biztosítja a helyes wrapper sorrendet:

```tsx
// apps/joinerytech/src/worlds/LazyWorldRoute.tsx
function LazyWorldRoute({ worldId, chunkLoader }: LazyWorldRouteProps) {
  const Lazy = useMemo(() => lazy(chunkLoader), [chunkLoader]);
  return (
    <ErrorBoundary fallback={<WorldErrorScreen worldId={worldId} />}>
      <WorldGuard worldId={worldId}>
        {/* Guard returns <Navigate> ha jogosulatlan → Suspense soha nem trigger */}
        <Suspense fallback={<WorldLoadingScreen worldId={worldId} />}>
          <Lazy />
        </Suspense>
      </WorldGuard>
    </ErrorBoundary>
  );
}
```

**Központi registry (`apps/joinerytech/src/worlds/registry.ts`):**

```ts
export const worldRoutes = [
  { worldId: 'sales',      chunkLoader: () => import('./sales/SalesWorld') },
  { worldId: 'production', chunkLoader: () => import('./production/ProductionWorld') },
  { worldId: 'settings',   chunkLoader: () => import('./settings/SettingsWorld') },
];
// shopfloor saját chrome="none" layout route-on, regiszten kívül
```

### 2.2 Code splitting stratégia (D-09 LOCKED)

**Per-world boundary:** minden `/w/{world}/*` szegmens önálló `React.lazy()` import.

**Nested Suspense — heavy screen kandidátusok:**

| Screen | Heavy lib (becsült gzip) | Boundary |
|---|---|---|
| `ProductConfiguratorScreen` | Abstractions Graph engine view (~30-50KB) | Nested Suspense `/w/sales` chunk-on belül |
| `NestingVisualizerScreen` | Custom canvas renderer (~40KB) | Nested Suspense `/w/production` chunk-on belül |
| `ManufacturingFsmBoardScreen` | recharts (~80KB gzip, FsmBadge animations) | Nested Suspense `/w/production` chunk-on belül |
| `AuditLogScreen` | (HashDisplay már létezik, könnyű) | Nincs — fő production chunk |

**Bundle budget gate (DoD):**
- App entry < 200KB gzip
- Per-world chunk < 80KB gzip (kivéve: Production, ha `recharts` nem nested → akkor downgrade kötelező)
- v2 frontend review automatikus FE-finding ha bármelyik világ chunk > 80KB és nincs nested boundary

### 2.3 enabledModules → route visibility

**Mapping (`@spaceos/domain/worldCatalog.ts`):**

| World ID | `requiredModules` | `requiredRoles` | `chrome` | `requiresPin` |
|---|---|---|---|---|
| `home` | `[]` (mindig látható) | `[]` | `standard` | false |
| `sales` | `['door']` v1-re (vagy `['cabinet']` ha bekapcsolt) | `['sales-rep', 'admin']` | `standard` | false |
| `production` | `['cutting']` (manufacturing implicit) | `['production-lead', 'admin']` | `standard` | false |
| `shopfloor` | `['cutting']` | `['shopfloor-worker']` | `none` | true |
| `settings` | `[]` | `['admin']` (csak részlegesen, audit lehet teljes role-nak) | `standard` | false |

**Kliens-oldali szűrés (UX) ≠ Server-oldali authorization (security).** A részleteket Section 3.4 és Section 5.5 tárgyalja.

### 2.4 Suspense + ErrorBoundary kötelező — `<LazyWorldRoute>` HOC

**Δ FE-01 + FE-06:** Minden lazy world a `<LazyWorldRoute>` HOC-on át regisztrálódik (Section 2.1). A wrapper sorrend kötelezve:

```
<ErrorBoundary>
  └── <WorldGuard>          ← guard FIRST: nem-engedélyezett → <Navigate>, lazy nem indul
        └── <Suspense>      ← csak ha guard PASS-olt
              └── <Lazy />  ← chunk download + render
```

**Δ FE-08:** Heavy screen-ek saját nested `<ErrorBoundary>` + `<Suspense>` párral, hogy egy Three.js/recharts crash ne kapja el a world-szintű boundary, ami az egész world-ből kidobná a usert:

```tsx
// Production world-ön belül, NestingVisualizerScreen használat:
<Route path="cutting-plan/:planId/nesting" element={
  <ErrorBoundary fallback={<HeavyScreenError onRetry={...} />}>
    <Suspense fallback={<HeavyScreenLoading />}>
      <NestingVisualizerScreen />
    </Suspense>
  </ErrorBoundary>
} />
```

**Frozen decision (memóriából, érvényes):** *"Lazy-loaded module routes require `<Suspense>` and `<ErrorBoundary>` wrappers"* — `<LazyWorldRoute>` ezt strukturálisan kényszeríti.

### 2.5 URL state, history és 404 stratégia (FE-03/04/05)

#### Deep linking — URL ↔ filter state szinkron (FE-03)

Minden lista/szűrhető screen `useSearchParams`-szal szinkronizálja a filter/sort/page state-et:

| Screen | URL paraméterek |
|---|---|
| `OrdersListScreen` | `?status=draft,submitted&from=2026-04-01&to=2026-04-30&page=1&sort=-updatedAt` |
| `CuttingPlanListScreen` | `?status=ready,executing&page=1` |
| `ManufacturingFsmBoardScreen` | `?stage=cnc,edge&assignee=me` |
| `AuditLogScreen` | `?from=...&to=...&actor=...&action=...&page=1` |
| `UserListScreen` | `?role=admin,sales-rep&page=1` |
| `B2bHandshakesScreen` | `?state=pending,accepted&page=1` |

**Pattern (sample):**
```ts
const [searchParams, setSearchParams] = useSearchParams();
const filters = parseOrderFilters(searchParams);
const { data } = useQuery({
  queryKey: ['sales', 'orders', filters],
  queryFn: () => api.orders.list(filters),
});
const updateFilter = (next: OrderFilters) =>
  setSearchParams(serializeOrderFilters(next), { replace: true });
```

#### History policy (FE-04)

| Akció | Push / Replace | Indok |
|---|---|---|
| World switch (`/w/sales` → `/w/production`) | Push | User vissza akar tudni menni |
| Filter változás (`?status=...`) | **Replace** | Back gomb ne lépdeljen filter-történetben |
| Pagination (`?page=2`) | Push | Back = előző oldal — lista UX standard |
| Modal open | Nincs history change v1-ben | (state in route v3-ra) |
| Guard redirect (no module/role) | **Replace** | History ne tartalmazza a tiltott URL-t |
| Auth redirect (`/login` → world) | **Replace** | Login flow ne legyen Back-gombbal visszanavigálható |

#### 404 granularity (FE-05)

| Eset | Komponens | Helyszín |
|---|---|---|
| Ismeretlen route (`/foo`) | `<NotFound404>` (globális) | App root, `*` route |
| Ismeretlen world (`/w/unknown`) | `<NotFound404>` (globális) | Globális, `<WorldShell>`-en kívül |
| Tiltott world (modul/role miss) | `<Navigate to="/w/home" replace>` | `<WorldGuard>`-on belül |
| Ismeretlen entity (`/w/sales/orders/9999`) | `<EntityNotFound resource="order" id="9999" />` | World content-area, `<WorldShell>` benne marad — kontextus megtartva, sidebar+topbar látható |
| Forbidden entity (cross-tenant kísérlet) | `<EntityForbidden />` + audit log entry | World content-area; admin contact CTA |

---

## 3. Komponens architektúra

### 3.1 Komponens fa — layout route-os shared shell (Δ FE-02)

```
<App>
└── <BrowserRouter>
    └── <AuthProvider> (Keycloak)
        └── <QueryClientProvider> (TanStack Query)
            └── <Toaster />                          (FE-11 — globális toast/notification)
                └── <Routes>
                    ├── /login, /callback, /logout (system routes — shell-mentes)
                    ├── /w → <WorldShell chrome="standard">  ← LAYOUT ROUTE (egyszer mountol)
                    │   ├── <header><WorldTopBar /></header>
                    │   │   ├── <BrandLogo>          (Δ stabil — nem újra-render world switch-en)
                    │   │   ├── <TenantBreadcrumb>
                    │   │   ├── <UserMenu>           (icon-only → aria-label, FE-22)
                    │   │   └── <LocaleSwitcher>     (FE-24)
                    │   ├── <nav aria-label="Sidebar"><WorldSidebar /></nav>  (FE-22)
                    │   │   ├── <WorldNavGroup>
                    │   │   │   └── <WorldNavItem>
                    │   │   └── <SidebarFooter>
                    │   └── <main>
                    │       └── <Outlet />           ← csak EZ cseréltődik world-váltáskor
                    │           └── <LazyWorldRoute>
                    │               └── <ErrorBoundary>
                    │                   └── <WorldGuard>
                    │                       └── <Suspense>
                    │                           └── <LazyWorldChunk />
                    │                               └── world-specific routes
                    │                                   └── [nested ErrorBoundary + Suspense
                    │                                       heavy screens-re — FE-08]
                    │
                    ├── /w/shopfloor → <WorldShell chrome="none"> ← KÜLÖN layout route
                    │   └── <ShopFloorPinGate>
                    │       └── <Outlet />
                    │
                    └── * → <NotFound404 />
```

**Stabilitási garanciák (Δ FE-02):**
- World-váltáskor a `<WorldShell>` NEM mountolódik újra
- A `<UserMenu>`, `<TenantBreadcrumb>`, `<LocaleSwitcher>` query-jei (`['session']`, `['tenant']`) cache-ből szolgálnak ki — nincs refetch
- `<WorldSidebar>` aktuális világ-jelölést `useParams()` és `useLocation()` alapján számolja, NEM unmountol

### 3.2 Új komponensek

**Δ v2:** + `LazyWorldRoute`, `EntityNotFound`, `EntityForbidden`, `HeavyScreenError`, `HeavyScreenLoading`, `Dialog` (radix-ui based). Új oszlop: rationale (FE-19).

| Komponens | Csomag | Típus | Props | Leírás | Rationale (FE-19) |
|---|---|---|---|---|---|
| `WorldShell` | app | Layout | `chrome, children` | Fő shell, layout route element | App-local v1 — single portal (D-02) |
| `WorldTopBar` | app | Layout | `showTenant?` | Felső sáv: brand, tenant, user, locale | App-local — portál-specifikus brand |
| `WorldSidebar` | app | Layout | — | Bal oldali nav, `useParams` alapján aktív world | App-local — `worldCatalog`-ra kötött |
| `WorldNavGroup` | app | Presentational | `label, items, collapsible?` | Sidebar csoport | App-local |
| `WorldNavItem` | app | Presentational | `label, icon, path, badge?` | Egy nav link | App-local |
| `WorldGuard` | app | Logic | `worldId, children` | Modul + role + PIN check, redirect | App-local — runtime config-driven |
| **`LazyWorldRoute`** *(ÚJ FE-06)* | app | HOC | `worldId, chunkLoader` | Wrapper sorrend kényszerítése | App-local — registry-driven |
| `WorldHome` | app | Page | — | Home logic (grid VS redirect) | App-local |
| `WorldCard` | app | Presentational | `world, accent?, badge?, onClick` | Home grid kártya | App-local |
| `WorldLoadingScreen` | app | Presentational | `worldId` | Suspense fallback (skeleton) | App-local |
| `WorldErrorScreen` | app | Presentational | `worldId, error, onRetry` | World ErrorBoundary fallback | App-local |
| **`EntityNotFound`** *(ÚJ FE-05)* | app | Presentational | `resource, id?, suggestUrl?` | Entity 404 a world content-areán belül | App-local |
| **`EntityForbidden`** *(ÚJ FE-05)* | app | Presentational | `resource, contactAdmin?` | Cross-tenant / forbidden entity UX | App-local |
| **`HeavyScreenError`** *(ÚJ FE-08)* | app | Presentational | `screenName, error, onRetry` | Nested ErrorBoundary fallback heavy screenen | App-local |
| **`HeavyScreenLoading`** *(ÚJ FE-08)* | app | Presentational | `screenName` | Nested Suspense fallback | App-local |
| `ShopFloorPinGate` | app | Logic | `children` | PIN gate, machine binding | App-local — interim v1 (D-07) |
| `PinEntryScreen` | app | Page | — | 6-digit PIN keypad UI | App-local |
| `OrdersListScreen` | app | Page | — | `PagedTable` + URL filters (FE-03) | App-local |
| `OrderDetailScreen` | app | Page | — | Order header + lines + handshake state | App-local |
| `ProductConfiguratorScreen` | app | Page | — | Graph engine view (Abstractions BFF) | App-local — heavy, nested boundary |
| `B2bHandshakesScreen` | app | Page | — | Handshake list + URL filters | App-local |
| `CuttingPlanListScreen` | app | Page | — | `PagedTable` + URL filters | App-local |
| `CuttingPlanDetailScreen` | app | Page | — | Plan header + line items + offcuts | App-local |
| `NestingVisualizerScreen` | app | Page | — | Canvas renderer (heavy, nested) | App-local — heavy |
| `ManufacturingFsmBoardScreen` | app | Page | — | FSM board, recharts (heavy, nested) | App-local — heavy |
| `ManufacturingTaskDetailScreen` | app | Page | — | Task header + state transitions | App-local |
| `ShopFloorTaskListScreen` | app | Page | — | Touch-optimized list, large rows | App-local |
| `ShopFloorTaskDetailScreen` | app | Page | — | Touch-optimized detail + action buttons | App-local |
| `TenantInfoScreen` | app | Page | — | Read-only tenant info v1 | App-local |
| `UserListScreen` | app | Page | — | `PagedTable`, admin-only | App-local |
| `AuditLogScreen` | app | Page | — | `PagedTable` + `HashDisplay` reuse | App-local |

### 3.3 Új @spaceos/ui komponensek (shared, vN candidates)

**Δ v2:** + `Toaster`/`useToast` (FE-11), `Dialog` (FE-18 — radix-ui adapter), `EmptyState` 3 variant (FE-23), `useLocaleFormat` hook (FE-25).

| Komponens | Csomag | Indok |
|---|---|---|
| `KpiCard` | `@spaceos/ui` | Home grid + Settings dashboard használja |
| `EmptyState` | `@spaceos/ui` | Több world ugyanazt a "nincs adat" pattern-t mutatja — **3 variant: `zero` / `error` / `forbidden`** (FE-23) |
| `LargeTouchButton` | `@spaceos/ui` | Shop Floor + jövőbeli mobile worker UX |
| `PinKeypad` | `@spaceos/ui` | Shop Floor PIN + jövőbeli kiosk-flow-k |
| **`Toaster` + `useToast`** *(ÚJ FE-11)* | `@spaceos/ui` | Globális toast/notification + ARIA live region (success/error/warning/info, auto-dismiss kivéve kritikus) |
| **`Dialog`** *(ÚJ FE-18)* | `@spaceos/ui` | Radix-ui adapter: focus trap + Esc + ARIA dialog role + initial focus + return focus |
| **`useLocaleFormat`** *(ÚJ FE-25)* | `@spaceos/i18n` | `Intl.DateTimeFormat` + `Intl.NumberFormat` lokál-alapú szám/dátum/pénznem formázás |

### 3.4 Meglévő `@spaceos/ui` újrahasználata

| Komponens | Hol használjuk | Módosítás |
|---|---|---|
| `FsmBadge` | Manufacturing FSM board, Order detail (handshake state) | Nincs |
| `HashDisplay` | Audit log, Order receipt | Nincs |
| `PagedTable` | Orders, Cutting plans, Users, Audit log | Nincs |
| `TradeTypeBadge` | Order list (door/cabinet trade jelzés) | Nincs |
| `JsonIntentEditor` | **NEM v1** — csak ha admin product config szerk. (roadmap) | — |

### 3.5 WorldGuard logika

```tsx
function WorldGuard({ worldId, children }: WorldGuardProps) {
  const session = useSession();
  const world = worldCatalog[worldId];

  // 1. Module check
  if (world.requiredModules.length > 0) {
    const hasModule = world.requiredModules.some(m =>
      session.enabledModules.includes(m)
    );
    if (!hasModule) return <Navigate to="/w/home" replace />;
  }

  // 2. Role check
  if (world.requiredRoles.length > 0) {
    const hasRole = world.requiredRoles.some(r =>
      session.user.roles.includes(r)
    );
    if (!hasRole) return <Navigate to="/w/home" replace />;
  }

  // 3. PIN check (ShopFloor)
  if (world.requiresPin && !useShopFloorSession.getState().pinValidated) {
    return <Navigate to="/w/shopfloor/pin" replace />;
  }

  return <>{children}</>;
}
```

**KRITIKUS:** ez **csak UX redirect**. A security boundary a BFF-en van (Section 5.5).

---

## 4. State management

### 4.1 Globális store-ok (D-08 hibrid)

| Store | Csomag | Státusz | Tartalom |
|---|---|---|---|
| `authStore` | app `stores/` | DEPLOYED (változatlan) | KC token, user, refresh, logout |
| `brandStore` | app `stores/` | DEPLOYED, **passzív v1** | Default skin betöltés; per-tenant override roadmap |
| `tenantStore` | app `stores/` | **ÚJ v1** | Tenant info, `enabledModules`, `tenantType` |
| `i18nStore` | `@spaceos/i18n` | DEPLOYED (változatlan) | Locale, hostname-based detection |

**`tenantStore` interface:**
```ts
interface TenantState {
  tenantId: string | null;
  tenantName: string;
  tenantType: 'Manufacturer' | 'PanelCutter' | 'Trader' | 'Logistics' | 'Installer' | 'EndCustomer';
  enabledModules: string[];
  brandSkin: string; // v1: always 'default'
  loaded: boolean;
  loadFromSession: () => Promise<void>;
}
```

**Persist policy:** `tenantStore` NEM persist — minden session start friss BFF lekéréssel (`/bff/api/me/session`). Frontend frozen decision: *"Zustand `persist` middleware MUST NOT persist sensitive auth state to localStorage."*

### 4.2 Per-world Zustand slice-ok

| Slice | World | Tartalom | Reset trigger |
|---|---|---|---|
| `useSalesOrderDraftStore` | sales | Order draft (in-progress configurator state, line item edits) | World unmount, order submit |
| `useProductionPlanStore` | production | Active plan filter, board column collapse state | World unmount |
| `useShopFloorSessionStore` | shopfloor | PIN validated, machine ID, current task ID, shift start time | PIN logout, end-of-shift, world unmount |
| `useSettingsUiStore` | settings | Aktív settings tab, audit filter | World unmount |

**Slice cleanup (world unmount):**
```tsx
useEffect(() => {
  return () => {
    // World unmount → reset domain slice
    useSalesOrderDraftStore.getState().reset();
  };
}, []);
```

### 4.3 TanStack Query — query key stratégia

**Konvenció:** `[worldId, resource, ...identifiers]`

**Δ FE-09:** Shop Floor live feed `refetchInterval` explicit (auto-update); `refetchIntervalInBackground: false` (battery).

| Key | Endpoint | Stale time | gcTime | Refetch interval |
|---|---|---|---|---|
| `['session']` | `/bff/api/me/session` | 5 min | 10 min | — |
| `['tenant']` | `/bff/api/tenant` | 10 min | 30 min | — |
| `['enabled-modules']` | (a session részeként) | 10 min | 30 min | — |
| `['sales', 'orders', filters]` | `/bff/api/orders?...` | 30 sec | 5 min | — |
| `['sales', 'order', orderId]` | `/bff/api/orders/:id` | 30 sec | 5 min | — |
| `['sales', 'configurator', productId, params]` | `/bff/abstractions/configure/...` | 5 min | 10 min | — |
| `['sales', 'handshakes']` | `/bff/api/handshakes` | 30 sec | 5 min | — |
| `['production', 'cutting-plans', filters]` | `/bff/cutting/plans?...` | 30 sec | 5 min | — |
| `['production', 'plan', planId]` | `/bff/cutting/plans/:id` | 30 sec | 5 min | — |
| `['production', 'manufacturing', filters]` | `/bff/manufacturing/tasks?...` | 15 sec | 5 min | **30 sec** (FsmBoard) |
| `['production', 'task', taskId]` | `/bff/manufacturing/tasks/:id` | 15 sec | 5 min | — |
| `['shopfloor', 'tasks', machineId]` | `/bff/shopfloor/tasks?machineId=...` | 5 sec (live) | 1 min | **10 sec** (FE-09, csak foreground) |
| `['shopfloor', 'task', taskId]` | `/bff/shopfloor/task/:id` | 5 sec | 1 min | **10 sec** (csak foreground) |
| `['settings', 'users']` | `/bff/api/users` | 1 min | 10 min | — |
| `['settings', 'audit', filters]` | `/bff/api/audit?...` | 30 sec | 5 min | — |

**v3 backlog:** SSE/WebSocket upgrade Shop Floor + Manufacturing FSM board-ra; jelenlegi polling interim.

### 4.4 World unmount cache cleanup

```ts
// apps/joinerytech/src/worlds/useWorldCleanup.ts
function useWorldCleanup(worldId: string) {
  const queryClient = useQueryClient();
  useEffect(() => {
    return () => {
      // World unmount → kifutó request cancel + cache eviction
      queryClient.cancelQueries({ queryKey: [worldId] });
      // gcTime majd elsöpri, ezért removeQueries opcionális; UX-szempontból
      // ha gyors re-entry várható (pl. settings tab váltás), removeQueries TILOS.
    };
  }, [worldId, queryClient]);
}
```

### 4.5 Server vs. client state szétválasztás

| Típus | Tárolás | Példa |
|---|---|---|
| **Server state** | TanStack Query | Orders, plans, tasks, users, audit log |
| **Client UI state** | Zustand slice | Filter selections, sidebar collapsed?, active tab |
| **Form draft state** | Zustand slice + form library (react-hook-form) | Order draft, configurator inputs |
| **Auth/session state** | Zustand globális (`authStore`, `tenantStore`) | KC token, user, tenant |

**Anti-pattern (v1-ben TILOS):** server state Zustand-be tükrözése. Minden BFF-eredmény közvetlenül a Query cache-ben.

### 4.6 Mutation policy — optimistic vs invalidation (Δ FE-10)

| Mutáció | Stratégia | Indok |
|---|---|---|
| Shop Floor task status update | **Optimistic + rollback** | LIVE feel; ha BFF reject → rollback + toast error |
| Order header save (draft → submitted) | **Invalidation** | Server-side state machine; legitim hibák gyakoribbak |
| Order line item edit (configurator) | **Optimistic + rollback** | Folyamatos szerkesztés UX |
| Configurator: Graph engine recompute | **Invalidation** + spinner | Server-side számítás authoritative; nem érdemes optimistic |
| B2B Handshake accept/decline | **Invalidation** | Audit-relevant — nem optimistic |
| Audit log filter | **N/A** (read-only) | — |
| Admin user create/disable | **Invalidation** | Audit-relevant |
| Tenant info update | **Invalidation** | Confirmation pattern (toast + reload) |

**Pattern (sample, optimistic):**
```ts
useMutation({
  mutationFn: api.shopfloor.updateTaskStatus,
  onMutate: async ({ taskId, nextStatus }) => {
    await queryClient.cancelQueries({ queryKey: ['shopfloor', 'task', taskId] });
    const prev = queryClient.getQueryData(['shopfloor', 'task', taskId]);
    queryClient.setQueryData(['shopfloor', 'task', taskId], old => ({ ...old, status: nextStatus }));
    return { prev };
  },
  onError: (_err, { taskId }, ctx) => {
    queryClient.setQueryData(['shopfloor', 'task', taskId], ctx?.prev);
    toast.error('Státuszváltás sikertelen, visszaállítva.');
  },
  onSettled: ({ taskId }) => {
    queryClient.invalidateQueries({ queryKey: ['shopfloor', 'task', taskId] });
  },
});
```

### 4.7 Loading state taxonomy (Δ FE-12)

| Állapot | Megjelenés | Hol |
|---|---|---|
| **World chunk load** | Full skeleton screen (header+sidebar+content placeholder) | `<WorldLoadingScreen>`, Suspense fallback |
| **Heavy screen chunk load** | Partial skeleton (csak content area) | `<HeavyScreenLoading>`, nested Suspense |
| **Query initial load** | Component-level skeleton: list rows = `PagedTable` skeleton; detail = card skeleton; chart = placeholder | Az adott screen render-jén |
| **Query background refetch** | Subtle indikátor (top-bar 2px progress, vagy nav-item dot) | TanStack `isFetching` state |
| **Mutation pending** | Action button disabled + inline spinner; screen lock CSAK ha kritikus (PIN login, payment) | Lokális komponens-szint |
| **Polling refresh** | Nincs visible UI (Shop Floor task feed) — frissül in-place | Background |

### 4.8 Memoization stratégia (Δ FE-13)

| Komponens / use-case | Pattern |
|---|---|
| `<PagedTable>` row child komponens | `React.memo` + stabil row prop reference |
| `<ManufacturingFsmBoardScreen>` cell | Cell komponens `React.memo`; status-mapper `useMemo` |
| `<NestingVisualizerScreen>` canvas redraw | `useMemo` viewport calc; `useCallback` event handlers |
| Zustand selectors | Shallow compare (`useShallow` from zustand/react/shallow) |
| Heavy form (configurator) | `react-hook-form` controlled mode + per-field subscription (avoid re-render whole form) |
| `<WorldSidebar>` items | `useMemo` filtered worldCatalog (changes ritkán: enabledModules + roles) |

### 4.9 Draft persistence + leave confirmation (Δ FE-16)

`useSalesOrderDraftStore` és `useShopFloorSessionStore` kritikus: accidental nav → munkavesztés.

| Mechanizmus | Trigger | Tárolás |
|---|---|---|
| `useBlocker` (React Router v6) | World/route change, ha `isDirty` | In-memory; confirm dialog |
| `beforeunload` event listener | Tab close / browser back | Native browser confirm |
| Auto-save sessionStorage | 30 sec debounce ha `isDirty` | sessionStorage (NEM localStorage — security) |
| Manual "Save draft" button | Order detail toolbar | BFF (server-side draft entity, opcionális v3) |

**KRITIKUS (memóriából frozen):** persist middleware NEM tárolhat sensitive auth state-et. Draft-ban személyes adat (név, cím) lehet → ezért sessionStorage és NEM localStorage; tab-bezáráskor elveszik.

### 4.10 Auth refresh edge case handler (Δ FE-17)

```ts
// apps/joinerytech/src/auth/onSilentRenewError.ts
userManager.events.addSilentRenewError((error) => {
  const currentPath = window.location.pathname + window.location.search;
  toast.warning('Munkamenet lejárt. Bejelentkezés szükséges.');
  // FE-04: replace history (login flow ne legyen Back-elhetőbb)
  navigate(`/login?returnTo=${encodeURIComponent(currentPath)}`, { replace: true });
});

// post-login callback handler
const returnTo = searchParams.get('returnTo') ?? '/w/home';
// validáció: csak relative path, ne external URL (open redirect védelem)
if (returnTo.startsWith('/') && !returnTo.startsWith('//')) {
  navigate(returnTo, { replace: true });
}
```

---

## 5. BFF API surface

### 5.1 Meglévő BFF route-ok (Codebase Status szerint)

| Route prefix | Backend | Port | Státusz |
|---|---|---|---|
| `/bff/api/*` | Kernel | 5000 | DEPLOYED |
| `/bff/joinery/*` | Joinery (deprecated → Door modulra vándorol) | 5002 | DEPLOYED |
| `/bff/cutting/*` | Cutting | 5005 | DEPLOYED |

### 5.2 Új BFF route-ok v1-hez

| Route | Backend | Port | Cél | Auth |
|---|---|---|---|---|
| `GET /bff/api/me/session` | Kernel | 5000 | User + tenant + enabledModules + roles + brand_skin (mindig 'default' v1) egy hívásban | KC user |
| `GET /bff/api/me/home-state` | Kernel (új endpoint) | 5000 | Home auto-redirect logikához (primary world derive) | KC user |
| `GET /bff/api/tenant` | Kernel | 5000 | Tenant detail + enabled modules | KC user |
| `GET /bff/api/audit` | Kernel | 5000 | Audit log (Hash chain) | KC user, role: admin |
| `GET /bff/api/users` | Kernel | 5000 | User list | KC user, role: admin |
| `/bff/manufacturing/*` | Manufacturing | 5007 | Phase 1 endpoints (EdgeBanding, CNC, Order saga) | KC user |
| `/bff/abstractions/*` | Abstractions | 5003 | Graph engine view (configurator) | KC user |
| `POST /bff/shopfloor/pin/login` | Manufacturing (új) | 5007 | PIN auth → machine token | Anonymous + machine fingerprint |
| `POST /bff/shopfloor/pin/logout` | Manufacturing (új) | 5007 | Machine session revoke | Machine token |
| `GET /bff/shopfloor/tasks` | Manufacturing | 5007 | Machine-scoped task feed | Machine token |
| `PATCH /bff/shopfloor/task/:id/status` | Manufacturing | 5007 | Task FSM transition | Machine token |

### 5.3 Aggregáló endpoint-ok (v1 minimum)

| Endpoint | Aggregáció | Cél |
|---|---|---|
| `/bff/api/me/session` | Kernel: user + tenant + enabledModules + roles | Avoid 4 round-trip a kezdő render-en |
| `/bff/api/me/home-state` | enabledModules + roles + recentActivity (opcionális v2) | Auto-redirect logika |

**Roadmap (v2-v3):**
- `/bff/dashboard/kpi` — cross-world KPI aggregátor (Cabinet 0.3 Federation pattern alapján)
- `/bff/sales/order-with-config` — Order + linked configurator graph összevonva (jelenleg külön Query-k)

### 5.4 OpenAPI codegen bővítés

| Service | Snapshot fájl | v1 hatás |
|---|---|---|
| Kernel | `@spaceos/api-client/snapshots/kernel.json` | Kibővítendő: `/me/session`, `/me/home-state`, `/audit`, `/users` |
| Cutting | `@spaceos/api-client/snapshots/cutting.json` | Változatlan |
| Manufacturing | `@spaceos/api-client/snapshots/manufacturing.json` | **ÚJ snapshot** — Phase 1 endpoints + Shop Floor |
| Abstractions | `@spaceos/api-client/snapshots/abstractions.json` | **ÚJ snapshot** — configurator endpoints |

**Frozen decision (memóriából):** *"OpenAPI codegen from committed snapshot — never runtime schema discovery."* Minden új endpoint snapshot regen + commit.

### 5.5 Authorization & error handling

#### Server-side authorization (KRITIKUS — Section 2.3 mirror)

Minden új BFF route Express middleware láncot implementál:

```
1. authMiddleware       — KC JWT verify (vagy machine token Shop Floor-on)
2. tenantMiddleware     — tenantId injection a request-be
3. enabledModulesGuard  — route prefix → required module mapping
4. roleGuard (opcionális) — role check ha route admin-only
```

**Mapping tábla (Orchestrator config):**

```ts
const routeModuleMap = {
  '/bff/cutting/*':       { requiredModules: ['cutting'] },
  '/bff/manufacturing/*': { requiredModules: ['cutting'] },
  '/bff/abstractions/*':  { requiredModules: ['door', 'cabinet'] }, // bármelyik elég
  '/bff/shopfloor/*':     { requiredModules: ['cutting'], authMode: 'machine' },
};
```

Ha a tenant `enabledModules` nem tartalmazza a route módulát → **403 Forbidden** + audit log entry.

#### Error response format

Egységes formátum (Orchestrator already deployed):
```json
{
  "error": {
    "code": "FORBIDDEN_MODULE",
    "message": "Tenant does not have 'cutting' module enabled",
    "details": { "module": "cutting", "tenantId": "..." }
  }
}
```

#### Retry / timeout policy

| Kategória | Timeout | Retry | Megjegyzés |
|---|---|---|---|
| GET (egyszerű) | 10 sec | 3 (exponential backoff) | TanStack Query default |
| GET (aggregátor) | 30 sec | 1 | `/me/session`, `/me/home-state` |
| POST/PATCH/DELETE | 15 sec | **0** | Mutáció — soha nem retry-olunk silently |
| Shop Floor task fetch | 10 sec | 3 | Eli rosszul tűrt hálózat → 5 sec stale time önmagában is jó |
| PIN login | 10 sec | 0 | Ne retry-oljunk auth-ot (rate limit védi a backendet) |

#### Cache stratégia

- **TanStack Query:** in-memory (Section 4.3 stale/gc time)
- **Service Worker cache:** **NEM v1** — service worker → roadmap (offline Shop Floor scenario)
- **HTTP cache:** Orchestrator nem küld `Cache-Control: public` semelyik `/bff/*` válaszra (security)

---

## 6. i18n & Brand

### 6.1 Új fordítási kulcsok (HU/EN)

**Namespace konvenció:** `worlds.{worldId}.{key}` és `nav.{worldId}.{key}` és `common.*`.

| Kulcs | EN | HU |
|---|---|---|
| `worlds.home.title` | Home | Kezdőlap |
| `worlds.home.welcome` | Welcome, {name} | Üdvözöllek, {name} |
| `worlds.home.cards.sales` | Sales | Értékesítés |
| `worlds.home.cards.production` | Production | Gyártás |
| `worlds.home.cards.shopfloor` | Shop Floor | Műhely |
| `worlds.home.cards.settings` | Settings | Beállítások |
| `worlds.sales.title` | Sales | Értékesítés |
| `worlds.sales.nav.orders` | Orders | Rendelések |
| `worlds.sales.nav.handshakes` | B2B Handshakes | B2B kapcsolatok |
| `worlds.production.title` | Production | Gyártás |
| `worlds.production.nav.cuttingPlan` | Cutting Plan | Szabászati terv |
| `worlds.production.nav.manufacturing` | Manufacturing | Gyártásirányítás |
| `worlds.shopfloor.title` | Shop Floor Terminal | Műhely terminál |
| `worlds.shopfloor.pin.title` | Enter PIN | PIN bevitele |
| `worlds.shopfloor.pin.invalid` | Invalid PIN | Hibás PIN |
| `worlds.shopfloor.pin.locked` | Locked. Contact supervisor. | Zárolt. Hívd a műszakvezetőt. |
| `worlds.shopfloor.tasks.empty` | No tasks assigned | Nincs aktív feladat |
| `worlds.settings.title` | Settings | Beállítások |
| `worlds.settings.nav.tenant` | Tenant | Cégadatok |
| `worlds.settings.nav.users` | Users | Felhasználók |
| `worlds.settings.nav.audit` | Audit Log | Naplók |
| `common.loading` | Loading… | Betöltés… |
| `common.error.generic` | Something went wrong | Hiba történt |
| `common.error.network` | Network error | Hálózati hiba |
| `common.error.forbidden` | Access denied | Hozzáférés megtagadva |
| `common.error.notFound` | Not found | Nincs ilyen elem |
| `common.action.retry` | Retry | Újrapróbál |
| `common.action.cancel` | Cancel | Mégse |
| `common.action.save` | Save | Mentés |
| `nav.user.profile` | Profile | Profil |
| `nav.user.logout` | Sign out | Kijelentkezés |

### 6.2 Brand token bővítés

**v1: NINCS új brand token.** D-02 LOCKED — egy közös moduláris portál, default skin (JoineryTech `@spaceos/brand-tokens` package). A `brandStore` betöltéskor mindig `'default'`-ra esik vissza, a tenant `brand_skin` mezőt nem alkalmazza runtime-ban.

**Roadmap (Section 11):**
- v2: per-tenant brand skin override layer aktiválás (`Doorstar` skin létezik, csak nincs activated)
- v2: hostname-based brand selection ha külön domain-ek brand-eltérnek

### 6.3 Locale hot-swap + format (Δ FE-24, FE-25)

**Hot-swap (no page reload):**
- `i18nStore.setLocale(next)` → React re-render az új namespace-szel
- Translation namespace per locale split (Vite import.meta.glob), lazy load: csak az aktuális + fallback
- User választás → `/bff/api/me/preferences` PATCH (BFF stores), következő login már perzisztált
- v1 fallback: hostname-based detection (`joinerytech.hu` → EN, `asztalostech.hu` → HU); user override előre

**Format (Intl API):**
```ts
// @spaceos/i18n/useLocaleFormat.ts (FE-25)
export function useLocaleFormat() {
  const { locale } = useI18nStore();
  return useMemo(() => ({
    date: (d: Date | string) => new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(new Date(d)),
    dateTime: (d: Date | string) => new Intl.DateTimeFormat(locale, { dateStyle: 'medium', timeStyle: 'short' }).format(new Date(d)),
    number: (n: number, opts?: Intl.NumberFormatOptions) => new Intl.NumberFormat(locale, opts).format(n),
    currency: (n: number, ccy = 'HUF') => new Intl.NumberFormat(locale, { style: 'currency', currency: ccy }).format(n),
  }), [locale]);
}
```

---

## 7. Responsive & Accessibility

### 7.1 Breakpoint stratégia

| Breakpoint | Szélesség | Cél | World viselkedés |
|---|---|---|---|
| Desktop | ≥ 1280px | Primary | Sidebar nyitva, multi-column layout |
| Tablet | 768-1279px | Adapted | Sidebar drawer-ré válik (toggle), single-column tartalom |
| Mobile | < 768px | Limited support v1-ben | Home, Settings olvasható; Sales/Production "use desktop" warning + degradált view; Shop Floor optimalizált touch UI |

**Tailwind config konvenció (változatlan):** `sm: 640px`, `md: 768px`, `lg: 1024px`, `xl: 1280px`. World shell max szélesség: `2xl: 1536px` content-area.

### 7.2 a11y követelmények (WCAG 2.1 AA minimum)

**Δ FE-22:** Semantic landmarks explicit; **Δ FE-18:** focus trap modal-okban kötelező.

| Terület | Minimum |
|---|---|
| **Színkontraszt** | 4.5:1 normál szöveg, 3:1 nagy szöveg |
| **Keyboard nav** | Minden interaktív elem Tab-elérhető; sidebar, modal, dropdown |
| **Focus management** | Route change után focus a `<h1>`-re VAGY skip-link-re |
| **Focus trap** *(FE-18)* | Modal/Dialog-ban kötelező: első/utolsó tabbable elem között ciklikus Tab; Esc bezár; return focus az opening trigger-re |
| **Form labels** | Minden input `<label>` + `aria-describedby` hibaüzenetre |
| **Live regions** | Toast (`<Toaster>` FE-11) `aria-live="polite"`; kritikus hiba `aria-live="assertive"` |
| **Skip-to-content** | Layout első elem `<a href="#main">` |
| **Heading hierarchy** | World screen: 1× `<h1>`, max 3 szint |
| **Image alt** | Minden non-decorative kép `alt=`; ikonok `aria-hidden="true"` ha label mellett |
| **Semantic landmarks** *(FE-22)* | `<header>` topbar, `<nav aria-label="Sidebar">` sidebar, `<main>` content area, `<aside>` opcionális segéd-panel |
| **ARIA labels icon-only** *(FE-22)* | Sidebar collapse, user menu, action button → `aria-label`; ikon next-to-text → `aria-hidden="true"` az SVG-n |

**Lighthouse Accessibility gate:** ≥ 95 (DoD).

### 7.3 Touch target minimumok

| Komponens | Min méret | Megjegyzés |
|---|---|---|
| `WorldNavItem` (sidebar) | 44×44px | WCAG 2.1 AA target size |
| `WorldCard` (home) | 200×200px | nagy kattintási felület |
| **Shop Floor minden interaktív** | **64×64px** | ipari környezet, esetleges kesztyű |
| `PinKeypad` gombok | 80×80px | Touch keypad |
| `LargeTouchButton` (új `@spaceos/ui`) | 64×64px default, 80×80px Shop Floor variant | — |
| Standard portál gombok | 40×40px | desktop UX |

---

## 8. Test stratégia

### 8.1 Vitest unit tesztek

**Δ v2:** + LazyWorldRoute wrapper-order tesztek, EntityNotFound/Forbidden, useBlocker, onSilentRenewError, Toaster/useToast, Dialog focus trap, useLocaleFormat, mutation policy patterns.

| Tesztelendő egység | Fájl | Min teszt |
|---|---|---|
| `worldCatalog` integritás | `worldCatalog.test.ts` | 5 (minden world létezik, requiredModules valid, chrome valid enum) |
| `WorldGuard` logika | `WorldGuard.test.tsx` | 8 (modul match, missing, role match/missing, PIN check, redirect, kombinációk) |
| **`LazyWorldRoute` wrapper order** *(FE-01/06)* | `LazyWorldRoute.test.tsx` | 4 (guard rejects → no chunk load; guard passes → suspense; error in chunk → world EB; ARIA correct) |
| `WorldShell` chrome variants + layout-route stability | `WorldShell.test.tsx` | 6 (standard, minimal, none variants; nem unmountol world-switch-en; Outlet renderelődik) |
| `WorldHome` redirect logic | `WorldHome.test.tsx` | 6 (single module → redirect, multi → grid, no module → grid empty) |
| `useEnabledWorlds` hook | `useEnabledWorlds.test.ts` | 4 (filter logic, role intersection, empty case) |
| `useShopFloorSession` PIN flow | `useShopFloorSession.test.ts` | 6 (PIN validate, invalid retry, lockout, machine binding, logout) |
| `tenantStore` | `tenantStore.test.ts` | 4 (load from session, reset, persist OFF) |
| **`EntityNotFound` / `EntityForbidden`** *(FE-05)* | `EntityNotFound.test.tsx` | 4 (resource render, suggest URL, forbidden CTA, audit log fired) |
| **`useBlocker` confirm dialog** *(FE-16)* | `useDraftBlocker.test.tsx` | 4 (dirty + nav → confirm; cancel → stay; confirm → leave; beforeunload registered) |
| **`onSilentRenewError` handler** *(FE-17)* | `onSilentRenewError.test.ts` | 3 (redirect with returnTo, validate returnTo not external, post-login restore) |
| **`Toaster` + `useToast`** *(FE-11)* | `Toaster.test.tsx` | 5 (4 variants, ARIA live, auto-dismiss, error persistent, queue) |
| **`Dialog` focus trap** *(FE-18)* | `Dialog.test.tsx` | 4 (initial focus, trap cycle, Esc, return focus) |
| **`useLocaleFormat`** *(FE-25)* | `useLocaleFormat.test.ts` | 5 (date, number, currency, locale switch, fallback) |
| BFF client mock | `apiClient.test.ts` | 5 per service (5× services = 25 teszt) |
| **Mutation policy patterns** *(FE-10)* | `mutations/*.test.ts` | 8 (3× optimistic + rollback, 5× invalidation) |
| **URL filter sync** *(FE-03)* | `useUrlFilters.test.ts` | 5 (parse, serialize, default, multi-filter, replace history) |

**Cél:** ≥ 90 új Vitest unit teszt v1 implementáció után (v1: 60 → v2: 90, +30 új teszt a finding-ek miatt).

### 8.2 Playwright E2E szcenáriók

**Δ v2:** + URL filter persistence, world switch shell stability, draft blocker, silent renew failure, optimistic UI rollback.

| Flow | Teszt | Becsült teszt# |
|---|---|---|
| Auth | KC login → /w/home redirect | 1 |
| Auth | Logout → /login | 1 |
| Auth | Token refresh silent renew | 1 |
| **Auth** *(FE-17)* | Silent renew failure → returnTo restore | 1 |
| Home | Single-module user → auto redirect | 1 |
| Home | Multi-module user → grid render | 1 |
| Home | Card click → world navigation | 1 |
| **Shell** *(FE-02)* | World switch → sidebar/topbar nem flicker, query nem refetch | 1 |
| Sales | Orders list → filter → paging | 2 |
| **Sales** *(FE-03)* | Filter → URL sync → reload → state restore | 1 |
| Sales | Order detail open → handshake state | 1 |
| Sales | Configurator open → Graph engine view → save | 1 (heavy) |
| **Sales** *(FE-16)* | Configurator dirty → world nav → confirm dialog | 1 |
| Production | Cutting plan list → detail | 1 |
| Production | Manufacturing FSM board → status transition | 1 |
| **Production** *(FE-10)* | Optimistic status update → simulate 500 → rollback + toast | 1 |
| **Production** *(FE-08)* | Heavy screen crash → nested EB catches → world stays | 1 |
| Shop Floor | PIN entry → invalid → lockout | 2 |
| Shop Floor | PIN entry → valid → task list | 1 |
| Shop Floor | Task detail → status update | 1 |
| **Shop Floor** *(FE-09)* | Task list polling → background update visible | 1 |
| Settings | Tenant info → audit log → users (admin) | 2 |
| Settings | Non-admin user → users page → 403 redirect | 1 |
| World guard | Disabled module → /w/cutting access → home redirect | 1 |
| **World guard** *(FE-01)* | Forbidden world URL access → no chunk in network log + redirect | 1 |
| **404** *(FE-05)* | `/w/sales/orders/9999` → EntityNotFound, sidebar+topbar megmarad | 1 |
| **a11y** *(FE-18)* | Dialog open → focus trap, Esc → close + return focus | 1 |
| **i18n** *(FE-24)* | Locale switch → no page reload → translations update | 1 |
| **Cél** | | **≥ 30 új E2E flow** |

### 8.3 MSW mock handler-ek

Minden új BFF route-hoz külön handler. Struktúra:

```
apps/joinerytech/src/test/mocks/
├── handlers/
│   ├── kernel.ts           (me/session, tenant, audit, users)
│   ├── manufacturing.ts    (tasks, task detail, status update)
│   ├── abstractions.ts     (configurator)
│   ├── shopfloor.ts        (pin login/logout, machine tasks)
│   └── cutting.ts          (existing, kibővítve plan endpoints)
├── fixtures/
│   ├── tenants.ts
│   ├── orders.ts
│   ├── plans.ts
│   ├── tasks.ts
│   └── users.ts
└── server.ts
```

**Cél:** minden új BFF route-hoz min 1 MSW handler — happy path + 1 error case (403 vagy 500).

---

## 9. Definition of Done

### 9.1 Frontend gates

- [ ] Minden lazy-loaded world `<LazyWorldRoute>` HOC-on át (FE-01, FE-06): ErrorBoundary > WorldGuard > Suspense > Lazy
- [ ] `<WorldShell>` layout route-ként mountol egyszer (FE-02); world-váltáskor csak a content cserélődik
- [ ] Heavy screen-ek nested ErrorBoundary + Suspense pár (FE-08): NestingVisualizer, FsmBoard, ProductConfigurator
- [ ] `enabledModules` mismatch → redirect /w/home replace (FE-04); chunk NEM töltődik le (FE-01)
- [ ] List screens URL-szinkronizált filter/sort/page (FE-03); standardizált search param konvenció
- [ ] World-en belüli entity 404 → `<EntityNotFound>` content area-ban (FE-05); WorldShell stabil
- [ ] Cross-tenant kísérlet → `<EntityForbidden>` + audit log entry (Section 5.5)
- [ ] Form: react-hook-form + Zod minden form-ra (FE-07)
- [ ] Modal/Dialog focus trap, Esc, return focus (FE-18)
- [ ] Live region (Toaster) ARIA-polite (success/info), ARIA-assertive (error) (FE-11)
- [ ] Loading state taxonomy alkalmazva (FE-12): world / heavy / query / mutation / polling
- [ ] Mutation policy table alkalmazva (FE-10): optimistic ahol kell, invalidation ahol nem
- [ ] Memoization: `<PagedTable>` rows, FsmBoard cells, configurator fields (FE-13)
- [ ] `useBlocker` confirm dialog ha draft dirty és nav-elsz (FE-16); `beforeunload` is
- [ ] `onSilentRenewError` handler: redirect `/login?returnTo=...`, post-login restore (FE-17)
- [ ] Locale switch hot-swap (no page reload) (FE-24); Intl-alapú format (FE-25)
- [ ] Minden form input `<label>` + `aria-describedby` hibára
- [ ] Responsive 3 breakpoint tesztelve (1280px, 768px, 375px)
- [ ] Touch target minimum 44px standard, 64px Shop Floor
- [ ] Keyboard nav minden world-ben (Tab order, Esc bezár modal, Enter submit)
- [ ] Skip-to-content link `<App>` legelső interaktív elemeként
- [ ] Semantic landmarks: `<header>`, `<nav aria-label>`, `<main>`, opcionális `<aside>` (FE-22)
- [ ] Sidebar collapse state per session (sessionStorage, NEM localStorage)
- [ ] World unmount → per-world Zustand slice reset + Query cancel
- [ ] List rendering: entity ID `key`, NEVER index (FE-20)

### 9.2 BFF gates

- [ ] Minden új BFF route: `authMiddleware` + `tenantMiddleware` + `enabledModulesGuard`
- [ ] Role-gated endpoint (`/audit`, `/users`) `roleGuard` middleware-rel
- [ ] OpenAPI snapshot frissítve (kernel, manufacturing, abstractions) + codegen lefuttatva, commitelve
- [ ] Error response format konzisztens: `{ error: { code, message, details? } }`
- [ ] Timeout: 10s default, 30s aggregátor (`/me/session`, `/me/home-state`)
- [ ] Shop Floor PIN endpoint: rate limit (5 invalid PIN / 5 perc / machine) + audit log entry minden invalid PIN-re
- [ ] CORS: csak `portal.joinerytech.hu` + dev `http://localhost:5173` (frozen)
- [ ] CSP header Orchestrator-ról: `default-src 'self'; script-src 'self'; ...` (no inline)

### 9.3 Test gates

- [ ] Meglévő FE tesztek zöld (Doorstar Portal: 99 → +90 = ~189; E2E: 277 → +30 = ~307)
- [ ] Új Vitest unit tesztek: ≥ 90 (Δ v2: +30 a finding-ek miatt)
- [ ] Új Playwright E2E flow-k: ≥ 30 (Δ v2: +10)
- [ ] MSW handler minden új BFF route-hoz (kernel +5, manufacturing +3, abstractions +1, shopfloor +4 = 13 új handler)
- [ ] Test coverage ≥ 70% statements, ≥ 60% branches a `apps/joinerytech/src/worlds/` mappában
- [ ] **Wrapper-order regression test** (FE-01): Network panel ellenőrzés — forbidden world chunk URL nincs a network log-ban
- [ ] **Layout-route stability test** (FE-02): React DevTools snapshot — `<WorldShell>` instance ID állandó world-switch során

### 9.4 Performance gates

**Δ FE-15 (granuláris budget) + FE-14 (virtualization):**

- [ ] Lighthouse Performance ≥ 90 (desktop, prod build, /w/home)
- [ ] Lighthouse Accessibility ≥ 95 (minden world-re)
- [ ] Bundle size: app entry < 200KB gzip
- [ ] **World container chunk < 50KB gzip** (FE-15) — csak a world-shell + routes + screen wrapper, heavy screens nélkül
- [ ] **Heavy screen chunk < 100KB gzip** (FE-15) — NestingVisualizer, FsmBoard, ProductConfigurator
- [ ] CI gate (`size-limit` lib): PR-szinten regression alert, konkrét per-chunk budget enforcement
- [ ] First Contentful Paint < 1.5s desktop prod
- [ ] Cumulative Layout Shift < 0.1
- [ ] Time to Interactive < 3s desktop prod
- [ ] **PagedTable virtualization audit Track A elején** (FE-14): ha PagedTable nem virtualizál, Audit Log + Orders list (potenciálisan 1000+ row) `react-virtuoso` integrációval
- [ ] **Asset opt** (FE-21): `lucide-react` icon set, `font-display: swap`, preconnect, raszter `loading="lazy"`

### 9.5 Összesített

- [ ] Minden Section 9.1-9.4 gate teljesül
- [ ] CONTRACT_ISSUES.md üres VAGY minden issue tracked Manufacturing/Kernel inboxban
- [ ] CLAUDE.md frissítve a v1 conventions-szel (worldCatalog, WorldShell, query keys)
- [ ] README.md `apps/joinerytech/src/worlds/` mappa-szintű, magyarázza a world pattern-t
- [ ] V1 deployolva `portal.joinerytech.hu`-n + smoke test (login → 5 world elérés)

---

## 10. Sprint terv (Claude Code implementáció)

### 10.1 Track-ek és sorrend

```
Track A — Foundation             ───────►  ┐
Track F — BFF route additions    ──────────┼──►  Track C — Sales
                                            │     Track D — Production
Track B — Home + Settings        ───────►   │     Track E — Shop Floor
                                            │
                                            └──►  Final integration + DoD
```

| Track | Tartalom | Becsült effort | Függőség |
|---|---|---|---|
| **A — Foundation** | `worldCatalog.ts`, `WorldShell`, `WorldGuard`, `WorldTopBar`, `WorldSidebar`, `tenantStore`, route shell, lazy boundaries | ~3 nap | — |
| **F — BFF route additions** | Orchestrator: `/me/session`, `/me/home-state`, `/audit`, `/users`, `/manufacturing/*` proxy, `/abstractions/*` proxy, `/shopfloor/*` proxy + middleware láncok | ~3 nap | Backend services DEPLOYED (Manufacturing 5007 már DEV COMPLETE) |
| **B — Home + Settings** | `WorldHome` (grid + redirect), `WorldCard`, `TenantInfoScreen`, `AuditLogScreen`, `UserListScreen` | ~2 nap | A, F |
| **C — Sales** | `OrdersListScreen`, `OrderDetailScreen`, `ProductConfiguratorScreen`, `B2bHandshakesScreen`, `useSalesOrderDraftStore` | ~5 nap | A, F |
| **D — Production** | `CuttingPlanListScreen`, `CuttingPlanDetailScreen`, `NestingVisualizerScreen`, `ManufacturingFsmBoardScreen`, `ManufacturingTaskDetailScreen` | ~5 nap | A, F |
| **E — Shop Floor** | `ShopFloorPinGate`, `PinEntryScreen`, `ShopFloorTaskListScreen`, `ShopFloorTaskDetailScreen`, `useShopFloorSessionStore`, `PinKeypad` (`@spaceos/ui`) | ~4 nap | A, F |
| **G — Integration + DoD** | E2E full flow, Lighthouse audit, bundle size analysis, CONTRACT_ISSUES drain | ~2 nap | B, C, D, E |

**Összesen:** ~24 nap fokozatosan, párhuzamosítással ~14-16 nap critical path.

### 10.2 Agent utasítás (Claude Code, `spaceos-fe` session)

```
Te a `spaceos-fe` tmux session vagy. WD: ~/spaceos-doorstar-portal/.

Forrás: SpaceOS_Portal_World_Architecture_v1.md (jelen dokumentum,
mailbox-on át kapod task-bontásban).

Sorrend (parallel-ezhető Track A és Track F):

NAP 1-3 — Track A:
  1. Hozd létre a worldCatalog.ts-t a @spaceos/domain csomagban
     (Section 2.3 tábla alapján).
  2. Implementáld a WorldShell-t a chrome prop-pal (Section 3.1 fa).
  3. Implementáld a WorldGuard-ot (Section 3.5 logika).
  4. Új tenantStore (Section 4.1 interface).
  5. Route shell az App.tsx-ben (Section 2.1 map alapján), minden
     world Suspense + ErrorBoundary-ban.
  6. Vitest tesztek: WorldShell, WorldGuard, worldCatalog, tenantStore.
  7. BUILD + TEST + LINT + TYPECHECK zöld → outbox DONE.

NAP 1-3 — Track F (parallel, külön session/inbox):
  CONTRACT_ISSUES alapján vagy közvetlen utasításra Orchestrator
  módosítás → új BFF route-ok + middleware láncok (Section 5.2, 5.5).

NAP 4-5 — Track B (függ A+F-től):
  WorldHome + WorldCard + Settings világ képernyői.

NAP 6-10 — Track C, D, E parallel:
  Sales / Production / Shop Floor világok.

NAP 11-12 — Track G:
  E2E + Lighthouse + bundle audit.

Constraint:
  - Minden új lazy world → Suspense + ErrorBoundary
  - Per-world cleanup useEffect → Query cancel + Zustand slice reset
  - OpenAPI codegen újrafuttatva minden BFF endpoint változás után
  - PIN flow (Shop Floor): TILOS PIN-t a localStorage-ben tárolni;
    machine token sessionStorage-ben (interim v1, v2-ben device-bound)
  - Bundle size mérés minden track végén: `pnpm build --report`
```

### 10.3 Kockázatok és mitigációk

**Δ v2:** + R-09 (PagedTable virtualization unknown), R-10 (radix-ui adoption), R-11 (size-limit CI integráció).

| ID | Kockázat | Valószínűség | Hatás | Mitigáció |
|---|---|---|---|---|
| R-01 | Manufacturing service Phase 1 BFF route-jai még nem proxy-zódnak az Orchestrator-on | Magas | Track F blokkol | Track F first day: új Orchestrator middleware + `/bff/manufacturing/*` proxy felépítés; ha a backend endpoint hiányzik, mock fixture-ral kezdjük + CONTRACT_ISSUES |
| R-02 | Shop Floor PIN flow security model még nem véglegesített (machine token vs. KC) | Magas | Track E SEC-FE finding (v3) | v1 interim: `/bff/shopfloor/*` saját auth middleware-rel, machine fingerprint + sessionStorage; v2: kiemelni külön app entry-pointra (D-07 LOCKED) |
| R-03 | Production world Three.js / recharts bundle > 100KB gzip heavy chunk | Közepes | Performance gate fail | Nested Suspense kötelező (FE-08); ha még akkor is sok, ManufacturingFsmBoard-ban recharts → custom CSS bar chart |
| R-04 | Configurator (Sales world) Abstractions BFF endpoint-ja nincs minden ProductTemplate-re | Közepes | Sales track scope creep | v1: csak deployed Doorstar Door template; Cabinet template → roadmap |
| R-05 | KC `portal-app` redirect URI nem tartalmazza a `localhost:5173`-t | Alacsony | Dev loop blokkol | v4.1 amendment §4 már tartalmazza, de Day 1 verify infra task |
| R-06 | `enabledModules` szerverkor-szerinti lemaradása (cache) → user új modult kap, de UX nem mutatja | Alacsony | UX zavar | `/me/session` stale time 5 min; admin-trigger `queryClient.invalidateQueries(['session'])` settings change után |
| R-07 | Audit log endpoint pagination + filter teljesítmény (Hash chain nagy) | Közepes | Settings world lassú | BFF: kötelező `?from=&to=&limit=50`; szerver oldal cursor-based paging Kernel oldalon |
| R-08 | Több párhuzamos Claude Code session ütközése (Track C+D+E parallel ugyanabban a repo-ban) | Magas | Merge conflict | Track-enkénti git branch; Track A jelez `main`-be merge előtt |
| **R-09** *(FE-14)* | `<PagedTable>` (`@spaceos/ui`) belső implementáció ismeretlen — virtualizál-e? | Közepes | Audit Log + Orders lassú N>500 esetén | Track A első napja: PagedTable forrás-audit; ha nem virtualizál → `react-virtuoso` integráció Audit + Orders specifikusan, vagy PagedTable v2 internal upgrade |
| **R-10** *(FE-18)* | Radix-ui adoption — új dependency a `@spaceos/ui` csomagba | Alacsony | Bundle bloat ha rosszul tree-shake-elt | Csak a használt primitive-ek import-ja (Dialog, DropdownMenu); size-limit CI-ban watchelni |
| **R-11** *(FE-15)* | `size-limit` lib CI integrációja még nincs | Közepes | Bundle regression észrevétlen | Track A: `size-limit` config commit + GitHub Action job; PR-szinten failing if budget exceeded |

---

## 11. Mi jön utána (roadmap)

### 11.1 v2 horizon (3-6 hónap)

| Feature | Prereq | Blokkoló |
|---|---|---|
| **Spatial world** (Three.js 3D editor) | Modules.Spatial backend | Three.js bundle stratégia (separate route-level chunk + WebGL detect) |
| **Cabinet world** | Modules.Cabinet aggregate | Cabinet 0.4+ |
| **Brand skin layer aktiválás** (per-tenant) | Doorstar skin csomagolva | `@spaceos/brand-tokens` runtime override engedélyezése; D-02 unlock |
| **Shop Floor → external app entry-point** | Új nginx vhost + CI | Security review (v3 finding mitigáció) |
| **Cross-world dashboard** | BFF aggregator (`/bff/dashboard/kpi`) | Cabinet 0.3 Federation pattern adaptation |
| **Service Worker + offline** Shop Floor | TanStack Query persistor + IndexedDB cache | Shop Floor terminál hálózat-megszakadás scenario |

### 11.2 v3 horizon (6-12 hónap)

| Feature | Aktor | Megjegyzés |
|---|---|---|
| **Trader world** | `Trader` aktor-típus | Modules.Trading szükséges |
| **Logistics/Delivery world** | `Logistics` aktor-típus | Modules.Delivery |
| **Installer world** | `Installer` aktor-típus | Modules.Installation |
| **EndCustomer portál** | `EndCustomer` aktor-típus | Külön domain (B2C UX más, mint a B2B portál) |
| **Mobile-first responsive** | — | Sales + Production world mobile-optimalizáció |
| **PWA + push notification** | — | Shop Floor + Production lead értesítések |

### 11.3 Continuous

- a11y audit minden release-en (WCAG 2.1 AA → AAA célzott)
- Lighthouse CI automation a deploy pipeline-ban
- Bundle size regression alert (PR-szintű)
- E2E coverage mátrix (új world / új persona kombinációk)

---

## 12. Design → Code mapping (placeholder)

A design bundle (`https://api.anthropic.com/v1/design/h/Gf2t9hUM4KFNw5f9Kr8igw`) **404** v1 készítésekor. Ha v2-re a bundle elérhetővé válik, a következő tábla töltődik:

| Design fájl | React komponens | Csomag | Megjegyzés |
|---|---|---|---|
| `page-home.jsx` | `WorldHome.tsx` | app | TBD |
| `page-shopfloor.jsx` | `ShopFloorTaskListScreen.tsx` + `PinEntryScreen.tsx` | app | TBD |
| `ui.jsx → StatusPill` | `FsmBadge.tsx` (létezik) | `@spaceos/ui` | Esetleges merge / variant |
| `data-worlds.js` | `worldCatalog.ts` | `@spaceos/domain` | Mapping ellenőrzés |

---

*SpaceOS — Portal World Architecture v2 (Frontend Review Pass) · 2026-04-29*
*5 world (Home, Sales, Production, Shop Floor, Settings) · Design-agnostic · 25 FE-NN finding adressed*
*Következő lépés: v3 security review (`references/sub-senior-security.md` betöltése)*
