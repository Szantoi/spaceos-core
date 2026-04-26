---
id: MSG-KERNEL-101
from: root
to: kernel
type: task
priority: high
status: READ
ref: SpaceOS_Cabinet_0.2_CatalogAssembly_Architecture_v4.md
created: 2026-04-26
---

# KERNEL-101 — FlowEpic Scope bővítés (Cabinet 0.2 előfeltétel)

> **Tervdok:** `docs/tasks/new/SpaceOS_Cabinet_0.2_CatalogAssembly_Architecture_v4.md` Section A16
> **Skill:** `/spaceos-terminal` szerint dolgozz
> **Blokkoló:** Cabinet 0.2 Catalog csomag nem indulhat amíg ez DEPLOYED
> **Használhatsz sub-agent-eket** ha szükséges

---

## Feladat

A `FlowEpic` aggregate-et bővíteni kell 3 additív változtatással:

### 1. FlowEpicScope enum bővítés

```csharp
public enum FlowEpicScope
{
    // Meglévők:
    DoorOrder,
    CuttingPlan,
    // ÚJ:
    MicroAssembly
}
```

### 2. FlowEpic aggregate bővítés

```csharp
// Új nullable mezők:
public IReadOnlyList<FlowEpicRequiredResource>? RequiredResources { get; }
public string? RequiredSkillLevel { get; }
```

### 3. Migration

```bash
dotnet ef migrations add FlowEpic_Scope_MicroAssembly \
  --project <Infrastructure projekt> \
  --startup-project <Api projekt>
```

A migration tartalmazza:
- `FlowEpicRequiredResources` child tábla (FlowEpicId FK, ResourceType, ResourceName, Quantity)
- `RequiredSkillLevel` VARCHAR(50) nullable mező a FlowEpics táblán
- `Scope` enum: MicroAssembly érték (`ALTER TYPE` ha PostgreSQL enum, vagy int)

### 4. Consumer-audit megjegyzés

A FlowEpicScope-ot használó switch-statement-ek a Joinery/Cutting/Orchestrator/Portal kódbázisban lehetnek. Ha a Kernel-ben van ilyen, add hozzá a MicroAssembly case-t (default ág vagy explicit skip).

---

## Tesztek (+5)

1. FlowEpic Create with MicroAssembly scope → OK
2. RequiredResources: add + read back
3. RequiredSkillLevel: set + read back
4. Migration: tábla létezik
5. Backward compatible: meglévő DoorOrder/CuttingPlan scope-ok változatlanok

## Definition of Done

- [ ] FlowEpicScope.MicroAssembly enum érték
- [ ] RequiredResources + RequiredSkillLevel mezők
- [ ] EF migration (`dotnet ef migrations add`)
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` ≥ 1143 pass (1138 + min 5 új)
- [ ] Outbox DONE
