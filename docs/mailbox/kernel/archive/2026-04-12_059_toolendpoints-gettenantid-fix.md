---
id: MSG-KERNEL-059
from: root
to: kernel
type: task
priority: high
status: READ
ref: MSG-E2E-001-DONE-v3
created: 2026-04-12
---

# MSG-KERNEL-059 — ToolEndpoints.GetTenantId() spaceos_tenants parse fix

## Kontextus

Az E2E diagnosztika (MSG-E2E-001-DONE v3) azonosította: a `24-tenant-summary` 4 tesztje azért bukik, mert a `ToolEndpoints.cs GetTenantId()` metódus nem kezeli helyesen a `spaceos_tenants` claim ASP.NET általi feldolgozását.

## Root cause

Az ASP.NET `JsonWebTokenHandler` a JSON tömb értéket (`spaceos_tenants: [{...}]`) **egyedi Claim objektumokra bontja** — minden tömbelem külön `Claim` lesz azonos névvel. Tehát:

```
user.FindFirst("spaceos_tenants")?.Value
→ "{\"tenant_id\":\"a1b2c3d4-...\",...}"  ← egyetlen objektum JSON, NEM tömb
```

A jelenlegi kód `StartsWith('[')` ágat ellenőriz → ez **false** → JSON string deserialization → elszáll → `Guid.Empty` → 401.

## Fix — `ToolEndpoints.cs`

```csharp
// RÉGI (hibás):
var spaceosTenantsRaw = user.FindFirst("spaceos_tenants")?.Value;
// ... StartsWith('[') branch → nem működik

// ÚJ:
private static Guid GetTenantId(ClaimsPrincipal user)
{
    // ASP.NET a tömb elemeket külön Claim-ekre bontja azonos névvel
    var tenantClaims = user.FindAll("spaceos_tenants");
    foreach (var claim in tenantClaims)
    {
        try
        {
            using var doc = JsonDocument.Parse(claim.Value);
            if (doc.RootElement.TryGetProperty("tenant_id", out var idEl))
                if (Guid.TryParse(idEl.GetString(), out var g) && g != Guid.Empty)
                    return g;
        }
        catch { /* continue to next claim */ }
    }

    // Legacy fallback: flat tid / tenant_id claim
    var tid = user.FindFirst("tid")?.Value ?? user.FindFirst("tenant_id")?.Value;
    return Guid.TryParse(tid, out var legacy) ? legacy : Guid.Empty;
}
```

## Definition of Done

- [ ] `ToolEndpoints.cs GetTenantId()` javítva (`FindAll` pattern)
- [ ] Unit teszt: `spaceos_tenants` claim tömb-ként érkezik → helyes GUID visszaadva
- [ ] `GET /bff/api/tools/summary` → 200 (nem 401) érvényes tokennel
- [ ] Meglévő 1068 teszt zöld · 0 build warning

## Visszajelzés

Outboxba: `MSG-KERNEL-059-DONE`
