# MSG-J036 — Joinery Repository Layer Done

**Date:** 2026-04-09
**Status:** CLOSED_DONE
**Build:** 0 errors, 0 warnings
**Tests:** 28 passed, 0 failed, 0 skipped

---

## Summary

All 5 tasks of MSG-J036 implemented and verified.

| Task | File | Change |
|------|------|--------|
| T1 | `Infrastructure/Persistence/Repositories/DoorOrderRepository.cs` | New — implements `IDoorOrderRepository` with Include, AddAsync, UpdateAsync, ListAsync (no OrderBy — no CreatedAt on base) |
| T2 | `Infrastructure/Persistence/Repositories/DoorRulesRepository.cs` | New — implements `IDoorRulesRepository` exactly per interface (GetDoorTypeRuleAsync, GetPartDimensionRulesAsync, GetGlobalConstantsAsync) |
| T3 | `Infrastructure/Seeding/DoorstarSeedData.cs` | New — static seed data: 3 GlobalConstants, 8 DoorTypeRules, 10 ProcessTaskTemplates |
| T4 | `Infrastructure/Seeding/DoorRulesDataSeeder.cs` | New — implements `IDataSeeder`, uses `ExecuteSqlInterpolatedAsync` + `ON CONFLICT DO NOTHING` |
| T5 | `Infrastructure/DependencyInjection.cs` | Updated — registers IDoorOrderRepository, IDoorRulesRepository, IDataSeeder |

---

## Implementation Notes

- `TenantScopedEntity` base class has no `CreatedAt` — `ListAsync` omits `OrderBy`; `DoorOrderDto.CreatedAt` is passed as `default` (DateTime.MinValue) until a migration adds the column
- `DoorOrderDto` 9-param constructor satisfied: `ProjectName ?? string.Empty` for the non-nullable string param, `ProjectInfo?.DeliveryDate` for the nullable `DateOnly?`
- `DoorRulesRepository` returns empty collection (not null) when `doorType` is whitespace — consistent with interface contract
- Seeder inserts are individual per-row to allow partial conflict resolution; all three tables use `ON CONFLICT (...PK...) DO NOTHING`
- All async methods use `ConfigureAwait(false)` per project convention

---

## Build Output

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Test Output

```
Passed!  - Failed: 0, Passed: 28, Skipped: 0, Total: 28, Duration: 102 ms
```
