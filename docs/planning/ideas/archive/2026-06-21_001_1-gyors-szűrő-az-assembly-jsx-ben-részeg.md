---
id: IDEA-20260621-001
title: "1. Gyors-szűrő панель az `assembly.jsx`-ben (részegység-lista kolapszálható kategóriákkal)"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-21
---

# 1. Gyors-szűrő панель az `assembly.jsx`-ben (részegység-lista kolapszálható kategóriákkal)

**Komponens:** `assembly.jsx` → FilterPanel subcomponent  
**Típus:** ui-component + state-management  
**Prioritás:** high

Az `assembly.jsx` jelenleg teljes listát mutat az összes részegységből. Adjunk hozzá egy **kolapszálható szűrő-sávot** kategóriánként (pl. "Keretrendszer", "Felület", "Vasalat", "Egyéb"), amely:
- Tailwind `collapse` / toggle osztályokkal működik
- Szűrt lista real-time frissül
- Checkboxes az "összes/egyik sem" gyorsválasztáshoz
- Talp-fejléc marad, csak a középső tartalom scrollozódik

**Kapcsolódó fájlok:**
- `assembly.jsx`
- `app-store.jsx` (szűrési állapot: `filterByCat`)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
