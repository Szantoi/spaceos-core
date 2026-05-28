---
id: MSG-FE-039
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

# MSG-FE-039 — Sales Phase 2: három SlideOver + useSalesDetail hook

## Kontextus

A designos tervek elkészültek. A spec az alábbi leírás + a design referencia alapján implementálandó.
A FE-038 (Phase 1) párhuzamosan futhat — a Phase 2 fájljai különállók, nem ütköznek.

**Design referencia:** `https://api.anthropic.com/v1/design/h/GDTKYQUpDVqbizKIO6mJxA?open_file=JoineryTech+Portal.html`

## Architektúra (kötelező struktúra)

```
src/
  data/
    data-sales-detail.ts       ← kanonikus mock adatok + helpers (QUOTE_STATUS_MAP, QUOTE_LINES, CUSTOMER_EXTRA, VAT)
  components/sales/
    QuoteDetailSlideOver.tsx   ← 680px
    CreateQuoteSlideOver.tsx   ← 500px
    CustomerDetailSlideOver.tsx ← 520px
    SalesDetailHost.tsx        ← mindhárom SlideOver wrapper, beágyazható minden listanézetbe
  hooks/
    useSalesDetail.ts          ← cross-navigation logika (quote↔customer nem stackel, CreateQuote success → detail)
```

Meglévő `SlideOver` komponens: `src/components/ui/SlideOver.tsx` — width prop-pal.

## 1. `data-sales-detail.ts`

```typescript
// 8-állapotú quote status map
export const QUOTE_STATUS_MAP = {
  Draft:             { label: 'Vázlat',          bg: 'bg-stone-100',  fg: 'text-stone-600',  dot: 'bg-stone-400' },
  Sent:              { label: 'Kiküldve',         bg: 'bg-sky-50',     fg: 'text-sky-700',    dot: 'bg-sky-400' },
  Accepted:          { label: 'Elfogadva',        bg: 'bg-emerald-50', fg: 'text-emerald-700',dot: 'bg-emerald-500' },
  Rejected:          { label: 'Elutasítva',       bg: 'bg-red-50',     fg: 'text-red-600',    dot: 'bg-red-400' },
  Archived:          { label: 'Archivált',        bg: 'bg-stone-50',   fg: 'text-stone-400',  dot: 'bg-stone-300' },
  ConversionPending: { label: 'Gyártásba küldve', bg: 'bg-amber-50',   fg: 'text-amber-700',  dot: 'bg-amber-400' },
  Converted:         { label: 'Átalakítva',       bg: 'bg-teal-50',    fg: 'text-teal-700',   dot: 'bg-teal-500' },
  Expired:           { label: 'Lejárt',           bg: 'bg-stone-100',  fg: 'text-stone-500',  dot: 'bg-stone-300' },
}

// Customer típus szerint avatar gradient
export const CUSTOMER_TYPE_STYLE = {
  Lead:     { label: 'Lead',           bg: 'bg-amber-100',  fg: 'text-amber-700' },
  Active:   { label: 'Aktív ügyfél',   bg: 'bg-indigo-100', fg: 'text-indigo-700' },
  Inactive: { label: 'Inaktív',        bg: 'bg-stone-100',  fg: 'text-stone-500' },
}

// HUF VAT helper
export const VAT_RATE = 0.27
export function calcVat(net: number) { return Math.round(net * VAT_RATE) }
export function calcGross(net: number) { return net + calcVat(net) }
export function fmtHuf(n: number) { return n.toLocaleString('hu-HU') + ' Ft' }

// Deterministic mock lines — quoteId seed alapján
export function getMockLines(quoteId: string): QuoteLineDto[] { /* ... */ }

// CUSTOMER_EXTRA — type + billing/shipping address mock
export const CUSTOMER_EXTRA: Record<string, CustomerExtra> = { /* ... */ }
```

## 2. `QuoteDetailSlideOver` (680px)

### Fejléc
- Quote number (`AJ-2026-XXXX`) + `StatusBadge` (QUOTE_STATUS_MAP alapján)
- Ügyfélnév, CreatedAt, ExpiresAt (ha van)
- X bezárás gomb

### Tételek táblázat
```
| # | Megnevezés | Mennyiség | Egységár | Összesen |
```

