---
id: MSG-CUTTING-046
from: root
to: cutting
type: task
priority: critical
status: READ
created: 2026-04-25
---

# CUTTING-046 — Migration UUID cast fix (deploy blocker)

> **BUG:** Service startup crash: `22P02: invalid input syntax for type uuid: ""`
> **Root cause:** A CUTTING-043/044 migration RLS policy `current_setting('app.tenant_id', TRUE)::uuid` — de a GUC default értéke üres string `''`, ami nem cast-olható uuid-ra.

## Fix

A migration raw SQL-ben az összes `current_setting('app.tenant_id', TRUE)::uuid` helyett:

```sql
COALESCE(NULLIF(current_setting('app.tenant_id', TRUE), ''), '00000000-0000-0000-0000-000000000000')::uuid
```

Ez a FreeTier-ben bevált pattern (F_0001).

**FONTOS:** Ha a migration már lefutott (de crash-elt) → a migration state inkonzisztens lehet. Ellenőrizd a `__EFMigrationsHistory` táblát és ha szükséges, rollback-elj vagy hozz létre javító migration-t.

## Tesztek

Meglévő tesztek nem törhetnek el.

## Definition of Done

- [ ] Migration UUID cast javítva
- [ ] Service elindul: `dotnet run` lokálisan → health OK
- [ ] `dotnet build` 0 error
- [ ] `dotnet test` ≥ 303 pass
- [ ] Outbox DONE
