# SpaceOS — Kódbázis összesített állapotleírás

**Utolsó frissítés:** 2026-05-28 — FULL DEPLOY ✅ Kernel M-0031 + Joinery J-003 + Sales 5009 + FE 304 teszt · Minden smoke teszt zöld
**Környezet:** VPS prod (109.122.222.198)
**Archívum:** [`docs/codebase-history/`](codebase-history/)

---

## ⚠️ 2026-04-30 Átalakítás

### Mappa restructure

```
/opt/spaceos/
  backend/                          ← MINDEN backend ide került
    spaceos-kernel/         (5000)
    spaceos-orchestrator/   (3000)  ← AI gateway (LLM only, NEM proxy)
    spaceos-modules-joinery/        (5002)
    spaceos-modules-abstractions/   (5003)
    spaceos-modules-inventory/      (5004)
    spaceos-modules-cutting/        (5005)
    spaceos-modules-procurement/    (5006)
    spaceos-modules-cabinet/        (NuGet lib)
    spaceos-modules-contracts/      (NuGet lib)
    spaceos-nesting-algorithms/     (NuGet lib)
    local-nuget/
  frontend/
    joinerytech-portal/             ← EGYETLEN frontend (újraépítés alatt)
  docs/                             ← dokumentáció (érintetlen)
  keycloak/                         ← IdP config
  tools/                            ← dispatcher
```

### Törölt mappák/service-ek

| Törölt | Ok |
|---|---|
| `design-portal/` | Frontend reset — újraépül |
| `spaceos-doorstar-portal/` | Frontend reset — beolvad joinerytech-portal-ba |
| `spaceos-freetier-portal/` | Frontend reset — beolvad joinerytech-portal-ba |
| `spaceos-freetier-api/` (5010) | Nesting → beépül a fő portálba |
| `spaceos-partner-api/` (5011) | PartnerTier — későbbre halasztva |
| `spaceos-modules-manufacturing/` (5007) | DEV COMPLETE — nem deployed, későbbre halasztva |
| `spaceos-workers-identity/` (5008) | Üres — későbbre halasztva |
| `e2e/`, `epics/`, `infra/`, `tester/`, `architect/`, `librarian/` | Terminál config-ok — nem kód |
| `agent_tools/`, `vision/`, `logs/` | Elavult |

### Architektúra változás

**VOLT:**
```
Frontend → Orchestrator (BFF proxy) → Backend service-ek
```

**LETT:**
```
Frontend → nginx (proxy) → Backend service-ek     (direkt API hívások)
Frontend → Orchestrator                            (csak LLM/AI hívások)
```

Az Orchestrator NEM proxy többé — **AI gateway** (LLM Tool Calling, chat). Az API proxy-zást nginx végzi.

### Frontend döntés (Manifesztum alapú)

- **Egy app:** `joinerytech.hu`
- **Minden ingyenes:** bejelentkezés = teljes hozzáférés
- **Nincs FreeTier/paid tier szétválasztás**
- **Támogatási formák** (donation) nem oldanak fel funkciót

---

## Rendszer architektúra (ÚJ)

```
Browser  https://joinerytech.hu
  │
  ▼
Nginx       (TLS 1.3 · HSTS · CSP)                          port 443
  │  /                    → frontend/joinerytech-portal/dist/  (SPA)
  │  /api/*               → backend/spaceos-kernel (5000)
  │  /joinery/*           → backend/spaceos-modules-joinery (5002)
  │  /cutting/*           → backend/spaceos-modules-cutting (5005)
  │  /inventory/*         → backend/spaceos-modules-inventory (5004)
  │  /procurement/*       → backend/spaceos-modules-procurement (5006)
  │  /abstractions/*      → backend/spaceos-modules-abstractions (5003)
  │  /sales/*             → backend/spaceos-modules-sales (5009)
  │  /ai/*                → backend/spaceos-orchestrator (3000)   ← CSAK AI/LLM
  │  /auth/*              → Keycloak (8080)
  ▼
Backend services          (loopback only, systemd)
```

---

## Service-ek

| Service | Port | Tesztek | Státusz | Path |
|---|---|---|---|---|
| **Kernel** | 5000 | **1186** | ✅ RUNNING · ADR-039 `GET /api/internal/tenants/{id}` kész | `backend/spaceos-kernel/` |
| **Orchestrator** | 3000 | **121** | ✅ RUNNING (systemd, spaceos user, AI gateway only) | `backend/spaceos-orchestrator/` |
| **Joinery** | 5002 | **420** | ✅ RUNNING · ADR-039 `POST /joinery/internal/orders/from-quote` kész | `backend/spaceos-modules-joinery/` |
| **Abstractions** | 5003 | **81** | ✅ RUNNING | `backend/spaceos-modules-abstractions/` |
| **Inventory** | 5004 | **164** | ✅ RUNNING · RS256 aktív (JWT_AUTHORITY deployed) | `backend/spaceos-modules-inventory/` |
| **Cutting** | 5005 | **931** | ✅ RUNNING · RS256 aktív (JWT_AUTHORITY deployed) | `backend/spaceos-modules-cutting/` |
| **Procurement** | 5006 | **53** | ✅ RUNNING · RS256 aktív (JWT_AUTHORITY deployed) | `backend/spaceos-modules-procurement/` |
| **Sales** | 5009 | **102** | ✅ RUNNING · systemd · nginx `/sales/` · spaceos_sales schema · RS256 aktív | `backend/spaceos-modules-sales/` |

