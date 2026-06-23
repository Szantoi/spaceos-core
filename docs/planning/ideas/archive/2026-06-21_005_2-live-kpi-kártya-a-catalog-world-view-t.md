---
id: IDEA-20260621-005
title: "2. Live KPI-kártya a Catalog World View tetejére"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-21
---

# 2. Live KPI-kártya a Catalog World View tetejére

**Komponens:** catalog-world-view.jsx
**Típus:** ui-component + state-management
**Prioritás:** high

A termékkatalógus fölé helyezz egy 4 cellás KPI-panelt (Termékok száma / Aktív SKU-k / Átlagár / Készlet-érték), amely a `sim.products` és `sim.inventory` alapján számított. A számok real-time frissülnek, ha új terméket adnak hozzá vagy módosítanak (localStorage change event).

**Megvalósítás:**
- `catalog-world-view.jsx`-ben új `<div className="grid grid-cols-4 gap-4 mb-6">` rész
- 4 kártya Tailwind box-shadow + gradient háttérrel
- Számítási függvény: `calculateCatalogKPIs()` az `app-store`-ban (termékeik száma, készlet-aggregáció)
- `useEffect()` localStorage change listener-rel

**Kapcsolódó fájlok:**
- catalog-world-view.jsx
- app-store.jsx

---

---
*Automatikusan generálva a JoineryTech prototípusból*
