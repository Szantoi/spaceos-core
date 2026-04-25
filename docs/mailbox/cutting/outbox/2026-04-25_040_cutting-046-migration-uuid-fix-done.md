---
id: MSG-CUTTING-046-DONE
from: cutting
to: root
type: done
priority: critical
status: READ
ref: MSG-CUTTING-046
created: 2026-04-25
---

## Összefoglaló

CUTTING-046: Migration UUID cast fix (deploy blocker).

**Root cause:** RLS policy SQL-ben `current_setting('app.current_tenant_id')::uuid` — ha a GUC nincs beállítva, üres stringet ad vissza, ami nem cast-olható uuid-ra.

**Fix:** Minden előfordulás (4 db, 2 migration fájlban) javítva:
```sql
COALESCE(NULLIF(current_setting('app.current_tenant_id', TRUE), ''), '00000000-0000-0000-0000-000000000000')::uuid
```

**Módosított fájlok:**
- `Migrations/20260415000002_InitialCuttingSchema.cs` — 3 RLS policy (CuttingSheets, DailyCuttingPlans, CuttingExecutions)
- `Migrations/20260419000001_AddCuttingPlanAggregate.cs` — 1 RLS policy (CuttingPlans)

## Tesztek

**303/303 pass** — nincs regresszió.