## NuGet Libraries

| Csomag | Tesztek | Path |
|---|---|---|
| **Cabinet** (10 csomag) | **755** | `backend/spaceos-modules-cabinet/` |
| **Contracts** | **57** | `backend/spaceos-modules-contracts/` |
| **Nesting.Algorithms** | **32** | `backend/spaceos-nesting-algorithms/` |

## Frontend

| App | Domain | Státusz | Path |
|---|---|---|---|
| **JoineryTech Portal** | joinerytech.hu | ✅ DEPLOYED · Sales Phase 1+2 kész (304 teszt, 0 build hiba) | `frontend/joinerytech-portal/` |

**FE-039 Sales Phase 2** (2026-05-28):
- `QuoteDetailSlideOver` (680px) — inline tételszerkesztés Draft-ban, Nettó/ÁFA/Bruttó, FSM akciók (Send/Accept/Reject/Convert + spinner)
- `CreateQuoteSlideOver` (500px) — ügyfél typeahead, validáció, success → QuoteDetail auto-open
- `CustomerDetailSlideOver` (520px) — gradient avatar, inline contact edit, mini quote lista, FSM promote/deactivate
- `useSalesDetail` hook + `SalesDetailHost` — cross-navigation (quote↔customer)
- Quote sorok + customer kártyák kattinthatók, "Új ajánlat" gomb aktív
- 304/304 teszt pass

**FE-038 Sales Phase 1** (2026-05-28):
- `SalesCustomers` → `GET /sales/api/customers` (mock fallback, debounce)
- `SalesQuotes` → `GET /sales/api/quotes` (status mapping, filter count-ok)
- `SalesDashboard` KPI-ok API-ból számítva
- `CreateCustomerSlideOver` (400px) — validált form, `POST /sales/api/customers`
- 271/271 teszt pass

**FE-037 Identity UsersPanel** (2026-05-28):
- `UsersPanel.tsx` — kétoszlopos layout, user lista + szinkronizáció összesítő
- `UserDetailSlideOver` (440px) — adatok, UUID copy, reset/disable/enable műveletek
- `InviteUserSlideOver` (400px) — validált form, `POST /identity/users`
- 258/258 teszt pass

**FE API integrations — Phase 8+9 (COMPLETE)** (2026-05-26):
- MachineParkPanel: `GET /api/tools/workstations` — WS_STATUS_MAP, mock fallback
- DesignPage dashboard: `GET /abstractions/api/modules/templates` — template count stat
- DashboardPage recent orders: `GET /joinery/api/orders?pageSize=5` — shared mapper
- Abstractions service JWT authority path fix (`/realms` → `/auth/realms`)
- Skill docs frissítve: `fe-api-integration` (12 befejezett integráció)
- 247/247 teszt pass

**Fennmaradó mock-only területek (nincs backend API):**
- SalesPage: ajánlatok, CRM ügyfelek
- ShopFloorPage: gép queue, operátor PIN login
- RolesPanel, PartnersPanel, CatalogPanel, StageChainEditor: nincs kernel/modul API

**FE-035 Raktár bugfix** (2026-05-26):
- MovementsPage: `embedded` prop — InventoryPage mozgások tabban nincs dupla padding, nincs redundáns summary card, filter bar azonnal látható
- ProcurementPage: `w-full` az outer div-re — `/w/warehouse/procurement` helyesen renderel
- App.test.tsx: 3 új warehouse route teszt (overview, procurement, movements)
- 247/247 teszt pass

**FE-034 Settings world URL-alapú navigáció** (2026-05-26):
- SettingsWorldPage létrehozva, WorldPage wrapper törölve
- SettingsPage: initialTab + onTabChange props, URL és belső tab szinkronban
- Minden `/w/settings/:screen` URL helyesen navigál
- 244/244 teszt pass

**FE-033 ShopFloor design igazítás + orphaned route cleanup** (2026-05-26):
- ShopFloor: bg-stone-900, header bg-stone-800, "Vissza a portálra" gomb, "Bejelentkezés" cím, "Töröl" gomb
- App.tsx: `/w/orders`, `/w/workflow`, `/w/analytics` orphaned route-ok törölve
- 244/244 teszt pass

