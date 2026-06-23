---
id: IDEA-20260622-006
title: "3. Catalog World View — Leszármazottak (BOŐ) fa és kritikus komponens jelölés"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 3. Catalog World View — Leszármazottak (BOŐ) fa és kritikus komponens jelölés

**Komponens:** catalog-world-view.jsx  
**Típus:** ui-component + styling  
**Prioritás:** medium

A `catalog-world-view.jsx` jelenleg a kategóriákat mutatja. Egy új **"Összeépítettség"** mód (toggle), amely egy **indented Tree** layout-ban mutatja az assembly–part relációkat (pl. "Konyhabútor XL" → [szekrény, ajtó, vasalat, …]). Minden leaf-komponensnél egy **risk badge** (pl. 🔴 *"Egyedüli forrás"* vagy 🟡 *"Alacsony készlet"*), amely az `suppliers` és `warehouseItems` alapján számított. Segít a termékvezető-nek azonosítani a kritikus beszerzési pontokat.

**Kapcsolódó fájlok:**
- catalog-world-view.jsx
- app-store.jsx (computeSupplyRisk util)
- data-catalog.js (assembly-part join)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
