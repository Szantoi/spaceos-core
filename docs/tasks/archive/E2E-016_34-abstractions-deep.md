---
id: E2E-016
title: 34-abstractions-deep.chain.test.ts — cutting-list + cnc-plan + process-plan
status: new
priority: medium
assignee: e2e
epic: batch3-abstractions-deep
blocked_by: E2E-015 (37-tools DONE)
created: 2026-04-14
updated: 2026-04-14
docs:
  - docs/mailbox/e2e/outbox/2026-04-13_011_coverage-gap-report.md
---

## Feladat

Írd meg: `34-abstractions-deep.chain.test.ts` — a meglévő 35-configuration-engine
tesztben nem lefedett Abstractions manufacturing endpoint-ok tesztje.

## Üzleti indok

A szabászat modul (2026 Q3, második ügyfél előfeltétele) technikai alapja az Abstractions
modul `cutting-list`, `cnc-plan`, `process-plan` pipeline-ja. Ez jelenleg 0 E2E coverage.

## Hiányzó coverage (35-ben nincs)

```
POST /bff/abstractions/modules/templates/:id/cutting-list
POST /bff/abstractions/modules/templates/:id/cnc-plan
POST /bff/abstractions/modules/templates/:id/process-plan
GET  /bff/abstractions/modules/templates/:name/calculate   (by-name variant)
POST /bff/abstractions/modules/templates/:id/clone
```

Forrás:
- `/opt/spaceos/spaceos-modules-abstractions/.../Endpoints/ProductTemplateEndpoints.cs`
- Meglévő alap: `35-configuration-engine.chain.test.ts` (template + slot + calculate 200/401/403)

## Tervezett chain

```
1. Template létrehozás (35-ből ismert minta)
2. Slot(ok) hozzáadása (anyagjegyzékhez szükséges)
3. POST cutting-list → szabászkép generálás
4. POST cnc-plan → CNC program kimeneti struktúra
5. POST process-plan → gyártási lépések lista
6. GET by-name calculate → névszerinti kalkuláció
```

## Blokkoló feltétel

E2E-015 (37-tools.chain.test.ts) elfogadva a root által, és az összes meglévő teszt zöld.

## Megjegyzés

A `probe-and-skip` minta kötelező: ha az Abstractions modul nincs elérhető
(`GET /bff/abstractions/modules/templates` → 404/502), a tesztek return early-vel lépjenek ki,
ne törjék a suite-ot.
