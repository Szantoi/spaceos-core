---
id: MSG-ORCH-069
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: MSG-ORCH-068-DONE
created: 2026-04-15
---

# MSG-ORCH-069 — Security fixes: K1 + K2 + K3

## Feladat

A security review (MSG-ORCH-068) 3 közepes találatot azonosított. Javítsd mind a hármat.

---

### K1 — `error.middleware.ts` — err.message production leak

```ts
// src/middleware/error.middleware.ts
const detail = process.env.NODE_ENV === 'production' ? undefined : err.message;
res.status(500).json({
  error: 'Internal orchestrator error.',
  ...(detail ? { message: detail } : {}),
});
```

---

### K2 — `stageDispatch.route.ts` — SSRF allowlist

```ts
// src/routes/stageDispatch.route.ts — a moduleEndpoint használata előtt
const ALLOWED_STAGE_PREFIX = /^http:\/\/127\.0\.0\.1:\d{4,5}\//;
if (!ALLOWED_STAGE_PREFIX.test(stage.moduleEndpoint)) {
  return res.status(502).json({ error: 'Stage endpoint not in allowed range' });
}
```

---

### K3 — `interpreter.service.ts` — user content sanitizáció

A `sanitize.ts` kiterjesztése: user message content-re legalább control character strip:

```ts
// Meglévő sanitize.ts-be vagy külön util:
export function sanitizeUserContent(content: string): string {
  return content
    .replace(/[\x00-\x08\x0B\x0C\x0E-\x1F\x7F]/g, '')  // control chars
    .slice(0, 4096);  // max length
}
```

Az `interpreter.service.ts`-ben a user message content-et hívd át `sanitizeUserContent()`-on mielőtt az LLM-nek megy.

---

## DoD

- [ ] K1: `err.message` production-ban nem kerül a response-ba
- [ ] K2: `stage.moduleEndpoint` loopback allowlist ellenőrzés aktív
- [ ] K3: user content sanitizáció `interpreter.service.ts`-ben
- [ ] `npm run build` → 0 TS error
- [ ] `npm test` → meglévők zöldek (207+)
- [ ] Commit + push develop
