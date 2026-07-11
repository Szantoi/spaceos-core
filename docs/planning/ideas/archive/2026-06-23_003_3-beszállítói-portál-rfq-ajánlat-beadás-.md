---
id: IDEA-20260623-003
title: "3. Beszállítói Portál — RFQ Ajánlat Beadás Formja"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-23
---

# 3. Beszállítói Portál — RFQ Ajánlat Beadás Formja

**Komponens:** `page-supplier.jsx` → Ajánlatadás szekció  
**Típus:** ui-component + state-management  
**Prioritás:** medium

A beszállítónak szóló önkiszolgáló forma az RFQ megoldáshoz: táblázat az aktív RFQ-kkal (szűrt `supplierName()`-re), minden sorban egy "Ajánlatot adok" gomb → modal → tételsor-mennyiség + Ár (Ft) + Szállítási idő (nap) + Megjegyzés + "Beküldés" → `submitSupplierBid` státusza `ajanlat-bejegyzett` lesz. A beküldés után egy zöld ✓ feedback és a modal bezár.

**Kapcsolódó fájlok:**
- `page-supplier.jsx`
- `app-store.jsx` (rfqs state, submitSupplierBid action)
- `data-nesting.js` (RFQ-tételek referencia)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
