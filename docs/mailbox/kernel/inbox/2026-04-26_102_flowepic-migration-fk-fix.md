---
id: MSG-KERNEL-102
from: root
to: kernel
type: task
priority: critical
status: READ
ref: MSG-KERNEL-101-DONE
created: 2026-04-26
---

# KERNEL-102 — FlowEpic migration FK fix (deploy blocker)

> **BUG:** `42804: foreign key constraint "FK_FlowEpicRequiredResources_FlowEpics_FlowEpicId" cannot be implemented`
> A deploy ROLLBACK-elve a régi binárisokra — Kernel healthy a régi verzióval.

## Root cause

A `FlowEpicRequiredResources.FlowEpicId` típusa (valószínűleg `int` vagy `Guid`) nem egyezik a `FlowEpics.Id` oszlop típusával. Az EF Core OwnsMany generált FK-ja nem kompatibilis.

## Fix

1. Ellenőrizd a `FlowEpics.Id` oszlop típusát:
```bash
sudo -u postgres psql -p 5433 spaceos -c "\d+ public.\"FlowEpics\"" | head -20
```

2. A `FlowEpicRequiredResource` entity `FlowEpicId` property-jének pontosan ugyanolyan típusúnak kell lennie.

3. Ha szükséges, töröld a hibás migrációt és generáld újra:
```bash
dotnet ef migrations remove --project <Infra> --startup-project <Api>
dotnet ef migrations add FlowEpic_Scope_MicroAssembly --project <Infra> --startup-project <Api>
```

4. Teszteld lokálisan: `dotnet ef database update` egy teszt DB-n.

## Definition of Done

- [ ] Migration FK típus egyezik
- [ ] `dotnet ef database update` sikeres (teszt DB-n)
- [ ] `dotnet build` 0 error
- [ ] `dotnet test` ≥ 1146 pass
- [ ] Outbox DONE
