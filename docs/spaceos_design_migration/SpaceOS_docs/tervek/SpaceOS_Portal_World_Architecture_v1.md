# SpaceOS — Portal World Architecture
## Frontend Architecture v1 (Draft)

> **Verzió:** v1 — 2026-04-29
> **Státusz:** DRAFT — frontend review (v2) → security review (v3) → BFF review (v4) hátravan
> **Prereq:** Kernel `EnabledModules` (Migration 0025) DEPLOYED · Orchestrator `/bff/api/*` + `/bff/cutting/*` DEPLOYED · Manufacturing service DEV COMPLETE (port 5007)
> **Design forrás:** `https://api.anthropic.com/v1/design/h/Gf2t9hUM4KFNw5f9Kr8igw` (404 — design-agnostic v1, mapping → v2)
> **Kumulált review:** — (v1 első kiadás, még nincs review-pass)
> **Nyitott döntések:** D-01..D-09 LOCKED (lásd Section 0)

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

## 1. Kumulált Finding Összesítő (v1 → vN)

| ID | Súly | Terület | Probléma | Javítás |
|---|---|---|---|---|
| — | — | — | _v1 első kiadás, még nincs finding_ | _v2 frontend review után töltődik_ |

---

## 2. Route architektúra

### 2.1 Route map (React Router v6)

```
/                                        → ProtectedRoute → /w/home (auth check)
/login                                   → KeycloakLoginRedirect
/callback                                → KeycloakCallback
/silent-callback                         → KeycloakSilentRenew
/logout                                  → KeycloakLogout

/w                                       → redirect /w/home
/w/home                                  → WorldShell(chrome='standard')
                                            └── HomeScreen (card grid OR auto-redirect)

/w/sales                                 → WorldShell(chrome='standard') + WorldGuard('sales')
   /                                       → SalesIndex (default: orders)
   orders                                  → OrdersListScreen
   orders/:orderId                         → OrderDetailScreen
   orders/:orderId/items/:lineId/configure → ProductConfiguratorScreen [nested Suspense]
   handshakes                              → B2bHandshakesScreen

/w/production                            → WorldShell(chrome='standard') + WorldGuard('production')
   /                                       → ProductionIndex (default: cutting-plan)
   cutting-plan                            → CuttingPlanListScreen
   cutting-plan/:planId                    → CuttingPlanDetailScreen
   cutting-plan/:planId/nesting            → NestingVisualizerScreen [nested Suspense]
   manufacturing                           → ManufacturingFsmBoardScreen [nested Suspense]
   manufacturing/:taskId                   → ManufacturingTaskDetailScreen

/w/shopfloor                             → WorldShell(chrome='none') + ShopFloorPinGate
   /                                       → ShopFloorIndex
   pin                                     → PinEntryScreen
   tasks                                   → ShopFloorTaskListScreen
   task/:taskId                            → ShopFloorTaskDetailScreen

/w/settings                              → WorldShell(chrome='standard') + WorldGuard('settings')
   /                                       → SettingsIndex (default: tenant)
   tenant                                  → TenantInfoScreen
   users                                   → UserListScreen [role: admin]
   audit                                   → AuditLogScreen (HashDisplay reuse)

*                                        → NotFound404
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

### 2.4 Suspense + ErrorBoundary kötelező

```tsx
// apps/joinerytech/src/worlds/lazyWorld.tsx
const SalesWorld = lazy(() => import('./sales/SalesWorld'));

