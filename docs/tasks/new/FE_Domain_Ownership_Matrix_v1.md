# FE Domain Ownership Matrix v1

> **Verzió:** 1.0 · **Dátum:** 2026-06-16  
> **Státusz:** ACTIONABLE — Root ebből azonnal ki tud adni FE integrációs taskot  
> **Forrás:** FE-055 API Audit + Codebase_Status + ENTITY_LINKS + Sales Reconciliation v1.1 + page-kód vizsgálat

---

## 0. Összefoglaló

| Kategória | Db |
|---|---|
| ⚡ PARTIAL (mock fallback él, de van real API) | 10 |
| ❌ MOCK only — backend már kész, bekötés hiányzik | 9 |
| ❌ MOCK only — új backend modul kell | 13 |
| — Auth/Landing (nincs adat) | 2 |
| Raktár aloldalak (MOCK) | 3 |
| Gyártás aloldal (MOCK) | 1 |
| **Összes page** | **36 + infra** |

**Slice 1 (most indítható):** 19 page — mock fallback eltávolítás + hiányzó bekötések meglévő backendre  
**Slice 2 (új modul kell):** 13 page — 5 új backend modul szükséges  
**Deferred:** 4 page — alacsony prioritás / ShopFloor kiosk / Interior geometria

---

## 1. Page → Backend Module Mapping

### 1.1 Fő oldalak (27 világ)

