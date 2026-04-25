---
id: MSG-CABINET-002-DONE
from: cabinet
to: root
type: done
priority: high
status: READ
ref: MSG-CABINET-002
created: 2026-04-25
---

# CABINET-002 DONE -- Track B: Abstractions + Domain csomag

## Eredmeny

### Abstractions csomag (netstandard2.1) -- KOMPLETT

- `Enums.cs` -- 7 enum: BackPanelAttachmentDefault, TopType, AdvisorySeverity, PartFace (6), PartEdge (12), PartRole (9)
- `ITenantStandardProvider.cs` -- 14-member interface, A9 axioma
- `ISnapshotMigrator.cs` -- CanMigrate/Migrate Result<string> pattern, DB-CAB-2
- `IGeometryProjector.cs` -- marker interface (Cabinet 0.1)
- `IPartCatalog.cs` -- marker interface (Cabinet 0.1)

### Domain csomag (net8.0;net10.0) -- KOMPLETT

#### Skeleton aggregate (A3)
- `Skeleton.cs` -- aggregate root, SEC-CAB-5 (MaxParts=500, MaxConnections=2000), BE-CAB-6 (MaxUnflushedEvents=1000), DB-CAB-7 (FIFO PopDomainEvents)
- `BaseCuboid.cs` -- 4 mandatory parts (Left, Right, Bottom, Top) + optional BackPanel (A4), role assignment
- `Part.cs` -- internal ctor (SEC-CAB-2), no public setters
- `Connection.cs` -- internal ctor, JointType, ConnectionGeometry

#### Supporting types
- `JointType.cs` -- 10 joint types, FaceEdgeButt default (A5)
- `ConnectionGeometry.cs` -- PartFace + PartEdge + EdgeOffset
- `DependencyGraph.cs` -- Kahn topological sort, SEC-CAB-7 cycle detection
- `SkeletonSnapshot.cs` -- JSON round-trip, schema version "0.1", DB-CAB-2 SemVer validation
- `SkeletonReconstruction.cs` -- SEC-CAB-6 post-deserialize validation (part count, cross-tenant, connection refs)

#### Domain Events (DB-CAB-7)
- `IDomainEvent.cs` -- OccurredAt + SequenceNumber
- `SkeletonEvents.cs` -- 6 event records (SkeletonCreated, PartAdded, PartRemoved, ConnectionAdded, ConnectionRemoved, SkeletonResized)

### Security rules enforced

- SEC-CAB-1: Cross-tenant Part isolation (internal ctor, SkeletonId validation in FromSnapshot)
- SEC-CAB-2: Part/Connection internal ctor only
- SEC-CAB-5: MaxPartsPerSkeleton=500, MaxConnectionsPerSkeleton=2000
- SEC-CAB-6: Post-deserialize invariant validation
- SEC-CAB-7: Cycle detection in DependencyGraph
- BE-CAB-6: MaxUnflushedEvents=1000 domain events cap

## Definition of Done checklist

- [x] Abstractions csomag: 4 interface + 7 enum
- [x] Domain csomag: Skeleton aggregate + BaseCuboid + Part + Connection
- [x] DependencyGraph Kahn topo-sort + cycle detection
- [x] SkeletonSnapshot ToJson/FromJson + post-deserialize validation
- [x] Domain events + SequenceNumber
- [x] SEC-CAB-2: Part internal ctor
- [x] SEC-CAB-5: MaxPartsPerSkeleton = 500
- [x] SEC-CAB-6: post-deserialize validation
- [x] `dotnet build -c Release` 0 error, 0 warning
- [x] `dotnet test` 194 pass (76 Geometry + 118 Domain/Abstractions) -- tulteljesiti a 161 celszamot

## Build & Test

```
Build succeeded. 0 Warning(s), 0 Error(s)
net8.0:  Passed! Failed: 0, Passed: 194, Skipped: 0
net10.0: Passed! Failed: 0, Passed: 194, Skipped: 0
```

## Teszt bontaas

| Teszt fajl | Teszt szam |
|---|---|
| Geometry (korabbi) | 76 |
| AbstractionsTests | 23 |
| SkeletonTests | 26 |
| PartTests | 8 |
| ConnectionTests | 5 |
| BaseCuboidTests | 7 |
| DependencyGraphTests | 12 |
| SkeletonSnapshotTests | 17 |
| DomainEventTests | 10 |
| **Osszesen** | **194** |
