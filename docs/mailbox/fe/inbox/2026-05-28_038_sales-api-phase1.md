---
id: MSG-FE-038
from: root
to: fe
type: task
priority: high
status: UNREAD
created: 2026-05-28
skill: /spaceos-terminal
agents: enabled
subagents: enabled
---

# MSG-FE-038 — Sales API Phase 1: lista bekötés + CustomerCreate

## Kontextus

A Sales modul (5009) implementáció kész (102 teszt, commit `2ab1586`). VPS deploy folyamatban
(INFRA MSG-ROOT-002) — addig **mock fallback** pattern kötelező, ugyanúgy mint a Phase 8+9
integrációknál.

A meglévő `SalesPage.tsx` teljesen mock adattal dolgozik. Ez a task a Phase 1 bekötés:
lista API-ok + CreateCustomer form. A Phase 2 (detail SlideOver-ök, Quote editor) designos spec
után indul — arról külön üzenet érkezik.

## Phase 1 scope

### 1. `SalesCustomers` — `GET /sales/api/customers`

```typescript
// Endpoint: GET /sales/api/customers?search=&page=1&pageSize=20
// Válasz: { items: CustomerDto[], totalCount: number }
interface CustomerDto {
  id: string
  name: string
  type: 'Lead' | 'Active' | 'Inactive'
  contactName: string
  contactEmail: string
  contactPhone: string
  city: string
  openQuoteCount: number
  totalOrderValue: number   // Ft
  createdAt: string
}
```

**Mit kell csinálni:**
- `useApi<PagedResult<CustomerDto>>` hook — `/sales/api/customers` + search param debounce
- Mock fallback: ha fetch error → CUSTOMERS mock adat (meglévő mocks/worlds.ts)
- Kártya tartalom frissítés: `openQuoteCount` (nyitott, volt: `openOrders`), `totalOrderValue` (volt: `ltv`)
- A kártya layout marad — nem kell redesign
- "Új ügyfél" gomb → `CreateCustomerSlideOver` (lásd lent)

### 2. `SalesQuotes` — `GET /sales/api/quotes`

```typescript
// Endpoint: GET /sales/api/quotes?status=&customerId=&page=1&pageSize=50
// Válasz: { items: QuoteListItemDto[], totalCount: number }
interface QuoteListItemDto {
  id: string
  quoteNumber: string        // pl. "AJ-2026-0042"
  customerName: string
  status: 'Draft' | 'Sent' | 'Accepted' | 'Rejected' | 'Archived' | 'ConversionPending' | 'Converted'
  createdAt: string
  expiresAt: string | null
  lineCount: number
  totalValue: number          // Ft
  ownerName: string
}
```

**Status → FE filter mapping:**
```typescript
const STATUS_MAP = {
  Draft: { label: 'Vázlat', filterKey: 'draft', bg: 'bg-stone-100', fg: 'text-stone-600', dot: 'bg-stone-400' },
  Sent: { label: 'Kiküldve', filterKey: 'sent', bg: 'bg-sky-50', fg: 'text-sky-700', dot: 'bg-sky-400' },
  Accepted: { label: 'Elfogadva', filterKey: 'approved', bg: 'bg-emerald-50', fg: 'text-emerald-700', dot: 'bg-emerald-500' },
  Rejected: { label: 'Elutasítva', filterKey: 'rejected', bg: 'bg-red-50', fg: 'text-red-600', dot: 'bg-red-400' },
  Archived: { label: 'Archivált', filterKey: 'archived', bg: 'bg-stone-50', fg: 'text-stone-400', dot: 'bg-stone-300' },
  ConversionPending: { label: 'Gyártásban', filterKey: 'pending', bg: 'bg-amber-50', fg: 'text-amber-700', dot: 'bg-amber-400' },
  Converted: { label: 'Kész', filterKey: 'converted', bg: 'bg-teal-50', fg: 'text-teal-700', dot: 'bg-teal-500' },
}
```

**Mit kell csinálni:**
- `useApi` hook — `/sales/api/quotes` + status filter
- Mock fallback: ha fetch error → QUOTES mock (meglévő)
- Táblázat adatok: `quoteNumber` (volt: `q.id`), `customerName`, `createdAt`, `expiresAt`, `lineCount`, status badge, `totalValue`
- Filter gombok: felső 6 (Összes, Vázlat, Kiküldve, Elfogadva, Elutasítva, Archivált) + count-ok az API-ból
- "Új ajánlat" gomb egyelőre disabled + tooltip: "Hamarosan — részletes szerkesztő fejlesztés alatt"

