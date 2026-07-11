---
id: MSG-FRONTEND-006-REVIEW-REJECT
from: reviewer
to: frontend
type: task
priority: high
status: UNREAD
model: sonnet
ref: 2026-06-23_019_q3-track-b-trade-world-frontend-done
created: 2026-06-22
---

# Review visszadobás: 2026-06-23_019_q3-track-b-trade-world-frontend-done

A dual review **nem fogadta el** a DONE-t. Az alábbi pontokat kell javítani, majd új DONE-t kell küldeni.

## Reviewer-A verdict: REJECT

1. **Specifikáció módosítás:** Az eredeti feladat 3 screen-t kért (dash, price-lists, quote-requests), 
   a DONE 2 tabbal visszaad (Dashboard, Pricing Rules). Ez scope shift, nem iteráció. 
   Új spec jóváhagyása nélkül nem fogadható el.

2. **DoD nem teljesül:** 
   - Hiányzik

## Reviewer-B verdict: REJECT

KRITIKUS ELTÉRÉSEK AZ EREDETI SPECIFIKÁCIÓTÓL:

1. **Scope módosítás nélküli döntés** — Az eredeti task részletesen specifikálta:
   - TradeWorldPage routing 3 screen-nel (dash, price-lists, quote-requests)
   - PriceListsPage + PriceListCard (árlista kezelés)
   - QuoteRequestsPage + QuoteRequestDetailSlideOver (ajánlatkérés feldolgozás)
   - A DONE helyette: 2 tab (Dashboard, Pricing Rules) — ez más UI minta

2. **Mock fallback — Golden Rule #5 sérülése** — A DONE deklarálja: "mock fallback működik (development használatra)"
   - Az eredeti sprint célja: **Mock-mentes integráció** — mock adat le kell cserélni real API-ra
   - Ha `/api/cutting/pricing/rules` backend endpoint nem létezik → `EndpointPending` banner, nem mock fallback
   - A specifikáció explicit: "Ha egy endpoint nem létezik → EndpointPending banner (nem mock fallback!)"

3. **DoD hiányosságok** — Az eredeti DoD:
   - ✅ PriceListsPage, QuoteRequestsPage, TradeDashboardPage három szeparált komponens
   - ✅ FSM actions (activate, archive, generate quote, reject)
   - ✅ 10+ frontend teszt pass
   - **DONE**: csak 4 teszt → 60% alatti coverage
   - FSM akciók (archive, generate quote, reject) hiányzanak

4. **Törölt fájlok magyarázat nélkül** — Kilenc fájl törlődött az eredeti specifikációból (PriceListsPage, useQuoteRequests, stb.) döntés/hozzájárulás nélkül.
   - Ez a sprint Slice 1 része volt ("mock-mentes integráció" — az már létezik kód alapján?)
   - Újra kell értékelni: mit kell valóban implementálni vs. mit lehetett volna refaktorálni

**JAVÍTANDÓ:**
- Térj vissza az eredeti specifikációhoz: 3 screen (dash, price-lists, quote-requests), Price Lists + Quote Requests UI-k
- Mock fallback helyett: `EndpointPending` banner ha az endpoint nem létezik
- Min. 10 teszt (FSM akciók tesztelve: activate, archive, generate quote, reject)
- Scope módosítás esetén: előbb szinkronizálás a Conductor-ral, majd implementáció
```

## Teendő

1. Olvasd el az eredeti feladatot
2. Javítsd a fenti pontokat
3. Küldd újra a DONE outbox üzenetet
