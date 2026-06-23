---
id: IDEA-20260621-004
title: "1. Dinamikus RFQ-szűrő az Assembly nézetben"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-21
---

# 1. Dinamikus RFQ-szűrő az Assembly nézetben

**Komponens:** assembly.jsx
**Típus:** state-management + ui-component
**Prioritás:** high

Az Assembly listában jelenleg statikus az RFQ-megjelenítés. Add egy gyors szűrő-sávot (szállító, dátum-tartomány, státusz) amely a `sim.rfqs`-t real-time szűri localStorage-ből. A szűrő-state helyi (React state, nem globális), de a kiválasztott RFQ-ra kattintás az `app-store`-ba persistálja az „aktív assembly"-t.

**Megvalósítás:**
- `assembly.jsx`-ben új `useState([filters])` + 3 input-mező (szállító dropdown, dátum-tartomány, státusz multi-select)
- `Array.filter()` az RFQ-listán a filter-objektum alapján
- Kattintás → `setActiveAssembly()` (globális store frissítés)

**Kapcsolódó fájlok:**
- assembly.jsx
- app-store.jsx

---

---
*Automatikusan generálva a JoineryTech prototípusból*