**FE-032 Warehouse world routing** (2026-05-26):
- WarehouseWorldPage: dash/inventory → InventoryPage, procurement → ProcurementPage, movements → MovementsPage
- `/w/procurement` orphaned route törölve
- 243/243 teszt pass

**FE-030 Sales world design impl kész** (2026-05-26, commit `8e1d1f1`):
- Sales world: screen-alapú navigáció (`/w/sales/:screen`)
- SalesOrders, SalesQuotes (rejected filter), SalesCustomers, SalesDashboard
- 235/235 teszt pass, pnpm build 0 error

**FE referencia-alapú oldalak kész** (2026-04-30, commit `720e106`):
- Landing page (világos téma, Keycloak CTA)
- Keycloak OIDC PKCE login/logout (sessionStorage stateStore)
- 13+ oldal teljes redesign a design reference alapján
- WorkflowPage kanban, SalesPage funnel+KPI, DesignPage wizard, ShopFloor kiosk rewrite
- Új komponensek: Avatar, SlideOver, ChatPanel, NewOrderDrawer, OffcutsPanel, stb.
- Settings 10 fül: Cég, Felhasználók, Jogosultságok, Telephely, Géppark, Partnerek, stb.
- 229/229 teszt pass, pnpm build 0 error

## Identity modul (új, 2026-05-27)

| Service | Port | Tesztek | Státusz | Path |
|---|---|---|---|---|
| **Identity** | 5008 | **63** | ✅ RUNNING · systemd · nginx `/identity/` · RS256 aktív | `backend/spaceos-modules-identity/` |

**Implementációs track-ek:**
- Track A (Domain): ✅ elfogadva — 21 teszt, commit `96e23f1`
- Track B (Application): ✅ elfogadva — 41 teszt (20 Application + 21 Domain), commit `c6ad6f8`
- Track C (Infrastructure/Persistence): ✅ elfogadva — 58 teszt, commit `012fef4` (kézi SQL migration, dotnet-ef v10 inkompatibilis)
- Track D (KC client + Workers + Redis): ✅ elfogadva — 54 teszt, commit `689d610` (plain HttpClient, Keycloak.AuthServices.Sdk net8.0 inkompatibilis)
- Track E (API + Program.cs): ✅ elfogadva — 63 teszt, commit `1749ea0`
- **Deploy: ✅ KÉSZ** (2026-05-27) — spaceos_identity DB, kézi SQL migration, systemd, nginx `/identity/`, KC client `spaceos-identity-service`
- Production fix: `616a89f` — EF Core ComplexProperty ctor, Port 5433 (natív PG)

## Backend tesztek összesen: ~3902 (verifikált)

```
Kernel 108+971+107=1186 (unit+IT+API) + Orchestrator 121 + Joinery 420 + Abstractions 81 +
Inventory 164 + Cutting 931 + Procurement 53 + Sales 102 + Contracts 57 +
Nesting 32 + Cabinet 755 = 3902
```

---

## Doorstar Soft Launch seed (2026-05-27)

| Erőforrás | ID | Státusz |
|---|---|---|
| Tenant "Doorstar Kft." | `63ef28b6-a43b-4d3f-a076-759a47911559` | ✅ |
| Facility "Doorstar Budapest Üzem" | `b3328bf2-b056-46f7-b42c-ee083dab275c` | ✅ |
| Workstations (5 db) | Holzma, Biesse, Homag, Weeke, SCM | ✅ |
| Abstractions templates (5 db) | Egyszárnyú, Kétszárnyú, Bejárati, Tolóajtó, Üveges | ✅ |
| Keycloak `tid` mapper | spaceos-tenant-scope → tenantId attr → tid claim | ✅ |
| Keycloak user attribútumok | 9 user: doorstar-admin/designer/op1 + névvel ellátott userek | ✅ |
| Keycloak user profilok | firstName/lastName/email minden usernél kitöltve, emailVerified=true, requiredActions=[] | ✅ |
| doorstar-admin jelszó | `Doorstar2026!` | ✅ |
| KC admin jelszó (reset) | `SpaceOS_Admin_2026!` | ✅ |

## Operátori teendők

| # | Feladat |
|---|---|
| nginx config: API proxy route-ok | ✅ KÉSZ — minden route konfigurálva (2026-04-30 óta) |
| Orchestrator cleanup | ✅ KÉSZ — AI gateway only, 254→121 teszt |
| Brevo email | ✅ KÉSZ — smtp-relay.brevo.com:587, joinerytech.hu domain hitelesítve, Keycloak SMTP beállítva, reset password engedélyezve |
| Turnstile site key | ⏸ Later — nincs publikus regisztrációs form, nem blokkoló |