### 3. `SalesDashboard` — KPI aggregáció

```typescript
// GET /sales/api/quotes → aggregálva
// GET /sales/api/customers → totalCount
```

**KPI kártyák frissítése:**
- "Nyitott ajánlatok" → `Sent + Accepted` státuszú quote-ok count az API-ból
- "Pipeline érték" → `Sent + Accepted + ConversionPending` összeg
- "Konverziós ráta" → `Accepted / (Sent + Accepted + Rejected)` — csak ha van adat, különben `—`
- "Heti bevétel" → mock marad (nincs idősor API) — cseréld le: "Összes ügyfél" → customers.totalCount kártyára

**Lejáró ajánlatok panel:** `Sent` státuszú quote-ok az API-ból (top 4, expiresAt alapján rendezve)

**Top ügyfelek panel:** `GET /sales/api/customers?pageSize=5` — mock LTV marad (`totalOrderValue`)

### 4. `CreateCustomerSlideOver` (400px) — új komponens

SlideOver minta: `InviteUserSlideOver` (400px, `src/components/ui/SlideOver.tsx`)

```typescript
interface CreateCustomerForm {
  name: string           // kötelező, min 2 char
  type: 'Lead' | 'Active'  // default: Lead
  contactName: string    // kötelező
  contactEmail: string   // kötelező, email formátum
  contactPhone: string   // opcionális
  city: string           // opcionális
}
// POST /sales/api/customers → 201 Created → lista refresh
```

**UI elemek:**
- Fejléc: "Új ügyfél"
- Mező: Cégnév (text, required)
- Mező: Típus (select: Lead / Aktív ügyfél) — default Lead
- Mező: Kapcsolattartó neve (text, required)
- Mező: Email (email input, required)
- Mező: Telefon (tel input, optional)
- Mező: Város (text, optional)
- Gombok: "Mégse" (close) + "Létrehozás" (submit, indigo-600)
- Loading state a submit gombban
- Error toast ha 400/500

### 5. `SalesOrders` tab kezelés

A `SalesOrders` komponens jelenleg Joinery mock ORDERS-t mutat. Ez marad mock egyelőre — a Sales modul nem kezel Orders-t, azok a Joinery-ben élnek. A tab neve maradhat "Megrendelések" de adjunk hozzá egy infó sávot:

```tsx
<div className="mx-7 mb-4 px-4 py-2.5 bg-amber-50 border border-amber-200 rounded-lg text-[11.5px] text-amber-800">
  A megrendelések a Gyártás modulban kezelhetők. Ez a nézet összesítő — élő adatok hamarosan.
</div>
```

## Mock fallback pattern

```typescript
// Ugyanaz mint Phase 8+9
const { data, loading, error } = useApi<PagedResult<CustomerDto>>(
  '/sales/api/customers',
  { params: { search, pageSize: 20 } }
)
const customers = error ? CUSTOMERS_MOCK : (data?.items ?? [])
```

## Tesztek

- `SalesPage.test.tsx` meglévő tesztek mind zöldek maradnak
- Új tesztek:
  - `SalesCustomers`: API loading skeleton, error → mock fallback, data render
  - `SalesQuotes`: filter badge count-ok, status badge mapping
  - `SalesDashboard`: KPI kártyák API-ból, lejáró ajánlatok lista
  - `CreateCustomerSlideOver`: form validation, submit → POST, loading state, close
- Min. +12 új teszt (az összes előző FE teszt zöld marad)

## Definition of Done

- [ ] `SalesCustomers` → `GET /sales/api/customers` (mock fallback ha offline)
- [ ] `SalesQuotes` → `GET /sales/api/quotes` (mock fallback, status mapping)
- [ ] `SalesDashboard` KPI kártyák API-ból számítva
- [ ] `CreateCustomerSlideOver` kész (validáció + POST + lista refresh)
- [ ] "Új ajánlat" gomb disabled + tooltip
- [ ] `SalesOrders` tab infó sáv hozzáadva
- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` → mind zöld, ≥ +12 új teszt

## Phase 2 — Design szükséges (NEM része ennek a feladatnak)

Az alábbi elemekhez designos terv készül — külön üzenetben jön:
- **CustomerDetailSlideOver** — contact + addresses + actor link státusz + FSM akciók
- **CreateQuoteSlideOver** — ügyfélválasztó, cím, lejárat, tételek
- **QuoteDetailSlideOver** — tételek szerkesztése, Send/Accept/Reject/Convert akciók

Addig ezek UI-ja nem indul — ne implementálj placeholder SlideOver-öket.