| # | Page | Backend modul | Nginx path | Státusz | Integrálható most? | Prioritás |
|---|---|---|---|---|---|---|
| 1 | **DashboardPage** | Kernel + Joinery | `/api/*` + `/joinery/*` | ⚡ PARTIAL | ✅ Igen — mock fallback eltávolítás | high |
| 2 | **SalesPage** | Sales | `/sales/*` | ⚡ PARTIAL | ✅ Igen — mock fallback eltávolítás | high |
| 3 | **OrdersPage** | Joinery | `/joinery/*` | ⚡ PARTIAL | ✅ Igen — mock fallback eltávolítás | high |
| 4 | **ProductionPage** | Cutting | `/cutting/*` | ⚡ PARTIAL | ✅ Igen — mock fallback eltávolítás | high |
| 5 | **AnalyticsPage** | Cutting | `/cutting/*` | ⚡ PARTIAL | ✅ Igen — mock fallback eltávolítás | medium |
| 6 | **WorkflowPage** | Kernel | `/api/*` | ⚡ PARTIAL | ✅ Igen — mock fallback eltávolítás | high |
| 7 | **InventoryPage** | Inventory | `/inventory/*` | ⚡ PARTIAL | ✅ Igen — mock fallback eltávolítás | high |
| 8 | **ProcurementPage** | Procurement | `/procurement/*` | ⚡ PARTIAL | ✅ Igen — mock fallback eltávolítás | high |
| 9 | **DesignPage** | Abstractions | `/abstractions/*` | ⚡ PARTIAL | ✅ Igen — template bekötés kibővítése | medium |
| 10 | **SettingsPage** | Kernel + Abstractions + Identity | `/api/*` + `/abstractions/*` + `/identity/*` | ⚡ PARTIAL | ✅ Igen — Roles/Partners/Stages szekciók deferred, a többi kész | high |
| 11 | **CrmPage** | ❌ **Hiányzó: CRM modul** | — | ❌ MOCK | ❌ Nem — új modul kell | high |
| 12 | **FinancePage** | ❌ **Hiányzó: Finance modul** | — | ❌ MOCK | ❌ Nem — új modul kell | high |
| 13 | **ProjectsPage** | ❌ **Hiányzó: Project modul** | — | ❌ MOCK | ❌ Nem — új modul kell | high |
| 14 | **LogisticsPage** | ❌ **Hiányzó: Logistics modul** | — | ❌ MOCK | ❌ Nem — új modul kell | medium |
| 15 | **MfgPrepPage** | Joinery (részben) + Cutting | `/joinery/*` + `/cutting/*` | ❌ MOCK | ⚠️ Részben — release/datasheet endpoint hiányzik | medium |
| 16 | **SupervisorPage** | Joinery + Cutting (részben) | `/joinery/*` + `/cutting/*` | ❌ MOCK | ⚠️ Részben — workstation állapot: Kernel `/api/tools/workstations` kész | medium |
| 17 | **MasterdataPage** | Inventory + Abstractions (részben) | `/inventory/*` + `/abstractions/*` | ❌ MOCK | ⚠️ Részben — anyagtörzs Inventory-ból, termék Abstractions-ból beköthetők | medium |
| 18 | **TradePage** | ❌ **Hiányzó: Trade/Commerce modul** | — | ❌ MOCK | ❌ Nem — új modul kell | low |
| 19 | **InteriorPage** | Abstractions (részben) | `/abstractions/*` | ❌ MOCK | ⚠️ Részben — template/sablonok beköthetők; geometria/floorplan deferred | low |
| 20 | **MaintenancePage** | ❌ **Hiányzó: Maintenance modul** | — | ❌ MOCK | ❌ Nem — új modul kell | medium |
| 21 | **QualityPage** | Joinery + Inventory (részben) | `/joinery/*` | ❌ MOCK | ⚠️ Részben — rendelés-alapú QA bekötés lehetséges (nincs dedikált QA endpoint) | medium |
| 22 | **EhsPage** | ❌ **Hiányzó: EHS/HR modul** | — | ❌ MOCK | ❌ Nem — új modul kell | low |
| 23 | **AttendancePage** | ❌ **Hiányzó: HR modul** | — | ❌ MOCK | ❌ Nem — új modul kell | low |
| 24 | **HrPage** | ❌ **Hiányzó: HR modul** | — | ❌ MOCK | ❌ Nem — új modul kell | low |
| 25 | **ControllingPage** | Sales + Joinery + Cutting (cross-domain) | több | ❌ MOCK | ⚠️ Részben — alap KPI-ok összerakhatók meglévő modulokból, de dedikált Controlling endpoint nincs | medium |
| 26 | **ServicePage** | ❌ **Hiányzó: Service/Ticket modul** | — | ❌ MOCK | ❌ Nem — új modul kell | low |
| 27 | **TasksPage** | Kernel (flow-epics részben) | `/api/*` | ❌ MOCK | ⚠️ Részben — unifiedTasks aggregátor nincs backenden | low |
| 28 | **DocsPage** | ❌ **Hiányzó: DMS modul** | — | ❌ MOCK | ❌ Nem — új modul kell | low |
| 29 | **AiPage** | Orchestrator | `/ai/*` | ❌ MOCK | ✅ Igen — Orchestrator AI gateway él | medium |
| 30 | **ExecBiPage** | Kernel + Sales + Cutting + Joinery (cross) | több | ❌ MOCK | ⚠️ Részben — cross-domain aggregátor, egyedi BI endpoint nincs | low |
| 31 | **ShopPage** | ❌ **Hiányzó: Commerce/Shop modul** | — | ❌ MOCK | ❌ Nem — alacsony prioritás | low |
| 32 | **ShopFloorPage** | Joinery + Cutting (kiosk nézet) | `/joinery/*` + `/cutting/*` | ❌ MOCK | ⚠️ Részben — PIN login + gép-queue nincs backend oldalon | low |

### 1.2 Raktár aloldalak

| # | Page | Backend modul | Státusz | Integrálható most? | Prioritás |
|---|---|---|---|---|---|
| 33 | `warehouse/MovementsPage` | Inventory | `/inventory/*` | ❌ MOCK (inline) | ✅ Igen — `GET /inventory/api/inventory/movements` endpoint szükséges (Inventory modul kész) | high |
| 34 | `warehouse/LotsPage` | Inventory | `/inventory/*` | ❌ MOCK | ✅ Igen — lot-kezelés Inventory-ban | medium |
| 35 | `warehouse/ZoneMapPage` | Inventory | `/inventory/*` | ❌ MOCK | ✅ Igen — zóna-struktúra Inventory-ból | low |

### 1.3 Gyártás aloldal

