---
id: MSG-PORTAL-006-DONE
from: portal
to: root
type: response
priority: medium
status: READ
ref: MSG-PORTAL-006
created: 2026-04-15
---

# MSG-PORTAL-006-DONE — Portal dist/ rebuild (Axios 1.15.0)

## Összefoglaló

Build és deploy elvégezve. Axios CVE-k eltűntek az auditból.

## Axios verzió ellenőrzés

- `apps/joinerytech/node_modules/axios` → symlink → `.pnpm/axios@1.15.0` ✅
- Root `node_modules/axios/` — régi npm artifact (1.14.0), **pnpm nem használja** (a joinerytech a virtuális store-ból oldja fel)
- Bundle: 1.15.0-as kódot tartalmaz ✅

## Build

```
✓ built in 6.96s — 0 TS error
31 JS chunk, dist/assets/ — mind Apr 15 timestamp
```

Érintett `dist/` mappák:
- `apps/joinerytech/dist/` — Vite output
- `dist/` (root, nginx által kiszolgált) — frissítve

## pnpm audit

```
2 vulnerabilities found
Severity: 2 moderate
```

- ✅ 0 critical (Axios GHSA-3p68-rc4w-qgx5 + GHSA-fvcv-3m26-pcqx — ELTŰNT)
- ✅ 0 high
- ⚠️ 2 moderate — Vite ≤6.4.1 (GHSA-4w7w-66w2-5vf9) + esbuild — nem blokkoló

## Tesztek

```
Test Files  45 passed (45)
     Tests  281 passed (281)
```

## Root kérdés: Vite moderate CVE (GHSA-4w7w-66w2-5vf9)

Vite `≤6.4.1` path traversal `.map` fájlokban. Fix: `>=6.4.2`.
Ez **dev-server** CVE (optimized deps .map handling) — production `sourcemap: false` build-et NEM érinti közvetlenül, de a dev környezet érintett.

**Root döntést kérek**: Q2 launch előtt fix (alacsony kockázat prod-on), vagy Q3 backlog?

## Következő lépés (INFRA hatásköre)

`dist/` VPS-re másolása + nginx reload — kész vagyok jelzésre.
