---
id: MSG-CABINET-002
from: root
to: cabinet
type: task
priority: high
status: READ
ref: MSG-CABINET-001-DONE
created: 2026-04-25
---

# CABINET-002 — Track B: Abstractions + Domain csomag (Nap 4–9.5)

> **Tervdok:** `/opt/spaceos/docs/tasks/new/SpaceOS_Cabinet_0.1_CoreFoundation_Architecture_v4.md` — Section 4.2, 4.7, 7
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** CABINET-001 ✅ (Geometry 76 teszt)
> **Használhatsz sub-agent-eket** ha szükséges

---

## Abstractions csomag (spec §4.7)

Port interfészek — netstandard2.1, nincs implementáció:

```csharp
// ITenantStandardProvider — A9 axióma
public interface ITenantStandardProvider
{
    Task<TenantStandard?> GetStandardAsync(Guid tenantId, CancellationToken ct = default);
}

// ISnapshotMigrator — DB-CAB-2: schema versioning
public interface ISnapshotMigrator
{
    bool CanMigrate(string fromVersion, string toVersion);
    Result<string> Migrate(string snapshotJson, string fromVersion, string toVersion);
}

// IGeometryProjector — A8: platform-független geometry export
public interface IGeometryProjector { }

// IPartCatalog — Cabinet 0.2 prep
public interface IPartCatalog { }
```

---

## Domain csomag (spec §4.2)

### Skeleton aggregate (A3)

```csharp
public sealed class Skeleton  // aggregate root
{
    public Guid Id { get; }
    public BaseCuboid BaseCuboid { get; }   // A3: gyökér kubus
    private readonly List<Part> _parts;
    private readonly List<Connection> _connections;
    
    // Factory
    public static Result<Skeleton> Create(BaseCuboid baseCuboid) { }
    
    // Commands
    public Result<Part> AddPart(PartSpec spec) { }           // SEC-CAB-2: internal ctor
    public Result RemovePart(Guid partId) { }
    public Result<Connection> Connect(Guid partA, Guid partB, JointType joint) { }
    public Result Disconnect(Guid connectionId) { }
    
    // A10: szelektív újraszámítás
    public Result Recalculate(IReadOnlyList<Guid> dirtyPartIds) { }
    
    // MaxParts limit (SEC-CAB-5)
    public const int MaxPartsPerSkeleton = 500;
}
```

### BaseCuboid, Part, Connection entities

- **BaseCuboid:** width, height, depth + PartFrame
- **Part:** internal ctor (SEC-CAB-2), PartDimension + PartFrame + Material
- **Connection:** partA, partB, JointType (A5: default butt), face references

### SkeletonSnapshot (DB-CAB-8)

```csharp
public sealed class SkeletonSnapshot
{
    public string SchemaVersion { get; }  // DB-CAB-2: SemVer string
    public string ToJson() { }
    public static Result<SkeletonSnapshot> FromJson(string json) { }  // SEC-CAB-6: post-deserialize validation
    public static Result<Skeleton> FromSnapshot(SkeletonSnapshot snapshot) { }
}
```

### DependencyGraph (A10)

```csharp
public sealed class DependencyGraph
{
    // Kahn topological sort — SEC-CAB-7: cycle detection
    public Result<IReadOnlyList<Guid>> GetRecalculationOrder(IReadOnlyList<Guid> dirtyIds) { }
}
```

### Domain Events (DB-CAB-7: SequenceNumber)

```csharp
public interface IDomainEvent
{
    long SequenceNumber { get; }
    DateTimeOffset OccurredAt { get; }
}
// PartAdded, PartRemoved, ConnectionCreated, SkeletonRecalculated
```

---

## Tesztek (85+)

**Domain (60+):**
- Skeleton: Create, AddPart, RemovePart, Connect, Disconnect
- MaxParts: 501. part → Result.Invalid
- Part internal ctor: reflection teszt (SEC-CAB-2)
- Connection: default JointType, invalid part references
- DependencyGraph: topological sort, cycle detection, empty graph
- Domain events: SequenceNumber increment

**Snapshot (15+):**
- ToJson/FromJson round-trip
- SchemaVersion ellenőrzés
- Post-deserialize validation (SEC-CAB-6): corrupted JSON → Result.Invalid
- 5 sample snapshot

**Abstractions (10+):**
- Interface-ek publikusak és implementálhatók
- ISnapshotMigrator contract teszt

---

## Definition of Done

- [ ] Abstractions csomag: 4 interface
- [ ] Domain csomag: Skeleton aggregate + BaseCuboid + Part + Connection
- [ ] DependencyGraph Kahn topo-sort + cycle detection
- [ ] SkeletonSnapshot ToJson/FromJson + post-deserialize validation
- [ ] Domain events + SequenceNumber
- [ ] SEC-CAB-2: Part internal ctor
- [ ] SEC-CAB-5: MaxPartsPerSkeleton = 500
- [ ] SEC-CAB-6: post-deserialize validation
- [ ] `dotnet build -c Release` 0 error, 0 warning
- [ ] `dotnet test` ≥ 161 pass (76 előző + 85 új)
- [ ] Outbox DONE
