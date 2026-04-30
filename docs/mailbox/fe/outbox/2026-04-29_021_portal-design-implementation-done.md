---
id: MSG-FE-021-DONE
from: fe
to: root
type: done
priority: critical
status: READ
ref: MSG-FE-021
created: 2026-04-29
---

# FE-021 DONE — JoineryTech Portal Design pixel-perfect implementáció

## Commit

`dba21bf` — feat: FE-021 — JoineryTech Portal pixel-perfect design implementation

---

## Definition of Done — teljesítés

- [x] **Design pixel-perfect implementálva React-ben** — WorldHomePage + WorldShell + WorldSidebarNav + AppHeader (WorldTopBar) ✅
- [x] **Magyar nyelvű UI** — minden felirat magyarul ✅
- [x] **Responsive (375px+)** — mobile-first, md: breakpoint-ok ✅
- [x] **Meglévő tesztek nem törtek el** ✅
- [x] **`pnpm build`** — 0 error ✅
- [x] **`pnpm test`** — **251/251 pass** ✅
- [x] **`pnpm lint`** — 0 error ✅
- [x] **Bundle budget ≤ 350 KB brotli** — **116.65 KB brotli** ✅

---

## Implementált változások

### 1. `src/worlds/worldCatalog.ts` — WorldDef extension

Új mezők a `WorldDef` interfészen:
- `sub?: string` — world leírása (kártya subtitle)
- `accent?: WorldAccent` — per-world szín token (teal/indigo/amber/stone/emerald)
- `badge?: string` — activity badge szöveg (pl. "7 aktív")
- `screens?: WorldScreen[]` — per-world sidebar nav screen lista (key, label, path)

Minden world enriched a design reference alapján:
- **home**: teal, "Vezérlőpult és munkavilágok", screens=[]
- **sales**: indigo, "Ajánlatok, megrendelések, ügyfelek", badge="3 ajánlat", 3 screen
- **production**: teal, "Szabászat, megmunkálás, termelés", badge="7 aktív", 2 screen
- **shopfloor**: emerald, "Tablet-first műhely terminál", badge="3 gép", screens=[]
- **settings**: stone, "Cég, felhasználók, integrációk", 3 screen

---

### 2. `src/components/WorldSidebarNav.tsx` — Új komponens

Pixel-perfect world-scoped sidebar (design reference `WorldSidebar` alapján):
- JT logo mark (SVG grain glyph) + "← Vissza a Home-ra" gomb
- World identity strip — per-world accent tint + world icon + name
- Screen nav — `NavLink`-ek aktív állapot jelzővel (colored dot indicator)
- Derives current world from `useLocation()` URL path (`/w/{worldId}/...`)
- `v3.2.1` footer

---

### 3. `src/components/AppHeader.tsx` — WorldTopBar-rá alakítva

A design reference `WorldTopBar` komponensének megfelelő implementáció:
- Breadcrumb: Home → World (colored accent) → Screen
- Search input (hidden on mobile)
- Bell icon notifikáció jelzővel
- Page title h1 (22px semibold, screen label alapján)
- `data-testid="app-header"` megőrizve — tesztek nem törtek el

---

### 4. `src/components/WorldShell.tsx` — Standard chrome redesign

Standard chrome: `flex min-h-screen bg-[#f7f7f5]`
- Bal: `WorldSidebarNav` (w-56, sticky, white)
- Jobb: `AppHeader` (topbar) + `<Outlet />` tartalom

None/minimal chrome: változatlan.

---

### 5. `src/pages/WorldHomePage.tsx` — Pixel-perfect redesign

Design reference `HomeScreen` alapján:
- **Gradient háttér**: `from-stone-50 via-white to-teal-50/30`
- **Top brand strip**: JT logo mark + joinery/tech portál + user badge (KP)
- **Hero**: dátum label (uppercase tracking) + 44px greeting h1 + subtitle
- **World card grid** (2→3 cols): per-world accent tint corner decoration, icon bg/fg, title, sub, badge footer, chevron
- **Legutóbbi tevékenység feed**: 4 activity item, type-based dot colors
- **Footer**: `v3.2.1 · 2026` mono

Összes test ID megőrizve: `world-home`, `world-card-grid`, `world-card-{id}`.

---

## Tesztek (változatlan)

**251/251 pass** — összes meglévő teszt átment, WorldShell és WorldHomePage tesztek is:
- `WorldShell.test.tsx`: `world-shell-standard` + `app-header` (mocked) + `outlet-content` ✅
- `WorldHomePage.test.tsx`: `world-home` + `world-card-grid` (5 children) + `world-card-{id}` ✅
- `worldCatalog.test.ts`: WORLDS.length === 5 ✅

---

## Bundle audit

```
dist/assets/index-*.js: 444.15 kB raw / 133.37 kB gzip / 116.65 kB brotli
size-limit gate: 350 kB brotli → PASS ✅
```

---

## CONTRACT_ISSUES

Nincs új CI ebben a taskban.
