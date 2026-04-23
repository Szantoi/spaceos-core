# SpaceOS Doorstar Portal — Sprint 3: Teljes üzleti logika UI

**Státusz:** TERVEZETT  
**Létrehozva:** 2026-04-17  
**Cél:** Minden üzleti logika modul UI-on keresztül tesztelhető + valós használati tapasztalat

---

## Kontextus

Sprint 2 lezárult: 214/214 E2E, teljes backend stack deployed. A jelenlegi portal
struktúra: Login, Orders (list + detail + cutting list), Profile. Nincs navigáció a
modulok között, nincs Inventory, Supplier, Cutting Plan UI.

**Sprint 3 célja:** egy gyártási ciklus végigvihető a portálon keresztül:

```
Szállító felvitele → Alapanyag bevételezés → Rendelés létrehozása
→ Kalkuláció → Szabáslista + nesting → Napi vágóplan → Készlet nyomon követés
```

---

## Elérhető backend BFF végpontok (minden late kész)

| BFF útvonal | Backend | Végpont |
|---|---|---|
| `GET /bff/procurement/suppliers` | Port 5006 | Szállítók listája |
| `POST /bff/procurement/suppliers` | Port 5006 | Szállító létrehozás |
| `POST /bff/procurement/orders` | Port 5006 | Beszerzési rendelés |
| `GET /bff/procurement/orders/{id}` | Port 5006 | Bsz. rendelés státusz |
| `POST /bff/procurement/deliveries` | Port 5006 | Szállítás rögzítése |
| `GET /bff/inventory/stock` | Port 5004 | Készlet lekérdezés |
| `GET /bff/inventory/offcuts` | Port 5004 | Maradékok |
| `POST /bff/inventory/movements/inbound` | Port 5004 | Bevételezés |
| `POST /bff/inventory/movements/consumption` | Port 5004 | Felhasználás |
| `GET /bff/inventory/trend` | Port 5004 | Fogyási trend |
| `GET /bff/cutting/sheets/{id}/nesting` | Port 5005 | Nesting eredmény |
| `GET /bff/cutting/sheets/{id}/status` | Port 5005 | Vágólap státusz |
| `GET /bff/cutting/waste` | Port 5005 | Hulladék riport |
| `POST /bff/cutting/plans` | Port 5005 | Napi vágóterv |
| `GET /bff/cutting/plans/{date}` | Port 5005 | Napi vágóterv lekérés |

---

## Sprint 3 feladat bontás

### FE-012: App Navigation Shell (AppLayout)
**Prioritás:** P0 — minden más erre épül

Jelenlegi állapot: nincs alkalmazás-szintű navigáció, minden oldal stand-alone.

**Megvalósítás:**
- `AppLayout.tsx` komponens: bal oldali sidebar + content area
- Sidebar linkek: Dashboard · Rendelések · Készlet · Szállítók · Vágótervek · Profil
- Mobile: hamburger menü (collapse)
- `AppHeader.tsx` refactor: brand logo, user avatar + logout
- Minden `ProtectedRoute` az `AppLayout`-on belül renderel

**Tesztek:** AppLayout unit test (navigation links, active state, collapse)

---

### FE-013: Suppliers page — `/suppliers`
**Prioritás:** P0 — CI-002 megoldása

**Megvalósítás:**
- `SuppliersPage.tsx` — szállítók táblázat (név, email, telefon, cím)
- `CreateSupplierModal.tsx` — új szállító form (name: required, email, phone, address)
- `suppliersApi.ts` — GET/POST `/bff/procurement/suppliers`
- `useSuppliers.ts`, `useCreateSupplier.ts` hooks
- App.tsx: `/suppliers` route hozzáadása

**Tesztek:** SuppliersPage + CreateSupplierModal unit teszt

---

### FE-014: Inventory page — `/inventory`
**Prioritás:** P1

**Megvalósítás:**
- `InventoryPage.tsx` — Tab layout: Készlet · Maradékok · Trend
- **Készlet tab:** material type selector (MDF 18mm / MDF 16mm / HDF 3mm stb.),
  panel count + total area kártyák, tábla a panel stock sorokkal
- **Bevételezés gomb:** `RecordInboundModal.tsx` (materialType, thickness, panelCount, area, reference)
- **Maradékok tab:** offcut lista (materialType, méretek, terület)
- **Trend tab:** fogyási trend táblázat (utolsó 30 nap)
- `inventoryApi.ts` + `useInventoryStock.ts` + `useRecordInbound.ts` hooks

