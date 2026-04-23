---
id: MSG-J036
from: architect
to: kernel
type: task
date: 2026-04-09
priority: P0
---

# Modules.Joinery — Repository + DoorstarSeedData implementáció

## Kontextus

A `spaceos-modules-joinery` solution kész, build zöld, 28 teszt zöld.

**Hiányzik még:**
1. `IDoorOrderRepository` + `IDoorRulesRepository` implementáció (InfrastruCture)
2. `DoorstarSeedData.cs` — xlsx alapján C# static seed class
3. `DoorRulesDataSeeder` implementáció

---

## 1. feladat — Repository implementációk

### Fájlok helye:
- `/opt/spaceos/spaceos-modules-joinery/SpaceOS.Modules.Joinery.Infrastructure/Persistence/Repositories/`

### DoorOrderRepository.cs
```csharp
public sealed class DoorOrderRepository : IDoorOrderRepository
{
    private readonly JoineryDbContext _db;
    public DoorOrderRepository(JoineryDbContext db) => _db = db;

    public async Task<DoorOrder?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct)
        => await _db.DoorOrders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id && o.TenantId == tenantId, ct)
            .ConfigureAwait(false);

    public async Task AddAsync(DoorOrder order, CancellationToken ct)
    {
        await _db.DoorOrders.AddAsync(order, ct).ConfigureAwait(false);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(DoorOrder order, CancellationToken ct)
    {
        _db.DoorOrders.Update(order);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<(IReadOnlyList<DoorOrderDto> Items, int TotalCount)> ListAsync(
        Guid tenantId, int page, int pageSize, CancellationToken ct)
    {
        var query = _db.DoorOrders.AsNoTracking()
            .Where(o => o.TenantId == tenantId && !o.IsArchived);
        var total = await query.CountAsync(ct).ConfigureAwait(false);
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .Select(o => new DoorOrderDto(o.Id, o.TenantId, o.FlowEpicId,
                o.ProjectId, o.ProjectName, o.Status.ToString(),
                o.Items.Count, o.ProjectInfo.DeliveryDate, o.CreatedAt))
            .ToListAsync(ct).ConfigureAwait(false);
        return (items, total);
    }
}
```

**Megjegyzés:** Ha a DoorOrder aggregate-en nincs `IsArchived` property, hagyd ki a szűrőből. Ha a `CreatedAt` nincs rajta, rendezés nélkül is OK.

### DoorRulesRepository.cs
```csharp
public sealed class DoorRulesRepository : IDoorRulesRepository
{
    private readonly JoineryDbContext _db;
    public DoorRulesRepository(JoineryDbContext db) => _db = db;

    public async Task<DoorTypeRule?> GetRuleAsync(string doorType, CancellationToken ct)
        => await _db.DoorTypeRules.AsNoTracking()
            .FirstOrDefaultAsync(r => r.DoorType == doorType, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<PartDimensionRule>> GetDimRulesAsync(string doorType, CancellationToken ct)
        => await _db.PartDimensionRules.AsNoTracking()
            .Where(r => r.DoorType == doorType)
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<GlobalConstant?> GetConstantAsync(string key, CancellationToken ct)
        => await _db.GlobalConstants.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Key == key, ct).ConfigureAwait(false);

    public async Task<IReadOnlyList<ProcessTaskTemplate>> GetProcessTemplatesAsync(CancellationToken ct)
        => await _db.ProcessTaskTemplates.AsNoTracking()
            .ToListAsync(ct).ConfigureAwait(false);
}
```

**DI-ba regisztráld** a `DependencyInjection.cs`-ben:
```csharp
services.AddScoped<IDoorOrderRepository, DoorOrderRepository>();
services.AddScoped<IDoorRulesRepository, DoorRulesRepository>();
```

---

## 2. feladat — DoorstarSeedData.cs

Hozd létre: `/opt/spaceos/spaceos-modules-joinery/SpaceOS.Modules.Joinery.Infrastructure/Seeding/DoorstarSeedData.cs`

Az xlsx forrásból a következő adatokat kell beégetni (amit tudsz a domain leírásból):

### GlobalConstants (3 rekord)
```csharp
public static readonly GlobalConstant[] Constants = new[]
{
    new GlobalConstant { Key = "CuttingOversize",  Value = 1m    },
    new GlobalConstant { Key = "CladdingOverhang", Value = 0.2m  },
    new GlobalConstant { Key = "MatyiWidth",       Value = 4.6m  },
};
```

### DoorTypeRules (legalább 6 rekord — a v4.md-ből)
```csharp
public static readonly DoorTypeRule[] DoorTypeRules = new[]
{
    new DoorTypeRule { DoorType = "Disztok",   AjtólapCount = 0, BkmWidthFixed = 8m,  BkmHeightFixed = 4m,  BkmWidthMoving = 1m,   BkmHeightMoving = -4m  },
    new DoorTypeRule { DoorType = "FAF_T",     AjtólapCount = 1, BkmWidthFixed = 8m,  BkmHeightFixed = 4m,  BkmWidthMoving = 2m,   BkmHeightMoving = -4m  },
    new DoorTypeRule { DoorType = "FAF_TN",    AjtólapCount = 1, BkmWidthFixed = 0m,  BkmHeightFixed = 0m,  BkmWidthMoving = 0m,   BkmHeightMoving = 0m   },
    new DoorTypeRule { DoorType = "Falsikban", AjtólapCount = 1, BkmWidthFixed = 1m,  BkmHeightFixed = 0m,  BkmWidthMoving = 0m,   BkmHeightMoving = 0m   },
    new DoorTypeRule { DoorType = "Falcos",    AjtólapCount = 1, BkmWidthFixed = 0m,  BkmHeightFixed = 0m,  BkmWidthMoving = 0m,   BkmHeightMoving = 0m   },
    new DoorTypeRule { DoorType = "Tokba",     AjtólapCount = 1, BkmWidthFixed = -6m, BkmHeightFixed = -6m, BkmWidthMoving = 0m,   BkmHeightMoving = 0m   },
    new DoorTypeRule { DoorType = "Sikban",    AjtólapCount = 1, BkmWidthFixed = 0m,  BkmHeightFixed = 0m,  BkmWidthMoving = 0m,   BkmHeightMoving = 0m   },
    new DoorTypeRule { DoorType = "Pivot",     AjtólapCount = 1, BkmWidthFixed = 0m,  BkmHeightFixed = 0m,  BkmWidthMoving = 0m,   BkmHeightMoving = 0m   },
};
```

