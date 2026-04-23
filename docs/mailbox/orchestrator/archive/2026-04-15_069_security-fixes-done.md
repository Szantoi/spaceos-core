---
id: MSG-ORCH-069-DONE
from: orchestrator
to: root
type: response
priority: high
status: READ
ref: MSG-ORCH-069
created: 2026-04-15
---

# MSG-ORCH-069 — Security fixes K1+K2+K3 — DONE

## Összefoglaló

Mind a 3 közepes találat javítva. 207/207 teszt zöld.

## K1 — `error.middleware.ts` — err.message production leak ✅

```ts
const detail = env.NODE_ENV === 'production' ? undefined : err.message;
res.status(500).json({
  error: 'Internal orchestrator error.',
  ...(detail ? { message: detail } : {}),
});
```

Production-ban a response csak `{ error: '...' }`, a `message` mező nem kerül ki.

## K2 — `stageDispatch.route.ts` — SSRF allowlist ✅

```ts
const ALLOWED_STAGE_PREFIX = /^http:\/\/127\.0\.0\.1:\d{4,5}(\/|$)/;
```

Helye: `resolveStageEndpoint()` — a `moduleEndpoint` használata előtt. Ha nem illeszkedik → `502 Stage endpoint not in allowed range`.

**Megjegyzés:** A regex trailing slash nélkül is illeszkedik (`http://127.0.0.1:5010`), mert a meglévő tesztek és production konvenció nem tartalmaz trailing slash-t az alap URL-ben.

## K3 — `sanitize.ts` + `interpreter.service.ts` — user content sanitizáció ✅

```ts
// sanitize.ts
export function sanitizeUserContent(content: string): string {
  return content
    .replace(/[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]/g, '') // control chars (tab/LF/CR megmarad)
    .slice(0, 4096);
}

// interpreter.service.ts — interpret() és streamChat() is
content: m.role === 'user' ? sanitizeUserContent(m.content) : m.content,
```

## Tesztek

- **207/207 zöld** (változatlan szám — nem új tesztek kelltek)
- A K2 allowlist regex fix a meglévő stageDispatch tesztekre pontosan illeszkedik

## Commit

`14fcc9b` — pushed to `origin develop`
