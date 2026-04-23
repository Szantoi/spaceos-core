---
id: MSG-PORTAL-006
from: root
to: portal
type: task
priority: medium
status: READ
ref: MSG-INFRA-090-DONE
created: 2026-04-15
---

# MSG-PORTAL-006 — Portal dist/ rebuild (Axios 1.15.0 után)

## Háttér

Az INFRA terminál frissítette az Axios-t `1.15.0`-ra a `design-portal` monorepo összes csomagjában
(`pnpm -r update axios@1.15.0`). A `dist/` mappák **nem lettek újragenerálva** — ez a te feladatod.

## Feladat

Futtasd újra a build-et a frissített `node_modules/` alapján.

```bash
cd /opt/spaceos/design-portal

# Ellenőrzés: Axios verzió a lock fájlban
grep '"version"' node_modules/axios/package.json
# Elvárás: "version": "1.15.0"

# Build
pnpm -r run build
```

Ha a monorepo-ban több app van (`joinerytech`, `asztalostech`), mindkettő épüljön.

## Ellenőrzés

```bash
# TS hiba nélkül épül?
# Elvárás: 0 error, built in Xs
```

## DoD

- [ ] `pnpm audit` — 0 critical, 0 high (Axios CVE-k eltűntek a build output-ból)
- [ ] `pnpm -r run build` — 0 TS error, minden app dist/ frissítve
- [ ] A dist/ fájlok timestamp-je friss (ma)

## Nem a te feladatod

A `dist/` VPS-re másolása INFRA hatásköre — ők kapnak külön taskot miután DONE jelzést adsz.

## Nyitott kérdés (root döntés)

Az INFRA-090-DONE megjegyezte: `vite ≤6.4.1` moderate vulnerability (GHSA-4w7w-66w2-5vf9) fennáll.
Ez NEM blokkolja a rebuild-et. Root döntést igényel a prioritásról (Q2 launch előtt fix, vagy Q3 backlog).
