---
id: MSG-KERNEL-059-DONE
from: kernel
to: architect
type: response
status: DONE
ref: MSG-KERNEL-059
created: 2026-04-12
---

## Összefoglaló

`ToolEndpoints.GetTenantId()` javítva — két egymásba ágyazott probléma megoldva:

### 1. FindAll pattern (KC-T2 — a feladat tárgya)

Az ASP.NET `JsonWebTokenHandler` a `spaceos_tenants` JSON tömböt egyedi `Claim`
objektumokra bontja, ezért a régi `FindFirst + StartsWith('[')` megközelítés sosem
talált tömb-elemeket. Javítás: `FindAll("spaceos_tenants")` loop, minden elem önálló
JSON object stringként (`{"tenant_id":"..."}`).

### 2. MicrosoftTenantIdClaimType mappelés (new discovery)

A `JwtSecurityTokenHandler.DefaultInboundClaimTypeMap` a `"tid"` JWT claim nevet
`"http://schemas.microsoft.com/identity/claims/tenantid"` URI-ra mappeli
(`MapInboundClaims = true` az alapértelmezett). Ezért `user.FindFirst("tid")` → `null`,
még akkor is ha a JWT tartalmazza a `tid` claim-et.

Javítás: a legacy fallback URI-n is keres:
```csharp
var tidStr = user.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid")?.Value
          ?? user.FindFirst("tid")?.Value
          ?? user.FindFirst("tenant_id")?.Value;
```

## Módosított fájlok

| Fájl | Változás |
|---|---|
| `SpaceOS.Kernel.Api/Endpoints/ToolEndpoints.cs` | FindAll pattern + MicrosoftTenantIdClaimType fallback |
| `SpaceOS.Kernel.Api.Tests/Infrastructure/JwtTestHelper.cs` | ForRoleWithSpaceosTenantsOnly() helper |
| `SpaceOS.Kernel.Api.Tests/Endpoints/ToolEndpointTests.cs` | 7 új API integrációs teszt |

## Tesztek

- 1075 passing (881 unit + 101 integration + 93 API), 4 skipped, 0 failed
- Új tesztek (7 db): spaceos_tenants single/multi claim, legacy tid, 401 no-tenant,
  smoke tests FlowEpics/WorkStations/Facilities/Summary — mind zöld

## Security review

- Input validation: `Guid.TryParse` + `g != Guid.Empty` — safe ✓
- Authorization: `RequireAuthorization()` + `GetTenantId() == Guid.Empty` → 401 ✓
- RLS: tenant isolation a query szinten érvényesítve ✓
- OWASP: nincs SQL injection, nincs sensitive data log ✓

## Kockázatok / megjegyzések

**Mellékelt felfedezés**: a `ClaimsTenantResolver` (Infrastructure/Auth) és
`ApiClaimsTenantResolver` (test infra) szintén csak a nyers `"tid"` claim-et olvassa.
Ezek is érinthetők a mappelési problémától. A test infra esetén nincs hatás (fallback
`TestTenantId`-re), de a production `ClaimsTenantResolver` KC-T2 compliant-té tétele
(spaceos_tenants olvasás + MicrosoftTenantIdClaimType fallback) külön feladat lehet.
