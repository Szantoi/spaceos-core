---
id: MSG-J042
from: architect
to: joinery
type: task
priority: P0
date: 2026-04-09
sprint: "Modules.Joinery v1 DoD Completion"
---

# Modules.Joinery v1 — DoD Completion

## Kontextus

A solution kész, 32 teszt zöld, a service fut. Három dolog hiányzik a DoD-hoz:

1. **DB migrációk nem alkalmazva** — `spaceos_joinery` schema nem létezik a prodDB-ben
2. **Tesztek: 32 → ≥65** (33+ új teszt szükséges)
3. **Seed adat hiányos** — DoorTypeRules: 8 (kell ≥15), ProcessTaskTemplates: 10 (kell ≥40)

---

## T1 — DB migráció alkalmazása

A `DependencyInjection.cs` nem hív `MigrateAsync()`-t. Két lépés:

### 1a — Program.cs: auto-migrate production-ban is

`SpaceOS.Modules.Joinery.Api/Program.cs` — az `app.Run()` elé, az `ApplicationStarted` hook elé:

```csharp
// Auto-migrate on startup (idempotent — safe to run every deploy)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<JoineryDbContext>();
    await db.Database.MigrateAsync().ConfigureAwait(false);
}
```

### 1b — Migrációk futtatása (VPS)

A service indítja a migrációt, de ha kézzel kell:

```bash
cd /opt/spaceos/spaceos-modules-joinery
dotnet ef database update \
  --project SpaceOS.Modules.Joinery.Infrastructure \
  --startup-project SpaceOS.Modules.Joinery.Api \
  --connection "Host=127.0.0.1;Port=5433;Database=spaceos_joinery;Username=spaceos;Password=spaceos_db_pass"
```

---

## T2 — DoorstarSeedData.cs bővítése

Fájl: `SpaceOS.Modules.Joinery.Infrastructure/Seeding/DoorstarSeedData.cs`

### DoorTypeRules: 8 → ≥15 rekord

Adj hozzá 7 új ajtótípust (ügyelj az érvényes BKM értékekre):

```csharp
new DoorTypeRule { DoorType = "Kétszárnyú",  AjtólapCount = 2, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
new DoorTypeRule { DoorType = "Tűzálló",     AjtólapCount = 1, BkmWidthFixed = 2m,    BkmHeightFixed = 2m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
new DoorTypeRule { DoorType = "Üveges",      AjtólapCount = 1, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
new DoorTypeRule { DoorType = "Csúszó",      AjtólapCount = 1, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
new DoorTypeRule { DoorType = "Redőnyes",    AjtólapCount = 0, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
new DoorTypeRule { DoorType = "Akusztikus",  AjtólapCount = 1, BkmWidthFixed = 4m,    BkmHeightFixed = 4m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
new DoorTypeRule { DoorType = "Forgóajtó",   AjtólapCount = 4, BkmWidthFixed = 0m,    BkmHeightFixed = 0m,    BkmWidthMoving = 0m,    BkmHeightMoving = 0m    },
```

### ProcessTaskTemplates: 10 → ≥40 rekord

Adj hozzá 30+ új sablont. Az alábbi struktúrát kövesd — a meglévő 10 mellé:

**Ajtólap gyártás (GyI-L csoport — 6 rekord):**
```csharp
new ProcessTaskTemplate { TaskId = "GyI-L.01", ShortName = "Lapszabás",             Description = "Ajtólap/panel méretvágása",           Department = "Gyártás",     UnitTimeSec = 600,  Headcount = 1, ParentTaskId = null        },
new ProcessTaskTemplate { TaskId = "GyI-L.02", ShortName = "Betétkivágás",           Description = "Betét méretvágása és előkészítés",     Department = "Gyártás",     UnitTimeSec = 300,  Headcount = 1, ParentTaskId = "GyI-L.01"  },
new ProcessTaskTemplate { TaskId = "GyI-L.03", ShortName = "Élzárás",                Description = "Látható élek élzárása",                Department = "Gyártás",     UnitTimeSec = 480,  Headcount = 1, ParentTaskId = "GyI-L.01"  },
new ProcessTaskTemplate { TaskId = "GyI-L.04", ShortName = "Lapfúrás",               Description = "Zsanér- és zárfúrások",                Department = "Gyártás",     UnitTimeSec = 360,  Headcount = 1, ParentTaskId = "GyI-L.01"  },
new ProcessTaskTemplate { TaskId = "GyI-L.05", ShortName = "Lapfelület-kezelés",     Description = "Csiszolás és alapozás",                Department = "Felület",     UnitTimeSec = 900,  Headcount = 1, ParentTaskId = "GyI-L.01"  },
new ProcessTaskTemplate { TaskId = "GyI-L.06", ShortName = "Lap-minőségellenőrzés",  Description = "Lap vizuális és méret-ellenőrzés",     Department = "Minőség",     UnitTimeSec = 300,  Headcount = 1, ParentTaskId = "GyI-L.01"  },
```