<Route path="/w/sales/*" element={
  <ErrorBoundary fallback={<WorldErrorScreen world="sales" />}>
    <Suspense fallback={<WorldLoadingScreen world="sales" />}>
      <WorldGuard worldId="sales">
        <SalesWorld />
      </WorldGuard>
    </Suspense>
  </ErrorBoundary>
} />
```

A frontend frozen decision (memóriából): *"Lazy-loaded module routes require `<Suspense>` and `<ErrorBoundary>` wrappers"* — minden world route így zárul.

---

## 3. Komponens architektúra

### 3.1 Komponens fa

```
<App>
└── <BrowserRouter>
    └── <AuthProvider> (Keycloak)
        └── <QueryClientProvider> (TanStack Query)
            └── <Routes>
                ├── /login, /callback, /logout (system routes)
                └── /w/* (world routes)
                    └── <WorldShell chrome={...}>
                        ├── <WorldTopBar>           (chrome != 'none')
                        │   ├── <BrandLogo>
                        │   ├── <TenantBreadcrumb>
                        │   ├── <UserMenu>
                        │   └── <LocaleSwitcher>
                        ├── <WorldSidebar>          (chrome === 'standard')
                        │   ├── <WorldNavGroup>
                        │   │   └── <WorldNavItem>
                        │   └── <SidebarFooter>
                        └── <WorldContent>
                            ├── <WorldGuard>        (enabledModules + role check)
                            │   └── <Outlet>        (world-specific screens)
                            │       └── [Nested Suspense for heavy screens]
                            └── <WorldErrorBoundary>
```

### 3.2 Új komponensek

| Komponens | Csomag | Típus | Props | Leírás |
|---|---|---|---|---|
| `WorldShell` | app | Layout | `chrome, worldId, children` | Fő shell, chrome variánsok |
| `WorldTopBar` | app | Layout | `worldId, showTenant?` | Felső sáv: brand, tenant, user |
| `WorldSidebar` | app | Layout | `worldId, navItems` | Bal oldali nav, world-specifikus |
| `WorldNavGroup` | app | Presentational | `label, items, collapsible?` | Sidebar csoport |
| `WorldNavItem` | app | Presentational | `label, icon, path, badge?` | Egy nav link |
| `WorldGuard` | app | Logic | `worldId, children` | enabledModules + role check, redirect ha nincs jog |
| `WorldHome` | app | Page | — | Home screen logic (grid VS redirect) |
| `WorldCard` | app | Presentational | `world, accent?, badge?, onClick` | Home grid kártya |
| `WorldLoadingScreen` | app | Presentational | `worldId` | Suspense fallback (skeleton) |
| `WorldErrorScreen` | app | Presentational | `worldId, error, onRetry` | ErrorBoundary fallback |
| `ShopFloorPinGate` | app | Logic | `children` | PIN gate, machine binding |
| `PinEntryScreen` | app | Page | — | 6-digit PIN keypad UI (touch-optimized) |
| `OrdersListScreen` | app | Page | — | `PagedTable` + filters |
| `OrderDetailScreen` | app | Page | — | Order header + lines + handshake state |
| `ProductConfiguratorScreen` | app | Page | — | Graph engine view (Abstractions BFF) |
| `B2bHandshakesScreen` | app | Page | — | Handshake list + status |
| `CuttingPlanListScreen` | app | Page | — | `PagedTable` + status filter |
| `CuttingPlanDetailScreen` | app | Page | — | Plan header + line items + offcuts |
| `NestingVisualizerScreen` | app | Page | — | Canvas renderer (heavy, nested Suspense) |
| `ManufacturingFsmBoardScreen` | app | Page | — | FSM board, recharts (heavy, nested Suspense) |
| `ManufacturingTaskDetailScreen` | app | Page | — | Task header + state transitions |
| `ShopFloorTaskListScreen` | app | Page | — | Touch-optimized list, large rows |
| `ShopFloorTaskDetailScreen` | app | Page | — | Touch-optimized detail + action buttons |
| `TenantInfoScreen` | app | Page | — | Read-only tenant info v1 |
| `UserListScreen` | app | Page | — | `PagedTable`, admin-only |
| `AuditLogScreen` | app | Page | — | `PagedTable` + `HashDisplay` reuse |

### 3.3 Új @spaceos/ui komponensek (shared, vN candidates)

| Komponens | Csomag | Indok |
|---|---|---|
| `KpiCard` | `@spaceos/ui` | Home grid + Settings dashboard használja |
| `EmptyState` | `@spaceos/ui` | Több world ugyanazt a "nincs adat" pattern-t mutatja |
| `LargeTouchButton` | `@spaceos/ui` | Shop Floor + jövőbeli mobile worker UX |
| `PinKeypad` | `@spaceos/ui` | Shop Floor PIN + jövőbeli kiosk-flow-k |

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

| Key | Endpoint | Stale time | gcTime |
|---|---|---|---|
| `['session']` | `/bff/api/me/session` | 5 min | 10 min |
| `['tenant']` | `/bff/api/tenant` | 10 min | 30 min |
| `['enabled-modules']` | (a session részeként) | 10 min | 30 min |
| `['sales', 'orders', filters]` | `/bff/api/orders?...` | 30 sec | 5 min |
| `['sales', 'order', orderId]` | `/bff/api/orders/:id` | 30 sec | 5 min |
| `['sales', 'configurator', productId, params]` | `/bff/abstractions/configure/...` | 5 min | 10 min |
| `['sales', 'handshakes']` | `/bff/api/handshakes` | 30 sec | 5 min |
| `['production', 'cutting-plans', filters]` | `/bff/cutting/plans?...` | 30 sec | 5 min |
| `['production', 'plan', planId]` | `/bff/cutting/plans/:id` | 30 sec | 5 min |
| `['production', 'manufacturing', filters]` | `/bff/manufacturing/tasks?...` | 15 sec | 5 min |
| `['production', 'task', taskId]` | `/bff/manufacturing/tasks/:id` | 15 sec | 5 min |
| `['shopfloor', 'tasks', machineId]` | `/bff/shopfloor/tasks?machineId=...` | **5 sec** (élő) | 1 min |
| `['shopfloor', 'task', taskId]` | `/bff/shopfloor/task/:id` | 5 sec | 1 min |
| `['settings', 'users']` | `/bff/api/users` | 1 min | 10 min |
| `['settings', 'audit', filters]` | `/bff/api/audit?...` | 30 sec | 5 min |

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

| Terület | Minimum |
|---|---|
| **Színkontraszt** | 4.5:1 normál szöveg, 3:1 nagy szöveg |
| **Keyboard nav** | Minden interaktív elem Tab-elérhető; sidebar, modal, dropdown |
| **Focus management** | Route change után focus a `<h1>`-re VAGY skip-link-re |
| **Form labels** | Minden input `<label>` + `aria-describedby` hibaüzenetre |
| **Live regions** | Toast/notification `aria-live="polite"`; kritikus hiba `aria-live="assertive"` |
| **Skip-to-content** | Layout első elem `<a href="#main">` |
| **Heading hierarchy** | World screen: 1× `<h1>`, max 3 szint |
| **Image alt** | Minden non-decorative kép `alt=`; ikonok `aria-hidden="true"` ha label mellett |

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

| Tesztelendő egység | Fájl | Min teszt |
|---|---|---|
| `worldCatalog` integritás | `worldCatalog.test.ts` | 5 (minden world létezik, requiredModules valid, chrome valid enum) |
| `WorldGuard` logika | `WorldGuard.test.tsx` | 8 (modul match, modul missing, role match, role missing, PIN check, default home redirect, kombinációk) |
| `WorldShell` chrome variants | `WorldShell.test.tsx` | 6 (standard, minimal, none, sidebar render/skip, topbar render/skip) |
| `WorldHome` redirect logic | `WorldHome.test.tsx` | 6 (single module → redirect, multi module → grid, no module → grid empty) |
| `useEnabledWorlds` hook | `useEnabledWorlds.test.ts` | 4 (filter logic, role intersection, empty case) |
| `useShopFloorSession` PIN flow | `useShopFloorSession.test.ts` | 6 (PIN validate, invalid retry, lockout, machine binding, logout) |
| `tenantStore` | `tenantStore.test.ts` | 4 (load from session, reset, persist OFF) |
| BFF client mock | `apiClient.test.ts` | 5 per service (4× services = 20 teszt) |

**Cél:** ≥ 60 új Vitest unit teszt v1 implementáció után.

### 8.2 Playwright E2E szcenáriók

| Flow | Teszt | Becsült teszt# |
|---|---|---|
| Auth | KC login → /w/home redirect | 1 |
| Auth | Logout → /login | 1 |
| Auth | Token refresh silent renew | 1 |
| Home | Single-module user → auto redirect | 1 |
| Home | Multi-module user → grid render | 1 |
| Home | Card click → world navigation | 1 |
| Sales | Orders list → filter → paging | 2 |
| Sales | Order detail open → handshake state | 1 |
| Sales | Configurator open → Graph engine view → save | 1 (heavy) |
| Production | Cutting plan list → detail | 1 |
| Production | Manufacturing FSM board → status transition | 1 |
| Shop Floor | PIN entry → invalid → lockout | 2 |
| Shop Floor | PIN entry → valid → task list | 1 |
| Shop Floor | Task detail → status update | 1 |
| Settings | Tenant info → audit log → users (admin) | 2 |
| Settings | Non-admin user → users page → 403 redirect | 1 |
| World guard | Disabled module → /w/cutting access → home redirect | 1 |
| **Cél** | | **≥ 20 új E2E flow** |

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

- [ ] Minden lazy-loaded world `<Suspense>` + `<ErrorBoundary>` wrapper-ben
- [ ] `WorldGuard` minden world-re alkalmazva (kivéve home)
- [ ] `enabledModules` mismatch → redirect /w/home (nem 404, nem hard error)
- [ ] Minden form input `<label>` + `aria-describedby` hibára
- [ ] Responsive 3 breakpoint tesztelve (1280px, 768px, 375px) — manuális E2E + Lighthouse
- [ ] Touch target minimum 44px standard, 64px Shop Floor
- [ ] Keyboard nav minden world-ben (Tab order, Esc bezár modal, Enter submit)
- [ ] Skip-to-content link az `<App>` legelső interaktív elemeként
- [ ] Sidebar collapse state perszisztálódik per session (sessionStorage, NEM localStorage)
- [ ] World unmount → per-world Zustand slice reset + Query cancel

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

- [ ] Meglévő FE tesztek zöld (Doorstar Portal: 99 → +60 = ~159; E2E: 277 → +20 = ~297)
- [ ] Új Vitest unit tesztek: ≥ 60
- [ ] Új Playwright E2E flow-k: ≥ 20
- [ ] MSW handler minden új BFF route-hoz (kernel +4, manufacturing +3, abstractions +1, shopfloor +4 = 12 új handler)
- [ ] Test coverage ≥ 70% statements, ≥ 60% branches a `apps/joinerytech/src/worlds/` mappában

### 9.4 Performance gates

- [ ] Lighthouse Performance ≥ 90 (desktop, prod build, /w/home)
- [ ] Lighthouse Accessibility ≥ 95 (minden world-re)
- [ ] Bundle size: app entry < 200KB gzip
- [ ] Lazy world chunk: < 80KB gzip per világ — Production world override OK ha nested heavy boundary is ott
- [ ] First Contentful Paint < 1.5s desktop prod
- [ ] Cumulative Layout Shift < 0.1
- [ ] Time to Interactive < 3s desktop prod

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

| ID | Kockázat | Valószínűség | Hatás | Mitigáció |
|---|---|---|---|---|
| R-01 | Manufacturing service Phase 1 BFF route-jai még nem proxy-zódnak az Orchestrator-on | Magas | Track F blokkol | Track F first day: új Orchestrator middleware + `/bff/manufacturing/*` proxy felépítés; ha a backend endpoint hiányzik, mock fixture-ral kezdjük + CONTRACT_ISSUES |
| R-02 | Shop Floor PIN flow security model még nem véglegesített (machine token vs. KC) | Magas | Track E SEC-FE finding (v3) | v1 interim: `/bff/shopfloor/*` saját auth middleware-rel, machine fingerprint + sessionStorage; v2: kiemelni külön app entry-pointra (D-07 LOCKED) |
| R-03 | Production world Three.js / recharts bundle > 80KB gzip | Közepes | Performance gate fail | Nested Suspense kötelező: NestingVisualizer + ManufacturingFsmBoard saját chunk; ha még akkor is sok, ManufacturingFsmBoard-ban recharts → custom CSS bar chart |
| R-04 | Configurator (Sales world) Abstractions BFF endpoint-ja nincs minden ProductTemplate-re | Közepes | Sales track scope creep | v1: csak deployed Doorstar Door template; Cabinet template → roadmap |
| R-05 | KC `portal-app` redirect URI nem tartalmazza a `localhost:5173`-t | Alacsony | Dev loop blokkol | v4.1 amendment §4 már tartalmazza, de Day 1 verify infra task |
| R-06 | `enabledModules` szerverkor-szerinti lemaradása (cache) → user új modult kap, de UX nem mutatja | Alacsony | UX zavar | `/me/session` stale time 5 min; admin-trigger `queryClient.invalidateQueries(['session'])` settings change után |
| R-07 | Audit log endpoint pagination + filter teljesítmény (Hash chain nagy) | Közepes | Settings world lassú | BFF: kötelező `?from=&to=&limit=50`; szerver oldal cursor-based paging Kernel oldalon |
| R-08 | Több párhuzamos Claude Code session ütközése (Track C+D+E parallel ugyanabban a repo-ban) | Magas | Merge conflict | Track-enkénti git branch; Track A jelez `main`-be merge előtt |

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

*SpaceOS — Portal World Architecture v1 (Draft) · 2026-04-29*
*5 world (Home, Sales, Production, Shop Floor, Settings) · Design-agnostic v1*
*Következő lépés: v2 frontend review (`references/sub-senior-frontend.md` betöltése)*
