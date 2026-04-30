---
id: MSG-FE-025
from: root
to: fe
type: task
priority: critical
status: READ
created: 2026-04-29
---

# FE-025 — Portal Design teljes implementáció + legacy eltávolítás

> **Gábor feedback:** "A mobil nézet nem működik jól. A menü nem jelenik meg. A régi elemeket és megjelenítést teljesen el kell távolítani. Mindent implementálni kell a tervből. Ha valami nem valósít meg működést, helyfoglalókat kell használni."
> **Design reference:** `design-reference/` mappa — OLVASD EL ÚJRA az összes fájlt!
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Használhatsz sub-agent-eket** ha szükséges

---

## KÖTELEZŐ LÉPÉSEK

### 1. Legacy eltávolítás (teljes!)

Töröld az összes régi komponenst/oldalt ami NEM a design reference-ben van:
- Régi `DashboardPage` — törölve legyen (már nincs route, de a fájl megmaradt?)
- Régi `ProfilePage` — törölve
- Régi `AppHeader` ha nem a design WorldTopBar-nak felel meg — cseréld le teljesen
- Bármilyen régi sidebar/nav ami nem a design `WorldSidebarNav` — töröld
- Régi stílusok, régi layout elemek — mind ki

### 2. Mobil nézet fix (KRITIKUS)

A design reference `WorldSidebarNav`-nak **mobil nézetben hamburger menüként kell működnie**:
- **Desktop (md+):** sidebar fix bal oldalt (w-56)
- **Mobile (<md):** hamburger ikon a header-ben → klikk → sidebar slide-in overlay (vagy sheet)
- A `@radix-ui/react-dialog` már installálva van — használd Sheet/Drawer pattern-ként

### 3. Design reference MINDENT implementálni

Olvasd el ÚJRA a `design-reference/project/` összes fájlját és implementáld ami hiányzik:

**page-home.jsx** → WorldHomePage (kész, de ellenőrizd a design-nel)
**page-sales.jsx** → Sales world oldalak
**page-orders.jsx** → OrdersPage / OrderDetailPage
**page-production.jsx** → Production world oldalak
**page-shopfloor.jsx** → Shop Floor oldalak
**page-design.jsx** → Product Configurator
**page-flow.jsx** → Workflow / Handshakes
**page-workflow.jsx** → Manufacturing FSM Board
**page-extras.jsx** → Settings oldalak
**page-extras-2.jsx** → További settings
**page-rest.jsx** → Egyéb oldalak
**page-world-pages.jsx** → World page templates
**page-dashboard.jsx** → Dashboard (ha van a design-ben)
**ui.jsx** → Közös UI komponensek (gombok, badge-ek, kártyák, táblázatok)
**tweaks-panel.jsx** → Design tweaks (ha releváns)
**data*.js** → Mock adatok — használd helyfoglalónak

### 4. Helyfoglalók

Ha egy oldal API hívást igényel ami nem működik (pl. Manufacturing endpoint nem elérhető):
- **Placeholder komponens:** üzenet + ikon, pl. "Ez a funkció hamarosan elérhető" vagy mock adattal renderelj
- **NE legyen üres oldal** — mindig legyen vizuális tartalom

---

## Definition of Done

- [ ] MINDEN régi/legacy elem eltávolítva
- [ ] Mobil nézet: hamburger menü + sidebar slide-in
- [ ] Design reference ÖSSZES oldala implementálva (vagy helyfoglaló)
- [ ] ui.jsx közös komponensek átültetve
- [ ] Magyar nyelvű UI mindenhol
- [ ] Responsive (375px+)
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` ≥ 200 pass (tesztek frissítendők a legacy törlés miatt)
- [ ] `pnpm lint` 0 error
- [ ] Outbox DONE — részletezze mit implementált és mit helyfoglalóval oldott meg
