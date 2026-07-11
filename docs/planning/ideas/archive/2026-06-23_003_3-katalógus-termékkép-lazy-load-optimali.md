---
id: IDEA-20260623-003
title: "3. Katalógus Termékkép Lazy-load Optimalizálása & Fallback Badge"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-23
---

# 3. Katalógus Termékkép Lazy-load Optimalizálása & Fallback Badge

**Komponens:** `catalog-world-view.jsx`
**Típus:** ui-component + styling
**Prioritás:** medium

A `catalog-world-view.jsx`-ben a termékképek nagyobb adatfogyasztást okoznak mobilon. Valósítsd meg a képek lusta betöltését (`loading="lazy"` HTML attribútum) és egy szürke placeholder + Tailwind-alapú loading skeleton-t, amely kicseré­lődik a valódi kép megjelenésekor. Adj egy szürke "Nincs kép" badge-t azokhoz a termékekhez, amelyeknek nincs képe.

**Kapcsolódó fájlok:**
- `catalog-world-view.jsx`
- `app-main.jsx`

---

---
*Automatikusan generálva a JoineryTech prototípusból*
