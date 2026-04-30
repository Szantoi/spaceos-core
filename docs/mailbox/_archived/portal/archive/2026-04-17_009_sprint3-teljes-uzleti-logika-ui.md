---
id: MSG-PORTAL-009
from: root
to: portal
type: task
priority: high
status: READ
ref: FE-SPRINT3
created: 2026-04-17
---

# Sprint 3 — Doorstar Portal: Teljes üzleti logika UI

## Kontextus

Sprint 2 LEZÁRVA (214/214 E2E ✅). A backend minden modulja deployed és tesztelve.
Cél: a portal legyen használható — minden üzleti folyamat végigvihető UI-on keresztül.

Tervdokumentum: `docs/SpaceOS_Doorstar_Portal_Sprint3_UI_Plan.md`

---

## Elérhető BFF proxy-k (mind live, nincs ORCH feladat)

```
GET/POST  /bff/procurement/suppliers     → 5006
POST      /bff/procurement/orders        → 5006
POST      /bff/procurement/deliveries    → 5006
GET       /bff/inventory/stock           → 5004
GET       /bff/inventory/offcuts         → 5004
POST      /bff/inventory/movements/*     → 5004
GET       /bff/inventory/trend           → 5004
GET       /bff/cutting/sheets/{id}/nesting  → 5005
GET       /bff/cutting/waste             → 5005
POST/GET  /bff/cutting/plans             → 5005
```

---

## Feladatok (kötelező sorrend)

### FE-012 — App Navigation Shell (P0, ELSŐ)

Jelenleg nincs navigáció a modulok között. Minden lap stand-alone.

**Létrehozandó:**
- `src/components/AppLayout.tsx` — sidebar layout wrapper
- Sidebar linkek: Dashboard · Rendelések · Készlet · Szállítók · Vágótervek · Profil
- `App.tsx`: minden `ProtectedRoute` az `AppLayout`-on belül
- Mobile collapse support (hamburger)
- `AppHeader.tsx` refactor: brand logo + user avatar + logout gomb

**Teszt:** `AppLayout.test.tsx` — link megjelenés, active state, logout gomb

---

### FE-013 — Suppliers page `/suppliers` (P0, CI-002 megoldás)

**Létrehozandó:**
- `src/pages/SuppliersPage.tsx` — táblázat: név, email, telefon, cím
- `src/components/CreateSupplierModal.tsx` — form: name (required), email, phone, address
- `src/api/suppliersApi.ts`:
  ```ts
  GET  /bff/procurement/suppliers  → SupplierDto[]
  POST /bff/procurement/suppliers  → { name, email, phone, address }
  ```
- `src/hooks/useSuppliers.ts`, `useCreateSupplier.ts`
- `App.tsx`: `/suppliers` route

**Teszt:** `SuppliersPage.test.tsx`, `CreateSupplierModal.test.tsx`

---

### FE-014 — Inventory page `/inventory` (P1)

**Létrehozandó:**
- `src/pages/InventoryPage.tsx` — 3 tab: Készlet · Maradékok · Trend
- **Készlet tab:**
  - Material type selector (MDF 18mm / MDF 16mm / HDF 3mm / Forgácslap / ABS él)
  - Stat kártyák: panel db + terület m²
  - `RecordInboundModal.tsx` — materialType, thickness, panelCount, areaM2, reference, occurredAt
- **Maradékok tab:** offcut lista
- **Trend tab:** fogyás táblázat (utolsó N bejegyzés)
- `src/api/inventoryApi.ts`:
  ```ts
  GET  /bff/inventory/stock?materialType=...
  GET  /bff/inventory/offcuts?materialType=...
  POST /bff/inventory/movements/inbound
  GET  /bff/inventory/trend
  ```
- Hooks: `useInventoryStock`, `useOffcuts`, `useConsumptionTrend`, `useRecordInbound`
- `App.tsx`: `/inventory` route

**Teszt:** `InventoryPage.test.tsx`, `RecordInboundModal.test.tsx`

---

### FE-015 — Cutting Plans + Nesting `/cutting` (P1)

**Létrehozandó:**
- `src/pages/CuttingPlansPage.tsx` — napi vágótervek, dátum picker
  - `CreateCuttingPlanModal.tsx`
  - `cuttingApi.ts`: POST/GET `/bff/cutting/plans`
- **NestingResultPanel integrálása a CuttingListPage-be:**
  - `src/components/NestingResultPanel.tsx`
  - `GET /bff/cutting/sheets/{cuttingSheetId}/nesting` → elhelyezett részek táblázata
  - Metrikák: felhasznált terület %, hulladék %
  - Ha a `cuttingSheetId` nincs az orders API-ban: skeleton/empty state graceful kezelése
  - `useNestingResult.ts` hook
- `App.tsx`: `/cutting` route

**Teszt:** `CuttingPlansPage.test.tsx`, `NestingResultPanel.test.tsx`

---

### FE-016 — Dashboard overhaul `/` (P2)

**Jelenlegi állapot:** csak welcome szöveg, semmi adat.

**Megvalósítás:**
- 4 stat kártya:
  - Nyitott rendelések (orders API `status=Draft,Submitted`)
  - Készlet — MDF 18mm panel db (inventory stock)
  - Szállítók száma (suppliers GET)
  - Utolsó vágóterv (cutting plans GET today)
- Quick action gombok: + Új rendelés · + Bevételezés · Szállítók →
- Loading skeleton a kártyákon

**Teszt:** `DashboardPage.test.tsx` overhaul

---

### FE-017 — Procurement Orders `/procurement` (P2)

**Létrehozandó:**
- `src/pages/ProcurementPage.tsx` — 2 tab: Rendelések · Szállítások
- **Rendelések tab:** PO lista (szállító neve, státusz, létrehozva)
- `CreatePurchaseOrderModal.tsx` — supplier dropdown (useSuppliers), összeg, várható szállítás
- `RecordDeliveryModal.tsx` — PO kiválasztás, szállított mennyiség, dátum
- `src/api/procurementApi.ts`:
  ```ts
  POST /bff/procurement/orders     → { supplierId, totalAmount, expectedDelivery }
  GET  /bff/procurement/orders/{id}
  POST /bff/procurement/deliveries → { purchaseOrderId, deliveredAt, ... }
  ```
- `App.tsx`: `/procurement` route

**Teszt:** `ProcurementPage.test.tsx`

---

## Build gate (minden task után kötelező)

```bash
npm run test -- --run   # ≥ meglévő teszt szám, 0 fail
npm run build           # 0 TS error, 0 warning
```

---

## DONE feltételek

- [ ] Mind a 6 FE task kész
- [ ] Minden új komponenshez unit teszt
- [ ] `npm run build` 0 error
- [ ] `npm run test -- --run` zöld (min. 90 teszt, +∼30 új)
- [ ] OUTBOX üzenet: DONE az összes task summaryval, tesztszám, commit hash

## Skill / agent instrukció

Használd a `/spaceos-terminal` skillt session elején (inbox olvasás) és DONE outbox írásakor.

A kód megírásához **sub-agent engedélyezett** (`Agent` tool), de minden sub-agent:
- Csak a `spaceos-doorstar-portal/` mappában dolgozzon
- Build gate-et futtasson commit előtt
- Ne írjon inbox/outbox üzenetet — azt csak te írsz

