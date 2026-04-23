---
id: MSG-CUTTING-007
from: root
to: cutting
type: task
priority: medium
status: READ
ref: MSG-INFRA-110-BLOCKED
created: 2026-04-16
---

# MSG-CUTTING-007 — GetTenantId claim fix: "tenant_id" → "tid"

## Gyökérok

A cutting / inventory / procurement service-ek `GetTenantId()` helper-je a
`"tenant_id"` claim nevet keresi:

```csharp
// CuttingEndpoints.cs (és InventoryEndpoints.cs, ProcurementEndpoints.cs)
private static Guid GetTenantId(HttpContext ctx)
{
    var claim = ctx.User?.FindFirst("tenant_id")?.Value;
    return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
}
```

A SpaceOS JWT azonban **`"tid"`** claimet tartalmaz (nem `"tenant_id"`).
Ezért `tenantId == Guid.Empty` → `Results.Unauthorized()` → **401** minden autentikált kérésnél.

Referencia: a Kernel `ClaimsTenantResolver.cs` ugyanígy `"tid"`-t keres.

## Fix — 3 fájl, 1-1 sor

```csharp
// ELŐTTE:
var claim = ctx.User?.FindFirst("tenant_id")?.Value;

// UTÁNA:
var claim = ctx.User?.FindFirst("tid")?.Value;
```

Érintett fájlok:
- `src/SpaceOS.Modules.Cutting.Api/Endpoints/CuttingEndpoints.cs`
- `src/SpaceOS.Modules.Inventory.Api/Endpoints/InventoryEndpoints.cs`
- `src/SpaceOS.Modules.Procurement.Api/Endpoints/ProcurementEndpoints.cs`

Ezen felül add hozzá a JWT config-hoz (cutting.env már tartalmazza, de a kódban is jobb):
A `AddJwtBearer()` hívást bővítsd:
```csharp
builder.Services.AddAuthentication().AddJwtBearer(opts =>
{
    opts.MapInboundClaims = false;
});
```

Ezzel a `"tid"` claim nevű marad (nem kerül átnevezésre MS URI-ra).

## Tesztek

- Build: 0 error, 0 warning
- Minden meglévő teszt zöld
- Manuális ellenőrzés: `POST /api/cutting/sheets` valid JWT-vel → **nem 401**

## DoD

- [ ] `GetTenantId` mindhárom fájlban javítva (`"tenant_id"` → `"tid"`)
- [ ] `MapInboundClaims = false` beállítva (opcionális de ajánlott)
- [ ] Build zöld
- [ ] Tesztek zöldek
- [ ] Commit + outbox: `MSG-CUTTING-007-DONE`
