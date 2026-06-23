---
id: IDEA-20260621-002
title: "2. KPI-kártya (élő) a `catalog-world-view.jsx`-ben — beszállítói megbízhatóság mutatók"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-21
---

# 2. KPI-kártya (élő) a `catalog-world-view.jsx`-ben — beszállítói megbízhatóság mutatók

**Komponens:** `catalog-world-view.jsx` → CatalogKPICard  
**Típus:** ui-component + state-management  
**Prioritás:** medium

A katalógus-világ-nézet tetejéhez adj egy **3-4 soros KPI-kártyát**, amely az `sim.partners` (beszállítók) aggregált mutatóit jeleníti meg:
- **On-time %** (szállítások időre % — a PO `actualDelivery <= requiredDelivery`)
- **Ár-stabilitás** (hány % az árváltozás az elmúlt 30 napban)
- **Minőségi értékelés** (átlag pontszám a `qualityRating`-ből)
- **Aktív szállítók** (hányan vannak `active` státuszban)

Tárolódjon az adatérték **5 percenként frissülve** (`setInterval`), ha az app nyitott. Zöld/sárga/piros indikátorok.

**Kapcsolódó fájlok:**
- `catalog-world-view.jsx`
- `app-store.jsx` (partner-KPI engine)
- `data-ehs.js` (vagy `data-crm.js`) a számításnak

---

---
*Automatikusan generálva a JoineryTech prototípusból*
