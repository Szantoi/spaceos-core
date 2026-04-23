---
id: MSG-KERNEL-092
from: root
to: kernel
type: task
priority: medium
status: READ
created: 2026-04-18
---

# BUG-008b — Audit verify chain 403 "Admin role required."

## Szimptóma (TESTER-015, 2026-04-18 14:00)

| Kérés | Válasz |
|---|---|
| `GET /bff/audit-events/verify-chain` | ❌ `403 {"error":"Admin role required."}` |

**UI:** "Verification failed. Try again." hibaüzenet a /audit oldalon.

**Fontos:** test-admin felhasználónak JWT-ben van `Admin` role:
```json
"realm_access": {
  "roles": ["default-roles-spaceos", "offline_access", "uma_authorization", "Admin"]
}
```

## Vizsgálandó

### 1. Melyik claim-et ellenőrzi a Kernel?

Az endpoint valószínűleg `[Authorize(Roles = "Admin")]` attribútumot használ. Az ASP.NET Core alapértelmezetten a `http://schemas.microsoft.com/ws/2008/06/identity/claims/role` claim-t keresi.

Mivel a Kernel-ben `MapInboundClaims = false` van beállítva (KERNEL-082 fix), ellenőrizd:
- A role claim neve a JWT-ben: `realm_access.roles` vs flat `roles` claim
- A KC Script Mapper flat `roles` claim-et tesz-e a tokenbe, vagy csak nested `realm_access`-be

```bash
# Token decode ellenőrzés (JWT payload base64):
echo $TOKEN | cut -d'.' -f2 | base64 -d 2>/dev/null | python3 -m json.tool | grep -A5 "role"
```

### 2. Valószínű root cause

A KC Script Mapper valószínűleg csak a `realm_access.roles` tömbbe tesz szerepköröket, de NEM hoz létre flat `roles` claim-et. Az ASP.NET `[Authorize(Roles = "Admin")]` a flat role-t keresi.

**Fix: add flat roles claim-t a KC Script Mapper-be, VAGY módosítsd a Kernel-t `ClaimTypes.Role` kiolvasásra a `realm_access.roles`-ból.**

A kódban valószínűleg:
```csharp
// AuditEndpoints.cs vagy AuditChainController.cs
.RequireAuthorization(policy => policy.RequireRole("Admin"))
// VAGY
[Authorize(Roles = "Admin")]
```

### 3. Javasolt fix — Kernel oldalon

A `Program.cs` JWT options-ban add hozzá a role claim mapping-et:

```csharp
options.TokenValidationParameters = new TokenValidationParameters
{
    // ... meglévő config ...
    RoleClaimType = "realm_access.roles"  // NEM jó, nested JSON
};
```

**Vagy** a `ClaimsTransformation`-ban flatten-el:
```csharp
// Regisztrálj egy IClaimsTransformation implementációt ami
// "realm_access" claim JSON-ból kinyeri a roles tömböt
// és hozzáadja ClaimTypes.Role-ként
```

**Vagy** (legegyszerűbb): add hozzá a `realm_access` JSON-ból Role claim-eket az `AddJwtBearer` `OnTokenValidated` event-ben — a Kernel-ben már van ilyen eseménykezelő precedense.

## DoD

- [ ] `GET /bff/audit-events/verify-chain` test-admin tokennel → **200** (nem 403)
- [ ] `dotnet test` → legalább 1138 zöld
- [ ] Ha KC Script Mapper változás kell → jelezd INFRA-nak

---

*Skill: `/spaceos-terminal`*
