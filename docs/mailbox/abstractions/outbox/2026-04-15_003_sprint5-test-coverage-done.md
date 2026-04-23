---
id: MSG-ABSTRACTIONS-005-DONE
from: abstractions
to: root
type: response
priority: high
status: READ
ref: MSG-ABSTRACTIONS-005
created: 2026-04-15
---

# MSG-ABSTRACTIONS-005 — Sprint 5: Test Coverage — DONE

## Diagnózis (R-18)

**Megállapítás:** Az `AddConnection()` csak self-loop-ot blokkolt (parentSlotId == childSlotId). DFS/BFS write-time ciklus check NEM volt implementálva. A ciklus detekció kizárólag traversal-time működött (`GraphCalculationEngine.KahnsTopologicalSort()`).

**Következmény:** Érvénytelen (ciklikus) gráf kerülhetett a DB-be — az audit immutability (Golden Rule 3) miatt javíthatatlan hiba lett volna.

**Fix szükséges volt. ✅**

## Implementált változások

### 1. `ProductTemplate.AddConnection()` — write-time BFS check

**Fájl:** `SpaceOS.Modules.Abstractions.Domain/Aggregates/ProductTemplate.cs`

Az INSERT előtt iteratív BFS ellenőrzés: van-e út `childSlotId → parentSlotId` az eddigi `_connections`-ben? Ha igen → ciklus keletkezne → `Result.Error("Cycle detected at write-time (DB-01)")`.

```csharp
if (HasPath(childSlotId, parentSlotId, _connections))
    return Result<SlotConnection>.Error("Cycle detected at write-time (DB-01)");
```

`HasPath()`: private static, iteratív BFS (BE-02 compliant — nincs rekurzív graph traversal).

### 2. `CycleDetectionTests.cs` — meglévő tesztek frissítve

- `DirectCycle_AToB_BToA_ThrowsDomainException` → `DirectCycle_AToB_BToA_RejectsAtWriteTime`
- `IndirectCycle_A_B_C_A_ThrowsDomainException` → `IndirectCycle_A_B_C_A_RejectsAtWriteTime`

Mindkettő most az `AddConnection()` return value-ját vizsgálja (nem engine DomainException-t).

### 3. `WriteTimeCycleDetectionTests.cs` — 5 új teszt

| Teszt | Mit ellenőriz |
|---|---|
| `AddEdge_WhenCreatesSelfLoop_RejectsAtWriteTime` | Self-loop guard, "Self-loop" error üzenet |
| `AddEdge_WhenCreatesCycle_RejectsAtWriteTime` | Write-time BFS rejecti a cycle-closing élt; a maradék gráf valid |
| `DeriveManufacturing_WithDiamondDependency_DerivesOnce` | Diamond (Root→B→D, Root→C→D): D csak egyszer deriválódik (Kahn's algoritmus) |
| `DeriveManufacturing_With100Nodes_CompletesWithinTimeout` | 100 csomópontos lineáris lánc < 1 másodpercen belül kész, nincs stack overflow |
| `GraphEngine_CrossTenant_ReturnsEmpty` | Handler-szintű RLS invariant: Tenant B nem fér hozzá Tenant A gráfjához |

## Tesztek

**81/81 zöld** (volt: 76, új: +5)

```
dotnet build → 0 error, 0 warning
dotnet test  → 81/81 passed
```

## Security review

| Ellenőrzés | Státusz |
|---|---|
| Write-time cycle detection (R-18) | ✅ Javítva |
| BE-02: Iteratív BFS, nem rekurzív | ✅ |
| Diamond dependency: D egyszer deriválódik | ✅ |
| 100-node: nem stack overflow | ✅ |
| Cross-tenant RLS handler invariant | ✅ |
| DB-01: cycle check dupla védelem (write-time + Kahn's engine) | ✅ |

## Kockázatok / kérdések

Nincs. Minden DoD pont teljesült. A write-time check O(V+E) komplexitású — 500 connection limitnél elhanyagolható overhead.
