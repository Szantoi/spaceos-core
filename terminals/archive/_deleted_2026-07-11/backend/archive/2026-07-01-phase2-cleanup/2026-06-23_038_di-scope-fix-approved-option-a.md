---
id: MSG-BACKEND-038
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-BACKEND-040-BLOCKED
created: 2026-06-23
content_hash: 04cb28c460ed0cbb9fad112811068d28e4414716075a6f7f2165d56a69ee0436
---

# APPROVED: Option A - Custom WebApplicationFactory

## Döntés

✅ **Option A jóváhagyva** - Custom WebApplicationFactory implementáció.

## Indoklás

1. **Production kód quality magas** - 954/966 teszt passed (98.8%)
2. **Integration coverage kritikus** - QuoteRequest endpoint core feature, automated testing szükséges
3. **Gyors megoldás** - 1-2h vs Option B (2-3h refactor)
4. **Low risk** - test infrastructure fix, production kód érintetlen
5. **Technical debt manageable** - később refaktorálható ha Option B indokolt lesz

## Feladat

Implementáld a Custom WebApplicationFactory-t a QuoteRequestEndpointTests számára:

### Implementáció lépései

1. **Új osztály létrehozása:**
   ```
   tests/SpaceOS.Modules.Cutting.Tests.Integration/Fixtures/CuttingWebApplicationFactory.cs
   ```

2. **WebApplicationFactory override:**
   - Override `ConfigureWebHost` method
   - Mock/replace problémás service-ek:
     - `ITenantResolver` → Mock (fix tenant ID visszaadása)
     - `IDbContextFactory<CuttingDbContext>` → In-memory DbContext
   - IsolationLevel: integration tesztek clean DB-vel indulnak

3. **QuoteRequestEndpointTests refactor:**
   - `IClassFixture<WebApplicationFactory<Program>>` → `IClassFixture<CuttingWebApplicationFactory>`
   - Minden teszt használja az új fixture-t

4. **Teszt futtatás:**
   ```bash
   dotnet test --filter "FullyQualifiedName~QuoteRequestEndpointTests"
   ```
   - Target: 12/12 teszt PASSED

### Acceptance Criteria

✅ All QuoteRequestEndpointTests PASSED (12/12)
✅ Existing 954 tesztek továbbra is PASSED
✅ Build 0 errors
✅ DI scope validation error eliminated

### Reference

**Hasonló minta:**
- `tests/SpaceOS.Kernel.Tests.Integration/Fixtures/` (ha van custom factory)
- ASP.NET Core docs: Custom WebApplicationFactory pattern

**Original error:**
```
System.InvalidOperationException: Cannot consume scoped service
'Microsoft.EntityFrameworkCore.DbContextOptions`1[...]'
from singleton 'Microsoft.EntityFrameworkCore.IDbContextFactory`1[...]'
```

## Next steps

1. Implementáld a CuttingWebApplicationFactory-t
2. Futtasd a QuoteRequestEndpointTests-t
3. Ha 12/12 PASSED → mark MSG-BACKEND-030 (Track A) DONE
4. DONE outbox-ba jelentés

## Estimated effort

⏱️ 1-2 óra

## Priority rationale

**HIGH** - Track A implementation production-ready, de automated test coverage hiányzik.
Ez blocker a MSG-BACKEND-030 DONE státuszához.

---

**Conductor**
2026-06-23
