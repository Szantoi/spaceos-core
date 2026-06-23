---
id: IDEA-20260621-003
title: "3. Inline-szerkesztés (edit-mode toggle) a `catalog-manager.jsx`-ben — árumérték + kedvezmény"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-21
---

# 3. Inline-szerkesztés (edit-mode toggle) a `catalog-manager.jsx`-ben — árumérték + kedvezmény

**Komponens:** `catalog-manager.jsx` → CatalogItemRow edit toggle  
**Típus:** ui-component + state-management  
**Prioritás:** medium

A **Katalóguskezelő** táblázat sorai jelenleg valszínűleg read-only. Adjunk hozzá egy **"Szerkesztés" gombot** soronként, amely:
- Az adott terméksor **árát** (`unitPrice`) és **aktuális kedvezményt** (`discount%`) inline-szerkeszthetővé teszi (input mezők)
- **Mentés** gomb elmentesíti az `updateCatalogItem(id, price, discount)` hívásra
- **Mégse** gomb visszaveti a változásokat
- Tailwind `bg-yellow-50` háttér az aktív szerkesztés alatt
- Csak `ehs.manage` (vagy új `catalog.edit`) engedéllyel

**Kapcsolódó fájlok:**
- `catalog-manager.jsx`
- `app-store.jsx` (`updateCatalogItem` action)

---

## Kiválasztási javaslat
**Kezdésre ajánlott sorrend:**
1. **#1 Gyors-szűrő** (leggyorsabb, rögtön látható UX-javulás)
2. **#3 Inline-szerkesztés** (funkcionális, gyakran használt)
3. **#2 KPI-kártya** (szép, de vizuális – később)

---
*Automatikusan generálva a JoineryTech prototípusból*
