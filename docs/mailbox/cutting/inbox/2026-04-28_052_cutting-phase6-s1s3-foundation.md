---
id: MSG-CUTTING-052
from: root
to: cutting
type: task
priority: high
status: READ
ref: SpaceOS_Modules_Cutting_Phase6_Adapters_Architecture_v4.md
created: 2026-04-28
---

# CUTTING-052 — Phase 6 S1-S3: Foundation + Adapter Framework + Resolver (Day 1–6)

> **Tervdok:** `docs/tasks/active/SpaceOS_Modules_Cutting_Phase6_Adapters_Architecture_v4.md` — KÖTELEZŐ olvasmány!
> **README:** `docs/tasks/active/SpaceOS_Modules_Cutting_Phase6_Adapters_Architecture_v4_README.md`
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** Cutting Phase 5 DEPLOYED (719 teszt)
> **FONTOS:** Migration prefix C-0020..C-0024 (NEM C-0017!), `--context CuttingDbContext` flag kötelező!
> **Használhatsz sub-agent-eket** ha szükséges

---

## S1 Foundation (~2 nap)

### Migrations C-0020..C-0024

```bash
dotnet ef migrations add C_0020_AdapterProviderConfig \
    --context CuttingDbContext \
    --project src/SpaceOS.Modules.Cutting.Infrastructure \
    --startup-project src/SpaceOS.Modules.Cutting.Api
```

5 tábla a `spaceos_cutting` sémában:
- `tenant_cutting_provider_config` — per-tenant adapter config
- `tenant_cutting_provider_config_history` — audit trail
- `adapter_call_audit` (RANGE PARTITION) — append-only
- `adapter_health_record` — adapter health status
- `uuidv7()` SQL function helper

RLS FORCE + COALESCE pattern minden tenant-specifikus táblán!

### Domain aggregates

- `TenantCuttingProviderConfig` — aggregate, capability flags, adapter selection
- `AdapterCallAudit` — append-only entity
- `AdapterHealthRecord` — health status entity

### EF configurations + Repository

---

## S2 Adapter Framework Core (~2 nap)

### Interfészek + implementációk

- `IExternalAdapterTransport` — 3 impl: FileExchangeTransport, RestApiTransport, CliWrapperTransport
- `IAdapterFactory` — factory pattern (BE-03: NEM IServiceProvider!)
- `IAdapterFormatConverter<TIn,TOut>` — pure, no I/O
- `BoundedSubprocessRunner` — cgroups v2 sandbox, argv-only (SEC-05), 1MB stdout truncate
- `TenantAdapterStorage` — file storage per-tenant
- Polly v8 resilience

---

## S3 Resolver + Cross-cutting (~2 nap)

- `ICuttingProviderResolver` — per-tenant, per-capability dispatch (A6-1)
- Capability runtime double-check (SEC-04)
- Redis cache invalidation (`adapter-config-changed:{tenantId}`)
- `IConfigSecretDetector` — Shannon entropy + regex (SEC-06)
- `IAdapterCallAuditWriter` — append-only, sanitized error (SEC-08)
- `PollSchedulerBackgroundService`

---

## Tesztek (75+)

**S1 (25+):** migration, config CRUD, RLS isolation, audit append-only
**S2 (25+):** transport implementations, factory, format converter, subprocess runner
**S3 (25+):** resolver dispatch, capability check, cache invalidation, secret detection

## Definition of Done

- [ ] Migrations C-0020..C-0024 (`--context CuttingDbContext`!)
- [ ] TenantCuttingProviderConfig aggregate
- [ ] 3 transport + factory + format converter
- [ ] BoundedSubprocessRunner (SEC-05)
- [ ] Resolver + capability double-check (SEC-04)
- [ ] Redis cache + secret detector + audit writer
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 794 pass (719 + 75 új)
- [ ] Outbox DONE
