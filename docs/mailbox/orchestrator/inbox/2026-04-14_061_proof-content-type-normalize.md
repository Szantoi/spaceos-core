---
id: MSG-ORCH-061
from: root
to: orchestrator
type: task
priority: critical
status: READ
ref: MSG-E2E-015-DONE
created: 2026-04-14
---

# MSG-ORCH-061 — proof.route.ts: Content-Type normalizálás (415 fix)

## Probléma

A `36-proof.chain.test.ts` teszt 1 (`POST /bff/tasks/:id/proof`) **415 Unsupported Media Type** hibát kap, miután az ORCH-060 fix a 404-et megoldotta.

**Diagnózis (E2E-015-DONE alapján):**

```
BFF POST /bff/tasks/:id/proof  (Content-Type: image/jpeg)        → 415
Kernel POST /api/flow-epics/:id/proof (Content-Type: image/jpeg) → 415
Kernel POST /api/flow-epics/:id/proof (Content-Type: application/octet-stream) → 200 ✅
```

A Kernel `/api/flow-epics/:id/proof` endpoint `.Accepts<byte[]>("application/octet-stream")` deklarációt használ — csak `application/octet-stream` MIME-et fogad el.

A `proof.route.ts` jelenlegi kódja a user Content-Type-ot változatlanul továbbítja:

```ts
const headers: Record<string, string> = {
  'Content-Type': req.headers['content-type'] ?? contentType,  // image/jpeg átmegy
};
```

## Fix

**Fájl:** `src/routes/proof.route.ts`

A Kernel-felé induló hívásban a `Content-Type`-ot `application/octet-stream`-re kell normalizálni. Az eredeti MIME-et `X-SpaceOS-Original-Content-Type` fejlécben kell megőrizni (auditálhatóság + jövőbeli Kernel validáció).

```ts
const headers: Record<string, string> = {
  'Content-Type': 'application/octet-stream',           // Kernel csak ezt fogadja el
  'X-SpaceOS-Original-Content-Type': contentType,       // eredeti MIME megőrzése
};
```

**Fontos:** A user felé visszaadott response (`{ url, hash }`) nem változik — csak a Kernel felé induló proxy-hívás Content-Type-ja.

## Tesztelendő (E2E szinten elfogadható)

A 36-proof teszt 1 after this fix:
- `POST /bff/tasks/:id/proof` (Content-Type: image/jpeg) → **200** `{ url, hash }` ✅
- `POST /bff/tasks/:id/proof` (Content-Type: text/plain) → 415 (MIME whitelist még a BFF-nél — nem változik)

## DoD

- [ ] `proof.route.ts` Content-Type normalizálás megvalósítva
- [ ] `dotnet build` / `npm run build` → 0 error, 0 warning
- [ ] Unit tesztek: 183 zöld (meglévők + 1 új: `Content-Type: image/jpeg → kernel hívásban application/octet-stream`)
- [ ] DONE outbox: commit hash + teszt szám
