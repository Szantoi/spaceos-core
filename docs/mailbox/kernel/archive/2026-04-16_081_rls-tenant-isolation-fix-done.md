---
id: MSG-KERNEL-081-DONE
from: kernel
to: root
type: done
priority: high
status: READ
ref: MSG-KERNEL-081
created: 2026-04-16
---

# MSG-KERNEL-081 DONE — RLS cross-tenant isolation fix

## Összefoglaló

A `ClaimsTenantResolver.TryResolve()` viselkedése megváltozott web request kontextusban:

| Helyzet | Régi viselkedés | Új viselkedés |
|---|---|---|
| `HttpContext == null` (háttérfolyamat) | `null` → bypass | `null` → bypass ✅ változatlan |
| `HttpContext != null`, `tid` hiányzik | `null` → bypass (security gap!) | `DenyWebRequestSentinel` → üres eredmény ✅ |
| `HttpContext != null`, `tid` érvényes | `TenantId(guid)` → szűr | `TenantId(guid)` → szűr ✅ változatlan |

### Implementáció

**`ClaimsTenantResolver.cs`** (egyetlen érintett produkciós fájl):
- Új `internal static readonly TenantId DenyWebRequestSentinel = TenantId.From(Guid.Parse("00000000-0000-0000-0000-000000000002"))`
- `TryResolve()`: ha `context != null` AND (claim hiányzik / malformed / empty GUID) → sentinel (nem null)
- Háttér (`context == null`) → null (bypass, változatlan)

**`AppDbContext.cs`**: XML doc frissítve — leírja a háromféle resolver-visszatérési értéket.

**Nincs `AppDbContext.HasQueryFilter` változás** — a logika helyes marad:
- `CurrentTenantGuid == null` → bypass (háttér)
- `CurrentTenantGuid == DenySentinel` → filter mindig false → üres → deny
- `CurrentTenantGuid == realTenantGuid` → tenant-szűrés → helyes

### Commit

`a9d3803` — `fix(auth): close RLS cross-tenant bypass for web requests without tid claim`

## Tesztek

```
Passed! — Failed: 0, Passed: 910, Skipped: 0, Total: 910  (SpaceOS.Kernel.Tests)
Passed! — Failed: 0, Passed: 107, Skipped: 0, Total: 107  (IntegrationTests)
Passed! — Failed: 0, Passed:  93, Skipped: 4, Total:  97  (Api.Tests)
```

Frissített tesztek:
- `ClaimsTenantResolverTests.cs` — 3 "ReturnsNull" teszt → "ReturnsDenySentinel"
- `HttpTenantResolverTests.cs` — 2 "Returns_Null" teszt → "Returns_DenySentinel"

## Security review

- ✅ Nincs bypassolható path web requestben tid nélkül
- ✅ Háttérfolyamatok (migration, job) nem érintve — null-bypass megmaradt
- ✅ `DenyWebRequestSentinel` GUID nem ütközhet valódi tenant ID-val (nem empty, nem 000...001)
- ✅ Nincs secret a logban
- ✅ `AppDbContext.HasQueryFilter` összes 13 entitása helyes marad (logika nem változott)

## E2E hatás

A `38-cross-tenant-isolation` teszt `rlsEnforced = probe.status === 404` probe-ja
a fix után 404-et vár. Ez **akkor lesz igaz, ha a `test-admin` Keycloak user `tid` claim-je
hiányzik** (amit a fix most deny-ra értékel).

⚠️ **Ha a `test-admin` usernek valóban nincs `tid` claim-je Keycloak-ban**, akkor az E2E
38-as tesztnél az admin token-ek esetén az összes facility endpoint üres listát/404-et ad vissza.
Ez helyes viselkedés (tenant izoláció), DE az E2E setup-ban seed facilty-k is érintve lehetnek.

Javasolt ellenőrzés: E2E rerun után nézzük meg, nem törtek-e más tesztek (pl. facility CRUD-ot
használó tesztek amelyek admin token-t és seed facility-t kombinálnak).

## Kockázatok / kérdések

Az E2E `ClaimsStubTenantResolver` (`SpaceOsApiFactory`) visszaad `TestTenantId`-t amikor nincs
`tid` → az API integration tesztek helyes maradtak. A valódi E2E (`npm test`) a VPS Keycloak
token-t használja — ha a `test-admin` nem kap `tid`-t a Keycloak mappernél, a 38-as teszt
aktiválódik (RLS enforced = true), de más facility-alapú tesztek is érintve lehetnek.

**Javasolt következő lépés:** E2E-035 rerun az INFRA-103 restart után.
