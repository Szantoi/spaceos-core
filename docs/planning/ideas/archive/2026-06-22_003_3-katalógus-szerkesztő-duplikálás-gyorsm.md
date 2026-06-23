---
id: IDEA-20260622-003
title: "3. Katalógus-szerkesztő „Duplikálás" gyorsművelet"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 3. Katalógus-szerkesztő „Duplikálás" gyorsművelet

**Komponens:** `catalog-manager.jsx` → ProductRow kontextus-menü
**Típus:** state-management
**Prioritás:** medium

A **Catalog Manager** termék-táblázatában (minden sor végén) adj egy **⋯ gomb** → dropdown menü:
- **Szerkesztés** (már van)
- **Duplikálás** (új)

A duplikálás:
1. Klónozza az aktuális termék összes adatát (név + SKU → `SKU-COPY`, ár, kép, kategória)
2. Betölti az edit modálba (új termékként, `id=null`)
3. Felhasználó módosítja + menti

Szimulációs logika: `duplicateProduct(productId)` az `app-store.jsx`-ben.

**Kapcsolódó fájlok:**
- `catalog-manager.jsx`
- `app-store.jsx` (új `duplicateProduct` action)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
