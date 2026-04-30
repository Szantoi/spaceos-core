---
id: MSG-FE-027
from: root
to: fe
type: task
priority: critical
status: READ
created: 2026-04-30
---

# FE-027 — JoineryTech Portal Scaffold (nulláról)

> **Cél:** Működő Vite + React 18 + TypeScript + Tailwind projekt felépítése a `frontend/joinerytech-portal/` mappában, az összes oldal implementálásával a design reference alapján.

## Kontextus

A teljes frontend törlésre került (2026-04-30 nagy átalakítás). Nulláról építünk egyetlen portált: **joinerytech.hu**. Minden ingyenes, bejelentkezés = teljes hozzáférés.

**Design reference:** `joinerytech_20260430/` mappa — OLVASD EL MINDEN fájlt!
- `page-*.jsx` — oldalak designja
- `data*.js` — mock adatok
- `ui.jsx` — közös UI komponensek
- `JoineryTech Portal.html` — landing page design

## Feladat (3 fázis, egy inbox-ban)

### Fázis 1 — Projekt scaffold

```bash
cd /opt/spaceos/frontend/joinerytech-portal
pnpm create vite . --template react-ts
pnpm add -D tailwindcss @tailwindcss/vite vitest @testing-library/react @testing-library/jest-dom jsdom
```

**Struktúra:**
```
src/
  components/
    ui/              ← Badge, KpiCard, ProgressBar, StatusDot, DataTable (ui.jsx alapján)
    layout/          ← Sidebar, Navbar, WorldLayout
  pages/             ← minden page-*.jsx alapján
  mocks/             ← minden data*.js → TypeScript mock
  hooks/             ← useAuth (placeholder), useApi (placeholder)
  lib/               ← utils (cn helper, fmtHUF, stb.)
  types/             ← TypeScript típusok
  App.tsx            ← router
  main.tsx
```

**Tailwind config:** magyar ipar színpaletta a design reference alapján.

### Fázis 2 — Közös UI komponensek

A `ui.jsx`-ből konvertáld TypeScript-re:
- `Badge` — variant rendszer (teal/indigo/amber/emerald/rose/stone/sky)
- `KpiCard` — KPI metric kártya
- `ProgressBar` — configurable track/fill/height
- `StatusDot` — colored dot + pulse animáció
- `DataTable` — responsive táblázat
- Bármi más ami a design-ben közös

### Fázis 3 — Oldalak implementálása

Minden `page-*.jsx` design fájlt implementálj:

| Design fájl | Mock adat | Route | Leírás |
|---|---|---|---|
| `page-home.jsx` | `data-worlds.js` | `/` | Landing + World kártyák |
| `page-dashboard.jsx` | `data.js` | `/w/dashboard` | Dashboard KPI-k |
| `page-sales.jsx` | `data.js` | `/w/sales` | Értékesítés |
| `page-orders.jsx` | `data.js` | `/w/orders` | Megrendelések |
| `page-design.jsx` | `data.js` | `/w/design` | Termék konfigurátor |
| `page-flow.jsx` | `data.js` | `/w/flow` | B2B Handshake-ek |
| `page-production.jsx` | `data.js` | `/w/production` | Gyártás áttekintés |
| `page-workflow.jsx` | `data.js` | `/w/workflow` | FSM Board |
| `page-shopfloor.jsx` | `data-extra.js` | `/w/shopfloor` | Shopfloor (kiosk dark UI) |
| `page-extras.jsx` | `data-extra.js` | `/w/settings` | Beállítások |
| `page-extras-2.jsx` | `data-extra-2.js` | `/w/settings/*` | További beállítások |
| `page-rest.jsx` | `data-extra-2.js` | `/w/*` | Egyéb oldalak |
| `page-world-pages.jsx` | `data-worlds.js` | `/w` | World page templates |

**Mock adat stratégia:** `src/mocks/` mappába TypeScript-ben. Minden oldal: `const displayData = apiData ?? DEMO_DATA;`

### Route struktúra

```
/                   → Landing (anonymous OK)
/w                  → World Home (auth placeholder)
/w/dashboard        → Dashboard
/w/sales            → Értékesítés
/w/orders           → Megrendelések
/w/design           → Termék konfigurátor
/w/flow             → B2B Handshake-ek
/w/production       → Gyártás
/w/workflow         → FSM Board
/w/shopfloor        → Shopfloor
/w/settings         → Beállítások
/w/settings/*       → Aloldalak
```

## Szabályok

- **Skill:** `/spaceos-terminal` szerint dolgozz, `/senior-frontend` mintákat kövesd
- **Használhatsz sub-agent-eket** ha szükséges a párhuzamos munkához
- **Magyar UI** mindenhol — gombfeliratok, címek, placeholder-ek
- **Responsive:** 375px (mobil hamburger) + md+ (desktop sidebar)
- **TypeScript strict** — nincs `any`, nincs `@ts-ignore`
- **Tesztek:** Vitest + React Testing Library — legalább minden oldal renderelődik
- Nincs `dangerouslySetInnerHTML`, nincs `console.log`, nincs `localStorage` direkt

## Definition of Done

- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` → ≥ 50 pass (minden oldal + UI komponens tesztelve)
- [ ] `pnpm lint` → 0 error (ha van lint config)
- [ ] Minden `page-*.jsx` design implementálva
- [ ] `src/mocks/` TypeScript mock adatok
- [ ] `src/components/ui/` közös UI komponensek
- [ ] Responsive (375px + desktop)
- [ ] Magyar UI feliratok
- [ ] Outbox DONE — commit hash, teszt összesítő, security review
