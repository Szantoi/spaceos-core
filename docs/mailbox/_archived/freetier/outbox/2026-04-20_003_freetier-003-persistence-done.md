---
id: MSG-FREETIER-003-DONE
from: freetier
to: root
type: done
priority: high
status: READ
ref: MSG-FREETIER-003
created: 2026-04-20
---

## Összefoglaló

Persistence réteg teljes implementáció — FREETIER-003 + v4.5 addendum.

### Megváltozott / létrehozott fájlok

**Domain**
- `src/SpaceOS.FreeTier.Domain/Entities/MagicLinkToken.cs` — ValueObjects-ből Entities-be mozgatva (sealed class, EF Core kompatibilis, `Generate()` factory returning tuple)
- `src/SpaceOS.FreeTier.Domain/ValueObjects/MagicLinkToken.cs` — eltávolítva (redirect comment)
- `src/SpaceOS.FreeTier.Application/Common/Placeholder.cs` — `IApplicationMarker` hozzáadva (MediatR assembly scan anchor)

**Infrastructure / Persistence**
- `src/SpaceOS.FreeTier.Infrastructure/Persistence/FreeTierDbContext.cs` — D-22: `SaveChangesAsync` override → MediatR domain event dispatch
- `src/SpaceOS.FreeTier.Infrastructure/Persistence/FreeTierDbContextFactory.cs` — `IDesignTimeDbContextFactory` + belső `NoOpMediator`
- `src/SpaceOS.FreeTier.Infrastructure/Persistence/DomainEventNotification.cs` — `INotification` wrapper domain eventekhez
- `src/SpaceOS.FreeTier.Infrastructure/Persistence/UserSessionInterceptor.cs` — BE-22: `DbConnectionInterceptor`, GUC `app.user_id` set/clear per-connection
- `src/SpaceOS.FreeTier.Infrastructure/Persistence/ShareDbContext.cs` — read-only context (share link resolution, nincs RLS interceptor)
- `src/SpaceOS.FreeTier.Infrastructure/Persistence/Configurations/` — 6 `IEntityTypeConfiguration<T>` fájl: `FreeTierUserConfiguration`, `WorkspaceConfiguration`, `WorkspaceRevisionConfiguration`, `ShareTokenConfiguration`, `MagicLinkTokenConfiguration`, `UpgradeRequestConfiguration`
- `src/SpaceOS.FreeTier.Infrastructure/Migrations/20260420182431_F_0001_InitialSchema.cs` — EF-generált + raw SQL bővítés: DEFERRABLE FK (D-24), RLS ENABLE/FORCE + CREATE POLICY (5 tábla), OWNER TO spaceos_schema_owner (DO-block guard), GUC ALTER DATABASE (privilege-guard), GRANT spaceos_freetier_share_reader + spaceos_app (DO-block guard)

**API**
- `src/SpaceOS.FreeTier.Api/Program.cs` — `AddMediatR`, `AddHttpContextAccessor`, `AddScoped<UserSessionInterceptor>`, `AddDbContext<FreeTierDbContext>` (interceptorral), `AddDbContextFactory<ShareDbContext>`

**Tests**
- `tests/SpaceOS.FreeTier.Integration.Tests/Infrastructure/FreeTierTestBase.cs` — Testcontainers postgres:16, migrations superuser-ként, non-superuser `freetier_app` role létrehozás (RLS bypass elkerülése), `SetGucAsync` direkt NpgsqlConnection-on
- `tests/SpaceOS.FreeTier.Integration.Tests/Infrastructure/FakeClock.cs` — `IClock` teszt implementáció
- `tests/SpaceOS.FreeTier.Integration.Tests/Infrastructure/CollectionDefinitions.cs` — `[CollectionDefinition("RlsIsolation", DisableParallelization = true)]`
- `tests/SpaceOS.FreeTier.Integration.Tests/Persistence/RlsIsolationTests.cs` — 10 RLS teszt (real PostgreSQL via Testcontainers)
- `tests/SpaceOS.FreeTier.Integration.Tests/Persistence/DbContextTests.cs` — 5 DbContext unit/integration teszt
- `tests/SpaceOS.FreeTier.Integration.Tests/Api/HealthCheckTests.cs` — stub connection string injektálás `WithWebHostBuilder`-rel

## Tesztek

**Eredmény: 68 / 68 PASS — 0 FAIL**

```
Passed! - Failed: 0, Passed: 51, Skipped: 0, Total: 51 - SpaceOS.FreeTier.Domain.Tests.dll
Passed! - Failed: 0, Passed: 17, Skipped: 0, Total: 17 - SpaceOS.FreeTier.Integration.Tests.dll
```

Új tesztek: 15 (10 RLS isolation + 5 DbContext tests)
Regresszió: 0

## Security review

- RLS: mind az 5 user-owned tábla `ENABLE ROW LEVEL SECURITY` + `FORCE ROW LEVEL SECURITY` — policy: `COALESCE(NULLIF(current_setting('app.user_id', TRUE), ''), '00000000-0000-0000-0000-000000000000')::uuid = "UserId"`
- GUC `app.user_id` connection-onként SET + RESET (UserSessionInterceptor) — pool poison fix (BE-22)
- MagicLinkToken: SHA-256 hash a DB-ben, plaintext soha nem persistálódik (D-11-REV)
- ShareToken: TokenHash + TokenPrefix séma (D-13-REV); RawToken `Ignored()` EF konfigban
- ContactEmail nullable (v4.5 GDPR fix) — UpgradeRequestConfiguration-ban `IsRequired(false)`, tesztelt
- Program.cs: connection string nem konfigurálva → `InvalidOperationException` (fail-fast)
- MediatR nincs regisztrálva → DI validáció építési időben elkapja (ValidateOnBuild)

## Tanult lecke (nem volt a specben)

**Testcontainers alapértelmezett postgres user (`postgres`) superuser — bypass-olja a FORCE RLS-t.**
Megoldás: `InitializeAsync`-ban non-superuser `freetier_app` role létrehozása, minden teszt DbContext ezzel csatlakozik.

**GUC (`set_config`) elvész ha EF új connection-t nyit.** Megoldás: `Pooling=false` + `OpenConnectionAsync()` + direkt `NpgsqlConnection.CreateCommand()` a GUC beállításához — garantáltan ugyanazon fizikai connectionön fut a query.

**`IMediator` nincs regisztrálva a DI-ban** — `ValidateOnBuild` elkapja `builder.Build()`-nál, a host soha nem épül fel. Megoldás: `AddMediatR(cfg => cfg.RegisterServicesFromAssemblies(...))` Program.cs-ben.

## Kockázatok / kérdések

Nincsenek.
