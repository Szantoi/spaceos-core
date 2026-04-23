---
id: MSG-ORCH-058
from: root
to: orchestrator
type: task
priority: medium
status: READ
ref: MSG-E2E-006-DONE
created: 2026-04-12
---

# MSG-ORCH-058 — BFF rate limiter middleware sorrend fix

## Kontextus

Az E2E-006 rerun során `26-sse-chat-live.chain.test.ts:190` fail-t jelzett:

```
expected [ 401 ] to include 429
```

A teszt no-auth hívást küld a chat stream endpointnak, és 401-et vár. Ehelyett 429-et kap — a rate limiter az auth check **előtt** válaszol.

**Root cause:** A BFF Express middleware stack-ben a rate limiter előbb fut, mint az auth/JWT validátor. Egy nem authentikált kérés így 429-et kap, ha a window-ban már elég hívás volt — ahelyett, hogy 401-et kapna.

---

## Feladat — Middleware sorrend korrigálása

Az Express app middleware stack-jében az auth middleware-nek a rate limiter **előtt** kell futnia:

```typescript
// app.ts vagy server.ts (jelenlegi, valószínűleg):
app.use(rateLimiter);       // ← ez fut először
app.use(authMiddleware);    // ← ez fut másodszor

// Helyes sorrend:
app.use(authMiddleware);    // ← 401 unauthorized → kérés meghal itt
app.use(rateLimiter);       // ← csak authentikált kérések érik el
```

**Alternatíva** (ha az auth middleware globálisan nem alkalmazható minden route-ra):

A rate limiter-t csak authentikált route-okra alkalmazd:
```typescript
// Route-szintű middleware:
router.post('/sse/chat', authMiddleware, rateLimiter, chatStreamHandler);
```

**Ellenőrizd** az Orchestrator `src/` könyvtárban:
- Hol van a rate limiter regisztrálva? (`express-rate-limit` vagy custom)
- Hol van az auth middleware? (JWT verify)
- Melyik route(ok)ra vonatkozik ez az ütközés? (SSE chat endpoint)

---

## Definition of Done

- [ ] Auth middleware a rate limiter előtt fut az SSE chat endpointon
- [ ] No-auth hívás → 401 (nem 429)
- [ ] Meglévő **177 teszt zöld**
- [ ] Commit + push

## Visszajelzés

Outboxba: `MSG-ORCH-058-DONE`

## Prioritás

`medium` — ez az egyetlen Orchestrator-oldali E2E fail, a többi 4 Kernel-oldali (MSG-KERNEL-062). Párhuzamosan futtatható.
