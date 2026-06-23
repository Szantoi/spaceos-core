---
id: IDEA-20260622-006
title: "2. Katalógus-szerkesztő — Gyors Duplikálás + Inline Szerkesztés"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 2. Katalógus-szerkesztő — Gyors Duplikálás + Inline Szerkesztés

**Komponens:** `catalog-manager.jsx`
**Típus:** ui-component + state-management
**Prioritás:** high

Az **Katalógus-szerkesztő** táblázatán belül add meg a **duplikálás ikont** (copy gomb) minden terméksornál, amely 1 kattintásra létrehoz egy új terméket az aktuális adatokkal (`data.createProduct(origProduct + { id: newId })` jellegű logika). Emellett implementálj **inline szerkesztést** (dupla kattintás a cellán) a legfontosabb mezőkhöz (név, ár, készlet) — a módosítások `localStorage`-be azonnal mentsek (`updateProduct`). A modal helyett Tailwind `focus:outline-none` inputok a táblázatban.

**Kapcsolódó fájlok:**
- `catalog-manager.jsx`
- `app-store.jsx`
- `data-catalog.js`

---

---
*Automatikusan generálva a JoineryTech prototípusból*