### ProcessTaskTemplates (legalább 10 rekord — jellemző feladatok)
```csharp
public static readonly ProcessTaskTemplate[] ProcessTasks = new[]
{
    new ProcessTaskTemplate { TaskId = "GyI-E.01", ShortName = "Anyag kivételezés",   Department = "Előkészítő", UnitTimeSec = 300, Headcount = 1 },
    new ProcessTaskTemplate { TaskId = "GyI-E.02", ShortName = "Keret szabás",        Department = "Előkészítő", UnitTimeSec = 600, Headcount = 1 },
    new ProcessTaskTemplate { TaskId = "GyI-E.03", ShortName = "Betét szabás",        Department = "Előkészítő", UnitTimeSec = 480, Headcount = 1 },
    new ProcessTaskTemplate { TaskId = "GyI-E.04", ShortName = "Borítás szabás",      Department = "Előkészítő", UnitTimeSec = 360, Headcount = 1 },
    new ProcessTaskTemplate { TaskId = "GyI-T.01", ShortName = "Keret összerakás",    Department = "Összeszerelő", UnitTimeSec = 900, Headcount = 2 },
    new ProcessTaskTemplate { TaskId = "GyI-T.02", ShortName = "Betét behelyezés",   Department = "Összeszerelő", UnitTimeSec = 600, Headcount = 1 },
    new ProcessTaskTemplate { TaskId = "GyI-T.03", ShortName = "Borítás ragasztás",  Department = "Összeszerelő", UnitTimeSec = 720, Headcount = 2 },
    new ProcessTaskTemplate { TaskId = "GyI-T.04", ShortName = "Préselés",           Department = "Összeszerelő", UnitTimeSec = 1800, Headcount = 1 },
    new ProcessTaskTemplate { TaskId = "GyV-B.01", ShortName = "Felületkezelés",     Department = "Felületkezelő", UnitTimeSec = 1200, Headcount = 1 },
    new ProcessTaskTemplate { TaskId = "GyV-B.02", ShortName = "Vasalat szerelés",   Department = "Szerelő", UnitTimeSec = 600, Headcount = 1 },
};
```

---

## 3. feladat — DoorRulesDataSeeder implementáció

Hozd létre: `/opt/spaceos/spaceos-modules-joinery/SpaceOS.Modules.Joinery.Infrastructure/Seeding/DoorRulesDataSeeder.cs`

```csharp
public sealed class DoorRulesDataSeeder : IDataSeeder
{
    private readonly JoineryDbContext _db;
    public DoorRulesDataSeeder(JoineryDbContext db) => _db = db;

    public async Task SeedAsync(CancellationToken ct = default)
    {
        // GlobalConstants
        foreach (var c in DoorstarSeedData.Constants)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"""INSERT INTO spaceos_joinery."GlobalConstants" ("Key","Value") VALUES ({c.Key},{c.Value}) ON CONFLICT ("Key") DO NOTHING""", ct)
                .ConfigureAwait(false);
        }

        // DoorTypeRules
        foreach (var r in DoorstarSeedData.DoorTypeRules)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"""INSERT INTO spaceos_joinery."DoorTypeRules" ("DoorType","AjtólapCount","BkmWidthFixed","BkmHeightFixed","BkmWidthMoving","BkmHeightMoving") VALUES ({r.DoorType},{r.AjtólapCount},{r.BkmWidthFixed},{r.BkmHeightFixed},{r.BkmWidthMoving},{r.BkmHeightMoving}) ON CONFLICT ("DoorType") DO NOTHING""", ct)
                .ConfigureAwait(false);
        }

        // ProcessTaskTemplates
        foreach (var t in DoorstarSeedData.ProcessTasks)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync(
                $"""INSERT INTO spaceos_joinery."ProcessTaskTemplates" ("TaskId","ShortName","Department","UnitTimeSec","Headcount") VALUES ({t.TaskId},{t.ShortName},{t.Department},{t.UnitTimeSec},{t.Headcount}) ON CONFLICT ("TaskId") DO NOTHING""", ct)
                .ConfigureAwait(false);
        }
    }
}
```

DI-ba regisztráld: `services.AddScoped<IDataSeeder, DoorRulesDataSeeder>();`

---

## Ellenőrzés

```bash
dotnet build /opt/spaceos/spaceos-modules-joinery/ 2>&1 | tail -3
dotnet test /opt/spaceos/spaceos-modules-joinery/ --no-build 2>&1 | tail -5
```

0 error, 0 warning, 28+ teszt zöld kell.

## Output

Ha kész: `/opt/spaceos/docs/mailbox/kernel/outbox/2026-04-09_036_joinery-repository-done.md`
