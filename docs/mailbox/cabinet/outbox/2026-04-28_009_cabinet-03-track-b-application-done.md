---
id: MSG-CABINET-009-DONE
from: cabinet
to: root
type: done
priority: high
status: READ
ref: MSG-CABINET-009
created: 2026-04-28
---

# CABINET-009 DONE — Cabinet 0.3 Track B: Application + Infrastructure

## Eredmény

### Application layer — új command/query/handler fájlok

| Fájl | Tartalom |
|---|---|
| `ITenantStandardWriteRepository.cs` | Write-side port: AddAsync, GetByIdAsync, UpdateAsync |
| `Commands/TenantStandardCommands.cs` | 5 command record (Create, UpdateMaterials, UpdateLineBore, UpdateRuleThresholds, OverrideRuleSeverity) |
| `Commands/FederationCommands.cs` | 5 command record (Rate, Flag, ClearFlagsByAdmin, AssignFingerprint, RecomputeClusters) |
| `Queries/FederationQueries.cs` | 4 query record (GetTenantStandard, ListTenantStandards, ListCommunityEntries, GetModerationQueue) |
| `Handlers/TenantStandardCommandHandlers.cs` | 5 handler (Create + 4 mutation) |
| `Handlers/FederationCommandHandlers.cs` | 5 handler (Rate, Flag, ClearFlags, AssignFingerprint, RecomputeClusters) |
| `Handlers/FederationQueryHandlers.cs` | 4 handler (GetTenantStandard, ListTenantStandards, ListCommunityEntries, GetModerationQueue) |
| `Validators/FederationCommandValidators.cs` | 3 FluentValidation validator (CreateTenantStandard, RateCatalogEntry, FlagCatalogEntry) |

### Abstractions bővítés

- `ITenantStandardRepository` — új metódus: `ListByTenantIdAsync(Guid, CancellationToken)`

### Infrastructure (SpaceOS.Cabinet.Catalog)

| Fájl | Tartalom |
|---|---|
| `Infrastructure/DefaultCatalogFingerprintExtractor.cs` | ICatalogFingerprintExtractor: `"{type}:{vendor}:{code}:{variant}"` lowercase normalizálva, null ha hiányzik mező |
| `Persistence/TenantStandardConfiguration.cs` | EF: owned entities (MaterialDefaults, LineBoreSettings, RuleThresholds), JSONB RuleSeverityOverrides, Version ConcurrencyToken |
| `Persistence/CatalogEntryRatingConfiguration.cs` | EF: catalog_entry_ratings, unique index (entry, rater) |
| `Persistence/CatalogEntryFlagConfiguration.cs` | EF: catalog_entry_flags, Reason/State string |
| `Persistence/CatalogEntryClusterConfiguration.cs` | EF: catalog_entry_clusters, JSONB MemberEntryIds, Version ConcurrencyToken |

### DI Extension

- `AddCabinetFederation()` — `DefaultCatalogFingerprintExtractor` singleton regisztráció

### Test doubles (tests projekt)

- `InMemoryTenantStandardRepository` — kettős interfész (read + write)
- `InMemoryRatingRepository` — IRatingRepository test double

## Definition of Done checklist

- [x] 5 TenantStandard command + handler
- [x] 5 Catalog Federation command + handler (Rate, Flag, ClearFlags, AssignFingerprint, RecomputeClusters)
- [x] 4 query handler (GetTenantStandard, ListTenantStandards, ListCommunityEntries, GetModerationQueue)
- [x] FluentValidation (CreateTenantStandard, RateCatalogEntry, FlagCatalogEntry) + ConfigureAwait(false)
- [x] DI: `AddCabinetFederation()` extension
- [x] `dotnet build -c Release` 0 error, 0 warning
- [x] `dotnet test` 669 pass (599 előző + 70 új) — teljesíti a ≥659 célt
- [x] net8.0 ÉS net10.0 PASS
- [x] Outbox DONE ✅

## Build & Test

```
Build succeeded. 0 Warning(s), 0 Error(s)
net8.0:  Passed! Failed: 0, Passed: 669, Skipped: 0
net10.0: Passed! Failed: 0, Passed: 669, Skipped: 0
```

## Teszt bontás (új tesztek)

| Terület | Új tesztek |
|---|---|
| TenantStandardHandlerTests | 17 |
| FederationHandlerTests | 28 |
| FingerprintExtractorTests | 13 |
| DependencyInjectionTests (addendum) | 3 |
| TenantStandardConfigurationTests | 9 |
| **Összesen új** | **70** |
| **Összesített Cabinet** | **669** |
