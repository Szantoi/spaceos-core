---
id: MSG-CUTTING-031
from: root
to: cutting
type: task
priority: high
status: READ
ref: MSG-ARCH-002-RESPONSE
created: 2026-04-20
---

# CUTTING-031 — CuttingPlanStatus typed enum + DB migration

> **Skill:** `/spaceos-terminal` szerint dolgozz — inbox → build → test → outbox DONE
> **Arch spec:** `docs/architecture/SpaceOS_Modules_Cutting_Planning_Architecture_v4.md` (D-41..D-43)
> **Timeline:** ~1 nap
> **Session B első task** — ez blokkol: CUTTING-032 (DaySlot)

---

## Kontextus

A `CuttingPlan.Status` jelenleg `string` típusú (`Draft/Approved/InProgress/Closed`). Ez törékeny — typed enum-ra cseréljük, és DB migrációt írunk a meglévő adatok konvertálásához.

**OQ-1 döntés (jóváhagyva):** `Approved` → `Published(1)` lesz az enum neve.

---

## Feladatok

### 1. CuttingPlanStatus enum létrehozása

**Fájl:** `SpaceOS.Modules.Cutting.Domain/Enums/CuttingPlanStatus.cs`

```csharp
namespace SpaceOS.Modules.Cutting.Domain.Enums;

public enum CuttingPlanStatus
{
    Draft = 0,
    Published = 1,
    Frozen = 2,
    Closed = 3
}
```

### 2. CuttingPlan.cs frissítése

- `public string Status` → `public CuttingPlanStatus Status`
- Konstruktor: `Status = CuttingPlanStatus.Draft`
- Eltávolítani a string konstansokat ha vannak

### 3. EF Core konfiguráció

Az EF Core konfig fájlban (`CuttingPlanConfiguration.cs` vagy `OnModelCreating`):

```csharp
builder.Property(p => p.Status)
    .HasConversion<int>()
    .IsRequired();
```

### 4. DB migration

```bash
dotnet ef migrations add CuttingPlanStatusToEnum \
  --project SpaceOS.Modules.Cutting.Infrastructure \
  --startup-project SpaceOS.Modules.Cutting.Api
```

A migration `Up()` metódusban CASE konverzió a meglévő string értékekről:

```sql
ALTER TABLE "CuttingPlans" ADD COLUMN "StatusInt" integer NOT NULL DEFAULT 0;

UPDATE "CuttingPlans" SET "StatusInt" = CASE "Status"
    WHEN 'Draft'       THEN 0
    WHEN 'Approved'    THEN 1
    WHEN 'InProgress'  THEN 2
    WHEN 'Closed'      THEN 3
    ELSE 0
END;

ALTER TABLE "CuttingPlans" DROP COLUMN "Status";
ALTER TABLE "CuttingPlans" RENAME COLUMN "StatusInt" TO "Status";
```

### 5. Érintett fájlok frissítése

Minden helyen ahol `string` status-t használtunk, cseréld `CuttingPlanStatus` enum-ra:
- Query handlerek (`GetCuttingPlan*`, `ListCuttingPlans*`)
- Command handlerek (`CreateCuttingPlan`, `ApproveCuttingPlan` stb.)
- DTO mapping-ek (response DTO-kban string-re konvertálás az API felé OK)

---

## Definition of Done

- [ ] `CuttingPlanStatus` enum létrehozva
- [ ] `CuttingPlan.Status` property típusa `CuttingPlanStatus`
- [ ] EF Core `HasConversion<int>()` konfigurálva
- [ ] Migration létrehozva CASE konverzióval
- [ ] `dotnet build` 0 error, 0 warning
- [ ] `dotnet test` mind zöld (194+)
- [ ] Outbox DONE üzenet küldve
