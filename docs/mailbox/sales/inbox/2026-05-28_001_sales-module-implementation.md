---
id: MSG-SALES-001
from: root
to: sales
type: task
priority: high
status: UNREAD
created: 2026-05-28
skill: /spaceos-terminal
agents: enabled
subagents: enabled
---

# MSG-SALES-001 — Sales modul implementáció (v4 spec)

## Kontextus

A Sales modul (5009) a SpaceOS értékesítési rétege: Customer/Lead CRM + Quote életciklus + Quote→Order konverzió. Az ADR-039 blokkolók (Kernel + Joinery receiver) elkészültek és deployolódnak.

**Teljes spec:** `docs/tasks/active/SpaceOS_Modules_Sales_Architecture_v4.md`

Ez egy **új polyrepo**: `spaceos-modules-sales` (net8.0, 5 projekt + Tests, a `spaceos-modules-cutting` solution struktúrája a sablon).

## Blokkolók státusza

- ✅ `GET /api/internal/tenants/{id}` — Kernel, commit `c70a359`, deploy folyamatban
- ✅ `POST /joinery/internal/orders/from-quote` — Joinery, commit `da7199f`, deploy folyamatban
- ⏳ `SpaceOS:InternalSecret` env var — INFRA operátor állítja be (MSG-ROOT-002)

Az `Integration test` szintű adapter tesztek stub-fixture-rel futtathatók amíg a deploy nem kész.

## Track sorrend (§13 alapján)

```
A) Domain       — 2 aggregate + QuoteLine + 5 VO + 5 enum + 12 event + repo/port interface-ek
B) Infrastructure — SalesDbContext + EF Config + S-0001/S-0002/S-0003 migration + repo-k + QuoteNumberGenerator + QuotaGuard
C) Outbox+Worker — OutboxMessage + ISalesWorkerDbContextFactory + SalesIntegrationWorker (tenant assert + log redaction) + Polly
D) Adapters     — JoineryOrderConversionClient (header-body) + KernelActorDirectoryClient
E) Application  — 12 command + 4 query handler + 12 validator + 6 spec + ITenantContext + AuditAndDispatchInterceptor
F) API          — Program.cs (JWT lockdown, RateLimiter, RBAC) + 24 endpoint + InternalHeaderMiddleware
G) Tests        — ≥88 teszt (30 domain + 22 handler + 14 API + 18 security + 4 concurrency/audit/worker-tx)
```

## Indítás előtt ellenőrizd

```bash
# Sablon solution struktúra
ls /opt/spaceos/backend/spaceos-modules-cutting/

# Contracts NuGet (Sales fog hivatkozni rá)
ls /opt/spaceos/backend/spaceos-modules-contracts/

# Joinery internal endpoint URL (adapter-ben kell)
# POST http://127.0.0.1:5002/joinery/internal/orders/from-quote

# Kernel internal endpoint URL
# GET http://127.0.0.1:5000/api/internal/tenants/{id}

# PostgreSQL schema neve
# spaceos_sales (önálló schema)
```

## Definition of Done (§8 összefoglaló)

- [ ] Új polyrepo: `spaceos-modules-sales` — 5 projekt (Domain/Application/Abstractions/Infrastructure/Api) + Tests
- [ ] Port 5009 loopback-only
- [ ] Migration S-0001 + S-0002 + S-0003 alkalmazva VPS-en
- [ ] ≥ 88 új teszt zöld
- [ ] Meglévő 3800+ backend teszt érintetlen
- [ ] 0 build warning
- [ ] `dotnet list package --vulnerable` = 0 high/critical
- [ ] `EXPLAIN ANALYZE` Index Scan minden list-endpoint-on
- [ ] `JoineryOrderConversionClient` + `KernelActorDirectoryClient` idempotency E2E integrációs teszt (lehet stub-fixture amíg deploy nem kész)
- [ ] `QuoteNumberGenerator` race-condition teszt (concurrent)
- [ ] Quote→Order konverzió sad path: hash mismatch → 409

Teljes spec: `docs/tasks/active/SpaceOS_Modules_Sales_Architecture_v4.md`
