---
id: MSG-CABINET-008-DONE
from: cabinet
to: root
type: done
priority: high
status: READ
ref: MSG-CABINET-008
created: 2026-04-28
---

# CABINET-008 DONE — Cabinet 0.3 Track A: DB + Domain

## Eredmény

### Új Abstractions interfészek (SpaceOS.Cabinet.Abstractions)

| Interfész | Tartalom |
|---|---|
| `ICatalogFingerprintExtractor` | SEC-02: server-side fingerprint kinyerése payload-ból |
| `IShareLineageResolver` | Megosztott entry eredet-követés |
| `IFlagModerationProvider` | Community moderation döntés provider |
| `ITenantStandardRepository` | TenantStandard persistence contract |
| `IRatingRepository` | Rating persistence contract |
| `IsExternalInit.cs` | netstandard2.1 polyfill record init-only setterekhez |

### AggregateRoot base class (SpaceOS.Cabinet.Domain)

```csharp
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();
    private long _nextSequence;
    public IReadOnlyList<IDomainEvent> PopDomainEvents()
    protected void RaiseEvent(IDomainEvent evt)
    protected long NextSeq()
}
```

### TenantStandard aggregate (SpaceOS.Cabinet.Domain)

| Komponens | Tartalom |
|---|---|
| `MaterialDefaults` | CarcassMaterial, CarcassThicknessMm, BackPanelMaterial, BackPanelThicknessMm |
| `LineBoreSettings` | Enabled, FirstHoleOffsetMm, SpacingMm, DiameterMm |
| `RuleThresholds` | TallCabinetHeightMm, LongShelfMm |
| `TenantStandard` | Aggregate root: Create, UpdateMaterials, UpdateLineBore, UpdateThresholds, UpdateConstructionDefaults, OverrideRuleSeverity, ClearRuleSeverityOverride |
| `TenantStandardEvents` | 7 domain event record |
| `Version` | bigint monotonic increment (DB-06) |

### CatalogEntry Federation bővítés (SpaceOS.Cabinet.Catalog)

| Új mező/metódus | Tartalom |
|---|---|
| `SimilarityFingerprint` | private set — csak server-side (SEC-02) |
| `ClusterId` | nullable Guid |
| `AdminAcknowledgedUntil` | nullable DateTimeOffset |
| `IsAutoHidden` | computed bool: ActiveFlagCount >= 3 && AdminAcknowledgedUntil nincs beállítva |
| `Ratings` | RatingAggregate VO (Count, decimal AverageStars, LastRatedAt) |
| `ActiveFlagCount` | int |
| `AssignFingerprintAndCluster()` | SEC-02: fingerprint server-only assignálás |
| `IngestRating()` | CatalogEntryRating felvétele, self-rating blocked |
| `IngestFlag()` | CatalogEntryFlag felvétele, note max 1000 char |
| `ClearFlagsByAdmin()` | AdminAcknowledgedUntil beállítása |

### Új entitások (SpaceOS.Cabinet.Catalog)

| Entitás | Tartalom |
|---|---|
| `CatalogEntryRating` | 1-5 csillag, comment max 500 char, self-rating blokkolva |
| `CatalogEntryFlag` | FlagReason (6 érték), FlagState (4 érték), StripPii() |
| `CatalogEntryCluster` | Fingerprint-alapú csoportosítás, 7 napos probation (SEC-05), RecomputeCanonical |
| `RatingAggregate` | decimal(3,2) pontosság — nem double (DB-02) |
| `FlagReason` / `FlagState` | enum-ok |

### SnapshotMigrator (SpaceOS.Cabinet.Catalog)

- `SnapshotMigrator_0_2_to_0_3` — "0.2" → "0.3" forward-only migrátor
- `AppliedTenantStandard = null` default az összes meglévő snapshot-hoz
- `SkeletonSnapshot` schema: "0.3", új mező: `AppliedTenantStandard` (Guid?)

## Definition of Done checklist

- [x] TenantStandard aggregate komplett (Create + 6 mutation + 7 domain event)
- [x] CatalogEntry Federation (SimilarityFingerprint, Cluster, IsAutoHidden, AdminAcknowledged)
- [x] Rating + Flag entities (CatalogEntryRating, CatalogEntryFlag, CatalogEntryCluster, RatingAggregate)
- [x] SnapshotMigrator_0_2_to_0_3 (forward-only, AppliedTenantStandard=null default)
- [x] `dotnet build -c Release` 0 error, 0 warning
- [x] `dotnet test` 599 pass (518 előző + 81 új)
- [x] net8.0 ÉS net10.0 PASS
- [x] Outbox DONE ✅

## Build & Test

```
Build succeeded. 0 Warning(s), 0 Error(s)
net8.0:  Passed! Failed: 0, Passed: 599, Skipped: 0
net10.0: Passed! Failed: 0, Passed: 599, Skipped: 0
```

## Teszt bontás (új tesztek)

| Terület | Új tesztek |
|---|---|
| TenantStandardTests | 25 |
| CatalogEntryClusterTests | 15 |
| CatalogEntryFederationTests | 15 |
| RatingAndFlagTests | 15 |
| SnapshotMigrator023Tests | 10 |
| Meglévő (schema bump fix) | +1 |
| **Összesen új** | **81** |
| **Összesített Cabinet** | **599** |
