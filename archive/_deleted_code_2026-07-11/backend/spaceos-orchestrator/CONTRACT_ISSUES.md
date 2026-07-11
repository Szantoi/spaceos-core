# CONTRACT_ISSUES — ORCH-085 / Track F Day 1 Audit

**Dátum:** 2026-04-29  
**Feladat:** F.1 Contract freeze — `/bff/abstractions/*` frontend caller ellenőrzés  
**Parancs:**
```bash
grep -r "abstractions" /opt/spaceos/spaceos-doorstar-portal/src/ \
  --include="*.ts" --include="*.tsx" | grep -v node_modules
```

---

## Eredmény

**ÜRES** — a `spaceos-doorstar-portal` kódbázisban egyetlen fájl sem hív `/bff/abstractions/*` endpointot.

---

## Következtetés

A `/bff/abstractions/*` BFF route-ra a frontend oldaláról **nincs dependency**.  
Ez konzisztens a tervdok BFF-01 döntésével:

> *"A Modules.Abstractions NuGet only — nincs önálló HTTP endpoint (nincs `:5003` port). A `/bff/abstractions/*` route törölve, configurator endpoint-ok a `/bff/api/*` alá kerültek."*

A meglévő `abstractions.route.ts` + `abstractionsRouter` mount (`/bff/abstractions`) **safe to keep** (backward compat), de aktív frontend caller nincs — contract freeze szabad.

---

## Track F Day 2-3 Érintett Route-ok

| Route | Típus | Státusz |
|---|---|---|
| `GET /bff/api/me/session` | Aggregátor (KC + Kernel) | ÚJ |
| `GET /bff/api/me/home-state` | Proxy → Kernel | ÚJ |
| `GET /bff/api/tenant` | Kernel proxy (meglévő) | PASS-THROUGH (kernelProxy) |
| `GET /bff/api/audit` | Kernel proxy (meglévő) | PASS-THROUGH (kernelProxy) |
| `GET /bff/api/users` | Kernel proxy (meglévő) | PASS-THROUGH (kernelProxy) |
| `GET /bff/manufacturing/orders` | Manufacturing proxy (5007) | ÚJ + circuit breaker |
| `GET /bff/manufacturing/orders/:id` | Manufacturing proxy | ÚJ + circuit breaker |
| `POST /bff/manufacturing/edge-banding/*` | Manufacturing proxy | ÚJ + circuit breaker |
| `POST /bff/manufacturing/cnc/*` | Manufacturing proxy | ÚJ + circuit breaker |
| `GET /bff/manufacturing/tasks/:id/full` | Manufacturing aggregátor | ÚJ + circuit breaker |
| `POST /bff/shopfloor/pin/login` | PIN auth (BFF-managed) | ÚJ |
| `POST /bff/shopfloor/pin/logout` | PIN logout | ÚJ |
| `GET /bff/shopfloor/tasks` | Manufacturing proxy | ÚJ + circuit breaker |
| `GET /bff/shopfloor/task/:id/status` | Manufacturing proxy | ÚJ + circuit breaker |
