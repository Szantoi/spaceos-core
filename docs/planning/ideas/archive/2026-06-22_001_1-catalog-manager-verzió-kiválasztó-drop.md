---
id: IDEA-20260622-001
title: "1. Catalog Manager — Verzió-kiválasztó dropdown + inline diff preview"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 1. Catalog Manager — Verzió-kiválasztó dropdown + inline diff preview

**Komponens:** `catalog-manager.jsx`  
**Típus:** ui-component + state-management  
**Prioritás:** high

A katalógus verziókezelésben (4.6, 2-es már feldolgozva) hiányzik a **gyors verzió-összehasonlító** az edit-nézeten belül. Új feature: amikor egy cikket szerkesztünk, a **"Verzió"** mező legyen egy **interactive dropdown**, amely:
- Az utolsó 3 verzió összefoglalóját mutatja (dátum + módosító + 1-2 fő változás szöveggel)
- Rákattintva inline diff-panel nyílik a jelenlegi vs. kiválasztott verzió között (highlight: árváltozás, méretek, anyag)
- **"Visszaállít erre"** gomb az üdvösséghez

Segíti: gyors módosítás-visszakövetés, visszaállítás 1-2 kattintásban.

**Kapcsolódó fájlok:**
- `catalog-manager.jsx`
- `app-store.jsx` (verzió-FSM lekérése)
- `data-catalog.js` (verzió-diff kalkuláció)

---

---
*Automatikusan generálva a JoineryTech prototípusból*
