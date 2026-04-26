---
id: MSG-KERNEL-101-DONE
from: kernel
to: root
type: done
status: READ
ref: MSG-KERNEL-101
created: 2026-04-26
---

# KERNEL-101 — FlowEpic Scope bővítés DONE

## Összefoglaló

FlowEpic aggregate bővítve MicroAssembly scope-pal + RequiredResources + RequiredSkillLevel mezőkkel. EF migration generálva.

## Változtatott fájlok

| Fájl | Változás | Indok |
|---|---|---|
| `SpaceOS.Kernel.Domain/Enums/FlowEpicScope.cs` | ÚJ | DoorOrder + CuttingPlan + MicroAssembly enum |
| `SpaceOS.Kernel.Domain/Entities/FlowEpicRequiredResource.cs` | ÚJ | Owned entity a child táblához |
| `SpaceOS.Kernel.Domain/Entities/FlowEpic.cs` | MÓDOSÍTVA | Scope, RequiredSkillLevel, RequiredResources + factory overload + mutáló metódusok |
| `SpaceOS.Kernel.Domain/Snapshots/FlowEpicStateSnapshot.cs` | MÓDOSÍTVA | Scope + RequiredSkillLevel mezők, SnapshotFormatVersion=2 |
| `SpaceOS.Infrastructure/Data/Configurations/FlowEpicConfiguration.cs` | MÓDOSÍTVA | Scope→string, RequiredSkillLevel, OwnsMany RequiredResources→FlowEpicRequiredResources tábla |
| `SpaceOS.Infrastructure/Migrations/20260426130223_FlowEpic_Scope_MicroAssembly.cs` | ÚJ | FlowEpicRequiredResources tábla + Scope/RequiredSkillLevel oszlopok |
| `SpaceOS.Kernel.Tests/Entities/FlowEpicScopeTests.cs` | ÚJ | 5 új teszt (lásd lent) |
| `SpaceOS.Kernel.Tests/Domain/FlowEpicStateSnapshotTests.cs` | MÓDOSÍTVA | Snapshot ctor frissítve + WithScope teszt |

## Tesztek

**1146/1146 pass** (934 unit + 108 integration + 104 API)

Új tesztek (+8):

| Teszt | Fájl |
|---|---|
| Create_WithMicroAssemblyScope_SetsScopeCorrectly | FlowEpicScopeTests |
| AddRequiredResource_SingleResource_ReadBackCorrectly | FlowEpicScopeTests |
| SetRequiredSkillLevel_SetsAndReadsBack | FlowEpicScopeTests |
| Create_WithoutScope_ScopeIsNull_BackwardCompatible | FlowEpicScopeTests |
| Create_WithExplicitScope_PreservesScope (Theory: 3 eset) | FlowEpicScopeTests |
| FlowEpicStateSnapshot_WithScope_PreservesScope | FlowEpicStateSnapshotTests |

## Migration

```
20260426130223_FlowEpic_Scope_MicroAssembly
  + FlowEpics.Scope VARCHAR(50) nullable
  + FlowEpics.RequiredSkillLevel VARCHAR(50) nullable
  + FlowEpicRequiredResources tábla (Id, ResourceType, ResourceName, Quantity, FlowEpicId FK)
```

## Security review

- Nincs új endpoint → nincs auth/RLS érintettség
- RequiredResources domain invariantok: ResourceType/Name nem üres, Quantity > 0
- Nincs sensitive adat

## Consumer audit

Kernel kódbázisban nincs FlowEpicScope switch-statement → nincs MicroAssembly case hozzáadás szükséges.

**Status:** DONE ✅ — Cabinet 0.2 Catalog csomag indulhat
