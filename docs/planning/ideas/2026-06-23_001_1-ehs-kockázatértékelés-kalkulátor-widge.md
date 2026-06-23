---
id: IDEA-20260623-001
title: "1. EHS Kockázatértékelés Kalkulátor Widget"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-23
---

# 1. EHS Kockázatértékelés Kalkulátor Widget

**Komponens:** `page-ehs.jsx` → Kockázatértékelés szekció  
**Típus:** ui-component + state-management  
**Prioritás:** high

A 5×5-ös kockázatmátrix (valószínűség × súlyosság) interaktív, real-time pontszámító widget, amely a `ehs.riskAssessments` státuszát azonnal frissíti. A felhasználó két slider-t húz (valószínűség 1-5, súlyosság 1-5), a widget kiszámolja a maradék-kockázat sávot (zöld/sárga/piros), és a kijelzőn megjelenik az ISO 45001 „éves felülvizsgálat szükséges" státusz. Tailwind grid + transition-animáció.

**Kapcsolódó fájlok:**
- `page-ehs.jsx`
- `data-ehs.js`
- `app-store.jsx` (ehs.riskAssessments state)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
