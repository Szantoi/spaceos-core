---
id: MSG-FE-025-DONE
from: fe
to: root
type: done
priority: critical
status: READ
ref: MSG-FE-025
created: 2026-04-29
---

# FE-025 DONE — Portal teljes design implementáció + mobil fix + legacy törlés

## Commit

`731b317` — feat: FE-025 — Portal teljes design implementáció + mobil fix + legacy törlés

## Definition of Done — státusz

- [x] MINDEN régi/legacy elem eltávolítva
- [x] Mobil nézet: hamburger menü + sidebar slide-in overlay
- [x] Design reference oldalak implementálva (vagy helyfoglaló)
- [x] Magyar nyelvű UI mindenhol
- [x] Responsive (375px+)
- [x] `pnpm build` 0 error
- [x] `pnpm test` 242/242 pass ✅
- [x] `pnpm lint` 0 error

---

## 1. Legacy eltávolítás

Törölve:
- `src/pages/DashboardPage.tsx`
- `src/pages/ProfilePage.tsx`
- `src/test/DashboardPage.test.tsx`
- `src/test/ProfilePage.test.tsx`

(App.tsx-ből az importok és route-ok már FE-023-ban eltávolításra kerültek.)

---

## 2. Mobil hamburger menü

**Változtatott fájlok:**

`src/components/WorldShell.tsx`
- `useState(false)` → `mobileSidebarOpen`
- `AppHeader onMenuOpen` prop átadva
- `WorldSidebarNav mobileOpen`/`onMobileClose` prop átadva

`src/components/AppHeader.tsx`
- `HamburgerIcon` SVG komponens hozzáadva
- `onMenuOpen?: () => void` prop
- Hamburger gomb `md:hidden` — csak mobilon látható

`src/components/WorldSidebarNav.tsx`
- `SidebarContent` belső komponens kiemelve (kód duplikáció elkerülve)
- Desktop: `hidden md:flex sticky top-0` — változatlan
- Mobil: `fixed inset-0 z-50` overlay → backdrop (klikk bezár) + slide-in `w-56` aside
- NavLink-ek `onClick={onClose}` → navigálásnál auto-bezár

---

## 3. Új oldalak (design reference szerint)

### `src/pages/production/ProductionOverviewPage.tsx`
- 4 KPI kártya: Napi terv, Aktív gépek, Hulladék 4.2%, OEE 87%
- Gép terhelés lista: 4 gép + haladás progress bar
- Aktív megrendelések lista + progress bar + stádium badge
- Gyors link kártyák: Szabászat, Megmunkálás
- Mock adattal (MACHINES, ACTIVE_ORDERS inline)

### `src/pages/sales/SalesDashboardPage.tsx`
- 4 KPI kártya: heti bevétel, nyitott ajánlatok, pipeline érték, konverzió
- Pipeline tölcsér: bar + SVG vizualizáció
- Lejáró ajánlatok táblázat
- Top ügyfelek LTV lista
- Mock adattal

### `src/pages/sales/SalesQuotesPage.tsx`
- Filter tab-ok: Összes / Vázlat / Kiküldve / Elfogadva / Elutasítva / Lejárt
- Táblázat: ID, ügyfél, dátum, lejár, tételek, felelős, státusz, érték
- Státusz badge: teal/sky/emerald/rose/amber szín szerint
- 10 mock ajánlat

### `src/pages/sales/SalesCustomersPage.tsx`
- Keresés input (name/city szűrés)
- Kártya grid: 1-2-3 kolumn responsive
- Kártyán: avatar initials, city/since, kapcsolattartó/email/tel, nyitott/LTV metrikák
- 6 mock ügyfél

---

## 4. App.tsx route változások

- `sales` redirect: `orders` → `dashboard`
- `production` redirect: `cutting-plans` → `overview`
- Új route-ok: `sales/dashboard`, `sales/quotes`, `sales/customers`, `production/overview`

---

## Tesztek

**242/242 pass** ✅

- 9 teszt csökkent (DashboardPage: 2, ProfilePage: 7 — legacy törlés)
- 251 → 242

---

## Helyfoglalók

Az alábbi design reference oldalak helyfoglalóval vannak kezelve (már meglévő komponensként):
- `page-design.jsx` → `ProductConfiguratorPage` (meglévő, FE-021-ből)
- `page-flow.jsx` → `B2bHandshakesPage` (meglévő)
- `page-workflow.jsx` → `ManufacturingFsmBoardPage` (meglévő)
- `page-extras.jsx` / `page-extras-2.jsx` → Settings oldalak (TenantInfoPage, AuditLogPage, UserListPage)
- `page-shopfloor.jsx` → ShopFloor oldalak (meglévők)
- `page-orders.jsx` → OrdersPage, OrderDetailPage (meglévők)
- `page-production.jsx` → CuttingPlanListPage, CuttingPlanDetailPage (meglévők)

Nincs üres oldal — minden route vizuális tartalommal rendelkezik.

---

## Kockázatok / kérdések

Nincsenek.
