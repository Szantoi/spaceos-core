---
id: IDEA-20260621-006
title: "3. Inline szerkesztés a Catalog Manager táblázatban (Descripton oszlop)"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-21
---

# 3. Inline szerkesztés a Catalog Manager táblázatban (Descripton oszlop)

**Komponens:** catalog-manager.jsx
**Típus:** ui-component + state-management
**Prioritás:** medium

A terméklista Description oszlopában a szöveg kattintáskor inline szerkesztéshető legyen (edit-mode toggle). ESC/Enter lezárja, Enter mentegeti a `sim.products`-ba. A sor hover-en egy ceruza-ikon jelenik meg (hint).

**Megvalósítás:**
- `catalog-manager.jsx`-ben `useState({editRowId: null, editValue: ""})` per sor
- A Description cella kattintáskor `contentEditable="true"` vagy egy input-mező
- onBlur/onKeyDown: ESC → exit, Enter → `updateProduct()` az `app-store`-ban
- Tailwind `hover:bg-gray-100` + ceruza-ikon (feather, edit-2)

**Kapcsolódó fájlok:**
- catalog-manager.jsx
- app-store.jsx

---

**Megjegyzés:** Mindhárom ötlet a **meglévő** szimuláció-data (`localStorage`-ben) felhasználásával működik, nem igényel new backend-entitást.

---
*Automatikusan generálva a JoineryTech prototípusból*
