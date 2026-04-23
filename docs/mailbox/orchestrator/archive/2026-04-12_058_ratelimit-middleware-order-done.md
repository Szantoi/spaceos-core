---
id: MSG-ORCH-058-DONE
from: orchestrator
to: root
type: response
ref: MSG-ORCH-058
status: DONE
created: 2026-04-12
---

## Összefoglaló

**Root cause:** `index.ts`-ben a `/bff/chat` route-on `chatLimiter` futott `requireAuth` előtt.
Unauthentikált kérések a rate limiter windowból 429-et kaptak 401 helyett.

**Fix — `src/index.ts`:**
```typescript
// Előtte:
app.use('/bff/chat', chatLimiter, chatRouter);

// Utána:
app.use('/bff/chat', requireAuth, chatLimiter, chatRouter);
```

**Commit:** `9d02196` — pushed to `origin develop`

## Változott fájlok

| Fájl | Változás |
|---|---|
| `src/index.ts` | `requireAuth` a `chatLimiter` elé került a `/bff/chat` route-on |
| `src/routes/chat.route.test.ts` | +1 teszt: no-auth → 401 akkor is, ha rate limit ki van merítve |

## Tesztek

- **178/178 teszt zöld** (volt 177 — +1 új teszt)
- Build: 0 TypeScript hiba

## Security review

- ✅ Auth middleware sorrendje javítva — unauthentikált kérések 401-et kapnak
- ✅ Rate limit megmarad autentikált kérésekre
- ✅ Nincs TODO/FIXME a kódban
- ✅ Zod validáció érintetlen

## Kockázatok / kérdések

Nincs. A `requireAuth` most redundánsan fut (app szinten + route handleren belül),
de ez funkcionálisan helyes és a meglévő unit tesztek struktúráját nem törte.
