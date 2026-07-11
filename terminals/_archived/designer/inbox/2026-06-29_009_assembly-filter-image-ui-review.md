---
processed: 2026-06-29
id: MSG-DESIGNER-009
from: conductor
to: designer
type: task
priority: medium
status: READ
model: haiku
ref: CONSENSUS-2026-06-23
created: 2026-06-29
content_hash: 6b559c281a7e0ada5b66596c5506e719d9506c1d827db685195b43e6eaed86de
---

# Assembly & Katalógus Features - UI/UX Review

## Összefoglalás

3 Frontend feature UI review szükséges a 2026-06-23 konszenzus alapján.

## Feladatok

### 1. Assembly DnD UI — Drag-and-Drop visual design review

**Konzultáció szükséges:**
- Drag-drop visual feedback (cursor, highlight, ghost image)
- Undo button elhelyezése
- Sequence számok megjelenítése
- Haptic feedback UX implikációi (mobilon szükséges jelzés?)

**Frontend task:** MSG-FRONTEND-055 (Assembly DnD section)

**Javaslatok:**
- DnD-kit alapértelmezett stílusai OK-ak
- Undo button: toolbar-ba kerüljön a main control-ok mellett
- Sequence numbers: baloldalt szürke, bold

### 2. Katalógus Filter Persistence — UI consistency

**Konzultáció szükséges:**
- Grid/List view toggle gomb design
- Filter panel megjelenítés localStorage-ből
- Multi-tab sync vizuális jelzése (kell-e notify user-t?)

**Javaslatok:**
- Toggle gombok: aktív állapot highlight
- Filter reset button hozzáadása ha szükséges
- Multi-tab sync: silent, nincs user notification szükséges

### 3. Képoptimalizálás Phase 1 — Skeleton loading design

**Konzultáció szükséges:**
- Shimmer animation sebesség (2s OK?)
- Error state badge design
- Product card aspect ratio (4:3) OK-e a portálon?

**Javaslatok:**
- Shimmer: 2s OK
- Error badge: szürke "Kép nem elérhető" OK
- Aspect ratio: erősítsd meg mobil layoutnál

---

## Elfogadási kritériumok

- ✅ UI review közzétéve (Frontend task szakaszok)
- ✅ Javaslatok konkrét (colors, spacing, interactions)
- ✅ Figma update ha szükséges

## Referencia

**Konsenzus:** `/opt/spaceos/docs/planning/consensus/2026-06-23_consensus.md`
**Backend task:** `MSG-BACKEND-074`
**Frontend task:** `MSG-FRONTEND-055`
