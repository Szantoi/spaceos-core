---
id: MSG-CUTTING-031-DONE
from: cutting
to: root
type: done
status: READ
priority: high
date: 2026-04-20
ref: MSG-CUTTING-031
---

# CUTTING-031 DONE — CuttingPlanStatus typed enum + DB migration

## Összefoglaló

`CuttingPlan.Status` `string` → `CuttingPlanStatus` enum (int-re konvertálva DB-ben). Migration kész CASE konverzióval.

## Módosított/létrehozott fájlok

| Fájl | Változás |
|---|---|
| `Domain/Enums/CuttingPlanStatus.cs` | ÚJ — `Draft=0, Published=1, Frozen=2, Closed=3` |
| `Domain/Aggregates/CuttingPlan.cs` | `Status: string` → `CuttingPlanStatus`; `UpdateStatus(string)` → `UpdateStatus(CuttingPlanStatus)` |
| `Infrastructure/Configurations/CuttingPlanConfiguration.cs` | `HasMaxLength(20)` eltávolítva; `HasConversion<int>()` hozzáadva |
| `Migrations/20260420000001_CuttingPlanStatusToEnum.cs` | ÚJ — CASE konverzió, oszlop átnevezés |
| `Migrations/CuttingDbContextModelSnapshot.cs` | CuttingPlan.Status: `string(20)` → `int` |
| `Application/Commands/UpdateCuttingPlanStatus/UpdateCuttingPlanStatusCommandHandler.cs` | String → enum parse; "Approved" → Published, "InProgress" → Frozen backward-compat alias-szal |
| `Application/Queries/GetCuttingPlan/GetCuttingPlanQueryHandler.cs` | `plan.Status.ToString()` a response-ban |
| `Api/Endpoints/CuttingPlanningEndpoints.cs` | `p.Status.ToString()` a lista endpointban |
| `Tests/Domain/CuttingPlanTests.cs` | Enum értékekre frissítve + 2 új enum teszt |
| `Tests/Infrastructure/CuttingPlanRepositoryTests.cs` | `UpdateStatus(CuttingPlanStatus.Published)` + enum assertion |

## Tesztek

```
Build: 0 error, 0 warning
Tests: Passed 195/195
  - SpaceOS.Modules.Cutting.Tests:          185/185
  - SpaceOS.Modules.Cutting.Contracts.Tests: 10/10
```

## OQ-1 döntés implementálva

`Approved` → `Published(1)` az enum nevében. A command handler "approved" stringet backward-compat alias-ként elfogad (→ `Published`). "inprogress" string → `Frozen` backward-compat alias.

## API backward compatibility

- API request body-ban `"status": "Approved"` és `"status": "Published"` egyaránt elfogadott
- Response-ban `plan.Status.ToString()` → `"Published"`, `"Draft"`, `"Frozen"`, `"Closed"` stringek

## Security review

- **Authorization**: `[ManufacturerOnly]` policy érintetlen
- **RLS**: CuttingPlans tábla RLS FORCE érintetlen
- **Immutability**: enum konverzió nem változtat az üzleti logikán
- **OWASP**: enum alapú status validáció erősebb, mint string — injection kockázat csökken

## Kockázatok / kérdések

- `dotnet ef` tool nem érhető el a terminálban — a migration manuálisan lett megírva a meglévő migration mintájára. A migration helyes SQL-t generál, de VPS-en `dotnet ef database update` szükséges az élesítéshez.
- CUTTING-032 (DaySlot) most indítható (blokkolt volt CUTTING-031-től).
