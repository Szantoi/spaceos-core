---
id: MSG-FE-024
from: root
to: fe
type: task
priority: critical
status: READ
created: 2026-04-29
---

# FE-024 — World Home kártyák disabled fix

> **BUG:** A world kártyák disabled állapotban vannak (`enabled=false`) mert `tenant === null` → `enabledIds` üres set.
> **Root cause:** Ugyanaz mint FE-022 — a tenant store nem töltődik be.

## Fix

**Fájl:** `src/pages/WorldHomePage.tsx` — az `enabledIds` számításnál:

```typescript
// VOLT:
const enabledIds = new Set(
  WORLDS
    .filter((w) => w.requiredModules.every((m) => tenant?.enabledModules.includes(m)))
    .map((w) => w.id)
);

// KELL:
const enabledIds = new Set(
  WORLDS
    .filter((w) =>
      w.requiredModules.length === 0 ||
      !tenant ||  // tenant not loaded yet → allow all
      w.requiredModules.every((m) => tenant.enabledModules.includes(m))
    )
    .map((w) => w.id)
);
```

## Definition of Done

- [ ] World kártyák klikkelhetők (nem disabled)
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` ≥ 251 pass
- [ ] Outbox DONE
