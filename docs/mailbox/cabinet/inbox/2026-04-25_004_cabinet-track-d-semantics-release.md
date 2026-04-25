---
id: MSG-CABINET-004
from: root
to: cabinet
type: task
priority: high
status: READ
ref: MSG-CABINET-003-DONE
created: 2026-04-25
---

# CABINET-004 — Track D: Semantics + Cross-cutting + Release (Nap 12–21.75)

> **Tervdok:** `/opt/spaceos/docs/tasks/new/SpaceOS_Cabinet_0.1_CoreFoundation_Architecture_v4.md` — Section 4.5, 7, 16
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Előfeltétel:** CABINET-003 ✅ (268 teszt, Geometry + Abstractions + Domain + Machining + Construction)
> **Használhatsz sub-agent-eket** ha szükséges

---

## Semantics csomag (spec §4.5)

### SemanticInferenceService (A7)

```csharp
public sealed class SemanticInferenceService
{
    // A7: szemantikus név derivált — gravitáció + topológia alapján
    // Bemenetek: Part pozíció (PartFrame), BaseCuboid geometria, Connection-ök
    // Kimenet: PartRole (LeftSide, RightSide, Bottom, Top, Shelf, BackPanel, Divider, stb.)
    
    public Result<PartRole> InferRole(Part part, Skeleton skeleton) { }
    
    // DB-CAB-6: lockless ConcurrentDictionary + LRU cache
    // A cache key: (SkeletonId, PartId, SnapshotVersion)
}
```

### Inferencia szabályok

- Gravitáció irány (AssemblyFrame.GravityDirection) → Top/Bottom meghatározás
- Szélső pozíció (min/max X) → Left/Right
- Belső vízszintes → Shelf
- Belső függőleges → Divider
- BackPanel slot → BackPanel
- Kick/Plinth pozíció → Kickboard

---

## Cross-cutting (spec §7, §16.1)

### Determinism tesztek

- Ugyanaz az input → ugyanaz a SkeletonSnapshot JSON (byte-exact)
- 10 futtatás → hash azonos

### Smoke tesztek

- Komplett flow: Create Skeleton → Add Parts → Connect → Construction → Semantics → Snapshot → FromSnapshot round-trip

### Meta package

- `SpaceOS.Cabinet` meta csomag: behúzza mind a 6 src csomagot
- Verifikáció: `dotnet add package SpaceOS.Cabinet` működik

---

## Tesztek (target: 230+ összesen)

**Semantics (30+):**
- InferRole: Left, Right, Top, Bottom, Shelf, Divider, BackPanel
- Gravity direction hatása
- Cache hit/miss
- Ismeretlen pozíció → Unknown role

**Cross-cutting (15+):**
- Determinism: 10 run same hash
- Smoke: full flow Create→Parts→Connect→Construction→Semantics→Snapshot
- Meta package: reference resolution
- JSON round-trip: Skeleton → Snapshot → Skeleton identity check

---

## Definition of Done

- [ ] Semantics csomag: SemanticInferenceService + cache
- [ ] Inferencia szabályok: gravity + topology → PartRole
- [ ] Determinism tesztek
- [ ] Smoke tesztek (full flow)
- [ ] Meta package (`SpaceOS.Cabinet`) behúzza mind a 6 csomagot
- [ ] `dotnet build -c Release` 0 error, 0 warning
- [ ] `dotnet test` ≥ 300 pass (268 előző + min 32 új, 230+ target túlteljesítve)
- [ ] net8.0 ÉS net10.0 mindkettő PASS
- [ ] git push
- [ ] Outbox DONE
