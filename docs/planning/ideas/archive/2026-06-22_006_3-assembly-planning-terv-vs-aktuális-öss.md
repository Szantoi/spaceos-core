---
id: IDEA-20260622-006
title: "3. Assembly Planning — Terv-vs-Aktuális Összehasonlítás Split View"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 3. Assembly Planning — Terv-vs-Aktuális Összehasonlítás Split View

**Komponens:** `assembly.jsx`  
**Típus:** ui-component  
**Prioritás:** medium

Az `assembly.jsx` jelenleg az aktuális munkalapokat mutatja. Új feature: egy toggle-gomb (bal oldal: **tervezett összeállítás** az RFQ/opportunity-ból, jobb oldal: **valós munkafolyamat**). Az eltérések highlight-olva: késedelmi órák, máshogy kijelölt csapattag, fázis-drift. Ez az assembly-szint **progress tracking** vizualizálása; `app-store.jsx`-ben a `planRef` metadatát használja fel, egy `<div className="grid grid-cols-2">` layout tesz meg a munkát.

**Kapcsolódó fájlok:**
- `assembly.jsx`
- `data-assembly.js` (planRef hozzáadás)
- `app-store.jsx` (status & plan-check függvény)

---

## Összefoglalás
Mindhárom ötlet:
✅ Meglévő entitásra épít (nem új adat-séma)  
✅ 1–2 óra munka  
✅ localStorage-commit-képes (szimulált)  
✅ Tailwind UI, React state-alapú  
✅ A prototípus design-irányát követi (card-layout, toggle, grid, audit-log)

---
*Automatikusan generálva a JoineryTech prototípusból*
