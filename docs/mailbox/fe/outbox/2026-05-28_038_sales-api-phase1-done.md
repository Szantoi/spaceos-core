---
id: MSG-FE-038-DONE
from: fe
to: root
type: done
priority: high
status: READ
ref: MSG-FE-038
created: 2026-05-28
---

# FE-038 DONE — Sales API Phase 1

## Státusz: KÉSZ

## Elvégzett munkák

### `src/hooks/useApi.ts`
- `API_BASE.sales: '/sales'` hozzáadva

### `src/pages/SalesPage.tsx` — teljes refaktor

**Új interfészek és típusok:**
- `PagedResult<T>`, `CustomerDto`, `QuoteListItemDto`, `CreateCustomerForm`
- `STATUS_MAP` — API statuses (`Draft/Sent/Accepted/Rejected/Archived/ConversionPending/Converted`) → display (label, bg, fg, dot)
- Mock fallback adapterek: `CUSTOMERS_FALLBACK`, `QUOTES_FALLBACK` (meglévő mock → DTO shape)

**`SalesDashboard`:**
- `GET /sales/api/quotes?pageSize=100` + `GET /sales/api/customers?pageSize=5`
- Mock fallback ha API nem elérhető
- KPI kártyák API-ból számítva:
  - "Összes ügyfél" → `customers.totalCount`
  - "Nyitott ajánlatok" → `Sent + Accepted` count
  - "Pipeline érték" → `Sent + Accepted + ConversionPending` összeg
  - "Konverziós ráta" → `Accepted / (Sent + Accepted + Rejected)` (vagy `—`)
- "Lejáró ajánlatok" panel: `Sent` quote-ok, `expiresAt` szerint rendezve, top 4
- "Top ügyfelek" panel: `totalOrderValue` szerint rendezve, top 5

**`SalesQuotes`:**
- `GET /sales/api/quotes?pageSize=100` — mock fallback
- `QUOTE_FILTER_KEYS` frissítve API status értékekre (`Draft/Sent/Accepted/Rejected/Archived`)
- Filter count-ok az API adatból számítva
- Tábla: `quoteNumber`, `customerName`, `createdAt`, `expiresAt`, `lineCount`, `ownerName`, `totalValue`
- "Új ajánlat" gomb `disabled` + hover tooltip: "Hamarosan — részletes szerkesztő fejlesztés alatt"
- Loading spinner amíg tölt

**`SalesCustomers`:**
- `GET /sales/api/customers?search=...&pageSize=20` — mock fallback
- 300ms debounce a keresőre
- Loading skeleton (6 animált kártya)
- Kártyák: `contactName`, `contactEmail`, `contactPhone` (API mezők), `openQuoteCount`, `totalOrderValue` (LTV)
- "Új ügyfél" gomb → `CreateCustomerSlideOver`

**`CreateCustomerSlideOver`** (új komponens, 400px):
- Mezők: Cégnév (required, min 2), Típus (select: Lead/Aktív), Kapcsolattartó neve (required), Email (required), Telefon (optional), Város (optional)
- Submit: `POST /sales/api/customers` → close + refetch
- Validáció: piros border + "Kötelező mező" üres mezőknél
- Loading state a submit gombban

**`SalesOrders`:**
- Infó sáv hozzáadva: "A megrendelések a Gyártás modulban kezelhetők..."

### `src/pages/__tests__/SalesPage.test.tsx`
- `afterEach(vi.unstubAllGlobals)` hozzáadva
- `mockFetchError()` / `mockFetchSuccess()` segédfüggvények
- 2 meglévő teszt frissítve (`waitFor` mintával)
- **13 új teszt:**
  - SalesCustomers: loading skeleton, error fallback, API data, Új ügyfél SlideOver
  - SalesQuotes: error fallback, API quoteNumber, disabled gomb, status badge
  - SalesDashboard: Összes ügyfél KPI, lejáró ajánlatok, konverziós ráta
  - CreateCustomerSlideOver: validáció
  - SalesOrders: infó sáv

## Tesztek
- **271 teszt, 0 fail** (volt 258, +13 nettó)
- `pnpm build` → 0 error

## Phase 2 (NEM része ennek a feladatnak)
- CustomerDetailSlideOver, CreateQuoteSlideOver, QuoteDetailSlideOver — külön spec után indul
