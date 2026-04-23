---
id: MSG-CUTTING-042
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-ARCH-004-RESPONSE
created: 2026-04-20
---

# CUTTING-042 — Offcut registration pipeline (Handler + Snapshot + Event)

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Spec:** `docs/architecture/ADR-038-offcut-creation-at-plan-freeze.md`
> **Timeline:** ~1 nap
> **Blokkolók feloldva:** NESTING-001 ✅ (v1.1.0, WastePieces[]) + INVENTORY-015 ✅ (POST /api/inventory/offcuts/batch)

---

## Kontextus

Az ADR-038 az offcut loop lezárása: amikor egy `CuttingPlan` Freeze-elésre kerül, az összes nesting futtatásból keletkező waste téglalapok automatikusan regisztrálódnak az Inventory-ban. A domain event + handler + snapshot infrastruktúra még hiányzik.

---

## Feladatok

### 1. CuttingPlanFrozen domain event

**Fájl:** `Domain/Events/CuttingPlanFrozen.cs`

```csharp
public sealed record CuttingPlanFrozen(
    Guid PlanId,
    Guid TenantId,
    DateTimeOffset FrozenAt
) : IDomainEvent;
```

**Fájl:** `Domain/Aggregates/CuttingPlan.cs` — `Freeze()` metódus bővítése:

```csharp
public Result Freeze()
{
    // ... meglévő invariánsok ...
    Status = CuttingPlanStatus.Frozen;
    RaiseDomainEvent(new CuttingPlanFrozen(Id, TenantId, DateTimeOffset.UtcNow));
    return Result.Success();
}
```

(Ha `CuttingPlan` még nem implementálja az `IDomainEventContainer`-t, add hozzá.)

### 2. PlanNestingSnapshot entity + repo + migration

**Fájl:** `Domain/Entities/PlanNestingSnapshot.cs`

```csharp
public sealed class PlanNestingSnapshot
{
    public Guid Id { get; private set; }
    public Guid CuttingPlanId { get; private set; }
    public Guid TenantId { get; private set; }
    public string NestingResultJson { get; private set; }  // WastePieces[] + metrics
    public DateTimeOffset CreatedAt { get; private set; }

    public static PlanNestingSnapshot Create(Guid planId, Guid tenantId, string nestingResultJson);
}
```

**Fájl:** `Domain/Interfaces/IPlanNestingSnapshotRepository.cs`

```csharp
public interface IPlanNestingSnapshotRepository
{
    Task<PlanNestingSnapshot?> GetByPlanAsync(Guid planId, CancellationToken ct);
    Task AddAsync(PlanNestingSnapshot snapshot, CancellationToken ct);
}
```

**Migration:**
```bash
dotnet ef migrations add AddPlanNestingSnapshot \
  --project SpaceOS.Modules.Cutting.Infrastructure \
  --startup-project SpaceOS.Modules.Cutting.Api
```

Tábla: `PlanNestingSnapshots` — saját `TenantId` oszloppal (RLS-hez).

### 3. GetNestingResultQueryHandler bővítése

**Fájl:** `Queries/GetNestingResult/GetNestingResultQueryHandler.cs`

Nesting futtatása után mentse el a snapshot-ot:

```csharp
// Ha snapshot még nem létezik ehhez a planhoz → mentés
var existing = await _snapshotRepo.GetByPlanAsync(request.PlanId, ct);
if (existing is null)
{
    var json = JsonSerializer.Serialize(nestingResult); // WastePieces + metrics
    var snapshot = PlanNestingSnapshot.Create(request.PlanId, tenantId, json);
    await _snapshotRepo.AddAsync(snapshot, ct);
}
```

### 4. IInventoryCuttingAdapter + HTTP implementáció

**Fájl:** `Domain/Interfaces/IInventoryCuttingAdapter.cs`

```csharp
public interface IInventoryCuttingAdapter
{
    Task<Result> RegisterOffcutsAsync(
        Guid planId,
        Guid tenantId,
        IReadOnlyList<OffcutRegistrationItem> items,
        CancellationToken ct);
}

public sealed record OffcutRegistrationItem(
    string MaterialCode,
    decimal WidthMm,
    decimal HeightMm,
    decimal X,
    decimal Y
);
```

**Fájl:** `Infrastructure/Adapters/InventoryCuttingHttpAdapter.cs`

- `POST /api/inventory/offcuts/batch` hívás
- Polly: 3x exponential retry (1s, 2s, 4s)
- `CorrelationId` = `PlanId` (idempotency)

### 5. RegisterOffcutsOnPlanFrozenHandler

**Fájl:** `Application/EventHandlers/RegisterOffcutsOnPlanFrozenHandler.cs`

```csharp
public sealed class RegisterOffcutsOnPlanFrozenHandler
    : INotificationHandler<CuttingPlanFrozen>
{
    // 1. Lekéri a PlanNestingSnapshot-ot a PlanId alapján
    // 2. Ha nincs snapshot → LogWarning + return (nem dob kivételt)
    // 3. Deserializálja a WastePiece listát
    // 4. Küszöb szűrés: csak WidthMm >= 400 && HeightMm >= 400 (default threshold)
    // 5. IInventoryCuttingAdapter.RegisterOffcutsAsync() hívás
    // 6. Best-effort: hiba esetén LogWarning, nem propagálja
}
```

### 6. DI regisztráció

**Fájl:** `ServiceCollectionExtensions.cs`

```csharp
services.AddScoped<IPlanNestingSnapshotRepository, PlanNestingSnapshotRepository>();
services.AddScoped<IInventoryCuttingAdapter, InventoryCuttingHttpAdapter>();
// MediatR már regisztrálva — a handler automatikusan felveszi
```

---

## Tesztek (min. 8 új)

- `CuttingPlanFrozen_RaisedWhenFreezeCalled` — domain event firing
- `RegisterOffcutsHandler_SnapshotMissing_LogsWarning_NoException` — graceful degradation
- `RegisterOffcutsHandler_BelowThreshold_NotRegistered` — 400mm küszöb
- `RegisterOffcutsHandler_AboveThreshold_RegistersCalled` — happy path
- `RegisterOffcutsHandler_AdapterFails_NoException` — best-effort
- `RegisterOffcutsHandler_IdempotentFreeze_NoDuplicateOffcuts` — kétszeri freeze → 0 duplikát
- `RegisterOffcutsHandler_CrossTenant_NotCreated` — Tenant A freeze nem hoz létre Tenant B offcutot
- `GetNestingResult_SavesSnapshot_IfNotExists` — snapshot mentés

Meglévő 265 teszt mind zölden.

---

## Definition of Done

- [ ] `CuttingPlanFrozen` domain event + `Freeze()` bekötés
- [ ] `PlanNestingSnapshot` entity + repository + migration (`dotnet ef migrations add`)
- [ ] `GetNestingResultQueryHandler` snapshot mentéssel bővítve
- [ ] `IInventoryCuttingAdapter` + `InventoryCuttingHttpAdapter` (Polly 3x)
- [ ] `RegisterOffcutsOnPlanFrozenHandler` — küszöb filter + best-effort
- [ ] DI regisztráció
- [ ] ≥8 új teszt
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` mind zöld (265+)
- [ ] Outbox DONE üzenet küldve