**Vasalat (GyI-V csoport — 5 rekord):**
```csharp
new ProcessTaskTemplate { TaskId = "GyI-V.01", ShortName = "Zsanér szerelés",        Description = "Zsanérok felszerelése a lapra",        Department = "Szerelés",    UnitTimeSec = 600,  Headcount = 1, ParentTaskId = null        },
new ProcessTaskTemplate { TaskId = "GyI-V.02", ShortName = "Zárfogadó marás",        Description = "Zárfogadó fészek marása",              Department = "Gyártás",     UnitTimeSec = 480,  Headcount = 1, ParentTaskId = "GyI-V.01"  },
new ProcessTaskTemplate { TaskId = "GyI-V.03", ShortName = "Kilincs szerelés",       Description = "Kilincsgarnitúra felszerelése",         Department = "Szerelés",    UnitTimeSec = 300,  Headcount = 1, ParentTaskId = "GyI-V.01"  },
new ProcessTaskTemplate { TaskId = "GyI-V.04", ShortName = "Zár szerelés",           Description = "Zártest beépítése és beállítása",      Department = "Szerelés",    UnitTimeSec = 420,  Headcount = 1, ParentTaskId = "GyI-V.01"  },
new ProcessTaskTemplate { TaskId = "GyI-V.05", ShortName = "Vasalat ellenőrzés",     Description = "Vasalatok QC és funkcionális teszt",   Department = "Minőség",     UnitTimeSec = 180,  Headcount = 1, ParentTaskId = "GyI-V.01"  },
```

**Összeszereléses fázis (GyII-A csoport — 5 rekord):**
```csharp
new ProcessTaskTemplate { TaskId = "GyII-A.01", ShortName = "Tok-lap illesztés",    Description = "Tok és lap egymáshoz illesztése",      Department = "Gyártás",     UnitTimeSec = 720,  Headcount = 2, ParentTaskId = null        },
new ProcessTaskTemplate { TaskId = "GyII-A.02", ShortName = "Ragasztás és préselés",Description = "Borítás ragasztása, présgép",          Department = "Gyártás",     UnitTimeSec = 1800, Headcount = 1, ParentTaskId = "GyII-A.01" },
new ProcessTaskTemplate { TaskId = "GyII-A.03", ShortName = "Szárítási idő",        Description = "Ragasztó kötési/szárítási idő",        Department = "Gyártás",     UnitTimeSec = 3600, Headcount = 0, ParentTaskId = "GyII-A.02" },
new ProcessTaskTemplate { TaskId = "GyII-A.04", ShortName = "Csiszolás",            Description = "Felületi csiszolás az összeszereléas után", Department = "Gyártás",  UnitTimeSec = 600,  Headcount = 1, ParentTaskId = "GyII-A.01" },
new ProcessTaskTemplate { TaskId = "GyII-A.05", ShortName = "Alapozás",             Description = "Festékalapozó felhordása",             Department = "Felület",     UnitTimeSec = 600,  Headcount = 1, ParentTaskId = "GyII-A.01" },
```

**Festési/borítási fázis (GyII-F csoport — 5 rekord):**
```csharp
new ProcessTaskTemplate { TaskId = "GyII-F.01", ShortName = "Festék előkészítés",   Description = "Festék keverés és előkészítés",        Department = "Felület",     UnitTimeSec = 300,  Headcount = 1, ParentTaskId = null        },
new ProcessTaskTemplate { TaskId = "GyII-F.02", ShortName = "Festés",               Description = "Fedőréteg felhordása",                 Department = "Felület",     UnitTimeSec = 900,  Headcount = 1, ParentTaskId = "GyII-F.01" },
new ProcessTaskTemplate { TaskId = "GyII-F.03", ShortName = "Szárítás",             Description = "Festék szárítási idő",                 Department = "Felület",     UnitTimeSec = 1200, Headcount = 0, ParentTaskId = "GyII-F.02" },
new ProcessTaskTemplate { TaskId = "GyII-F.04", ShortName = "Felületi QC",          Description = "Felszín vizuális minőség-ellenőrzés",  Department = "Minőség",     UnitTimeSec = 300,  Headcount = 1, ParentTaskId = "GyII-F.01" },
new ProcessTaskTemplate { TaskId = "GyII-F.05", ShortName = "Lakkozás",             Description = "Záróréteget lakkozás",                 Department = "Felület",     UnitTimeSec = 600,  Headcount = 1, ParentTaskId = "GyII-F.01" },
```

