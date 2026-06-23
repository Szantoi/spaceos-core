---
id: IDEA-20260622-005
title: "1. Beszállítói Portal — RFQ-Státusz Filter & Gyorskeresés"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 1. Beszállítói Portal — RFQ-Státusz Filter & Gyorskeresés

**Komponens:** `page-supplier.jsx` (SupplierPortal)
**Típus:** ui-component + state-management
**Prioritás:** high

A beszállítói nézet RFQ-listájához add hozzá egy **státusz-filter bar**-t (szűrés: Nyitott, Ajánlat Beadva, Lezárva) és egy **valós idejű keresőmezőt** (RFQ szám / termék név alapján). Az `app-store.jsx`-ben hozz létre egy `supplierFilterState` objektumot (`statusFilter`, `searchQuery`), és a lista végigmenete során alkalmazz `.filter()` logikát. A UI-ban a Tailwind `bg-blue-50` háttérrel egy sortáblázat feletti filtermezőt helyezz el.

**Kapcsolódó fájlok:**
- `page-supplier.jsx`
- `app-store.jsx`
- `data-supplier.js` (ha szükséges)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
