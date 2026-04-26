---
id: MSG-CABINET-007
from: root
to: cabinet
type: task
priority: high
status: READ
ref: MSG-CABINET-006-DONE
created: 2026-04-26
---

# CABINET-007 — Track D+E: Persistence + Security + API + Release (Nap 11.75–16.25)

> **Tervdok:** `/opt/spaceos/docs/tasks/active/SpaceOS_Cabinet_0.2_CatalogAssembly_Architecture_v4.md` — Section 8, 9, 10, 13
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** CABINET-006 ✅ (428 teszt, Catalog + Assembly + Domain/Semantics bump)
> **FONTOS:** A Cabinet 0.2 pure NuGet library — NINCS runtime service, NINCS EF migration a VPS-en. A persistence réteg a fogyasztó (Kernel, CabinetBilder) felelőssége. A Cabinet csak az EF config-ot és entity-ket biztosítja.
> **Használhatsz sub-agent-eket** ha szükséges

---

## Track D: Persistence + Security (spec §8, §10)

### EF Core Configuration (a fogyasztó DI-ba regisztrálja)

```csharp
// SpaceOS.Cabinet.Catalog/Persistence/CatalogEntryConfiguration.cs
// IEntityTypeConfiguration<CatalogEntry>
// - PayloadJson: HasColumnType("jsonb"), 64KB CHECK constraint
// - ContentHash: VARCHAR(64), Published immutability
// - State: VARCHAR(50), string stored enum
// - Index: (TenantId, Visibility, Type, State) partial
// - Version: ConcurrencyCheck (optimistic locking, DB-CAB02-2)
```

### StaffAuditLog entity (SEC-CAB02-4)

```csharp
// Curated mutation audit
public sealed class StaffAuditLogEntry
{
    public Guid Id { get; }
    public Guid StaffUserId { get; }
    public string Action { get; }     // "Approve", "Publish", "Deprecate"
    public Guid CatalogEntryId { get; }
    public DateTimeOffset Timestamp { get; }
    public string? Details { get; }
}
```

### IStaffAuditLogger (SEC-CAB02-1, BE-CAB02-6)

- Singleton DI lifetime
- IServiceScopeFactory belül (Captive-dependency elkerülés)
- `LogSystemActorActivationAsync` — `app.is_system_actor` audit

### DI Extension Methods (spec §13.4)

```csharp
public static class CabinetServiceCollectionExtensions
{
    public static IServiceCollection AddCabinetCatalog(this IServiceCollection services) { }
    public static IServiceCollection AddCabinetAssembly(this IServiceCollection services) { }
}
```

---

## Track D: CQRS Handlers (spec §9)

9 command handler (MediatR pattern):
1. `CreateCatalogEntryCommand` → Draft
2. `SubmitCatalogEntryCommand` → Submitted (BE-CAB02-7: FluentValidation)
3. `ApproveCatalogEntryCommand` → Approved
4. `RejectCatalogEntryCommand` → Rejected
5. `PublishCatalogEntryCommand` → Published (ContentHash set)
6. `DeprecateCatalogEntryCommand` → Deprecated
7. `PinCatalogEntryCommand` → Skeleton.PinCatalogEntry
8. `DeriveAssemblyCommand` → Skeleton.DeriveAssembly
9. `DeriveBillOfServicesCommand` → Skeleton.DeriveBillOfServices

Query handlers:
- `GetCatalogEntryQuery` / `ListCatalogEntriesQuery`
- `GetAssemblyDocumentationQuery`
- `GetExplodedViewQuery`

**ConfigureAwait(false)** minden async handler-ben (BE-CAB02-3).

---

## Track E: Cross-cutting + Release

- Reference snapshot `snapshots/reference-0.2.json`
- Multi-target verifikáció (net8.0 + net10.0)
- OpenAPI snapshot placeholder (`docs/api/openapi-cabinet-0.2.yaml`)
- Smoke test: full pipeline Create→Catalog→Assembly→Snapshot
- Git push + tag candidate

---

## Tesztek (90+)

**Persistence/Config (20+):** EF config, PayloadJson round-trip, ContentHash immutability, Version concurrency
**Handlers (40+):** 9 command + 4 query, FSM transitions, validation, ConfigureAwait check
**Security (15+):** StaffAuditLog, system actor activation, 64KB payload limit
**DI (5+):** AddCabinetCatalog/Assembly registration, no BuildServiceProvider anti-pattern
**Smoke (10+):** Full pipeline end-to-end, determinism, snapshot round-trip

---

## Definition of Done

- [ ] EF Core CatalogEntryConfiguration (jsonb, index, concurrency)
- [ ] StaffAuditLogEntry + IStaffAuditLogger (Singleton + IServiceScopeFactory)
- [ ] DI: AddCabinetCatalog + AddCabinetAssembly
- [ ] 9 command handler + 4 query handler
- [ ] FluentValidation (BE-CAB02-7)
- [ ] ConfigureAwait(false) minden async-ban (BE-CAB02-3)
- [ ] Reference snapshot 0.2
- [ ] `dotnet build -c Release` 0 error, 0 warning
- [ ] `dotnet test` ≥ 518 pass (428 előző + 90 új)
- [ ] net8.0 ÉS net10.0 PASS
- [ ] git push
- [ ] Outbox DONE