**Draft státuszban:**
- Sorok kattintásra inline szerkeszthetők: Mennyiség (number input) + Egységár (number input)
- Szerkesztés: kattintás → input megjelenik, Enter/blur → mentés (`PUT /sales/api/quotes/{id}/lines/{lineId}`)
- Törlés: sor végén ✕ ikon → `DELETE /sales/api/quotes/{id}/lines/{lineId}`
- Utolsó sor: "+ Új tétel" sor — kattintásra blank inputs + Megnevezés field → `POST /sales/api/quotes/{id}/lines`

**Sent/Accepted/egyéb státuszban:** csak olvasható táblázat, nincs szerkesztés

### Összegzés (táblázat alatt, jobbra igazítva)
```
Nettó:   123 456 Ft
ÁFA 27%:  33 333 Ft
Bruttó:  156 789 Ft
```
Valós idejű számítás a sorokban lévő értékekből (mock + API adat vegyesen).

### FSM-vezérelt akciósor (alap)

**Draft:**
- "Kiküldés" (indigo) → inline érvényesség form megjelenik (date picker, min = holnap) + "Elküldés megerősítése" gomb → `POST /sales/api/quotes/{id}/send`
- "Archiválás" (stone) → confirm dialog → `POST /sales/api/quotes/{id}/archive`

**Sent:**
- "Elfogadás" (emerald) → `POST /sales/api/quotes/{id}/accept` → státusz frissül
- "Elutasítás" (red outline) → inline indoklás textarea (optional) → `POST /sales/api/quotes/{id}/reject`
- "Archiválás" (stone) → `POST /sales/api/quotes/{id}/archive`

**Accepted:**
- "Gyártásba konvertálás" (teal, solid) → spinner indul → 1.6s → `POST /sales/api/quotes/{id}/convert` → státusz: `ConversionPending` → `Converted` → success toast: _"Az ajánlat sikeresen megrendeléssé konvertálva. A Joinery modul feldolgozza."_ → SlideOver bezárul, lista frissül

**ConversionPending / Converted / Archived / Rejected / Expired:** csak olvasható, nincs akciógomb

### Mock fallback
- `GET /sales/api/quotes/{id}` hiba → `getMockLines(quoteId)` mock adatokkal
- Mutáló akciók (POST/PUT/DELETE) offline módban: optimistic UI + silent no-op

## 3. `CreateQuoteSlideOver` (500px)

### Fejléc
"Új ajánlat létrehozása"

### Mezők
1. **Ügyfél** (typeahead, kötelező)
   - Input: keresés névre → `GET /sales/api/customers?search=…&pageSize=6`
   - Találat: avatar (initials) + cégnév + város — max 6 találat dropdown
   - Nincs találat: "Új ügyfél létrehozása →" link → `CreateCustomerSlideOver` nyílik (CreateQuote megmarad, ügyfél kiválasztódik sikeres létrehozás után)
   - Mock fallback: CUSTOMERS mock adat offline esetén

2. **Érvényesség dátuma** (date, kötelező, min = holnap)

3. **Megjegyzés** (textarea, optional, max 500 char)

### Validáció
- Ügyfél: kötelező → piros border + "Kérjük válasszon ügyfelet"
- Dátum: min=holnap → "Az érvényesség legalább holnap kell legyen"
- Validáció submit-kor és blur után

### Submit: `POST /sales/api/customers/{id}` (ehm, `POST /sales/api/quotes`)
```typescript
{ customerId: string, expiresAt: string, note?: string }
// → 201 { id, quoteNumber, ... }
```
Sikeres létrehozás után: CreateQuoteSlideOver bezárul → **QuoteDetailSlideOver automatikusan megnyílik** az új quote-tal (useSalesDetail hook kezeli).

## 4. `CustomerDetailSlideOver` (520px)

### Fejléc
- Gradient avatar: initials + típus szerint szín (Lead: amber-100/700, Active: indigo-100/700, Inactive: stone-100/500)
- Cégnév (bold) + típus badge + város
- X bezárás

### Kapcsolattartó szekció (inline szerkesztés)
- Megjelenít: Kapcsolattartó neve, E-mail, Telefon
- Jobb felső: "Szerkesztés" toggle gomb → mezők editálhatóvá válnak → "Mentés" gomb → `PUT /sales/api/customers/{id}/contact` → toggle vissza

### Cím szekciók (collapsible)
- "Számlázási cím" ▸ toggle → megjelenik: ország, irányítószám, város, utca
- "Szállítási cím" ▸ toggle → ua.
- Adatok: CUSTOMER_EXTRA mock (API: `PUT /sales/api/customers/{id}/addresses` — Phase 3, most csak megjelenítés)

