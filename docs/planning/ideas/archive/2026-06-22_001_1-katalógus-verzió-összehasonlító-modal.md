---
id: IDEA-20260622-001
title: "1. Katalógus-verzió összehasonlító modal"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 1. Katalógus-verzió összehasonlító modal

**Komponens:** `catalog-manager.jsx`
**Típus:** ui-component + state-management
**Prioritás:** high

A katalógus-módosítások Change Log nézete mellett egy **side-by-side diff modal** implementálása, ahol a felhasználó az aktuális verzió és egy régebbi verzió között láthatja az eltéréseket (ár, leírás, SKU, méret). A modal becsukásakor az eredeti nézetre tér vissza. State: `app-store.jsx`-ben egy `compareVersions(catalogId, v1, v2)` action.

**Kapcsolódó fájlok:**
- `catalog-manager.jsx`
- `app-store.jsx`
- `data-catalog.js` (version history)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