| # | Page | Backend modul | Státusz | Integrálható most? | Prioritás |
|---|---|---|---|---|---|
| 36 | `production/ProductionDashboardPage` | Cutting + Joinery | `/cutting/*` + `/joinery/*` | ❌ MOCK | ✅ Igen — cutting plans + orders beköthetők | medium |

### 1.4 Auth/Landing (nincs integrációs feladat)

| Page | Státusz |
|---|---|
| `LandingPage` | — (csak auth navigáció) |
| `LoginPage` | — (csak auth navigáció) |

---

## 2. FE Terminal Domain Split Javaslat

### 2.1 Jelenlegi helyzet

Az FE-A / FE-B szplit ideiglenes volt: csak build-párhuzamosítás (Turborepo worktree alapú), **nem domain szerinti felelősség**. Hosszú távon ez fenntarthatatlan — nincs clear ownership, nincs párhuzamos domain-szintű fejlesztés.

### 2.2 Javasolt 4 FE terminál (domain szerint)

```
FE-CORE    → Mag-folyamatok (Orders/Workflow/Production/Dashboard/Settings)
FE-SALES   → Értékesítési domain (Sales/CRM/Projects/Finance/Service)
FE-OPS     → Üzemi domain (Inventory/Procurement/Analytics/MfgPrep/Quality)
FE-PEOPLE  → Emberek + Support (HR/Attendance/EHS/Maintenance/Tasks/Docs/AI)
```

#### FE-CORE
**Domain:** Kernel + Joinery + Cutting + Abstractions + Identity  
**Oldalak:** DashboardPage · OrdersPage · WorkflowPage · ProductionPage · ProductionDashboardPage · ShopFloorPage · SupervisorPage · DesignPage · SettingsPage · LandingPage · LoginPage  
**Backend:** Kernel (5000) · Joinery (5002) · Abstractions (5003) · Cutting (5005) · Identity (5008)  
**Státusz:** Legtöbb oldal már PARTIAL — a mock fallback eltávolítás innen indul  
**Prioritás:** 1 (legmagasabb — Doorstar Soft Launch alapja)

#### FE-SALES
**Domain:** Sales + CRM + Projects + Finance + Service  
**Oldalak:** SalesPage · CrmPage · ProjectsPage · FinancePage · ServicePage · InteriorPage  
**Backend:** Sales (5009) kész · CRM/Finance/Projects/Service → új modulok  
**Státusz:** SalesPage PARTIAL (kész) · többi MOCK  
**Prioritás:** 2 (Sales Slice 1 most indul)

