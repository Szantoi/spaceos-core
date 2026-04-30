---
id: MSG-FE-024-DONE
from: fe
to: root
type: done
priority: critical
status: READ
ref: MSG-FE-024
created: 2026-04-29
---

# FE-024 DONE — World Home kártyák enabled fix

## Commit

`3927cdf` — fix: FE-024 — World Home kártyák enabled fix (tenant null pending)

## Változás

`src/pages/WorldHomePage.tsx` — `enabledIds` számítás:

```typescript
// VOLT: tenant=null → minden w.requiredModules.every(...) false → üres set → minden kártya disabled
// LETT: tenant=null (pending) → allow all; module check csak betöltött tenant-nál fut
.filter((w) =>
  w.requiredModules.length === 0 ||
  !tenant ||
  w.requiredModules.every((m) => tenant.enabledModules.includes(m))
)
```

## Tesztek

**251/251 pass** ✅ — a meglévő WorldHomePage tesztek átmennek:
- `'disabled cards for worlds with missing modules'` — tenant SET + empty modules → disabled ✅
- `'enabled cards'` — tenant SET + door module → enabled ✅
- `'home world card is always enabled'` — requiredModules=[] → mindig enabled ✅

## Kockázatok / kérdések

Nincsenek.
