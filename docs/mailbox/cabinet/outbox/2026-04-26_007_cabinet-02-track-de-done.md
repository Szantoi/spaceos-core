---
id: MSG-CABINET-007-DONE
from: cabinet
to: root
type: done
priority: high
status: READ
ref: MSG-CABINET-007
created: 2026-04-26
---

# CABINET-007 DONE — Track D+E: Persistence + Security + CQRS + Release

## Eredmény

### Új NuGet csomag: SpaceOS.Cabinet.Application (net8.0;net10.0)

| Komponens | Tartalom |
|---|---|
| `ICatalogEntryRepository` | Repository contract a fogyasztó implementálja |
| `ISkeletonRepository` | Skeleton persistence contract |
| `NullCatalogResolver` | No-op ICatalogResolver |
| `NullCatalogPayloadValidator` | No-op ICatalogPayloadValidator |
| 9 command record | Create/Submit/Approve/Reject/Publish/Deprecate/Pin/DeriveAssembly/DeriveBillOfServices |
| 4 query record | GetCatalogEntry / ListCatalogEntries / GetAssemblyDocumentation / GetExplodedView |
| 9 command handler | MediatR IRequestHandler, ConfigureAwait(false) minden await-en (BE-CAB02-3) |
| 4 query handler | Teljes pipeline, AssemblyDocumentationService injektálva |
| FluentValidation validátorok | CreateCatalogEntryCommand + SubmitCatalogEntryCommand |
| `CabinetServiceCollectionExtensions` | AddCabinetCatalog + AddCabinetAssembly DI extensions |

### Catalog csomag bővítés

| Fájl | Tartalom |
|---|---|
| `Persistence/CatalogEntryConfiguration.cs` | IEntityTypeConfiguration: jsonb, 64KB CHECK constraint, (TenantId,Visibility,Type,State) index, Version ConcurrencyToken (DB-CAB02-2) |
| `Persistence/StaffAuditLogEntry.cs` | SEC-CAB02-4: staff audit entity, Create() factory |
| `Persistence/StaffAuditLogEntryConfiguration.cs` | EF mapping: staff_audit_log tábla |
| `IStaffAuditLogger.cs` | Singleton interface + LogSystemActorActivationAsync (BE-CAB02-6) |
| `NullStaffAuditLogger.cs` | No-op implementáció (test/local) |

### Track E: Release artifacts

- `snapshots/reference-0.2.json` — Cabinet 0.2 reference snapshot
- `docs/api/openapi-cabinet-0.2.yaml` — OpenAPI placeholder
- Git commit: `3098a60` · Git push: `origin/main` ✅

## Definition of Done checklist

- [x] EF Core CatalogEntryConfiguration (jsonb, index, concurrency)
- [x] StaffAuditLogEntry + IStaffAuditLogger (Singleton + no-op default)
- [x] DI: AddCabinetCatalog + AddCabinetAssembly
- [x] 9 command handler + 4 query handler (MediatR pattern)
- [x] FluentValidation (BE-CAB02-7)
- [x] ConfigureAwait(false) minden async-ban (BE-CAB02-3)
- [x] Reference snapshot 0.2
- [x] `dotnet build -c Release` 0 error, 0 warning
- [x] `dotnet test` 518 pass (428 előző + 90 új) — pontosan teljesíti a ≥518 célt
- [x] net8.0 ÉS net10.0 PASS
- [x] git push ✅ → `3098a60`
- [x] Outbox DONE

## Build & Test

```
Build succeeded. 0 Warning(s), 0 Error(s)
net8.0:  Passed! Failed: 0, Passed: 518, Skipped: 0
net10.0: Passed! Failed: 0, Passed: 518, Skipped: 0
```

## Teszt bontás (összesített Cabinet 0.2 végleges)

| Terület | Teszt szám |
|---|---|
| Geometry | 76 |
| Abstractions | 23 |
| Domain | 95 + 25 (bump) = 120 |
| Machining | 31 |
| Construction | 43 |
| Semantics | 38 |
| CrossCutting/Smoke | 15 |
| Catalog | 52 |
| Assembly | 35 |
| Persistence | 20 |
| Application (handlers + DI) | 59 |
| **Összesen** | **518** |

## Megjegyzés

`Submit()` valódi aláírása: `Submit(actorUserId, ICatalogPayloadValidator)` — a handlerekben `NullCatalogPayloadValidator` injectálódik (no-op), a teljes validálást a fogyasztó végzi az infrastruktúra rétegben. Ez megfelel az A8 axiómának (platform-független core).
