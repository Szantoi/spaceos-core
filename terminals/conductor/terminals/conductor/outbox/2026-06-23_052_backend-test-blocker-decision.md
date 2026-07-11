---
id: MSG-CONDUCTOR-052
from: conductor
to: backend
type: task
priority: high
status: READ
ref: MSG-BACKEND-040-BLOCKED
created: 2026-06-23
---

# Backend Test Blocker - Conductor Decision: Option A

## Döntés

✅ **APPROVE: Option A - Custom WebApplicationFactory**

## Indoklás

1. **Test coverage kritikus** - 12 QuoteRequest integration teszt lefedés fontos
2. **Clean megoldás** - Production kód nem módosul
3. **Effort-value arány optimális** - 1-2h befektetés vs. teljes test suite coverage
4. **Option C (skip) elfogadhatatlan** - automatizált tesztek nélkül deployment kockázatos

## Feladat

**MSG-BACKEND-041: Custom WebApplicationFactory implementáció**

**Scope:**
- Készíts `CuttingWebApplicationFactory<TProgram>` osztályt
- Override `ConfigureWebHost` method
- Mock/isolate problémás service-ek:
  - TenantResolver → in-memory mock
  - IDbContextFactory → in-memory DB (no interceptors)
- QuoteRequestEndpointTests használja az új factory-t
- 966/966 teszt PASS required

**Acceptance criteria:**
- [ ] CuttingWebApplicationFactory.cs created
- [ ] QuoteRequestEndpointTests updated (use custom factory)
- [ ] dotnet test → 966/966 PASSED
- [ ] Build 0 errors, 0 warnings (suppress irrelevant warnings OK)

**Becsült effort:** 1-2 óra

**Priority:** HIGH (blokkol Track A deployment-et)

## Referencia

- Original blocker: MSG-BACKEND-040-BLOCKED
- Production kód READY, csak tesztek failelnek
- 954/966 teszt már zöld, csak QuoteRequest integration tesztek hibásak

---

**Conductor**
2026-06-23
