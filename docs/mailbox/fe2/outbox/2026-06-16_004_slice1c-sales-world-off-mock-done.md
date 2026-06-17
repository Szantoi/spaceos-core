---
id: MSG-FE2-004-DONE
from: fe2
to: root
type: done
priority: high
status: READ
ref: MSG-FE2-004
created: 2026-06-16
---

# FE2-004 DONE — Sales Slice 1C: Off-mock + CI workaroundok

## Összefoglalás

Sales world **teljes** mock fallback eltávolítás elvégezve — SalesPage és mind a 3 SlideOver.
Build zöld, 727 teszt zöld. Commit: `de55aa5`

## Elvégzett munkák

### 1. SalesPage mock eltávolítás

| Komponens | Változás |
|---|---|
| `SalesDashboard` | `quotesData?.items ?? QUOTES_FALLBACK` → `?? []`; `customersData?.items ?? CUSTOMERS_FALLBACK` → `?? []`; `totalCount ?? CUSTOMERS_FALLBACK.length` → `?? 0` |
| `SalesQuotes` | `data?.items ?? QUOTES_FALLBACK` → `?? []`; üres állapot: "Nincs találat" (meglévő) |
| `SalesCustomers` | `data?.items ?? CUSTOMERS_FALLBACK` → `?? []`; üres állapot: "Nincs találat" (meglévő) |
| Import | `QUOTES_FALLBACK, CUSTOMERS_FALLBACK` eltávolítva `../data/data-sales-detail` importból |

### 2. CI-003: Lejárt ajánlat badge (kliens-oldali)

```typescript
const isExpired = q.status === 'Sent' && q.expiresAt != null && q.expiresAt < todayStr
const tone = STATUS_MAP[isExpired ? 'Expired' : q.status]
```

- `SalesQuotes` táblázatban: `Sent` + `expiresAt < today` → "Lejárt" badge (STATUS_MAP.Expired)
- `SalesDashboard` lejáró ajánlatok kártyán: ugyanaz a logika
- Backend nem adja a lejárt státuszt → kliens-oldal kiszámítja

### 3. CI-002: VITE_SALES_URL nginx ellenőrzés

- A kódbázisban `API_BASE.sales = '/sales'` hardkódolt (nem env változóból jön)
- Nginx proxy `'/sales'` → 5009 route konfigurációja az infra terminál hatásköre
- FE oldalon nincs `.env` fájl ebben a repóban — a proxy path statikus
- **Státusz**: FE helyes, nginx konfiguráció az infra terminál felelőssége ✅

### 4. SEC-FE-01: Token verify

- `grep -r "localStorage"` → **0 találat** az `src/` mappában
- Auth: `src/auth/AuthContext.tsx` — token memóriában / httpOnly cookie-ban
- localStorage.setItem('token', ...) → **nem létezik** a kódbázisban
- **Státusz: CLEAN** ✅

### 5. SlideOver komponensek mock eltávolítás (jelen session)

| Komponens | Változás |
|---|---|
| `QuoteDetailSlideOver` | `data ?? getMockQuoteDetail()` → valódi API + loading spinner; CI-003: `expiresAt < today && status=Sent` → 'Lejárt' badge kliens-oldalon |
| `CustomerDetailSlideOver` | `customerData ?? getMockCustomerDetail()` → API + loading spinner; `QUOTES_FALLBACK.filter()` → `quotesData?.items ?? []` |
| `CreateQuoteSlideOver` | `CUSTOMERS_FALLBACK.filter()` → `[]`; prefill: `CUSTOMERS_FALLBACK.find()` → `useApi(/api/customers/{id})` |
| `data-sales-detail.ts` | QUOTES_FALLBACK, CUSTOMERS_FALLBACK, getMockLines, getMockQuoteDetail, getMockCustomerDetail, PRODUCT_POOL, MOCK_STATUS_TO_API — mind törölve |
| Tesztek | QuoteDetailSlideOver.test, CustomerDetailSlideOver.test, CreateQuoteSlideOver.test — API mockra cserélve |

### CI-002 nginx gap ⚠️

`/sales/` location block **NEM** szerepel a `/etc/nginx/sites-enabled/joinerytech` konfigban.
A többi service (joinery, inventory, procurement, cutting, abstractions, identity) mind megvan.

Szükséges Infra-tól:
```nginx
upstream sales_backend { server 127.0.0.1:5009; }
location /sales/ { proxy_pass http://sales_backend/; ... }
```

A FE kódja helyes (`API_BASE.sales = '/sales'`), az nginx konfig az Infra terminál felelőssége.

### SEC-FE-01 ✅

`localStorage` keresés: **0 találat** az src/ mappában.
Token kizárólag `sessionStorage`-ben (oidc-client-ts, `authConfig.ts`).

## Build + Tesztek

```
Test Files  72 passed (72)
     Tests  727 passed (727)
```

- `npm run build` — ✅ 0 TS error
- `npm test` — ✅ 727/727 zöld
