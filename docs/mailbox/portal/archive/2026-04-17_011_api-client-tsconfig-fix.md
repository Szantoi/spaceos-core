---
id: MSG-PORTAL-011
from: root
to: portal
type: task
priority: low
status: READ
ref: MSG-INFRA-147-DONE
created: 2026-04-17
---

# PORTAL-011 — Tech debt: api-client tsconfig vite/client fix

## Kontextus

Az INFRA-147 deploy során a `turbo build` parancs az `@spaceos/api-client` package
`tsc --noEmit` lépésénél hibával bukott — `import.meta.env` típus hiányzik, mert a
`packages/@spaceos/api-client/tsconfig.json`-ban nincs `"types": ["vite/client"]`.

Ez **pre-existing tech debt**, nem Sprint 3 regresszió. A `joinerytech` app közvetlen
build-je (`apps/joinerytech && pnpm run build`) nem érintett — az éles build-en nincs hatás.

## Feladat

`packages/@spaceos/api-client/tsconfig.json` javítása:

```json
{
  "compilerOptions": {
    "types": ["vite/client"]
    // ... meglévő config
  }
}
```

Ellenőrzés:
```bash
cd /opt/spaceos/spaceos-design-portal
pnpm turbo build   # 0 error elvárt
```

## DONE feltételek

- [ ] `packages/@spaceos/api-client/tsconfig.json` frissítve
- [ ] `pnpm turbo build` 0 error
- [ ] `npm run test -- --run` (portal unit tesztek) zöld
- [ ] commit + push develop-ra
- [ ] OUTBOX DONE üzenet commit hash-sel
