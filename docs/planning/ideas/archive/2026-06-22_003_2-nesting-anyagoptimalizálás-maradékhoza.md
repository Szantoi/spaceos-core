---
id: IDEA-20260622-003
title: "2. Nesting / Anyagoptimalizálás — Maradékhozam export (CSV + műsortervbe integrálás)"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 2. Nesting / Anyagoptimalizálás — Maradékhozam export (CSV + műsortervbe integrálás)

**Komponens:** `page-nesting.jsx` / `configurator` világ
**Típus:** api-integration + ui-component
**Prioritás:** medium

A 2D nesting nézet (amely már működik az `offcuts[]` maradékanyag-raktárral) kap egy **"Maradék-naplózás"** gomb, amely az aktuális nesting után maradt anyagdarabokat (hossz, szélesség, típus) CSV-ként exportálja és automatikusan **beviteli javaslatot** küld a `catalog-world-view.jsx` Készlet-moduljához, mint ún. "anyagtöbblet-raktári szócikk". Ez csökkenti az anyagvesztést és az ásványvizet egy láncon belül.

**Kapcsolódó fájlok:**
- `page-nesting.jsx` (offcuts export + CSV builder)
- `catalog-world-view.jsx` (inventory intake suggestion modal)
- `data-nesting.js` (offcuts array + export formatter)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
