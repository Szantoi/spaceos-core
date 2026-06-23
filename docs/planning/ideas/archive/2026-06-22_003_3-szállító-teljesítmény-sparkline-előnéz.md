---
id: IDEA-20260622-003
title: "3. Szállító-teljesítmény sparkline előnézete (Supplier Portal)"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 3. Szállító-teljesítmény sparkline előnézete (Supplier Portal)

**Komponens:** `catalog-world-view.jsx` / új supplier-perf-badge
**Típus:** ui-component + state-management
**Prioritás:** medium

A katalógus világnézetben vagy a beszállítók listájában minden szállító mellett egy **mini sparkline** (6 hónapos on-time delivery %), valamint egy **toggle** az azt tartalmazó popoverhez (átlag leadtime, quality score, cost trend). Adatok: szimulált `sim.suppliers[].performance` objektum. Ez a Partner-kapcsolat nézet (4.11) adatait előkészíti.

**Kapcsolódó fájlok:**
- `catalog-world-view.jsx`
- `app-store.jsx`
- `data-catalog.js` (supplier metrics)

---
*Automatikusan generálva a JoineryTech prototípusból*
