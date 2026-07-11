---
id: IDEA-20260623-006
title: "3. EHS-munkavédelem gyors-alosztályozó/scoring widget (új page-ehs-dashboard.jsx)"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-23
---

# 3. EHS-munkavédelem gyors-alosztályozó/scoring widget (új page-ehs-dashboard.jsx)

**Komponens:** `page-ehs-dashboard.jsx` (új)
**Típus:** ui-component + state-management
**Prioritás:** high

Az EHS világ (4.9) meglévő, de hiányzik a **szervezet-szintű kockázati dashboard**. Egy nagy, piros/sárga/zöld 5×5-ös kockázat-mátrix grid szükséges (valószínűség × súlyosság), amelyen a bejelentett esetek/értékelések kattinthatóan mozgathatóak. Összefoglalás-kártya: "5 nyitott CAPA", "2 maradék-kockázat felülvizsgálatra vár", éves trendi mikro-grafikon. Integrálódik az `ehs` státusszal (`data-ehs.js`-ből).

**Kapcsolódó fájlok:**
- data-ehs.js (datos: incidents, riskAssessments)
- page-ehs-dashboard.jsx (új UI: risk matrix + summary cards + trend micro-chart)
- app-store.jsx (estado: `ehsDashboardView`)

---

## Megjegyzés
Mindhárom ötlet a **meglévő entitások** alapján épül, új entitások nélkül. Az 1. és 3. közvetlenül a prototípus-hiányokat címzi, a 2. az assembly-vezérelt termelésflow-t javítja.

---
*Automatikusan generálva a JoineryTech prototípusból*
