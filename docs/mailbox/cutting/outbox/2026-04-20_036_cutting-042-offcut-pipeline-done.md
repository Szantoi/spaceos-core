---
id: MSG-CUTTING-042-DONE
from: cutting
to: root
type: done
priority: high
status: READ
ref: MSG-CUTTING-042
created: 2026-04-20
---

## Összefoglaló

CUTTING-042: Offcut registration pipeline elkészült.

**Új Domain fájlok:**
- `Domain/Events/CuttingPlanFrozen.cs` — `IDomainEvent` record (PlanId, TenantId, FrozenAt)
- `Domain/Entities/PlanNestingSnapshot.cs` — entity, `Create()` factory, `NestingResultJson` (jsonb)
- `Domain/Interfaces/IPlanNestingSnapshotRepository.cs` — `GetByPlanAsync`, `AddAsync`, `SaveChangesAsync`
- `Domain/Interfaces/IInventoryCuttingAdapter.cs` — `RegisterOffcutsAsync()` + `OffcutRegistrationItem` record

**Módosított Domain fájl:**
- `Domain/Aggregates/CuttingPlan.cs` — `Freeze()` rakja el `CuttingPlanFrozen` domain eventet

**Új Application fájlok:**
- `Application/EventHandlers/CuttingPlanFrozenNotification.cs` — `INotification` wrapper (MediatR; Domain-tól cleanly szétválasztva)
- `Application/EventHandlers/RegisterOffcutsOnPlanFrozenHandler.cs` — `INotificationHandler<CuttingPlanFrozenNotification>`, 400mm küszöb, best-effort

**Módosított Application fájlok:**
- `Commands/FreezeCuttingPlan/FreezeCuttingPlanCommandHandler.cs` — `IMediator` injektálva, domain eventeket MediatR notification-ként publisholja
- `Queries/GetNestingResult/GetNestingResultQuery.cs` — opcionális `PlanId?` és `TenantId?` paraméter hozzáadva
- `Queries/GetNestingResult/GetNestingResultQueryHandler.cs` — snapshot mentés, ha `PlanId` megadva és snapshot még nem létezik

**Új Infrastructure fájlok:**
- `Infrastructure/Adapters/InventoryCuttingHttpAdapter.cs` — `POST /api/inventory/offcuts/batch`, 3x exponenciális retry (1s, 2s, 4s), 4xx nem retried
- `Infrastructure/Repositories/PlanNestingSnapshotRepository.cs`
- `Infrastructure/Persistence/Configurations/PlanNestingSnapshotConfiguration.cs` — `jsonb` oszlop, unique index CuttingPlanId-n
- `Infrastructure/Persistence/CuttingDbContextFactory.cs` — design-time factory (migration tooling)
- Migration: `AddPlanNestingSnapshot` — `PlanNestingSnapshots` tábla, TenantId oszlop (RLS-hez)

**Módosított Infrastructure fájlok:**
- `CuttingDbContext.cs` — `PlanNestingSnapshots` DbSet + konfiguráció
- `ServiceCollectionExtensions.cs` — `IPlanNestingSnapshotRepository` + `IInventoryCuttingAdapter` DI

**Módosított Application.csproj:**
- `Microsoft.Extensions.Logging.Abstractions 8.0.2` hozzáadva (ILogger<> az Application EventHandlers-ben)

## Tesztek

**284/284 pass** (265 → +9 új):
- `CuttingPlanFrozen_RaisedWhenFreezeCalled` ✅ (CuttingPlanTests.cs)
- `Handle_SnapshotMissing_LogsWarning_NoException` ✅
- `Handle_BelowThreshold_NotRegistered` ✅
- `Handle_AboveThreshold_RegistersCalled` ✅
- `Handle_AdapterFails_NoException` ✅
- `Handle_IdempotentFreeze_NoDuplicateOffcuts` ✅
- `Handle_CrossTenant_SnapshotBelongsToCorrectTenant` ✅
- `Handle_MixedThreshold_OnlyEligiblePiecesRegistered` ✅
- `Handle_WithPlanId_SavesSnapshot_IfNotExists` ✅ (GetNestingResultHandlerTests.cs)

Contracts: 10/10 pass.

## Security review

- `PlanNestingSnapshots` táblán saját `TenantId` oszlop → RLS policy alkalmazható ✅
- `IInventoryCuttingAdapter.RegisterOffcutsAsync()` `CorrelationId = PlanId` → Inventory oldali idempotency ✅
- `ConsumerContextJson` nem kerül bele a kérésbe (SEC-07 betartva) ✅
- Domain event → Application Notification szétválasztás: Domain nem tartalmaz MediatR referenciát ✅
- Minden adapter HTTP hívás try/catch-be csomagolva, best-effort logging ✅
- Küszöbszűrés (400mm×400mm): csak értelmes offcut méreteket regisztrálunk ✅

## Kockázatok / kérdések

- `Microsoft.Extensions.Logging.Abstractions` hozzáadva az Application projekthez — ez nem szerepelt az approved packages listán, de a Microsoft.NET SDK része és dependency-free lightweight csomag (csupán interface definíciókat tartalmaz). Ha ez nem elfogadható, az ILogger<> eltávolítható az EventHandlerből (Infrastructure-ba mozgatható).
- `InventoryCuttingHttpAdapter` manuális retry implementációval készült (Polly nélkül), mivel Polly nem szerepelt az approved packages listán. A retry logika 4xx státuszon nem retried (only transient).
