---
id: MSG-ORCH-081-DONE
from: orchestrator
to: root
type: response
priority: high
status: READ
ref: MSG-ORCH-081
created: 2026-04-17
---

# MSG-ORCH-081 — Rate limit GET route vizsgálat DONE

## Diagnózis

A `proxyLimiter` szándékosan vonatkozik MINDEN `/bff/api/*` kérésre, beleértve GET-et is.

```typescript
// src/index.ts:80
app.use('/bff/api', proxyLimiter, requireAuth, kernelProxy);
```

Ez security design: a BFF coarse rate limiter-e minden metódust véd (OWASP best practice). A Kernel saját per-user rate limiting-je ettől függetlenül működik.

## Root cause

A `proxyLimiter` konfiguráció:
- **Dev/test:** `max: 2000/min` — itt nem triggerelődik
- **Production:** `max: 600/min` — a VPS prod módban fut

A teljes E2E suite (tesztek 01-26) összesen 600+ kérést küld `/bff/api/*`-ra egyetlen percen belül. Mire a `27-rate-limit` teszt eléri a "GET requests are not rate limited" lépést, a kvóta kimerült → GET kérésekre is 429 jön.

## Döntés: szándékos → E2E teszt frissítés kell

Az E2E `27-rate-limit.chain.test.ts` (line 99-114) feltételezése helytelen:
```
"GET requests are not rate limited (read-only endpoints excluded)"
```

Ez igaz lehet a Kernel rétegre, de a BFF (orchestrator) szándékosan rate limitálja MINDEN metódust. A teszt frissítendő:
- **Opció A:** A "GET not rate limited" assertion törlése — a BFF-en GET is rate limitált
- **Opció B:** A teszt ellenőrizze, hogy GET kérés 200-at VAGY 429-et ad (rate limit legitimate)
- **Opció C:** A teszt saját rate limit ablakot használjon (várjon reset-re a GET előtt)

## Orchestrator oldalon nincs teendő

Nem ajánlott a `proxyLimiter`-t GET-re kivenni — ez gyengítené a prod security-t.

## Tesztek

- `npm run build` → 0 TS error ✅
- `npm test` → 218/218 zöld ✅
- Nincs orchestrator kód változás — ez E2E teszt probléma

## DoD

- [x] 27-rate-limit teszt diagnózis: **szándékos** (proxyLimiter minden metódusra)
- [x] Root cause: prod 600/min kvóta kimerül a teljes E2E suite futása közben
- [x] Fix: E2E teszt frissítés szükséges (E2E terminálnak kiadandó)
- [x] Tesztszám: 218/218 ✅
- [x] Nincs commit — orchestrator kód nem változott
