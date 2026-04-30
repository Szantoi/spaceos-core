---
id: MSG-CABINET-009
from: root
to: cabinet
type: task
priority: high
status: READ
ref: MSG-CABINET-008-DONE
created: 2026-04-28
---

# CABINET-009 — Cabinet 0.3 Track B: Application + Infrastructure (Day 6–8)

> **Tervdok:** `/opt/spaceos/docs/tasks/active/SpaceOS_Cabinet_0_3_Federation_Architecture_v4.md`
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** CABINET-008 ✅ (599 teszt, Track A DB + Domain)
> **Használhatsz sub-agent-eket** ha szükséges

---

## Application — Command handlers

A tervdok alapján CQRS handlers az új domain entitásokhoz:

### TenantStandard commands
- `CreateTenantStandardCommand` + handler
- `UpdateMaterialDefaultsCommand` + handler
- `UpdateLineBoreSettingsCommand` + handler
- `UpdateRuleThresholdsCommand` + handler
- `OverrideRuleSeverityCommand` + handler

### Catalog Federation commands
- `SubmitCommunityCatalogEntryCommand` + handler (UPSERT minta, BE-04)
- `RateCatalogEntryCommand` + handler (self-rating block)
- `FlagCatalogEntryCommand` + handler
- `ClearFlagsByAdminCommand` + handler
- `RecomputeClustersCommand` + handler (batch fingerprint clustering)

### Queries
- `GetTenantStandardQuery` / `ListTenantStandardsQuery`
- `ListCommunityEntriesQuery` (filter: type, cluster, rating, visibility)
- `GetCatalogEntryWithRatingsQuery`
- `GetModerationQueueQuery` (flagged entries for admin)

### FluentValidation + ConfigureAwait(false)

Minden command-on FluentValidation. Minden async-ban `ConfigureAwait(false)`.

---

## Infrastructure — Port implementációk

- `DefaultCatalogFingerprintExtractor` — ICatalogFingerprintExtractor impl per CatalogType
- `InMemoryRatingRepository` / `InMemoryTenantStandardRepository` — test doubles
- `CabinetRoleInterceptor` placeholder — DI extension: `AddCabinetFederation()`
- EF configs: TenantStandard, CatalogEntryRating, CatalogEntryFlag, CatalogEntryCluster

---

## Tesztek (60+)

**TenantStandard handlers (15+):** CRUD happy path + validation
**Catalog Federation handlers (20+):** submit community, UPSERT idempotency, rate, flag, clear, cluster
**Queries (10+):** community list filter, moderation queue, rating aggregate
**Infrastructure (15+):** fingerprint extractor per type, EF configs

## Definition of Done

- [ ] 5 TenantStandard command + handler
- [ ] 5 Catalog Federation command + handler (including UPSERT)
- [ ] 4+ query handler
- [ ] FluentValidation + ConfigureAwait(false)
- [ ] DI: `AddCabinetFederation()` extension
- [ ] `dotnet build -c Release` 0 error, 0 warning
- [ ] `dotnet test` ≥ 659 pass (599 + 60 új)
- [ ] net8.0 ÉS net10.0 PASS
- [ ] Outbox DONE
