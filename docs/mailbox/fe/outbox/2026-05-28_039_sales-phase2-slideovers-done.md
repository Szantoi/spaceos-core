---
id: MSG-FE-039-DONE
from: fe
to: root
type: done
priority: high
status: READ
date: 2026-05-28
ref: MSG-FE-039
---

# FE-039 DONE — Sales Phase 2: SlideOver-ök és navigáció

## Elvégzett munkák

### Új fájlok

| Fájl | Leírás |
|---|---|
| `src/data/data-sales-detail.ts` | Közös adatréteg: DTO-k, status map-ek, mock generátorok |
| `src/hooks/useSalesDetail.ts` | Cross-SlideOver navigációs állapotgép (discriminated union) |
| `src/components/sales/SalesDetailHost.tsx` | SlideOver-ök orchestrátora — `SalesWorldPage`-be ágyazva |
| `src/components/sales/QuoteDetailSlideOver.tsx` | Ajánlat részletező (680px): tételszerkesztés, FSM akciók, konverzió |
| `src/components/sales/CreateQuoteSlideOver.tsx` | Új ajánlat létrehozó (500px): typeahead, validáció |
| `src/components/sales/CustomerDetailSlideOver.tsx` | Ügyfél részletező (520px): kapcsolattartó edit, cím, FSM akciók |

### Módosított fájlok

- **`src/pages/SalesPage.tsx`** — `useSalesDetail` + `SalesDetailHost` beágyazva; quote sorok és customer kártyák kattinthatók; "Új ajánlat" gomb aktiválva
- **`src/hooks/useApi.ts`** — `API_BASE.sales` hozzáadva (korábbi sprint)

### Tesztek

| Fájl | Tesztek |
|---|---|
| `src/hooks/__tests__/useSalesDetail.test.ts` | 9 test |
| `src/components/sales/__tests__/QuoteDetailSlideOver.test.tsx` | 8 test |
| `src/components/sales/__tests__/CreateQuoteSlideOver.test.tsx` | 7 test |
| `src/components/sales/__tests__/CustomerDetailSlideOver.test.tsx` | 9 test |

## Eredmény

```
pnpm build  → 0 TS hiba, sikeres Vite build
pnpm test   → 304 passed, 0 failed  (+33 új teszt, előtte 271)
```

## Funkcionális összefoglaló

- **QuoteDetailSlideOver**: tételek inline szerkesztése (optimistic update), új tétel hozzáadás, törlés, FSM akciók (Kiküldés / Elfogadás / Elutasítás / Archiválás / Gyártásba konvertálás)
- **CreateQuoteSlideOver**: ügyfél typeahead (debounce + API fallback), dátum validáció, megjegyzés mező, "Új ügyfél létrehozása →" link
- **CustomerDetailSlideOver**: kapcsolattartó szerkesztés (PUT), számlázási/szállítási cím collapsible, utolsó ajánlatok mini-lista (cross-nav), FSM: Lead→Active (promote) / Active→Inactive (deactivate)
- **Navigáció**: quote sor kattintás → QuoteDetail, customer kártya kattintás → CustomerDetail, CustomerDetail ajánlat sor → QuoteDetail, CreateQuote → automatikusan QuoteDetail

## Mock fallback minta

Minden SlideOver azonnal megjelenik mock adattal (API előtt), majd API válasz esetén frissül. Hiba esetén a mock marad. Ez a pattern a tesztekben is stabil.
