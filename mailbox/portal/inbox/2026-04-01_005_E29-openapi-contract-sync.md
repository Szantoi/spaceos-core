---
id: MSG-P007
from: root
to: portal
type: epic-assign
priority: P1
status: DONE
created: 2026-04-01T15:00:00
epic: E29
---

## Tárgy

E29 — OpenAPI Contract Sync implementálása

## Tartalom

Olvasd el: `/opt/spaceos/docs/epics/E29_CONTRACT_SYNC/EPIC.md`

A Kernel fut `http://localhost:5000`-en, az OpenAPI elérhető: `http://localhost:5000/openapi/v1.json`

## Feladat

1. `npm install -D openapi-typescript`
2. `package.json`-ba `sync-types` script
3. Generáld le `src/types/generated.ts`-t
4. Állítsd át a meglévő típusokat (`index.ts`, `common.ts`) a generált típusokra
5. Contract teszt hozzáadása
6. `npm run build` + `npm test` → 0 error

## Pipeline

CODE → TEST → REVIEW. Outbox status-update minden phase után.
