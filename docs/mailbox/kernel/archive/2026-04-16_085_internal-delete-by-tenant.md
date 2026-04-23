---
id: MSG-KERNEL-085
from: root
to: kernel
type: task
priority: high
status: READ
ref: SpaceOS_Doorstar_Portal_UI_Test_Strategy_v4
created: 2026-04-16
---

# KERNEL-085 — BE-TEST-02: `DELETE /internal/flow-epics/by-tenant/{tenantId}`

## Kontextus

Ref: `docs/tasks/new/SpaceOS_Doorstar_Portal_UI_Test_Strategy_v4.md` §6.4 + §6.5

A Test BFF reset endpoint (Orchestrator) a test tenant összes adatát törli reset előtt.
A Kernel feladata: FlowEpic + Snapshot + AuditEvent törlés egy adott tenanthoz.

**Security kényszer (SEC-TS-01):**
- Csak `X-SpaceOS-Internal` header-rel hívható (meglévő SEC-01 mechanizmus)
- **Kernel-szintű tenant allowlist ellenőrzés** (defense in depth): `TEST_TENANT_ALLOWLIST` env var
- Ha a tenantId nincs a listán → 403 + audit log
- Ez az endpoint véd az Orchestrator esetleges kompromittálódása ellen is

## Implementálandó

### Endpoint

```
DELETE /internal/flow-epics/by-tenant/{tenantId}

Headers:
  X-SpaceOS-Internal: true       ← meglévő mechanizmus, kötelező
  Content-Type: application/json

Query:
  ?confirm=true                  ← kötelező safety param (véletlenszerű hívás ellen)

Response 200:
{
  "tenantId": "uuid",
  "deletedCounts": {
    "flowEpics": 3,
    "snapshots": 6,
    "auditEvents": 42
  }
}

Response 403:
{ "error": "Forbidden", "message": "Tenant not in test allowlist" }

Response 400:
{ "error": "Bad request", "message": "Missing confirm=true parameter" }
```

### Implementáció

```csharp
// Endpoint regisztráció (Minimal API)
app.MapDelete("/internal/flow-epics/by-tenant/{tenantId}", 
    async (string tenantId, bool? confirm, IFlowEpicRepository repo, 
           IConfiguration config, IAuditService audit) =>
{
    // 1. confirm param ellenőrzés
    if (confirm != true)
        return Results.BadRequest(new { error = "Bad request", message = "Missing confirm=true parameter" });

    // 2. Tenant allowlist ellenőrzés
    var allowlist = (config["TEST_TENANT_ALLOWLIST"] ?? "").Split(',').Select(s => s.Trim());
    if (!allowlist.Contains(tenantId))
    {
        await audit.LogAsync("TestResetRejected", tenantId, "Tenant not in allowlist");
        return Results.Forbid();
    }

    // 3. Tenant GUID parse
    if (!Guid.TryParse(tenantId, out var tenantGuid))
        return Results.BadRequest(new { error = "Invalid tenantId format" });

    // 4. Törlés (cascade)
    var counts = await repo.DeleteAllByTenantAsync(tenantGuid);

    return Results.Ok(new { tenantId, deletedCounts = counts });
})
.RequireHeader("X-SpaceOS-Internal")   // meglévő middleware
.WithTags("Internal");
```

### Repository extension

```csharp
// IFlowEpicRepository
Task<DeletedCounts> DeleteAllByTenantAsync(Guid tenantId);

// FlowEpicRepository implementáció:
// DELETE FROM "FlowEpics" WHERE "TenantId" = @tenantId CASCADE
// A CASCADE törli a Snapshot-okat és AuditEvent-eket is (ha FK van)
// Ha nincs CASCADE: explicit sorrendben töröl
```

### Environment variable

```bash
# /etc/spaceos/kernel.env (VPS-en, INFRA-TEST-03 veszi fel)
TEST_TENANT_ALLOWLIST=<TEST_TENANT_UUID>
```

## Tesztek (kötelező)

- `DELETE /internal/flow-epics/by-tenant/{tenantId}` — sikeres törlés, deletedCounts visszakapott
- Hiányzó `confirm=true` → 400
- Tenant nem a listán → 403
- Érvénytelen GUID → 400
- `X-SpaceOS-Internal` header hiánya → 403 (meglévő mechanizmus tesztje)
- **≥5 új teszt**

## DoD

- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `dotnet test` → mind zöld, ≥5 új teszt
- [ ] Endpoint csak `X-SpaceOS-Internal`-lel hívható
- [ ] TEST_TENANT_ALLOWLIST env var-ral véd prod tenantok ellen
- [ ] `confirm=true` param kötelező
- [ ] git commit + push (develop branch)

## Outbox

DONE: `mailbox/kernel/outbox/2026-04-16_085_internal-delete-by-tenant-done.md`

## Skillек & Agentек

- `/senior-backend` — Minimal API delete endpoint, EF Core cascade delete
- `/senior-security` — defense in depth, allowlist pattern, audit log
- `/javascript-typescript-jest` — .NET integration tesztek a delete endpoint-ra
- Agent: `se-security-reviewer` — internal endpoint biztonsági audit
- Sub-agenteket nyugodtan indíts párhuzamosan
