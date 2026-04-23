---
id: MSG-JOINERY-010
from: root
to: joinery
type: task
priority: high
status: READ
ref: SpaceOS_Doorstar_Portal_UI_Test_Strategy_v4
created: 2026-04-16
---

# JOINERY-010 — BE-TEST-03: `DELETE /internal/orders/by-tenant/{tenantId}`

## Kontextus

Ref: `docs/tasks/new/SpaceOS_Doorstar_Portal_UI_Test_Strategy_v4.md` §6.5

Párhuzamos feladat KERNEL-085-tel. A Test BFF reset során az Orchestrator hívja az összes modul internal delete endpointját. A Joinery feladata: DoorOrder + CuttingListSnapshot törlés adott tenanthoz.

**Security kényszer (SEC-TS-01 + modul-szintű defense in depth):**
- Csak `X-SpaceOS-Internal` header-rel hívható
- `TEST_TENANT_ALLOWLIST` env var alapján allowlist ellenőrzés
- Ha nincs a listán → 403 (véd az Orchestrator kompromittálódása ellen)

## Implementálandó

### Endpoint

```
DELETE /internal/orders/by-tenant/{tenantId}?confirm=true

Headers:
  X-SpaceOS-Internal: true
  
Response 200:
{
  "tenantId": "uuid",
  "deletedCounts": {
    "doorOrders": 3,
    "cuttingListSnapshots": 2
  }
}

Response 403: { "error": "Forbidden", "message": "Tenant not in test allowlist" }
Response 400: { "error": "Bad request", "message": "Missing confirm=true parameter" }
```

### Implementáció (Minimal API .NET 8)

```csharp
app.MapDelete("/internal/orders/by-tenant/{tenantId}",
    async (string tenantId, bool? confirm, IOrderRepository repo,
           IConfiguration config, IAuditLogger audit) =>
{
    if (confirm != true)
        return Results.BadRequest(new { error = "Missing confirm=true" });

    var allowlist = (config["TEST_TENANT_ALLOWLIST"] ?? "").Split(',').Select(s => s.Trim());
    if (!allowlist.Contains(tenantId))
    {
        audit.Log("TestResetRejected", tenantId);
        return Results.Forbid();
    }

    if (!Guid.TryParse(tenantId, out var tenantGuid))
        return Results.BadRequest(new { error = "Invalid tenantId" });

    var counts = await repo.DeleteAllByTenantAsync(tenantGuid);
    return Results.Ok(new { tenantId, deletedCounts = counts });
})
.RequireHeader("X-SpaceOS-Internal")
.WithTags("Internal");
```

### Environment variable

```bash
# /etc/spaceos/joinery.env
TEST_TENANT_ALLOWLIST=<TEST_TENANT_UUID>   # INFRA-TEST-03 veszi fel
```

## Tesztek (kötelező)

- Sikeres törlés → 200 + deletedCounts
- `confirm=true` hiánya → 400
- Tenant nem allowlistban → 403
- `X-SpaceOS-Internal` hiánya → 403
- **≥4 új teszt**

## DoD

- [ ] `dotnet build` → 0 error
- [ ] `dotnet test` → ≥4 új teszt zöld
- [ ] Allowlist + confirm + internal header védelme
- [ ] git commit + push (develop branch)

## Outbox

DONE: `mailbox/joinery/outbox/2026-04-16_010_internal-delete-by-tenant-done.md`

## Skillек & Agentек

- `/senior-backend` — Minimal API, EF Core delete, allowlist pattern (mint KERNEL-085)
- `/senior-security` — defense in depth, belső endpoint védelme
- Agent: `se-security-reviewer` — allowlist + audit log ellenőrzés
- Sub-agenteket nyugodtan indíts
