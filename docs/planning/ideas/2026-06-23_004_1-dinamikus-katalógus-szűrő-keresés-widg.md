---
id: IDEA-20260623-004
title: "1. Dinamikus katalógus szűrő + keresés widget (catalog-world-view.jsx)"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-23
---

# 1. Dinamikus katalógus szűrő + keresés widget (catalog-world-view.jsx)

**Komponens:** `catalog-world-view.jsx`
**Típus:** ui-component + state-management
**Prioritás:** high

A jelenlegi katalógus-nézet statikus. Szükséges egy **interaktív szűrő sáv** (kategória, ár-tartomány, készlet-státusz) + **real-time keresőmező**, amely frissíti az `app-store.jsx`-ben a `catalogFilters` állapotot. A szűrés a `catalog[]` tömb méretét csökkenti, a találati szám pedig live megjelenik. Tailwind chip-k a kiválasztott szűrőkhöz + "Szűrés törlése" gomb.

**Kapcsolódó fájlok:**
- app-store.jsx (estado: `catalogFilters`, `filteredCatalog()`)
- catalog-world-view.jsx (UI: input + chips + results count)
- catalog-manager.jsx (data source)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
