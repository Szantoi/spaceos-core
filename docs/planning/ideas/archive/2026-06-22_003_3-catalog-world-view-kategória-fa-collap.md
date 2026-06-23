---
id: IDEA-20260622-003
title: "3. Catalog World View — Kategória-fa collapse/expand + favorit-csillag"
type: idea
source: joinerytech-prototype
priority: medium
status: new
created: 2026-06-22
---

# 3. Catalog World View — Kategória-fa collapse/expand + favorit-csillag

**Komponens:** `catalog-world-view.jsx`  
**Típus:** ui-component + styling  
**Prioritás:** medium

A katalógus navigáció (bal oldali fa) jelenleg statikus; új UX:
- **Collapse/expand nyilak** minden kategória mellett (kis szürke chevron, hover: kékre vált)
- **Favorit csillag** (outline ☆ → filled ★) minden kategórián, amely:
  - Mentés az **`app-store`-ban** (user preferenciák)
  - Az "Kedvencek" tab-ban felül megjelennek (új tab a fa felett)
  - Mobil-nézeten: favorit-szűrés gyors-hozzáférés gomb (♥ ikon)
- **Keresés-box** a fa felett (input + clear ✕), amely kategória-szinten szűr

Segíti: nagy katalógusok gyorsabb navigációja, mobilon könnyebb tájékozódás.

**Kapcsolódó fájlok:**
- `catalog-world-view.jsx`
- `app-store.jsx` (userPrefs.favoriteCatalogCategories)
- `app-main.jsx` (CSS: collapse animation, Tailwind utility)

---

## Megjegyzés
Mindhárom ötlet:
- **Meglévő adat-strukturára** épül (verzió-FSM, assembly BOM, catalog fa már létezik)
- **2-4 komponens-módosítás**, kevés napi logika
- **Tailwind-ben megoldható** UI (badge, chevron, star, highlight)
- **localStorage/app-store** már jól integrálható

---
*Automatikusan generálva a JoineryTech prototípusból*
