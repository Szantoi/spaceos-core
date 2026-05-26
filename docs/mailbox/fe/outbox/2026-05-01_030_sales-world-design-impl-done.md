---
id: MSG-FE-030-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-030
created: 2026-05-01
---

# FE-030 DONE — Értékesítés (Sales world) teljes design implementáció

## Státusz: KÉSZ

Minden Definition of Done kritérium teljesítve.

## Elvégzett munkák

### 1. `SalesWorldPage` — screen-alapú navigáció
A `SalesPage.tsx` teljes refaktora `ProductionWorldPage` mintájára:
- `useParams` + `useNavigate` alapú screen routing
- `WorldShell` saját maga kezeli (nem `WorldPage` wrapperből)
- `key={currentScreen}` wrapper a force remounthoz

### 2. Navigálható screen-ek
| URL | Komponens |
|-----|-----------|
| `/w/sales` | `SalesDashboard` |
| `/w/sales/dash` | `SalesDashboard` |
| `/w/sales/quotes` | `SalesQuotes` |
| `/w/sales/customers` | `SalesCustomers` |
| `/w/sales/orders` | `SalesOrders` (új) |

### 3. `SalesDashboard`
- Saját padding: `px-7 py-6 space-y-5`
- Prop: `onScreen` (nem `onTab`)
- "Ajánlatok →" és "Mind →" linkek: `onScreen("quotes")`

### 4. `SalesQuotes`
- `rejected` filter hozzáadva: `{ key: 'rejected', label: 'Elutasítva' }`
- "Új ajánlat" gomb a filter sorban (jobb oldalon, indigo-600)
- Saját padding: `px-7 py-6 space-y-4`

### 5. `SalesCustomers`
- Saját padding: `px-7 py-6`
- Live search filter hozzáadva (név + város)

### 6. `SalesOrders` (új komponens)
- `ORDERS` mock adatból (JT-24xx sorozat)
- Státusz filter: draft / calc / released / ready
- Táblázatos nézet: rendelésszám, ügyfél, dátum, tételek, státusz, érték

### 7. `App.tsx`
- Sales route-ok: `WorldPage` wrapper → `SalesWorldPage` (standalone)

## Tesztek
- **235 teszt, 0 fail** (volt 232, +3 új: rejected filter, Új ajánlat gomb, orders screen)
- `pnpm build` → 0 error, 0 TS warning

## Commit
`8e1d1f1` — feat: Sales world — screen-alapú navigáció + teljes design impl
