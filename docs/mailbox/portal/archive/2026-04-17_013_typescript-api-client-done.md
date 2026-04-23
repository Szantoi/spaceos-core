---
id: MSG-PORTAL-013-DONE
from: portal
to: root
type: done
priority: high
status: READ
ref: MSG-PORTAL-013
created: 2026-04-17
---

# PORTAL-013 — TypeScript API client generálás — KÉSZ

## Összefoglaló

Commit: `358a6be` (branch: `develop`)

## Eszköz választás

- **openapi-typescript 7.13.0** — MIT license, OpenAPI 3.x → TypeScript types
- **openapi-fetch 0.13.x** — típusbiztos fetch wrapper

## Generálás eredménye

| Metrika | Érték |
|---|---|
| Forrás spec | `docs/openapi/kernel-v1.json` (165KB) |
| Generált fájl | `packages/@spaceos/api-client/src/generated/schema.d.ts` |
| Sorok száma | 5 062 |
| Endpoint path-ok | 55 |
| Generálási idő | 286ms |

## Exportált típusok (`@spaceos/api-client`)

```typescript
export type { KernelPaths, KernelComponents, KernelOperations } from './generated/schema.d.ts';
export { default as createClient } from 'openapi-fetch';
export type { paths } from './generated/schema.d.ts';
```

## Script

```bash
pnpm run generate:api
# openapi-typescript ../../../docs/openapi/kernel-v1.json -o src/generated/schema.d.ts
```

## Build + teszt

```
pnpm turbo build → Tasks: 2 successful, 2 total — 0 error
Tests: 306/306 green
```

## Kockázatok / kérdések

Nincsenek. A 6. pontban említett „opcionális demó migráció" nem lett elvégezve — a meglévő `apiClient` (Axios) marad, a generált típusokat az új feature modulok importálhatják `createClient`-en keresztül.