**Csomagolás és szállítás (GyV-C csoport — 5 rekord):**
```csharp
new ProcessTaskTemplate { TaskId = "GyV-C.01", ShortName = "Csomagolás előkészítés",Description = "Csomagolóanyag előkészítése",          Department = "Csomagolás",  UnitTimeSec = 180,  Headcount = 1, ParentTaskId = null        },
new ProcessTaskTemplate { TaskId = "GyV-C.02", ShortName = "Csomagolás",            Description = "Ajtó becsomagolása védőfóliával",      Department = "Csomagolás",  UnitTimeSec = 600,  Headcount = 2, ParentTaskId = "GyV-C.01"  },
new ProcessTaskTemplate { TaskId = "GyV-C.03", ShortName = "Csomag ellenőrzés",     Description = "Csomagolás sértetlenség ellenőrzése",  Department = "Minőség",     UnitTimeSec = 180,  Headcount = 1, ParentTaskId = "GyV-C.01"  },
new ProcessTaskTemplate { TaskId = "GyV-C.04", ShortName = "Rakodás",               Description = "Szállítójárműre rakodás",              Department = "Raktár",      UnitTimeSec = 600,  Headcount = 2, ParentTaskId = "GyV-C.01"  },
new ProcessTaskTemplate { TaskId = "GyV-C.05", ShortName = "Szállítás",             Description = "Helyszínre szállítás",                 Department = "Szállítás",   UnitTimeSec = 0,    Headcount = 1, ParentTaskId = "GyV-C.01"  },
```

**Reklamáció / javítás (GyR-J csoport — 5 rekord):**
```csharp
new ProcessTaskTemplate { TaskId = "GyR-J.01", ShortName = "Reklamáció fogadás",    Description = "Ügyfél reklamáció felvétele",          Department = "Értékesítés", UnitTimeSec = 300,  Headcount = 1, ParentTaskId = null        },
new ProcessTaskTemplate { TaskId = "GyR-J.02", ShortName = "Helyszíni szemle",      Description = "Hiba helyszíni azonosítása",           Department = "Szerelés",    UnitTimeSec = 1800, Headcount = 1, ParentTaskId = "GyR-J.01"  },
new ProcessTaskTemplate { TaskId = "GyR-J.03", ShortName = "Javítás",               Description = "Hiba elhárítása / csere",              Department = "Szerelés",    UnitTimeSec = 3600, Headcount = 2, ParentTaskId = "GyR-J.01"  },
new ProcessTaskTemplate { TaskId = "GyR-J.04", ShortName = "Javítás QC",            Description = "Javítás utáni ellenőrzés",             Department = "Minőség",     UnitTimeSec = 600,  Headcount = 1, ParentTaskId = "GyR-J.03"  },
new ProcessTaskTemplate { TaskId = "GyR-J.05", ShortName = "Ügyfél-jóváhagyás",     Description = "Ügyfél aláírja az átadást",            Department = "Értékesítés", UnitTimeSec = 600,  Headcount = 1, ParentTaskId = "GyR-J.03"  },
```

**Végeredmény:** 10 (meglévő) + 6+5+5+5+5+5 = **41 ProcessTaskTemplate** ✅

**DoorRulesDataSeeder frissítése** — a `DoorRulesDataSeeder.cs`-ben a ProcessTaskTemplates INSERT-hez adj hozzá a `ParentTaskId` mezőt:
```csharp
$"""INSERT INTO spaceos_joinery."ProcessTaskTemplates"
   ("TaskId","ShortName","Description","Department","UnitTimeSec","Headcount","ParentTaskId")
   VALUES ({t.TaskId},{t.ShortName},{t.Description},{t.Department},{t.UnitTimeSec},{t.Headcount},{t.ParentTaskId})
   ON CONFLICT ("TaskId") DO NOTHING"""
```

---

## T3 — Tesztek bővítése: 32 → ≥65

### 3a — API integrációs tesztek (WebApplicationFactory, ≥15 új teszt)

Hozd létre: `SpaceOS.Modules.Joinery.Tests/Api/DoorOrderApiTests.cs`

```csharp
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.Joinery.Api;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;
using Xunit;

public class DoorOrderApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    // WebApplicationFactory + InMemory DB + JWT-less (ManufacturerOnly policy mock-kal)
    // Teszteld:
    //   - POST /api/orders → 201 (érvényes body)
    //   - POST /api/orders → 422 (üres FlowEpicId)
    //   - POST /api/orders → 401 (nincs auth token)
    //   - POST /api/orders/{id}/items → 201 (Draft státuszú order-hez)
    //   - POST /api/orders/{id}/items → 400 (érvénytelen UUID)
    //   - POST /api/orders/{id}/items (Submitted order) → 422 (FSM guard)
    //   - POST /api/orders/{id}/items (501. item) → 422 (MaxItems)
    //   - POST /api/orders/{id}/calculate → 200
    //   - GET  /api/orders/{id}/cutting-list → Cache-Control: no-store header
    //   - GET  /api/orders/{id}/cutting-list → 404 (ismeretlen id)
    //   - POST /api/orders/{id}/submit → 200
    //   - POST /api/orders/{id}/submit (már Submitted) → 422
    //   - GET  /api/orders?page=1&pageSize=10 → 200 paged response
    //   - GET  /api/orders/{id} → 200
    //   - GET  /api/orders/{id} → 404
    //   - GET  /health → 200 { status: "healthy" } (anonymous)
}
```

