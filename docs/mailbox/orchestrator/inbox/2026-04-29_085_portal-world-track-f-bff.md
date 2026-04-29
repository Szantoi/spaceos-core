---
id: MSG-ORCH-085
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: SpaceOS_Portal_World_Architecture_v4_final.md
created: 2026-04-29
---

# ORCH-085 — Portal World Track F: BFF Routes (Day 1–4)

> **Tervdok:** `docs/tasks/active/SpaceOS_Portal_World_Architecture_v4_final.md` — Section 5.2
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **FONTOS:** F.1 Day 1 CONTRACT_ISSUES audit — `/bff/abstractions/*` frontend callers ellenőrzés!
> **Használhatsz sub-agent-eket** ha szükséges

---

## Day 1 — F.1 Contract freeze + dependency

### Új dependency

```bash
npm install opossum @types/opossum
```

### CONTRACT_ISSUES audit

Ellenőrizd: a `spaceos-doorstar-portal` kódbázisban melyik file hívja `/bff/abstractions/*` endpointot?

```bash
grep -r "abstractions" /opt/spaceos/spaceos-doorstar-portal/src/ --include="*.ts" --include="*.tsx" | grep -v node_modules
```

Eredményt írd CONTRACT_ISSUES.md-be.

---

## Day 2–3 — 16 új route

A tervdok Section 5.2 alapján:

### Session + aggregátor

```
GET  /bff/api/me/session     — KC userinfo + tenant + enabledModules aggregálás
GET  /bff/api/me/home-state  — dashboard data
```

### Meglévő bővítés

```
GET  /bff/api/tenant         — tenant info
GET  /bff/api/audit          — audit events (proxy Kernel)
GET  /bff/api/users          — user list (proxy Kernel)
```

### Manufacturing proxy (mock-first!)

```
GET  /bff/manufacturing/orders            → http://127.0.0.1:5007/api/manufacturing/orders
GET  /bff/manufacturing/orders/:id        → proxy
POST /bff/manufacturing/edge-banding/*    → proxy
POST /bff/manufacturing/cnc/*             → proxy
GET  /bff/manufacturing/tasks/:id/full    → aggregátor
```

**Mock-first:** Ha Manufacturing (5007) nem elérhető → 503 + circuit breaker (opossum)

### Shop Floor

```
POST /bff/shopfloor/pin/login   — PIN auth (BFF-managed)
POST /bff/shopfloor/pin/logout
GET  /bff/shopfloor/tasks
GET  /bff/shopfloor/task/:id/status
```

### Middleware chain

- Error normalizer (BFF-04)
- Circuit breaker (opossum, BFF-11)
- Rate limit bővítés Manufacturing route-okra

---

## Day 4 — Health bővítés + OpenAPI

- `/bff/health` bővítés: Manufacturing ping (5007)
- OpenAPI snapshot commit

---

## Tesztek (+20)

- `/me/session` aggregátor
- Manufacturing proxy mock-first + circuit breaker
- Shop Floor PIN routes
- Error normalizer
- Health check bővítés

## Definition of Done

- [ ] opossum installálva
- [ ] CONTRACT_ISSUES audit (`/bff/abstractions/*`)
- [ ] 16 új BFF route
- [ ] Circuit breaker (opossum)
- [ ] `/bff/health` Manufacturing ping
- [ ] `npm run build` 0 error
- [ ] `npm run test` ≥ 247 pass (227 + 20 új)
- [ ] Outbox DONE
