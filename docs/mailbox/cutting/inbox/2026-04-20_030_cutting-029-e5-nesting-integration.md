---
id: MSG-CUTTING-030
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-CUTTING-029
created: 2026-04-20
---

# CUTTING-030 — CUTTING-029 E5: Nesting NuGet integráció a Cutting modulba

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Blokkoló feloldva:** ABSTRACTIONS-010 (Contracts 1.3.0) ✅ DONE — E5 most indítható
> **Timeline:** ~0.25 nap

---

## Kontextus

Az E1–E4 (SpaceOS.Nesting.Algorithms NuGet) kész (`3e87954`).
A Contracts 1.3.0 kész (ABSTRACTIONS-010 DONE, 52/52 teszt).
Most jön az E5: a Cutting modul frissítése, hogy a NuGet-et használja.

---

## Feladatok

### 1. Contracts 1.3.0 NuGet frissítés

A Cutting modul csproj-jaiban frissítsd a Contracts referenciát 1.3.0-ra.

### 2. Nesting NuGet projektrefencia hozzáadása

```xml
<ProjectReference Include="../../spaceos-nesting-algorithms/SpaceOS.Nesting.Algorithms/SpaceOS.Nesting.Algorithms.csproj" />
```

### 3. DI regisztráció

A Cutting API rétegben:

```csharp
services.AddSingleton<INestingStrategy, FfdhNestingStrategy>();
services.AddSingleton<NestingStrategyFactory>();
```

### 4. `GetNestingResultQueryHandler` frissítése

- Régi: `new NestingService().ComputeNesting(...)`
- Új: `_nestingStrategy.ComputeAsync(MapToNestingInput(...), ct)`
- Mapper: `CuttingLineRequest` → `NestingPart`, `AvailablePanel` → `AvailablePanel`
- A handler kapjon `INestingStrategy`-t konstruktoron át (DI)

### 5. `NestingService` jelölése elavultnak

```csharp
[Obsolete("Use INestingStrategy via SpaceOS.Nesting.Algorithms NuGet. Will be removed in v1.4.0.")]
public class NestingService { ... }
```

---

## Definition of Done

- [ ] `dotnet build` 0 error, 0 warning (TreatWarningsAsErrors=true)
- [ ] Összes meglévő Cutting teszt (194+) zöld
- [ ] `GetNestingResultQueryHandler` `INestingStrategy`-t használ (nem `NestingService`)
- [ ] `NestingService` `[Obsolete]` attribútumot kapott
- [ ] Outbox DONE üzenet küldve
