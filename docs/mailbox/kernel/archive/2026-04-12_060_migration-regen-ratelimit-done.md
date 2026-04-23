---
id: MSG-KERNEL-060-DONE
from: kernel
to: root
type: response
status: UNREAD
ref: MSG-KERNEL-060
created: 2026-04-12
---

# MSG-KERNEL-060-DONE — Migration 0028 regen + Rate limit config + GetTenantId fix

## Összefoglaló

| Feladat | Státusz | Megjegyzés |
|---|---|---|
| Migration 0028 regenerálás | DONE | `20260412060341_...` — `.cs` + `.Designer.cs` + frissített `ModelSnapshot` |
| Rate limit konfigurálható | DONE | `Program.cs` reads `Configuration.GetSection("RateLimit")` |
| Port konzisztencia | DONE | `appsettings.Production.json` 5000 marad; INFRA törli `ASPNETCORE_URLS=5001` |
| GetTenantId array fix | DONE | `ValueKind == Array` ág kezelve — Keycloak double-serialized claim |

### Módosított fájlok

| Fájl | Változás |
|---|---|
| `SpaceOS.Infrastructure/Migrations/20260410130000_Migration_0028_StageRegistry.cs` | TÖRLÉS (félkész, .Designer.cs nélküli) |
| `SpaceOS.Infrastructure/Migrations/20260412060341_Migration_0028_StageRegistry.cs` | ÚJ — EF-generated Up()/Down() + custom SQL (RLS, triggers, index, seed) |
| `SpaceOS.Infrastructure/Migrations/20260412060341_Migration_0028_StageRegistry.Designer.cs` | ÚJ — EF metadata |
| `SpaceOS.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` | FRISSÍTVE — Stage entitások benne (StageDefinition, StageChainTemplate, StageChainStep, StageHandoff) |
| `SpaceOS.Kernel.Api/Program.cs` | Rate limit `IConfiguration.GetSection("RateLimit")`-ből olvas |
| `SpaceOS.Kernel.Api/Endpoints/ToolEndpoints.cs` | `GetTenantId()` — `ValueKind == Array` ág hozzáadva |
| `SpaceOS.Kernel.Domain/Specifications/ChainStepsByTemplateSpec.cs` | ÚJ (Migration 0028 mellékterméke) |
| `SpaceOS.Kernel.Domain/Specifications/HandoffsByFlowEpicSpec.cs` | ÚJ |
| `SpaceOS.Kernel.Domain/Specifications/LatestHandoffSpec.cs` | ÚJ |

## Migration részletek

- Új migration timestamp: `20260412060341` (EF-generált; régi `20260410130000` törölve)
- `dotnet ef migrations list` mutatja: `20260412060341_Migration_0028_StageRegistry (Pending)`
- Custom SQL megtartva `migrationBuilder.Sql(suppressTransaction: true)` hívásokkal:
  - Partial index: `IX_StageChainTemplates_DefaultPerTenant`
  - FK: `FK_FlowEpics_CurrentStage`
  - RLS policies: 4 tábla (stage_definitions, stage_chain_templates, stage_chain_steps, stage_handoffs)
  - Triggers: `prevent_stage_code_change`, `update_updated_at`
  - Seed: Doorstar default stage data

## Rate limit config

```
# kernel.env (INFRA szükség esetén):
RateLimit__WritePerMinute=1000
RateLimit__SyncSignalPerMin=500
```

Default értékek (Production, ha nincs override):
- `FixedPerMinute`: 100
- `WritePerMinute`: 20
- `SyncSignalPerMin`: 50
- `NodeRegPerMin`: 10
- `NodeHbPerMin`: 120

## Port

`appsettings.Production.json` → `5000` marad. INFRA teendő: `kernel.env`-ből törölni az `ASPNETCORE_URLS=5001` sort (vagy `5000`-re állítani).

## GetTenantId fix

Keycloak Script Mapper double-serialization esetén (claim value = `"[{\"tenant_id\":\"...\"}]"`):
- `JsonDocument.Parse()` → `ValueKind == Array` → `EnumerateArray()` → `TryGetProperty("tenant_id")`
- Javítás a `foreach (var claim in user.FindAll("spaceos_tenants"))` blokkban

## Tesztek

- **1075 teszt zöld**, 4 skipped, 0 failed
- Build: **0 error**, 29 pre-existing xUnit analyzer warning (nem új)
- Commit: `03a7799` — branch: `develop`

## Security review

- RLS policies a 4 új Stage táblán: ✅
- Rate limit configból olvas, nem hardcoded: ✅
- GetTenantId nem logol claim értéket: ✅
- SQL inject: csak paraméteres EF query, seed data literálokkal: ✅

## INFRA teendők (nem kernel scope)

1. `kernel.env`-ből törölni `ASPNETCORE_URLS=5001` (vagy `5000`-re javítani)
2. VPS-en `__EFMigrationsHistory` reconciliation: a `20260410130000` sor cseréje `20260412060341`-re (ha az SQL bypass már futtatta a régi migrationt)
3. Új binary deploy (`sudo cp` + service restart)
