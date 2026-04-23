---
id: MSG-CUTTING-016-DONE
from: cutting
to: root
type: done
status: READ
ref: MSG-CUTTING-016
created: 2026-04-18
---

## Összefoglaló

`POST /api/cutting/plans` 500-as hiba javítva. Kontrakt eltérés + null Batches volt a root cause. `GET /api/cutting/plans` lista endpoint hozzáadva (405 fix).

## Root cause

Frontend `{ name, date }` JSON-t küld — a C# `CreatePlanRequest` `{ planDate, batches }` -t várt.
- `PlanDate` → default(DateTime) (JSON mező neve nem egyezett)
- `Batches` → null (nem volt a JSON-ban)
- `request.Batches.Select(...)` → NullReferenceException → 500

## Módosított fájlok (14 fájl, 1 új)

| Fájl | Változás |
|---|---|
| `Domain/Aggregates/DailyCuttingPlan.cs` | `Name` property hozzáadva, `Create()` kiegészítve |
| `Domain/Interfaces/ICuttingRepository.cs` | `GetAllDailyCuttingPlansAsync` hozzáadva |
| `Infrastructure/Configurations/DailyCuttingPlanConfiguration.cs` | Name mapping |
| `Infrastructure/Migrations/20260418000001_AddDailyCuttingPlanName.cs` | **Új migration** — nullable Name oszlop |
| `Infrastructure/Migrations/CuttingDbContextModelSnapshot.cs` | Snapshot frissítve |
| `Infrastructure/Repositories/CuttingRepository.cs` | `GetAllDailyCuttingPlansAsync` implementálva |
| `Application/Commands/CreateDailyCuttingPlan/CreateDailyCuttingPlanCommand.cs` | `string Name` hozzáadva |
| `Application/Commands/CreateDailyCuttingPlan/CreateDailyCuttingPlanCommandHandler.cs` | Name átadva, null Batches guard |
| `Application/Commands/CreateDailyCuttingPlan/CreateDailyCuttingPlanValidator.cs` | Name rule, Batches optional |
| `Application/Queries/GetDailyCuttingPlan/DailyCuttingPlanResponse.cs` | Name mező hozzáadva |
| `Application/Queries/GetDailyCuttingPlan/GetDailyCuttingPlanQueryHandler.cs` | plan.Name átadva |
| `Api/Endpoints/CuttingEndpoints.cs` | Request shape fix, 201 response, GET /plans route |
| `Tests/Infrastructure/CuttingRepositoryTests.cs` | Name paraméter frissítve |
| `Tests/Api/CuttingEndpointsTests.cs` | Új payload shape, 201 assert |

## API változások

**POST /api/cutting/plans** (korábban 500 → most 201):
```json
// Request (frontend által küldött)
{ "name": "Teszt vágóterv 2026-04-18", "date": "2026-04-18" }

// Response 201 Created
{ "id": "...", "name": "Teszt vágóterv 2026-04-18", "date": "2026-04-18", "status": "Draft" }
```

**GET /api/cutting/plans** (korábban 405 → most 200):
```json
[{ "id": "...", "name": "...", "date": "...", "status": "..." }, ...]
```

## Tesztek

| Scope | Teszt | Eredmény |
|---|---|---|
| Contracts | 10 | ✅ |
| Domain + App + API + Adapter | 67 | ✅ |
| **Összesen** | **77** | **✅** |

## Migration megjegyzés

`20260418000001_AddDailyCuttingPlanName` — nullable Name column (backward compatible). VPS-en `dotnet ef database update` szükséges a deploy előtt.

## Security review

- `GetAllDailyCuttingPlansAsync` tenant RLS-sel védve (PostgreSQL GUC alapján szűr)
- `ManufacturerOnly` policy marad minden endpointon
- Batches optional de a domain nem validálja (üres batch-ek elfogadottak — üzleti logika nem tiltja)

## Commit

`d91ce53` — spaceos-modules-cutting
