---
id: MSG-ORCH-067
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: R-16
created: 2026-04-15
---

# MSG-ORCH-067 — Sprint 5: Test Coverage — Proxy hibakezelés + requireAuth mátrix

## Háttér

Devils-advocate audit (2026-04-15) két kritikus gap-et azonosított (R-16):

1. A `/bff/api/*` catch-all proxy-nak nincs egyetlen tesztje sem upstream hibákra (Kernel le, 500, timeout) → production incident esetén a portal 30s-ig fagy
2. A `requireAuth` middleware missing `tid` claim esete a legveszélyesebb: ha csendesen default-ol, cross-tenant adatszivárgás lehetséges

## Feladat

### 1. `proxy.errors.test.ts`

nock vagy msw segítségével mockold a Kernel-t, és teszteld:
- `ECONNREFUSED` → BFF válasz: 502, bounded timeout (max 5s)
- Kernel 500 → BFF 502, hibakód megőrzve
- Kernel 504 → BFF 504
- Kernel 30s hang → BFF timeout, nem fagy be

### 2. `requireAuth.matrix.test.ts`

Tesztelj minden negatív esetet:

| Eset | Elvárás |
|---|---|
| Nincs Authorization header | 401 |
| Lejárt token (exp múltban) | 401 |
| Rossz issuer (másik realm) | 401 |
| Rossz audience | 401 |
| Hiányzó `tid` claim | 401 |
| `tid` értéke nem egyezik az útvonallal | 403 |

### 3. SSE abort teszt

Ha a kliens disconnectel, az upstream `AbortController` meghívódik-e?
Egy egyszerű teszt: kliens disconnectel → upstream request abortált → nincs socket leak.

## DoD

- [ ] `proxy.errors.test.ts` létezik, 4 eset zöld
- [ ] `requireAuth.matrix.test.ts` létezik, 6 eset zöld
- [ ] SSE abort teszt zöld
- [ ] Tesztszám ≥ 191
- [ ] DONE outbox: új tesztszám + összefoglaló

