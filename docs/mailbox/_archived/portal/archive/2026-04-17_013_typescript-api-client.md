---
id: MSG-PORTAL-013
from: root
to: portal
type: task
priority: high
status: READ
ref: SPRINT6
created: 2026-04-17
---

# PORTAL-013 — TypeScript API client generálás (@spaceos/api-client)

## Kontextus

KERNEL-087 (Sprint 5) óta a `/openapi/v1.json` spec elérhető Production-ban is,
és `docs/openapi/kernel-v1.json`-ban gitben van. Ez feloldja az ADR-07 TypeScript
client generálást. A `packages/@spaceos/api-client` package már létezik a monorepo-ban
— ezt kell auto-generált típusokkal feltölteni.

## Tudásbázis referencia

- `docs/knowledge/context/PORTAL_CONTEXT.md` — terminál kontextus
- `docs/openapi/kernel-v1.json` — Kernel OpenAPI spec (165KB, forrás)

## Feladat

### 1. Tool telepítés

```bash
cd packages/@spaceos/api-client
pnpm add -D openapi-typescript openapi-fetch
```

`openapi-typescript` — MIT license, TypeScript típusok generálása OpenAPI 3.x spec-ből.
`openapi-fetch` — típusbiztos fetch wrapper a generált típusok alapján.

### 2. Generáló script

`packages/@spaceos/api-client/package.json`:
```json
{
  "scripts": {
    "generate:api": "openapi-typescript ../../../docs/openapi/kernel-v1.json -o src/generated/schema.d.ts"
  }
}
```

### 3. Generálás futtatása

```bash
pnpm run generate:api
# → src/generated/schema.d.ts létrejön
```

### 4. Export

`packages/@spaceos/api-client/src/index.ts`:
```typescript
export type { paths, components, operations } from './generated/schema.d.ts';
export { createClient } from 'openapi-fetch';
export type { paths as KernelPaths } from './generated/schema.d.ts';
```

### 5. turbo build zöld

```bash
pnpm turbo build
# Tasks: 7 successful, 7 total — 0 error
```

### 6. Doorstar Portal import (opcionális demó)

Ha van idő: a Doorstar Portal-ban egy meglévő API hívást (`fetch('/bff/api/...')`)
migráld a generált típusokra — demonstrálja a flow-t.

## Build gate

```bash
pnpm turbo build
# 0 error

pnpm test
# ≥ 306 pass, 0 fail
```

## DONE feltételek

- [ ] `src/generated/schema.d.ts` létrejön és valid TypeScript
- [ ] `pnpm run generate:api` script fut hibátlanul
- [ ] `@spaceos/api-client` exportálja a típusokat
- [ ] `pnpm turbo build` 0 error
- [ ] Tesztszám ≥ 306
- [ ] Commit hash
- [ ] OUTBOX DONE: tool választás + hány endpoint/típus generálódott

## Skill

Használd a `/spaceos-terminal` skillt. Sub-agent **engedélyezett**.
