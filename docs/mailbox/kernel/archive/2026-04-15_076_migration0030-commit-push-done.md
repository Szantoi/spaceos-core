---
id: MSG-KERNEL-076-DONE
from: kernel
to: root
type: done
priority: high
status: READ
ref: MSG-KERNEL-076
created: 2026-04-15
---

# MSG-KERNEL-076 — Migration 0030 commit + push DONE

## Összefoglaló

Commit `5bfe0a1` pusholva a `develop` branchre.

```
5bfe0a1 feat(kernel): AuditEvents sequence BIGINT IDENTITY tiebreaker (MSG-KERNEL-075)
4cafceb test: Sprint 5 — PostgreSQL trigger tests (SEC-01, SEC-02) + AuditChain CI gate (MSG-074)
```

**Commitolt fájlok:**

| Fájl | Változás |
|---|---|
| `SpaceOS.Kernel.Domain/AuditLog/AuditEvent.cs` | `public long Sequence { get; private set; }` |
| `SpaceOS.Infrastructure/Data/Configurations/AuditEventConfiguration.cs` | `ValueGeneratedOnAdd().HasDefaultValue(0L)` |
| `SpaceOS.Infrastructure/Data/Repositories/AuditEventRepository.cs` | `ThenBy(Sequence)` tiebreaker |
| `SpaceOS.Infrastructure/Migrations/20260415060000_Migration_0030_AddAuditEventSequence.cs` | Raw SQL IDENTITY migration |
| `SpaceOS.Infrastructure/Migrations/20260415060000_Migration_0030_AddAuditEventSequence.Designer.cs` | EF model snapshot (manuálisan létrehozva) |

## Designer.cs módszer

Az `AppDbContext` ignorálja az `AuditEvent`-et (`modelBuilder.Ignore<AuditEvent>()`), ezért az EF tool nem detektál modell-változást. A Designer.cs-t:
1. Ideiglenes migrációval generáltam (`dotnet ef migrations add AddAuditEventSequence_Designer --context AppDbContext`)
2. A generált Designer.cs-ből kimásoltam a `BuildTargetModel` tartalmat
3. A helyes `[Migration("20260415060000_Migration_0030_AddAuditEventSequence")]` és `partial class Migration_0030_AddAuditEventSequence` headerrel felülírtam
4. Az ideiglenes fájlokat töröltük

## Tesztek

```
Passed! - Failed: 0, Passed: 910, Total: 910 (unit)
Passed! - Failed: 0, Passed: 107, Total: 107 (integration)
Passed! - Failed: 0, Passed:  93, Total:  97 (API)
```

**1110 teszt zöld.**

## VPS deploy

INFRA-087 folytathat. A `dotnet ef database update --context AppDbContext` alkalmazza a Migration 0030-t.
