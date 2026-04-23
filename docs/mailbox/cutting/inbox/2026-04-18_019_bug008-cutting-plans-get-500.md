---
id: MSG-CUTTING-019
from: root
to: cutting
type: task
priority: high
status: READ
created: 2026-04-18
---

# BUG-008 — GET /bff/cutting/plans → 500

## Szimptóma (böngésző console)

```
bff/cutting/plans: Failed to load resource: 500 (Internal Server Error)
```

Manuális tesztelés közben (test-admin felhasználó) a Vágótervek oldal nem töltődik be.

## A kód helyes elvileg

`GetAllCuttingPlans` handler:
```csharp
private static async Task<IResult> GetAllCuttingPlans(
    ICuttingRepository repo,
    CancellationToken ct)
{
    var plans = await repo.GetAllDailyCuttingPlansAsync(ct).ConfigureAwait(false);
    var result = plans.Select(p => new { ... });
    return Results.Ok(result);
}
```

Üres lista esetén `Results.Ok(emptyArray)` kellene → 200. Valami más dob 500-at.

## Vizsgálandó

### 1. VPS log — mi a tényleges exception?

```bash
journalctl -u spaceos-cutting-svc --since "1 hour ago" | grep -E "error|exception|500|Error" -i | tail -30
```

### 2. TenantSessionInterceptor — dob-e kivételt?

A `DbConnectionInterceptor.ConnectionOpeningAsync`:
- Ha `tid` null vagy Guid.Empty → `early return` (CUTTING-013 fix) ✅
- De ha `set_config('app.current_tenant_id', ...)` hibát dob PG-ben → 500

Ellenőrizd: a `CUTTING-013` commit (`c3323ed`) valóban a jelenlegi deployed binárisban van-e:
```bash
cd /opt/spaceos/spaceos-modules-cutting && git log --oneline | head -5
```

### 3. DateTime serialization — meglévő tervek

Ha a DB-ben van olyan `DailyCuttingPlan` sor, amelynek `PlanDate` Kind=Unspecified, az `ToString("yyyy-MM-dd")` nem dob hibát — de az EF query fetch dobhat Npgsql exception-t (`timestamptz` + Unspecified Kind).

Ellenőrizd: van-e már sor a `DailyCuttingPlans` táblában?
```sql
SELECT id, name, plan_date FROM spaceos_cutting.daily_cutting_plans LIMIT 5;
```

## DoD

- [ ] 500 gyökérokja azonosítva és dokumentálva
- [ ] `GET /api/cutting/plans` → **200** (üres tömb is OK)
- [ ] `dotnet test` → legalább 77 zöld
- [ ] Ha migration vagy INFRA deploy kell → jelezd

---

*Skill: `/spaceos-terminal`*
