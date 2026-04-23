---
id: MSG-CUTTING-036
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-CUTTING-035
created: 2026-04-20
---

# CUTTING-036 — PriorityProfile aggregate + seed presets

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Arch spec:** `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md` (D-54)
> **Timeline:** ~1 nap
> **Blokkoló feloldva:** CUTTING-035 ✅ DONE

---

## Kontextus

A `PriorityProfile` tárolja, hogy egy tenant milyen `ICapacityModel` és `IReworkPolicy` implementációt alkalmaz, és milyen prioritási szabályokat használ a `CuttingPlan`-ekhez. Egy tenant N profilt tarthat aktívan (OQ-2 döntés).

**OQ-7 döntés (jóváhagyva):** `PanelCutter` preset default strategy = `MaxCutStrategy`.

---

## Feladatok

### 1. PriorityRule value object

**Fájl:** `Domain/ValueObjects/PriorityRule.cs`

```csharp
public sealed record PriorityRule(
    int Order,
    string RuleName,
    string? Parameter = null);
```

### 2. PriorityProfile aggregate

**Fájl:** `Domain/Aggregates/PriorityProfile.cs`

```csharp
public sealed class PriorityProfile : AggregateRoot<Guid>
{
    public Guid TenantId { get; private set; }
    public string Name { get; private set; }
    public bool IsDefault { get; private set; }
    public string CapacityModelId { get; private set; }
    public string ReworkPolicyId { get; private set; }
    public string PlanningStrategyId { get; private set; }

    private readonly List<PriorityRule> _rules = new();
    public IReadOnlyList<PriorityRule> Rules => _rules.AsReadOnly();

    public static PriorityProfile Create(Guid tenantId, string name,
        string capacityModelId, string reworkPolicyId, string planningStrategyId,
        bool isDefault = false);

    public void SetDefault(bool value);
}
```

### 3. EF Core konfiguráció + migration

**Fájl:** `Infrastructure/Configurations/PriorityProfileConfiguration.cs`

- Tábla: `PriorityProfiles`
- `Rules` → owned entity collection (JSON vagy külön tábla — JSON oszlop javasolt, egyszerűbb)
- RLS policy: tenant-szintű izoláció (mint a többi entitásnál)

```bash
dotnet ef migrations add AddPriorityProfile \
  --project SpaceOS.Modules.Cutting.Infrastructure \
  --startup-project SpaceOS.Modules.Cutting.Api
```

### 4. Seed presets

A migration `Up()` metódusban seed data a két standard presetre:

```sql
-- Manufacturer preset (area + FIFO + warn)
INSERT INTO "PriorityProfiles" (...)
VALUES ('00000000-0000-0000-0000-000000000001', NULL, 'Manufacturer',
        true, 'area-v1', 'warn-and-apply-v1', 'fifo', ...);

-- PanelCutter preset (area + MaxCut + warn)
INSERT INTO "PriorityProfiles" (...)
VALUES ('00000000-0000-0000-0000-000000000002', NULL, 'PanelCutter',
        false, 'area-v1', 'warn-and-apply-v1', 'max-cut', ...);
```

**Megjegyzés:** `TenantId = NULL` = globális preset (minden tenant látja).

### 5. Repository + query handler

- `IPriorityProfileRepository` interfész (Get, GetByTenant, GetDefault)
- `GetPriorityProfilesQueryHandler` — GET /api/cutting/priority-profiles
- `CreatePriorityProfileCommandHandler` — POST /api/cutting/priority-profiles

### 6. Endpoint regisztráció

```csharp
// CuttingPlanningEndpoints.cs-ben:
app.MapGet("/api/cutting/priority-profiles", ...)
    .RequireAuthorization("ManufacturerOnly");
app.MapPost("/api/cutting/priority-profiles", ...)
    .RequireAuthorization("ManufacturerOnly");
```

---

## Tesztek

- `PriorityProfileTests.cs` (ÚJ): Create, SetDefault, Rules immutability — min. 6 teszt
- `PriorityProfileRepositoryTests.cs` (ÚJ): GetByTenant, GetDefault, seed presets láthatók — min. 5 teszt
- Meglévő 218 teszt mind zölden

---

## Definition of Done

- [ ] `PriorityRule` value object létrehozva
- [ ] `PriorityProfile` aggregate létrehozva
- [ ] EF konfiguráció + migration (`dotnet ef migrations add`)
- [ ] Seed presets: Manufacturer + PanelCutter
- [ ] 2 endpoint regisztrálva (GET + POST)
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` mind zöld (218+)
- [ ] Outbox DONE üzenet küldve
