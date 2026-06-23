---
id: IDEA-20260623-005
title: "2. Assembly szerelési művelet-sorrendezés és útvonalkeresés (assembly.jsx)"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-23
---

# 2. Assembly szerelési művelet-sorrendezés és útvonalkeresés (assembly.jsx)

**Komponens:** `assembly.jsx`
**Típus:** ui-component + state-management
**Prioritás:** medium

Az assembly munkalapok jelenleg csak listázottak. Egy **vizuális drag-and-drop sorrend-editor** szükséges, ahol a szerelésvezetők az egyes szerelési lépéseket átrendezhetik, és egy **prioritás-sáv** (alacsony/közepes/magas) jelzi az ürgető feladatokat. Egy tetszőleges munkalapon belüli **"Nézet optimalizált útvonalként"** gomb végrehajtaná a logikai lépéseket (pl. megelőző alkatrészgyűjtés). LocalStorage-ben tárolva az egyéni sorrend.

**Kapcsolódó fájlok:**
- app-store.jsx (estado: `assemblyOrder[]`, `assemblyPriorities{}`)
- assembly.jsx (UI: sortable list + priority badges + route preview)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