### Utolsó 5 ajánlat (mini-lista)
```
AJ-2026-0042   Kiküldve    12 400 Ft
AJ-2026-0038   Elfogadva  340 000 Ft
...
```
- Adat: `GET /sales/api/quotes?customerId={id}&pageSize=5` (mock fallback: QUOTES szűrve)
- Sor kattintásra: `CustomerDetailSlideOver` bezárul → `QuoteDetailSlideOver` megnyílik az adott quote-tal

### FSM akciók (alap)
- **Lead:** "Promóció → Aktív ügyfél" (indigo outline) → confirm dialog → `PUT /sales/api/customers/{id}/promote` → típus badge frissül
- **Active:** "Deaktiválás" (rose outline) → confirm dialog → `PUT /sales/api/customers/{id}/deactivate`
- **Inactive:** nincs akció

## 5. `useSalesDetail` hook + `SalesDetailHost`

```typescript
// Navigáció szabályok:
// - quote és customer SlideOver nem nyílik egyszerre (quote prioritás)
// - CreateQuote success → QuoteDetail (nem CreateQuote)
// - CustomerDetail → QuoteDetail: CustomerDetail bezárul, QuoteDetail nyílik

type SalesDetailState =
  | { type: 'none' }
  | { type: 'quoteDetail'; quoteId: string }
  | { type: 'customerDetail'; customerId: string }
  | { type: 'createQuote' }
  | { type: 'createCustomer' }  // CreateQuoteSlideOver-ből hívva
```

`SalesDetailHost` — beágyazandó a `SalesWorldPage`-be és mindhárom listanézet alá:
- Rendereli az aktív SlideOver-t
- Átadja az `open*` / `close` handlereket a listanézeteknek

**Quote sor kattintás:** `SalesQuotes` táblázat sorai → `openQuoteDetail(q.id)`
**Customer kártya kattintás:** `SalesCustomers` kártyák → `openCustomerDetail(c.id)`
**"Új ajánlat" gomb:** `SalesQuotes` → `openCreateQuote()`

## Nem része ennek a feladatnak (Phase 3+)

- Pipeline funnel API bekötése (layout marad, csak adatcsere lenne)
- Konverziós ráta dedikált endpoint
- Customer↔Keycloak/Platform Actor link UI (Settings > Partners fedi le)
- Cím szerkesztés (`PUT .../addresses`) — csak megjelenítés most

## Tesztek

Meglévő tesztek mind zöldek. Új tesztek:

- `QuoteDetailSlideOver.test.tsx`:
  - Draft: inline sor szerkesztés (kattintás → input megjelenik)
  - Draft: új tétel sor render
  - Nettó/ÁFA/Bruttó összegzés helyes számítás
  - Sent: "Elfogadás" + "Elutasítás" gombok láthatók, szerkesztés nem
  - Accepted: "Gyártásba konvertálás" gomb → spinner → ConversionPending → Converted + toast
- `CreateQuoteSlideOver.test.tsx`:
  - Ügyfél typeahead: keresés → dropdown
  - Validáció: üres ügyfél submit → piros border
  - "Új ügyfél létrehozása →" fallback megjelenik
- `CustomerDetailSlideOver.test.tsx`:
  - Lead badge + amber avatar
  - "Szerkesztés" toggle → mezők szerkeszthetők
  - Mini quote lista render
  - Lead → Promóció confirm gomb látható
- `useSalesDetail.test.ts`:
  - CustomerDetail → QuoteDetail cross-navigation
  - CreateQuote success → QuoteDetail nyílik

Min. **+20 új teszt.**

## Definition of Done

- [ ] `QuoteDetailSlideOver` 680px — inline tétel szerkesztés Draft-ban, összegzés, FSM akciók
- [ ] `CreateQuoteSlideOver` 500px — typeahead, validáció, success → QuoteDetail auto-open
- [ ] `CustomerDetailSlideOver` 520px — gradient avatar, inline contact edit, mini quote lista, FSM
- [ ] `useSalesDetail` hook — cross-navigation szabályok
- [ ] `SalesDetailHost` beágyazva `SalesWorldPage`-be
- [ ] "Új ajánlat" gomb aktív, quote sorok és customer kártyák kattinthatók
- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` → mind zöld, ≥ +20 új teszt
