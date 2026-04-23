---
id: MSG-ORCH-067-DONE
from: orchestrator
to: root
type: response
priority: high
status: READ
ref: MSG-ORCH-067
created: 2026-04-15
---

# MSG-ORCH-067 — Sprint 5: Test Coverage DONE

## Összefoglaló

3 DoD pont teljesítve, + 1 security kódbővítés.

### proxy.errors.test.ts — 7 teszt ✅

| Eset | Eredmény |
|---|---|
| ECONNREFUSED → BFF 502 | ✅ |
| ETIMEDOUT (30s hang) → BFF 502, teszt azonnali | ✅ |
| Raw ServerResponse (nincs .status) → no crash | ✅ |
| Kernel 500 → proxyRes.statusCode remapped 502 | ✅ |
| Kernel 504 → passthrough (504 marad) | ✅ |
| Kernel 200 → passthrough (változatlan) | ✅ |
| Proxy timeout konfigurálva (>0) | ✅ |

### requireAuth.matrix.test.ts — 8 teszt ✅

| Eset | HTTP | |
|---|---|---|
| Nincs Authorization header | 401 | ✅ |
| Lejárt token (TokenExpiredError) | 401 | ✅ |
| Rossz issuer | 401 | ✅ |
| Rossz audience | 401 | ✅ |
| Hiányzó tid claim | 401 | ✅ |
| tid mismatch (requireTenantScope) | 403 | ✅ |
| Helyes token tid-del | 200 | ✅ |
| Egyező tid → 200 | 200 | ✅ |

### SSE abort teszt — 1 teszt ✅

Client disconnect → `AbortController.abort()` ténylegesen lefut — nincsenek socket leak-ek.

## Kódbővítések (production)

1. **`kernel.proxy.ts`** — `proxyRes` handler hozzáadva: Kernel 500 → 502 (statusCode mutáció a proxy-ban, mielőtt a response kiküldődik)
2. **`auth.middleware.ts`** — `tid` claim validáció: hiányzó tid → 401 (cross-tenant adatszivárgás megelőzése); `requireTenantScope(param)` factory exportálva

## Tesztek

- **207/207 teszt zöld** (volt: 191, új: 16)
- 28 tesztfájl, 0 failure

## Security review

- ✅ Proxy error handler: 502 visszaadva, nem lép ki csendesen
- ✅ tid claim validáció: minden requireAuth-t használó route-on aktív
- ✅ requireTenantScope: csak az explicit paramétert ellenőrzi, nem implicit default
- ✅ SSE abort: nincs socket leak client disconnect esetén
- ✅ Nincs TODO/FIXME a kódban

## Commit

`b3860ac` — pushed to `origin develop`
