---
id: MSG-KERNEL-082
from: root
to: kernel
type: task
priority: critical
status: READ
ref: MSG-E2E-037-BLOCKED
created: 2026-04-16
---

# MSG-KERNEL-082 — Facility POST 201 → GET 404 diagnosztika

## Háttér

Az E2E-037 és E2E-036 blocker: az admin (`test-admin`) létrehoz egy facilityt — `POST /bff/api/tenants/${adminTenantId}/facilities` → 201, facility ID visszajön. De utána `GET /bff/api/facilities/${id}` → 404.

**Kizárva:**
- `test-admin` JWT `tid` claim helyes: `a1b2c3d4-e5f6-7890-abcd-ef1234567890` = Doorstar Kft. UUID ✅ (INFRA-106 DONE)
- Keycloak URL helyes: `.env` → `KC_URL=http://localhost:8080/auth` → full URL `http://localhost:8080/auth/realms/spaceos/...` ✅
- `CreateFacilityCommandHandler` a `request.TenantId` URL-paramétert használja, **nem** `ITenantResolver` — ezt root megerősítette kódolvasással

**Jelenlegi hipotézis (root részéről):**

Az EF global query filter:
```csharp
HasQueryFilter(f => !f.IsArchived && (CurrentTenantGuid == null || f.TenantId == CurrentTenantGuid));
```
ahol `CurrentTenantGuid => _tenantResolver.TryResolve()?.Value`.

A `test-admin` JWT `tid`-val → `TryResolve()` visszaad DoorstarGuid → filter: `f.TenantId == DoorstarGuid`.
A facility mentve `TenantId = DoorstarGuid` (URL-ből) — tehát kellene egyezni.

**Valami mégis meggátolja.** Diagnosztika szükséges.

## Feladat — Vizsgáld meg ezeket

### 1. PostgreSQL native RLS ellenőrzés

Van-e PostgreSQL native RLS policy a `Facilities` táblán?

```sql
SELECT tablename, policyname, permissive, roles, cmd, qual
FROM pg_policies
WHERE tablename ILIKE 'facilities';

-- Vagy rövidebben:
\d+ spaceos."Facilities"
```

Ha van PG RLS policy, akkor a `TenantSessionInterceptor` által beállított `app.current_tenant_id` PG session variable számít. Ellenőrizd, hogy `test-admin` kéréskor mi kerül beállításra.

### 2. `Facility.Create()` factory method

Nézd meg: `SpaceOS.Kernel.Domain/Facilities/Facility.cs`

Hogyan állítja be a `TenantId`-t? Van-e bármilyen logika, amely felülírhatja a paraméterként kapott `tenantId`-t?

### 3. EF SaveChanges interceptor — TenantId override?

Van-e olyan `DbCommandInterceptor` vagy `SaveChangesInterceptor`, amely entity creation során a `TenantId` mezőt automatikusan felülírja (pl. `ITenantResolver`-ből)?

Különösen nézd meg:
- `SpaceOS.Infrastructure/Data/Interceptors/`
- `TenantSessionInterceptor.cs` — csak PG session variable-t állít, vagy entity property-t is?

### 4. `GetFacilityByIdQuery` handler

```
SpaceOS.Kernel.Application/Facilities/Queries/GetFacilityByIdQueryHandler.cs
```

A query handler csak az EF global query filter-re támaszkodik (implicit RLS), vagy van extra `WHERE TenantId = ...` feltétel is? Ha igen, mi az a tenantId forrása?

### 5. DB ellenőrzés — mi kerül ténylegesen mentésre?

A legegyszerűbb debug: facility létrehozás után direktben:

```sql
SELECT id, "TenantId", name FROM spaceos."Facilities" ORDER BY id DESC LIMIT 3;
```

Ha `TenantId` = `00000000-0000-0000-0000-000000000002` (DenyWebRequestSentinel) → SaveChanges interceptor felülírja.
Ha `TenantId` = DoorstarGuid → a filter oldalon kell keresni.
Ha `TenantId` = `00000000-0000-0000-0000-000000000000` → `Guid.Empty` → `Facility.Create()` nem kapja meg a tenantId-t.

## Elvárt DoD

- [ ] Root cause azonosítva: miért 404 a GET, ha a POST 201-et ad vissza?
- [ ] DB-szintű ellenőrzés elvégezve (SELECT TenantId a fresh facility után)
- [ ] Ha kódhiba: javítás + build + 1110 teszt zöld + commit
- [ ] Ha konfig/infra hiba: leírás, mit kell változtatni
- [ ] Outbox: `MSG-KERNEL-082-DONE` (vagy BLOCKED, ha emberi döntés kell)

## Megjegyzés

Ha a DB-ben `TenantId = DoorstarGuid` van (mentés helyes), de GET mégis 404 → a filter vagy a query handler oldala a hiba → ott kell keresni.

Ha a DB-ben `TenantId ≠ DoorstarGuid` → a create oldala a hiba → SaveChanges interceptor vagy `Facility.Create()` factory.