**Tesztek:** InventoryPage unit teszt (tab váltás, loading, error state, modal)

---

### FE-015: Cutting Plans page — `/cutting`
**Prioritás:** P1

**Megvalósítás:**
- `CuttingPlansPage.tsx` — napi vágótervek listája, dátum picker
- `CreateCuttingPlanModal.tsx` — dátum + megjegyzés
- **Nesting integráció a CuttingListPage-be:**
  - `NestingResultPanel.tsx` — vágólap nesting eredménye
  - `GET /bff/cutting/sheets/{cuttingSheetId}/nesting` hívás
  - Elhelyezett részek táblázata (partCode, widthMm, heightMm, x, y pozíció)
  - Hatékonysági metrikák: felhasznált terület %, hulladék %
- **Hulladék riport:** `WasteReportPanel.tsx` az `InventoryPage`-ben vagy külön `/cutting/waste` route-on
- `cuttingApi.ts` + `useCuttingPlan.ts` + `useNestingResult.ts` hooks

**Tesztek:** CuttingPlansPage unit teszt, NestingResultPanel teszt

---

### FE-016: Dashboard overhaul — `/`
**Prioritás:** P2

**Megvalósítás:**
- 4 stat kártya: Nyitott rendelések · Készlet (panel db) · Szállítók száma · Utolsó vágóterv dátuma
- Adatok: meglévő orders API + inventory stock + suppliers GET + cutting plans GET
- Quick action gombok: + Új rendelés · + Bevételezés · + Szállító

**Tesztek:** DashboardPage overhaul teszt (stat kártyák megjelennek, loading state)

---

### FE-017: Procurement Orders — `/procurement`
**Prioritás:** P2

**Megvalósítás:**
- `ProcurementPage.tsx` — két tab: Rendelések · Szállítások
- **Rendelések tab:** PO lista (szállító, státusz, dátum, összeg)
- `CreatePurchaseOrderModal.tsx` — supplier selector (a szállítók listájából), tételek, várható szállítási dátum
- **Szállítás rögzítés:** `RecordDeliveryModal.tsx` — PO ID, szállítási mennyiség, dátum
- `procurementApi.ts` + `usePurchaseOrders.ts` + `useCreatePurchaseOrder.ts` hooks
- App.tsx: `/procurement` route

**Tesztek:** ProcurementPage unit teszt

---

## E2E lefedettség (E2E terminál kísérő feladat)

Új E2E tesztfájlok az Sprint 3 után:
- `44-suppliers.spec.ts` — GET /bff/procurement/suppliers 401+200
- `45-inventory-stock.spec.ts` — GET /bff/inventory/stock + inbound movement
- `46-nesting-api.spec.ts` — GET /bff/cutting/sheets/{id}/nesting
- `47-procurement-orders.spec.ts` — GET/POST /bff/procurement/orders
- `48-cutting-plans.spec.ts` — POST/GET /bff/cutting/plans

---

## INFRA feladat

Portal dist rebuild + deploy (portal.joinerytech.hu) Sprint 3 kód után.

---

## Prioritás összefoglalás

| # | Task | Prioritás | Blokkolás |
|---|---|---|---|
| 1 | FE-012 App Navigation Shell | P0 | Minden FE feladat erre vár |
| 2 | FE-013 Suppliers (CI-002) | P0 | Önálló (API kész) |
| 3 | FE-014 Inventory | P1 | API kész |
| 4 | FE-015 Cutting Plans + Nesting | P1 | API kész |
| 5 | FE-016 Dashboard overhaul | P2 | FE-012 után |
| 6 | FE-017 Procurement Orders | P2 | FE-013 szállítók kell |

**Javasolt sorrend:** FE-012 → FE-013 + FE-014 párhuzamosan → FE-015 → FE-016 + FE-017 párhuzamosan

---

## Sikerkritérium

Egy Doorstar-os felhasználó a portálon keresztül végigviszi:
1. Szállító felvitele
2. Alapanyag bevételezés a szállítótól
3. Ajtórendelés létrehozása + tételek + kalkuláció
4. Cutting lista + nesting eredmény megtekintése
5. Készlet ellenőrzése a fogyás után

**Minimum: 214/214 E2E zöld marad + új tesztek zöldek.**
