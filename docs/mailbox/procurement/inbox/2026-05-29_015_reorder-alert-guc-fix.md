---
id: MSG-PROCUREMENT-015
from: root
to: procurement
type: task
priority: critical
status: READ
ref: MSG-PROCUREMENT-014-DONE
created: 2026-05-29
---

# Procurement v2 — `from-reorder-alert` RLS fix (GUC nincs beállítva)

## Probléma

A `POST /internal/from-reorder-alert` endpoint 500-at ad vissza a VPS-en:

```
42501: new row violates row-level security policy for table "purchase_requisition"
Routine: ExecWithCheckOptions
```

**Root cause:** EF Core `SaveChangesAsync` → `INSERT ... RETURNING` → PostgreSQL a RETURNING
sorait a SELECT USING policy-vel ellenőrzi. GUC `app.current_tenant_id = ''` (DB default) →
`NULLIF('', '')::uuid = NULL` → `TenantId = NULL` → FALSE → 42501.

A `WITH CHECK (true)` csak az INSERT check-et engedi át, de a RETURNING láthatóságát
a USING clause dönti el — és ha az NULL-t ad, PostgreSQL hibát dob.

## Fix — `InternalEndpoints.cs`

A meglévő `delete by tenant` endpoint **pontosan ezt a mintát alkalmazza** — ugyanezt kell
a `from-reorder-alert` endpointba is behozni.

**Fájl:** `src/SpaceOS.Modules.Procurement.Api/Endpoints/InternalEndpoints.cs`

A `from-reorder-alert` MapPost lambdában, a `mediator.Send(command, ct)` hívás elé:

```csharp
// Pin GUC to this connection so RETURNING sees the inserted row (RLS USING check)
var dbContext = ctx.RequestServices.GetRequiredService<ProcurementDbContext>();

if (dbContext.Database.IsRelational())
    await dbContext.Database.OpenConnectionAsync(ct).ConfigureAwait(false);

ReorderAlertReceiverResult result;
try
{
    if (dbContext.Database.IsRelational())
        await dbContext.Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.current_tenant_id', {0}, false)",
            request.TenantId.ToString()).ConfigureAwait(false);

    result = await mediator.Send(command, ct).ConfigureAwait(false);
}
catch (InvalidOperationException ex) when (ex.Message.Contains("MaterialCode"))
{
    return Results.UnprocessableEntity(new { error = "Unprocessable", message = ex.Message });
}
finally
{
    if (dbContext.Database.IsRelational())
        await dbContext.Database.CloseConnectionAsync().ConfigureAwait(false);
}
```

A meglévő `try/catch (InvalidOperationException)` blokk beleolvad ebbe a finally-ba.

**Fontos:** a `ProcurementDbContext` scoped → ugyanaz a connection, amit a repository is használ →
a `set_config` értéke látható a handler SaveChangesAsync hívásakor.

## Tesztelés

```bash
# Build + test
dotnet build && dotnet test

# VPS publish + restart — commit hash jegyezd fel
sudo systemctl restart spaceos-procurement

# Smoke test 1: orphan materialCode → 422
curl -s -w "\nHTTP: %{http_code}" \
  -X POST http://127.0.0.1:5006/internal/from-reorder-alert \
  -H "Authorization: Bearer 5a9caf8c2e96b87ed37278a78b67cf061de3bec556e5df2d5a5b70effd0dcbe4" \
  -H "X-SpaceOS-TenantId: 63ef28b6-a43b-4d3f-a076-759a47911559" \
  -H "Content-Type: application/json" \
  -d '{"tenantId":"63ef28b6-a43b-4d3f-a076-759a47911559","materialCode":"NONEXISTENT","currentStock":0,"reorderPoint":5,"suggestedQuantity":10,"preferredSupplierId":null,"unitOfMeasure":"pcs","alertedAt":"2026-05-29T12:00:00Z"}'
# Elvárt: 422

# Smoke test 2: valós materialCode → 201 (ha létezik anyag) vagy egyéb helyes válasz
```

## DONE kritériumok

- [ ] Build zöld
- [ ] Tesztek zöldek
- [ ] VPS-en publish + restart elvégezve
- [ ] `from-reorder-alert` orphan materialCode → 422
- [ ] Commit hash az outbox DONE üzenetben

## Scope

Csak `InternalEndpoints.cs` — a repository és handler nem változik.
