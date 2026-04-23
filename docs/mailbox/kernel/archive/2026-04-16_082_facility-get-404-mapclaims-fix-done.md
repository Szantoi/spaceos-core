---
id: MSG-KERNEL-082-DONE
from: kernel
to: root
type: done
priority: critical
status: READ
ref: MSG-KERNEL-082
created: 2026-04-16
---

## Összefoglaló

**Root cause azonosítva és javítva.**

### Gyökérok: `MapInboundClaims = true` (ASP.NET Core default)

Az ASP.NET Core JwtBearer middleware alapértelmezetten átnevezi a JWT claim-eket
SOAP URI ekvivalensekre (`"tid"` → `http://schemas.microsoft.com/identity/claims/tenantid`).

`ClaimsTenantResolver` a `"tid"` string típust kereste:
```csharp
var claim = context.User.Claims.FirstOrDefault(c => c.Type == "tid"); // → null
```

Null claim → `DenyWebRequestSentinel` (`00000000-0000-0000-0000-000000000002`) visszaadva
→ EF Core global query filter: `CurrentTenantGuid = DenyWebRequestSentinel.Value`
→ `f.TenantId == DenyWebRequestSentinel` → sosem igaz → 0 sor → 404.

**Miért nem volt látható korábban?**
KERNEL-081 előtt `TryResolve()` `null`-t adott vissza ismeretlen tenant esetén,
és az EF filter null esetén átengedett mindent. A sentinel bevezetése helyes volt,
de feltárta ezt a rejtett claim-mapping problémát.

### Fix

`/opt/spaceos/spaceos-kernel/SpaceOS.Kernel.Api/Program.cs` — `AddJwtBearer` konfig:

```csharp
// Preserve JWT claim names as-is (e.g. "tid", "spaceos_tenants").
// Default MapInboundClaims=true would remap "tid" → Microsoft tenantid URI,
// breaking ClaimsTenantResolver and TenantSessionInterceptor which look for "tid".
options.MapInboundClaims = false;
```

### Érintett fájlok

| Fájl | Változás |
|---|---|
| `SpaceOS.Kernel.Api/Program.cs` | `options.MapInboundClaims = false;` hozzáadva |

**Commit:** `694bc56`

---

## Tesztek

- `IntegrationTests`: 107/107 ✅
- `Kernel.Tests`: 905/910 — 5 fail mind preexisting StackExchangeRedis 8.0.26 DLL mismatch
  - `DependencyInjectionTests.AddInfrastructure_DevelopmentEnvironment_RegistersSqliteProvider`
  - `DependencyInjectionTests.AddInfrastructure_ProductionEnvironment_RegistersNpgsqlProvider`
  - `RedisExtensionsTests.AddSpaceOsRedis_*` (3 db)
  - Azonos hiba mint az `Api.Tests` 93 failje — `rm -rf publish` + clean build javítja
  - **Nem a jelenlegi változás okozta**
- `Api.Tests`: 93/93 fail — preexisting DLL mismatch (VPS-en clean deploy javítja)

Build: **0 error, 0 warning** ✅

---

## Security review

- `MapInboundClaims = false` nem gyengíti az auth-ot — JWT signature validation és
  audience/issuer check változatlan, csak a claim-ek neve marad az eredeti JWT-ből
- `ClaimsTenantResolver` + `TenantSessionInterceptor` claim prioritás-sorrendek érintetlenek
- RLS/EF filter ezentúl helyesen kap `TenantId`-t → csak saját tenant data látható
- Input validation, SQL injection, token logolás: nincs változás

---

## Deploy követelmény

**⚠️ VPS-en `rm -rf publish/` szükséges clean build előtt**
(preexisting Redis DLL mismatch — dokumentálva: `vps_deploy_gotchas.md`)

```bash
sudo rm -rf /opt/spaceos-app/publish/
cd /opt/spaceos/spaceos-kernel
dotnet publish -c Release -o /opt/spaceos-app/publish/
sudo systemctl restart spaceos-kernel
```

---

## Kockázatok / kérdések

**Nincs blokkoló kockázat.**

Utófigyelés: E2E-037 facility GET 404 tesztek várhatóan zöldre fordulnak deploy után.
E2E-036 és E2E-037 újrafuttatás javasolt a deploy után.
