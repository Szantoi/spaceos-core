---
id: IDEA-20260623-001
title: "1. Dinamikus Katalógus Filter-panel szinkronizálása a localStorage Store-val"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-23
---

# 1. Dinamikus Katalógus Filter-panel szinkronizálása a localStorage Store-val

**Komponens:** `catalog-manager.jsx` + `app-store.jsx`
**Típus:** state-management
**Prioritás:** high

A `catalog-manager.jsx`-ben a felhasználó által beállított szűrők (ár, méret, anyag, készlet) jelenleg nem perzisztálódnak. Hozz létre egy `saveCatalogFilters()` / `loadCatalogFilters()` függvényt az `app-store.jsx`-ben, amely a kiválasztott szűrőket az `localStorage`-ba menti és a komponens mount-jakor visszatöltve újra alkalmazza. Ez lehetővé teszi a keresési állapot megtartását az oldal frissítésénél.

**Kapcsolódó fájlok:**
- `catalog-manager.jsx`
- `app-store.jsx`
- `catalog-world-view.jsx`

---

---
*Automatikusan generálva a JoineryTech prototípusból*
