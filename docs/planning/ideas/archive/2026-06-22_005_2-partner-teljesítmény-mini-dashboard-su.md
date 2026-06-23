---
id: IDEA-20260622-005
title: "2. **Partner-Teljesítmény Mini Dashboard (Supplier Portal Kimeneti oldal)**"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 2. **Partner-Teljesítmény Mini Dashboard (Supplier Portal Kimeneti oldal)**

**Komponens:** `page-supplier.jsx` → új `SupplierPerformanceWidget`  
**Típus:** ui-component + state-management  
**Prioritás:** high

A beszállítói portál (4.10) főoldalán egy "Ön teljesítménye" kártya szükséges: **4 KPI sparkline-nal** (Teljesítési pontosság %, Minőség-átlag, Válaszidő napok, Fizetett számlák %). Szűrve az aktuális `supplierName()`-re a `sim.handshakes` + `sim.orders` alapján. Zöld/sárga/piros band az SLA alapján. Valós eset: beszállító bejelentkezve látja: "90% pontosság, 2 nap alatt válasz".

**Kapcsolódó fájlok:**
- `app-store.jsx` → `calculateSupplierKpis(name)` új selector
- `page-supplier.jsx` → widget integráció
- `data-handshakes.js` / `data-orders.js` → ref-data

---

---
*Automatikusan generálva a JoineryTech prototípusból*
