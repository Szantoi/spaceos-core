---

## TOP 2: Beszállítói portál — PO-nyugta + ASN nyomkövetés (QR-kód)

**Miért top:**
- **Valós gyártási pain point**: Papíralapú fuvarlevél ↔ rendszer inkonzisztencia a bútoripari beszállítói láncban
- **Mobil-first**: QR-kód szkenner = telefon kamera, munkavállalók már ismerik a mintát
- **Offline-tűrő**: QR-kód működik rossz internet mellett is (később sync)
- **Iparági standard hiány SpaceOS-ben**: Más ERP-kben (SAP, Odoo) ez alap, nálunk nincs
- **Backend már van**: `sim.partners[].orders` + `po.status` létezik, csak UI hiányzik

**Webes minták:**

Kutatás után (*"supplier portal ASN tracking QR code", "manufacturing inbound receipt mobile", "purchase order acknowledgement UX"*):

1. **SAP Ariba Supplier Portal** — PO confirmation + ASN generation + barcode tracking
   - **Minta:** 3-lépéses folyamat (PO accept → Ship confirm + ASN → Receipt acknowledge)
   - **QR:** ASN number + PO reference + Expected delivery date

2. **Odoo Purchase Mobile** — Mobilos bevételezés QR/barcode szkennerrel
   - **Minta:** Camera API → beolvas → auto-match PO → quantity confirm → done
   - **Offline:** LocalStorage cache, sync amikor van net

3. **Shopify POS Receipt** — QR-kód a nyugtán, utólagos visszakereséshez
   - **Minta:** QR = order ID + verification hash (egyszerű, nem kell crypto)

4. **FedEx/UPS Tracking** — Mobil-optimalizált ASN tracking page
   - **Minta:** Timeline view (Ordered → Shipped → In Transit → Delivered)

**Közös UX minta:**
```
Beszállító oldal (desktop):
┌─────────────────────────────┐
│ PO #12345 — Feladás         │
│ ┌─────────────────────────┐ │
│ │   [QR-kód itt]          │ │ ← ASN + PO + Date
│ │   ASN-2026-0622-001     │ │
│ └─────────────────────────┘ │
│ [Nyomtatás fuvarlevélre]    │
└─────────────────────────────┘

Inbound oldal (mobil):
┌─────────────────────────────┐
│ Csomag bevételezés          │
│ ┌─────────────────────────┐ │
│ │   📷 Szkennelj QR-ot    │ │ ← Kamera gomb
│ └─────────────────────────┘ │
│ Vagy: [Kézi ASN bevitel]    │
└─────────────────────────────┘
```

**Javasolt megközelítés:**

```
Phase 1 — QR generálás (1 nap):
├── page-supplier.jsx: "Feladás" gomb → QR-kód modal
├── qrcode.js library (lightweight, 5KB)
└── ASN format: "ASN-{date}-{seq}" + PO ref + hash

Phase 2 — Mobil szkenner UI (1-2 nap):
├── page-inbound.jsx: új "Szkennelés" nézet
├── Mock camera input (input file type="file" accept="image/*")
├── jsQR library → dekódolás
└── Auto-match PO + quantity confirm dialog

Phase 3 — Offline támogatás (1 nap):
├── LocalStorage: pending receipts queue
├── Background sync (amikor van net)
└── Conflict resolution (ha PO már befejezve)

Phase 4 — Beszállítói visszaigazolás (0.5 nap):
├── Email notification: "A(z) ASN-xxx beérkezett"
└── Supplier portal: "Leszállított" státusz + timestamp
```

**Teljes becslés:** 3.5-4.5 nap, egyből használható az első 2 nap után.

---

## TOP 3: Partner Cockpit — Teljesítmény KPI widget (élő szűrés)

**Miért top:**
- **Adatvizualizáció = döntéstámogatás**: Gyártásvezetők szeretik a számokat, de csak ha relevánsak
- **Már létező adatmodell**: `sim.partners[].orders` tömb készen van, csak aggregálni kell
- **Gyors win**: Pure frontend fejlesztés, nincs backend módosítás
- **Iparági elvárás**: Minden modern ERP/CRM-ben van partner performance dashboard (Salesforce, HubSpot, Monday)
- **Mobil-barát**: Tailwind progress bar-ok már reszponzívak

**Webes minták:**

Kutatás után (*"supplier performance dashboard KPI", "partner scorecard UX", "manufacturing vendor rating"*):

1. **Salesforce Partner Portal — Scorecard tab**
   - **Metrikák:** On-time delivery %, Quality rating (defect rate), Response time (avg)
   - **Szűrés:** Date range picker (Last 30/60/90 days, Custom)
   - **Vizualizáció:** Progress ring + trend arrow (↑ jobb, ↓ rosszabb vs előző periódus)