**Tipp:** A ManufacturerOnly policy-t teszt-környezetben bypass-old egy `TestAuthHandler`-rel (lásd: `WithWebHostBuilder` + `AddAuthentication("Test")`).

### 3b — Biztonsági gate tesztek (≥5 új teszt)

Hozd létre: `SpaceOS.Modules.Joinery.Tests/Security/JoinerySecurityGateTests.cs`

```csharp
// Reflection-alapú, futásidőben ellenőrzik a rule-okat:
// - DoorOrder nincs publikus setter (összes property)
// - IDoorCalculationService nem referenciál DateTime.Now / Random / IO
// - CuttingList endpoint-on Cache-Control: no-store megkövetelve (attribútum check)
// - DoorOrderStatus enum tartalmaz pontosan: Draft, Submitted, Calculated, Archived
// - DoorItem nem örököl DoorOrder-től (nincs nav prop visszafelé)
```

### 3c — Handler tesztek bővítése (≥8 új teszt)

Bővítsd a meglévő Handler teszteket:

**`AddDoorItemHandlerTests.cs`** (+3 teszt):
- Submitted státuszú order-hez AddItem → `Result.Invalid`
- 500 item után a 501. → `Result.Invalid`
- Tenant mismatch (order.TenantId != JWT tenant) → `Result.Forbidden`

**`GetCuttingListHandlerTests.cs`** (+3 teszt):
- Kalkulált order → cutting list tartalmaz legalább 1 elemet
- Kalkálatlan order → `Result.Invalid`
- Ismeretlen orderId → `Result.NotFound`

**`SubmitDoorOrderHandlerTests.cs`** (+2 teszt):
- Már Submitted order újra Submit → `Result.Invalid`
- 0 item-es Draft order Submit → `Result.Invalid`

### 3d — DoorstarSeedDataTests bővítése (+5 teszt)

`SpaceOS.Modules.Joinery.Tests/Seeding/DoorstarSeedDataTests.cs` bővítése:

```csharp
[Fact]
public void DoorTypeRules_AtLeast15Records()
    => Assert.True(DoorstarSeedData.DoorTypeRules.Count >= 15);

[Fact]
public void ProcessTasks_AtLeast40Records()
    => Assert.True(DoorstarSeedData.ProcessTasks.Count >= 40);

[Fact]
public void ProcessTasks_AllTaskIdsUnique()
    => Assert.Equal(DoorstarSeedData.ProcessTasks.Count,
        DoorstarSeedData.ProcessTasks.Select(t => t.TaskId).Distinct().Count());

[Fact]
public void DoorTypeRules_AllDoorTypesUnique()
    => Assert.Equal(DoorstarSeedData.DoorTypeRules.Count,
        DoorstarSeedData.DoorTypeRules.Select(r => r.DoorType).Distinct().Count());

[Fact]
public void PartDimensionRules_AllHaveValidDoorType()
{
    var validTypes = DoorstarSeedData.DoorTypeRules.Select(r => r.DoorType).ToHashSet();
    Assert.All(DoorstarSeedData.PartDimensionRules,
        r => Assert.Contains(r.DoorType, validTypes));
}
```

---

## DoD ellenőrzés

```bash
cd /opt/spaceos/spaceos-modules-joinery
dotnet build 2>&1 | tail -3   # 0 error, 0 warning
dotnet test --no-build 2>&1 | tail -5  # ≥65 pass, 0 fail
```

DB ellenőrzés (migrációk után):
```bash
PGPASSWORD=spaceos_db_pass psql -U spaceos -h 127.0.0.1 -p 5433 \
  -c 'SELECT COUNT(*) FROM spaceos_joinery."DoorTypeRules";'
# → ≥15

PGPASSWORD=spaceos_db_pass psql -U spaceos -h 127.0.0.1 -p 5433 \
  -c 'SELECT COUNT(*) FROM spaceos_joinery."ProcessTaskTemplates";'
# → ≥40
```

---

## Output

Ha kész: `docs/mailbox/joinery/outbox/2026-04-09_042_joinery-dod-completion-done.md`

Visszajelzés tartalma:
- Teszt összesítő (Passed/Failed)
- DB migráció állapot (schema létrejött?)
- DoD gate-ek checklistje
