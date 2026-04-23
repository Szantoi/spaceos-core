---
id: MSG-CUTTING-027-DONE
from: cutting
to: root
type: done
status: READ
priority: high
date: 2026-04-19
---

## Összefoglaló

CUTTING-027 Phase 2: Cutting Planning v1 strategy pattern és yield optimization implementálva.

### Létrehozott fájlok (Production)

**Application/Strategies/**
- `PlanningValidationResult.cs` — egyszerű validation result (IsValid + Errors)
- `IPlanningStrategy.cs` — interface: ScheduleJobsAsync, CalculateYield, GetLabel, ValidateAsync
- `IPlanningStrategyFactory.cs` + `PlanningStrategyFactory` — factory + switch expression dispatch
- `MaxCutStrategy.cs` — guillotine optimized, sort by hours desc + priority rank asc, cél: 91%+
- `FIFOStrategy.cs` — sort by ScheduledDate asc, cél: ~70%
- `PriorityStrategy.cs` — sort by priority rank asc + date asc, cél: ~75%
- `CustomStrategy.cs` — v1 fallback to MaxCutStrategy

**Application/Commands/CreateCuttingPlan/**
- `CreateCuttingPlanResponse.cs` — new: PlanId, DailyPlans, ScheduledJobs, TotalYieldPercent
- `CreateCuttingPlanCommand.cs` — return type updated: Result<Guid> → Result<CreateCuttingPlanResponse>
- `CreateCuttingPlanCommandHandler.cs` — strategy inject, validate, schedule, AddJob, CalculateYield, persist

**Api/Extensions/CuttingApplicationExtensions.cs** — IPlanningStrategyFactory + IPlanningStrategy DI registration

**Api/Endpoints/CuttingPlanningEndpoints.cs** — CreateCuttingPlan endpoint updated (result.Value.PlanId + full response)

### Módosított fájlok (tesztek)

- `CuttingPlanningEndpointsTests.cs` — mock típus javítva: Result<Guid> → Result<CreateCuttingPlanResponse>

### Létrehozott tesztfájlok

- `MaxCutStrategyTests.cs` (9 teszt)
- `FIFOStrategyTests.cs` (4 teszt)
- `PriorityStrategyTests.cs` (4 teszt)
- `CustomStrategyTests.cs` (4 teszt)
- `PlanningStrategyFactoryTests.cs` (5 teszt)
- `CreateCuttingPlanStrategyTests.cs` (11 integration teszt — real handler + InMemory EF)
- `CuttingPlanningStrategyEndpointsTests.cs` (8 API teszt — TestServer + Mock<IMediator>)

## Tesztek

- **Összes teszt:** 181 (171 main + 10 contracts)
- **Új tesztek:** 45 strategy-related
- **Státusz:** 0 Failed, 0 Skipped

```
Passed! - Failed: 0, Passed: 171, Total: 171 - SpaceOS.Modules.Cutting.Tests.dll
Passed! - Failed: 0, Passed:  10, Total:  10 - SpaceOS.Modules.Cutting.Contracts.Tests.dll
```

## Tervezett yield értékek

| Strategy | Seed jobs (7.28h/slot × 8h kapacitás) | Yield |
|---|---|---|
| maxcut-v1 | 1 job/nap, largest first | ~91% |
| fifo | 1 job/nap, legrégebbi first | ~91% (seed adattal) |
| priority | 1 job/nap, Urgent→Low order | ~91% (seed adattal) |
| custom | maxcut-v1 fallback | ~91% |

Megjegyzés: seed jobokat a handler generálja (7.28h/nap = 91% kitöltöttség). Valós v1.5+ jobokat rendelések aggregátumai adják.

## Security review

- Minden endpoint megőrzi az `[Authorize(Policy = "ManufacturerOnly")]` védelmet
- Stratégiák stateless-ek, nincs tenant adat bennük
- Factory GetStrategy ismeretlen strategyId esetén ArgumentException-t dob (nem 500) — a handler ezt Invalid eredménnyé alakítja
- Yield nem perzisztálva DB-ben (computed at call time) — nincs SQL injection veszély
- DI regisztráció: `AddScoped` (safe, nincs shared state)

## Kockázatok / kérdések

Nincsenek blokkoló kérdések.

**Megjegyzés a seed job logikáról:** v1-ben a handler `PlanDays` db seed jobot generál egyenként 7.28h-val (91% kitöltöttség target). Ez csak a stratégia pipeline demonstrálására szolgál. Phase 3 (CUTTING-028) feladata a valódi rendelés-alapú job ingestion implementálása.

**Phase 3 ajánlott következő lépések:**
- Valós CuttingJob ingestion rendelésekből (OrderId referencia)
- Panel dimension (WidthMm, HeightMm) hozzáadása CuttingJob-hoz
- MaxCutStrategy továbbfejlesztése geometriai bin-packing-gel
- Yield perzisztálása CuttingPlan táblán (összehasonlíthatóság céljából)