2. **SAP Supplier Evaluation**
   - **Metrikák:** Delivery performance, Price variance, Quality score
   - **Szűrés:** Material group, Plant, Time period
   - **Drill-down:** Klikk a metrikára → részletes order lista

3. **Odoo Purchase Dashboard**
   - **Metrikák:** Orders on-time, Avg lead time, Open POs count
   - **Szűrés:** Status (RFQ/Confirmed/Done), Date, Product category
   - **Export:** CSV/PDF riport

4. **Monday.com Supplier Board**
   - **Kanban + Metrics hibrid:** Cards (orders) + Summary bar (KPIs)
   - **Real-time:** Auto-refresh minden 30 sec
   - **Color coding:** 🟢 >90%, 🟡 70-90%, 🔴 <70%

**Közös UX minta:**
```
┌──────────────────────────────────────────┐
│ Partner Teljesítmény                     │
│ ┌──────────────┬─────────────────────┐  │
│ │ 📅 Időszak:  │ [Last 30 days ▾]    │  │ ← Dropdown
│ │ 📦 Státusz:  │ [All ▾] Teljesített │  │ ← Multi-select
│ └──────────────┴─────────────────────┘  │
│                                          │
│ ┌──────────────────────────────────────┐│
│ │ Időben teljesítés:  87% ████████░░   ││ ← Progress bar
│ │ Átl. átfutás:       12 nap (↓ -2)    ││ ← Trend vs előző
│ │ Minőségi ráta:      94% █████████░   ││
│ └──────────────────────────────────────┘│
│ [Részletes riport letöltése (CSV)]      │
└──────────────────────────────────────────┘
```

**Javasolt megközelítés:**

```
Phase 1 — KPI számítás + szűrés (1 nap):
├── data-partners.js: calculateKPIs(partnerId, filters)
│   ├── On-time %: orders.filter(delivered <= expected).length / total
│   ├── Avg lead time: mean(delivered_date - order_date)
│   └── Quality %: orders.filter(no_issues).length / total
├── app-store.jsx: filterPartnerOrdersByPeriod(partnerId, days)
└── Tailwind progress component (már van, újrahasználás)

Phase 2 — UI komponens (1 nap):
├── page-partner.jsx: KPI Card Component
│   ├── Date range picker (30/60/90/Custom)
│   ├── Status multi-select (Open/Closed/Late)
│   └── Real-time számítás (useEffect dependency: filters)
└── Trend arrow (compare vs previous period)

Phase 3 — Export + Drill-down (0.5 nap):
├── CSV export button: KPI summary + order list
└── Klikk metrikára → filtered order table (már létező component)
```

**Teljes becslés:** 2.5 nap, első nap után már működik alapszinten.

---

## Elvetett ötletek (és miért)

### ❌ Nesting / Anyagoptimalizálás — Maradékhozam export

**Miért nem TOP 3:**
- **Domain specifikus**: Csak a nesting modul használói profitálnak (szűk célcsoport)
- **Backend integráció hiányzik**: A `catalog-world-view.jsx` inventory intake még nem production-ready (proof-of-concept fázis)
- **ROI bizonytalanság**: Nem tudjuk, a Doorstar asztalosipari munkafolyamat tényleg használja-e a nesting modult napi szinten
- **Alternatíva létezik**: Manuális CSV export + Excel → raktár-bevitelezés (workaround működik)

**Mikor érdemes újragondolni:**
- Ha a nesting modul aktív használatba kerül (>10 session/hét)
- Ha a catalog inventory API stabilizálódik
- Ha a material waste tracking üzleti KPI-vá válik

---

## Összegzés — Prioritási sorrend

| # | Ötlet | Becsült ROI | Implementációs nehézség | Sürgősség |
|---|-------|------------|------------------------|-----------|
| 🥇 | **Autonóm Kutatási Rendszer** | ⭐⭐⭐⭐⭐ (minden terminált gyorsít) | 🟡 Közepes (infrastruktúra) | 🔴 Magas (meta-feature) |
| 🥈 | **QR-kód ASN tracking** | ⭐⭐⭐⭐ (beszállítói pain point) | 🟢 Könnyű (frontend + mock) | 🟡 Közepes (operatív) |
| 🥉 | **Partner KPI widget** | ⭐⭐⭐ (döntéstámogatás) | 🟢 Könnyű (pure frontend) | 🟢 Alacsony (nice-to-have) |

**Ajánlott sorrend:**
1. **Autonóm kutatás** — bekapcsolás és monitoring (2 nap)
2. **QR ASN** — gyors win a beszállítói folyamatban (4 nap)
3. **Partner KPI** — amikor van 2-3 nap puffer (opcionális)