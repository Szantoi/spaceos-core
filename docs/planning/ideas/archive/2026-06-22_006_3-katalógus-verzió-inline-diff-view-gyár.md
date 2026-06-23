---
id: IDEA-20260622-006
title: "3. **Katalógus-Verzió Inline Diff View (Gyártásvezető szűréshez)**"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 3. **Katalógus-Verzió Inline Diff View (Gyártásvezető szűréshez)**

**Komponens:** `catalog-world-view.jsx` → expandable row detail  
**Típus:** ui-component + styling  
**Prioritás:** medium

A katalógus verziózás már létezik (már feldolgozva), de a gyártásvezető nézet hiányzik: katalógus-tábla egy sorára kattintva **inline kiterjesztés** (slide-down animation) mutatja a **jelenleg aktív vs. előző verzió különbségét** (ár, méretek, szállítási idő) karikázott diff-cellákkal (piros/zöld inline badge). Nem modal, hanem táblázaton belüli view. Segít: "Ez a tétel visszavonult, ez az új verzió."

**Kapcsolódó fájlok:**
- `catalog-world-view.jsx` → `<CatalogDiffRow />` új komponens
- `data-catalog.js` → verzió-diff számítás helper
- Tailwind: `group-hover:bg-yellow-50` + `transition-max-height` animáció

---

## Megjegyzés
Mind a 3 ötlet **additív** (nem változtat meglévő logikán), **<2 óra**, és közvetlenül a PROJECT_STATUS 4.9–4.10 narratívájához kötik a hiányzó mobile/partner-facing UI-t.

---
*Automatikusan generálva a JoineryTech prototípusból*
