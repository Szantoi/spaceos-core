---
id: MSG-KERNEL-081
from: root
to: kernel
type: task
priority: high
status: READ
created: 2026-04-16
---

# MSG-KERNEL-081 — RLS cross-tenant isolation fix

## Probléma

A `38-cross-tenant-isolation` E2E teszt probe-ja `rlsEnforced = false`-t ad vissza:

```
GET /bff/api/facilities/{facilityA_id} → 200  (elvárt: 404)
```

Ez azt jelenti, hogy egy Doorstar admin token-nel hozzáférhető egy másik tenant facilitye.

## Root cause (diagnosztizált)

```
ClaimsTenantResolver.TryResolve()
  → HttpContext van (web request)
  → de tid claim hiányzik a JWT-ből (admin user nincs Keycloak-ban tenant-hez kötve)
  → return null

AppDbContext.HasQueryFilter(Facility):
  .HasQueryFilter(f => !f.IsArchived && (CurrentTenantGuid == null || f.TenantId == CurrentTenantGuid))
                                          ↑ null == null → true → bypass → minden tenant látható
```

**A null-bypass eredetileg background job / migration kontextushoz lett tervezve** (amikor nincs HttpContext). De web requestben sincs `tid` → ugyanaz a null-bypass aktiválódik → security gap.

## Feladat

### 1. Fix: HttpContext + no tid = üres eredmény, NE bypass

A `ClaimsTenantResolver` vagy az `AppDbContext` különböztesse meg:
- `HttpContext == null` → háttérfolyamat → bypass OK (meglévő viselkedés megmarad)
- `HttpContext != null` AND `tid` hiányzik → web request, ismeretlen tenant → **üres eredmény** (NE bypass)

**Javasolt fix (AppDbContext szintén megoldható):**

Az `AppDbContext`-ben vezess be egy `IsWebRequestWithoutTenant` property-t:

```csharp
// true = HTTP request van, de tid nincs → ne bypass
private bool IsWebRequestWithoutTenant =>
    _accessor.HttpContext is not null && _tenantResolver.TryResolve() is null;

// false = háttérfolyamat (nincs HTTP context) → bypass OK
private Guid? CurrentTenantGuid => _tenantResolver.TryResolve()?.Value;
```

Filter logika minden tenant-szűrt entitáson:
```csharp
.HasQueryFilter(f =>
    !f.IsArchived &&
    !IsWebRequestWithoutTenant &&                     // web request tid nélkül → üres
    (CurrentTenantGuid == null ||                     // háttér (no HTTP ctx) → bypass
     f.TenantId == CurrentTenantGuid));               // normál web request → szűr
```

> Alternatív: `ClaimsTenantResolver`-ben `HttpContext != null && tid missing → return TenantId.From(Guid.Parse("00000000-0000-0000-0000-fffffffffffe"))` (dummy, semmihez nem illeszkedik). Válaszd amelyik tisztább.

### 2. Érintett entitások (mind frissítendő AppDbContext-ben)

Az összes jelenleg `CurrentTenantGuid == null || ...` mintát használó filter:
- `Facility`, `WorkStation`, `SpaceLayer`, `FlowEpic`
- `SpaceElement`, `BlockNode`, `StageList`, `StageDefinition`, `StageChain`, `StageHandoff`
- (ld. AppDbContext.cs sorokat ahol `CurrentTenantGuid == null`)

### 3. Tesztek

- Meglévő 1110 unit/integration teszt mind zöld marad
- Különösen: `GetFacilityByIdQueryHandlerTests.cs` — ellenőrizd, hogy null TenantId eset le van fedve
- Új unit teszt (ha szükséges): `ClaimsTenantResolver` — HttpContext present, no tid → verify behavior

## DoD

- [ ] `dotnet build` → 0 error, 0 warning
- [ ] `dotnet test` → min. 1110/1110 pass
- [ ] `IsWebRequestWithoutTenant` (vagy ekvivalens) implementálva
- [ ] Összes `CurrentTenantGuid == null` filter frissítve
- [ ] Outbox: `MSG-KERNEL-081-DONE`

## Megjegyzés

A `test-admin` Keycloak user `tid` claim-je egy párhuzamos INFRA vizsgálat tárgya — de a Kernel fix attól független. A fix után egy `tid`-et nem tartalmazó JWT is üres eredményt ad (nem 500, nem bypass), ami a helyes viselkedés.