#### FE-OPS
**Domain:** Inventory + Procurement + Cutting (analitika) + MfgPrep + Quality  
**Oldalak:** InventoryPage · ProcurementPage · AnalyticsPage · MasterdataPage · MfgPrepPage · QualityPage · warehouse/* · TradePage  
**Backend:** Inventory (5004) · Procurement (5006) · Cutting (5005) — mind kész  
**Státusz:** InventoryPage + ProcurementPage + AnalyticsPage PARTIAL · többi MOCK  
**Prioritás:** 2 (Inventory/Procurement bekötés magas értékű)

#### FE-PEOPLE
**Domain:** HR + Attendance + EHS + Maintenance + AI + Tasks + Docs + ControllingPage + ExecBiPage  
**Oldalak:** HrPage · AttendancePage · EhsPage · MaintenancePage · TasksPage · DocsPage · AiPage · ControllingPage · ExecBiPage · ShopPage  
**Backend:** Orchestrator (3000) — AI kész · HR/Attendance/EHS/Maintenance/DMS → új modulok  
**Státusz:** AiPage beköthetők · többi mind MOCK  
**Prioritás:** 3 (Soft Launch után)

### 2.3 Párhuzamosíthatóság

```
FE-CORE  ───────────────── Slice 1 ──→  (parallel lehet Slice 1 után)
FE-SALES ───── Slice 1 (Sales) ────→
FE-OPS   ─── Slice 1 (Inv/Proc) ──→    (ezek párhuzamosíthatók FE-SALES-szel)
FE-PEOPLE ──────────────────────────→  (Slice 2+ után, nem blokkoló)
```

**FE-CORE + FE-SALES + FE-OPS párhuzamosan futtatható** a Slice 1 fázisban — nincs köztük blokkoló függőség. FE-PEOPLE csak Slice 2 backend modulok után érdemes indítani.

---

## 3. Integrációs Szekvencia

### SLICE 1 — Most indítható (backend kész)

**Cél:** minden PARTIAL oldal mock fallback-jének eltávolítása + hiányzó közvetlen bekötések pótlása. Becsült scope: 2-3 sprint.

#### Slice 1A — Mock fallback eltávolítás (legalacsonyabb kockázat)

| Sorrend | Page | Feladat | Backend endpoint(ek) |
|---|---|---|---|
| 1 | **OrdersPage** | `ORDERS` mock fallback törlése, error state kezelés | `GET /joinery/api/orders?pageSize=50` |
| 2 | **WorkflowPage** | `FLOW_EPICS` mock fallback törlése | `GET /kernel/facilities/{id}/flow-epics?pageSize=50` |
| 3 | **SalesPage** | `QUOTES_FALLBACK` mock fallback törlése | `GET /sales/api/quotes` + `GET /sales/api/customers` |
| 4 | **InventoryPage** | `MATERIALS` mock fallback törlése | `GET /inventory/api/inventory/stock` |
| 5 | **ProcurementPage** | `ACTIVE_PO` + `SUPPLIERS` mock fallback törlése | `GET /procurement/api/procurement/orders` + `/suppliers` |
| 6 | **ProductionPage** | `CUTTING_PLANS` mock fallback törlése | `GET /cutting/api/cutting/plans` |
| 7 | **AnalyticsPage** | `WASTE_DATA_FALLBACK` mock fallback törlése | `GET /cutting/api/cutting/waste` |
| 8 | **DashboardPage** | `ORDERS` + `STATS_FALLBACK` mock fallback törlése | `GET /kernel/dashboard/stats` + `/joinery/api/orders?pageSize=5` |

#### Slice 1B — Hiányzó bekötések pótlása (meglévő backendre)

| Sorrend | Page | Feladat | Szükséges endpoint |
|---|---|---|---|
| 9 | **SettingsPage** | Roles/Partners/Stages mock szekciók → jelölés hogy nincs backend (deferred badge) | — (UI cleanup) |
| 10 | **DesignPage** | Template wizard teljes API bekötése (PUT params, POST calculate) | `PUT /abstractions/api/modules/templates/{id}` + `POST /abstractions/api/modules/calculate` |
| 11 | **warehouse/MovementsPage** | Inline mock csere, API bekötés | `GET /inventory/api/inventory/movements` [?] |
| 12 | **warehouse/LotsPage** | Lot-lista API bekötés | `GET /inventory/api/inventory/lots` [?] |
| 13 | **ProductionDashboardPage** | Cutting plans + orders bekötés | meglévő `/cutting/*` + `/joinery/*` |
| 14 | **SupervisorPage** | Workstation állapot bekötés | `GET /api/tools/workstations` (Kernel — már kész!) |
| 15 | **AiPage** | Orchestrator AI gateway bekötés | `POST /ai/chat` vagy `/ai/complete` |
| 16 | **MasterdataPage** | Anyagtörzs Inventory-ból | `GET /inventory/api/inventory/items` [?] |

> [?] = endpoint egzakt path-ja ellenőrizendő az adott modul CLAUDE.md vagy controller-kódból

#### Slice 1C — Sales Slice 1 (Sales Reconciliation spec szerint)

Ez párhuzamosan fut a Slice 1A/1B-vel, a Sales Reconciliation v1.1 spec vezérli:

| Sorrend | Feladat | Spec ref |
|---|---|---|
| 1 | nginx upstream `sales:5009` verify + Portal API base `/sales/api` | CI-002 resolution |
| 2 | SalesPage mock réteg lekapcsolása, Customer + Quote live wiring | §4 screen→endpoint mapping |
| 3 | CI-001 display-only line-tree megtartása flat sorok felett | CI-001 workaround |
| 4 | CI-003 számított "lejárt" badge (ValidUntil < now) | CI-003 workaround |
| 5 | Token DevTools verify: 0 token localStorage-ben | SEC-FE-01 |

---

### SLICE 2 — Új backend modul kell először

**Cél:** az 5 azonosított hiányzó backend modul után a FE bekötés.

| Sorrend | Új modul | FE page-ek | Prereq | Prioritás |
|---|---|---|---|---|
| 1 | **CRM modul** | CrmPage (Leads, Opps, CRM pipeline) | Sales (Customer alap kész) | high |
| 2 | **Finance modul** | FinancePage (kimenő/bejövő számlák, kifizetések, szerződések) | Sales (Quote→Order→Invoice lánc) | high |
| 3 | **Project modul** | ProjectsPage (projekt hierarchia, mérföldkövek, epikek) | Sales + CRM (Opportunity→Project lánc) | high |
| 4 | **Maintenance modul** | MaintenancePage (eszköznyilvántartás, karbantartási munkalapok) | Kernel (assets) | medium |
| 5 | **HR/Attendance/EHS modul** | HrPage + AttendancePage + EhsPage | Kernel (employees/facilities) | low |

**Slice 2 sorrend indoklás:**
- CRM + Finance → Projects: a ENTITY_LINKS lánc szerint `Lead → Opp → Quote → Order → Invoice` — a Finance a Sales lánc zárása, a Projects a Sales melletti dimenzió
- Maintenance: a Joinery/Production mellé természetes kiegészítés, de nem Soft Launch blokkoló
- HR/Attendance/EHS: belső operációs modulok, Doorstar Soft Launch-hoz nem kritikusak

---

### DEFERRED — Alacsony prioritás

| Page | Ok |
|---|---|
| **ServicePage** | Szerviz/garancia reklamáció — Soft Launch után, nincs Doorstar-specifikus pressure |
| **DocsPage** | DMS modul — cross-domain, alacsony ROI egyelőre |
| **TradePage** | Trade/Commerce — C típusú ügyfelek, Doorstar nem érintett |
| **ExecBiPage** | Cross-domain BI aggregátor — külön Controlling/BI endpoint szükséges, komplex |
| **ShopPage** | B2C webshop — Doorstar-nál nem elsődleges csatorna |
| **ShopFloorPage** | Kiosk UI — PIN login + gép-queue backend nélkül; machine park API különben kész | 
| **InteriorPage** | Geometria/floorplan rész deferred; sablon nézet Abstractions-ból beköthetők (Slice 1B) |
| **TasksPage** | UnifiedTasks aggregátor — cross-domain, nincs backend aggregátor endpoint |

---

## 4. Hiányzó Backend Modulok Azonosítása

Az alábbi 5 modulra nincs (deployolt) backend. A scope a prototípus page-kódjából és az ENTITY_LINKS.md elemzéséből következik.

---

### 4.1 CRM modul (Modulok.CRM)

**Hiányzó backend → CrmPage teljesen MOCK**

| Mező | Részlet |
|---|---|
| **Modul neve** | `spaceos-modules-crm` |
| **Port (javasolt)** | 5010 |
| **Nginx path** | `/crm/*` |
| **Scope** | Lead FSM (uj→kapcsolat→minosites→nurturing→konvertalva/elvetve) · Opportunity FSM (nyitott→igenyfelmeres→osszeallitas→ajanlat→targyalas→megnyert/elveszett) · `convertLeadToOpp` · `oppCreateQuote` (Sales bekötés) · CRM Tasks + Activities · Forecast (weighted pipeline) · Webshop érdeklődés → auto-lead |
| **FE page-ek** | CrmPage (CrmDashboard / CrmPipeline / CrmLeads / CrmOpps / CrmForecast) |
| **Dependenciák** | Sales (Customer alap, Quote referencia) · Kernel (Tenant, Facility) |
| **Prioritás** | **high** — Doorstar értékesítési pipeline alapja |
| **Megjegyzés** | A Sales modul Customer entitása az alap — CRM csak Sales után indulhat |

---

### 4.2 Finance modul (Modulok.Finance)

**Hiányzó backend → FinancePage teljesen MOCK**

| Mező | Részlet |
|---|---|
| **Modul neve** | `spaceos-modules-finance` |
| **Port (javasolt)** | 5011 |
| **Nginx path** | `/finance/*` |
| **Scope** | Kimenő számla FSM (draft→issued→partial→paid, mellék void) · Bejövő számla (dir:in, szállítói) · Kifizetések (részfizetés) · Szerződések + milestone billing · `createInvoiceFromOrder` (Joinery bekötés) · `invoiceDraftFromDelivery` (Logistics bekötés — Slice 2) · FinStats (receivable/payable/overdue) |
| **FE page-ek** | FinancePage (Áttekintés / Kimenő számlák / Bejövő számlák / Kifizetések / Szerződések) |
| **Dependenciák** | Sales (Quote→Order konverzió) · Joinery (Order referencia) · Procurement (PO→bejövő számla) · Kernel (Tenant) |
| **Prioritás** | **high** — az értéklánc pénzügyi zárása, Doorstar számlázás |
| **Megjegyzés** | A Sales Reconciliation v1.1 §2.4 szerint a Finance a `invoiceDraftFromDelivery` lánc végpontja; ez Slice 2 |

---

### 4.3 Project modul (Modulok.Project)

**Hiányzó backend → ProjectsPage teljesen MOCK**

| Mező | Részlet |
|---|---|
| **Modul neve** | `spaceos-modules-project` |
| **Port (javasolt)** | 5012 |
| **Nginx path** | `/projects/*` |
| **Scope** | Projekt hierarchia (Program→Projekt→Mérföldkő→Epik→Task) · `createProjectFromQuote` · `assembleProjectFromConcept` · CustomerMilestones (vevő-látható haladás) · B2BHandshake (delegálás) · `customerProjectPhases` (HR-beosztásból) · Brief-fa (opcionálisan külön Modules.Brief) |
| **FE page-ek** | ProjectsPage · InteriorPage (projekt-betekintő) |
| **Dependenciák** | Sales (Quote→Project) · CRM (Opportunity→Concept→Project) · Joinery (Order referencia) · HR (beosztás → phases) |
| **Prioritás** | **high** — belsőépítészeti/bútor projektek Doorstar-nál kulcsfontosságú |
| **Megjegyzés** | Sales Reconciliation §7 szerint Project a `Slice 2` első eleme; külön `spaceos-arch-planner` pipeline |

---

### 4.4 Maintenance modul (Modulok.Maintenance)

**Hiányzó backend → MaintenancePage teljesen MOCK**

| Mező | Részlet |
|---|---|
| **Modul neve** | `spaceos-modules-maintenance` |
| **Port (javasolt)** | 5013 |
| **Nginx path** | `/maintenance/*` |
| **Scope** | Asset törzs (gép/jármű/szerszám/infra, `machineId` kötés a Joinery workstations-hoz) · Karbantartási munkalap FSM (bejelentve→utemezve→folyamatban→kesz) · Megelőző tervek (interval/hours) · Állásidő-napló · Downtime → Production scheduler blokkolás (`prodDownMap`) · Gyártás dashboard `assetsUnderMaintenance()` |
| **FE page-ek** | MaintenancePage (AssetRegistry / WorkOrders / MaintSchedule) |
| **Dependenciák** | Kernel (Facilities, Workstations) · Joinery (machineId kötés) · HR [opcionális] (beosztás) |
| **Prioritás** | **medium** — Doorstar üzemi géppark kezelés, de Soft Launch-nál nem blokkoló |

---

### 4.5 HR / Attendance / EHS modul (Modulok.HR)

**Hiányzó backend → HrPage + AttendancePage + EhsPage mind MOCK**

| Mező | Részlet |
|---|---|
| **Modul neve** | `spaceos-modules-hr` |
| **Port (javasolt)** | 5014 |
| **Nginx path** | `/hr/*` |
| **Scope** | Dolgozói törzs (employees: `payGrade`, `weeklyHours`, `skills`, `personal`) · Kapacitás-naptár · Távollét-kérelem FSM · Szabadság/betegszabadság egyenleg · Jelenlét/műszak (attendance, be-/kijelentkezés) · EHS (incidens FSM, kockázat 5×5 mátrix, oktatás) · Bérköltség → Kontrolling |
| **FE page-ek** | HrPage · AttendancePage · EhsPage |
| **Dependenciák** | Kernel (Tenant, Facility) · Identity (user-employee mapping) |
| **Prioritás** | **low** — fontos, de Doorstar Soft Launch nem HR-funkcióra épül |
| **Megjegyzés** | Az Identity modul (5008) már kész és deploy-olva — az employee-user mapping seam ott van; a HR modul erre épül |

---

### 4.6 Kiegészítő: Service / DMS (alacsony prioritás, Deferred)

Ezek teljes tervdokumentum és arch-planner pipeline nélkül nem indíthatók; a Slice 2 után tervezendők:

| Modul | Scope összefoglaló | FE page-ek | Prioritás |
|---|---|---|---|
| **Modulok.Service** | Reklamáció/szerviz jegy FSM (bejelentve→lezarva) · warranty · SLA · ticket→shipment + order | ServicePage | low |
| **Modulok.DMS** | Dokumentum regiszter FSM (piszkozat→kiadott) · verziózás · link-típusok (project/order/customer) | DocsPage | low |
| **Modulok.Controlling** | Cross-domain BI aggregátor (terv vs tény projekt-szinten) · EAC · labor-rate resolver | ControllingPage + ExecBiPage | medium (Slice 2 után) |

> **Megjegyzés Controlling-ra:** a ControllingPage és ExecBiPage nem egyedi backend modulok — a Controlling egy **cross-domain aggregátor**, amely a meglévő Sales/Joinery/Cutting/Inventory adatokból számol. Implementálható egy dedikált `/controlling/*` route-tal a Kernel-en belül, vagy új Controlling service-szel. Arch döntés szükséges Slice 2 előtt.

---

## 5. Döntési Mátrix — Prioritások

### Slice 1 kiadható taskot generál (most)

| Task | Terminál | Blokkoló |
|---|---|---|
| Mock fallback eltávolítás: OrdersPage + WorkflowPage + InventoryPage + ProcurementPage + ProductionPage + AnalyticsPage + DashboardPage | FE-CORE | — |
| Sales world off-mock (CI-002 feloldás, Sales Reconciliation §5 Track B) | FE-SALES | Sales backend kész (5009 live) |
| DesignPage teljes wizard API bekötés | FE-CORE | Abstractions (5003) kész |
| warehouse/MovementsPage + LotsPage bekötés | FE-OPS | Inventory (5004) kész — endpoint path ellenőrizendő |
| SupervisorPage workstation bekötés | FE-CORE | Kernel `/api/tools/workstations` kész |
| AiPage Orchestrator bekötés | FE-PEOPLE | Orchestrator (3000) AI gateway kész |

### Slice 2 előfeltételei (arch-planner után)

| Szükséges döntés | Hatás |
|---|---|
| CRM modul architekturális spec | CrmPage bekötés |
| Finance modul — Sales/Joinery Invoice lánc specifikálása | FinancePage bekötés |
| Project modul — külön arch-planner pipeline (Sales Reconciliation §7 szerint) | ProjectsPage bekötés |
| Controlling: Kernel-bővítés vs. új service döntés | ControllingPage + ExecBiPage |

---

## 6. Referencia Index

| Fájl | Tartalom |
|---|---|
| `docs/tasks/new/SpaceOS_Sales_FrontOffice_Contract_Reconciliation_v1.md` | Sales Slice 1 spec — Track A (BE) + Track B (FE), CI-001/002/003 |
| `docs/tasks/new/joinerytech/ENTITY_LINKS.md` | Cross-domain entitás-kapcsolatok, mező-szintű regiszter |
| `frontend/joinerytech-portal/API_INTEGRATION_STATUS.md` | FE-055 audit eredmény — PARTIAL/MOCK listing |
| `docs/Codebase_Status.md` | Backend service státuszok, port-map, teszt-számok |

---

*FE Domain Ownership Matrix v1 · 2026-06-16 · Architect terminál*  
*19 page Slice 1 (backend kész) · 13 page Slice 2 (5 új modul) · 4 deferred · 4 terminál domain split*
