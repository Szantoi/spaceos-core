---
id: IDEA-20260622-005
title: "1. Assembly-lista csoportosítás és gyorsszűrés"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 1. Assembly-lista csoportosítás és gyorsszűrés

**Komponens:** assembly.jsx
**Típus:** state-management | ui-component
**Prioritás:** high

Az `assembly.jsx` jelenleg lineáris listázza az összeállításokat. Adjunk hozzá egy **toggle-gomb csoportot** (Státusz szerinti fülekre: "Aktív", "Befejezett", "Paused") és egy **egysorosú keresőmezőt** (assembly név/kód), amely szűri a `sim.assemblies` tömböt lokális state-ben. A szűrésnek azonnal frissülnie kell, anélkül, hogy a backend-et terhelné.

**Kapcsolódó fájlok:**
- assembly.jsx
- app-store.jsx (a sim.assemblies adatstruktúrához)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
