---
id: MSG-K021
from: architect
to: kernel
type: task
status: DONE
priority: P0
sprint: "Sprint D · Phase 1.5"
ref: "/opt/spaceos/docs/SpaceOS_Sprint_D_Phase1_5_v4.md"
---

# Sprint D · Phase 1.5 — Application Security Hardening (Kernel)

## Összefoglaló

5 feladat, **12 fejlesztői nap**, 22 finding (9 CRITICAL, 13 HIGH/MEDIUM) javítása.

**Implementációs sorrend:** `T-01 ‖ T-02` → `T-03` → `T-04 ‖ T-05`

**Migration sorrend:** `0011` (SourceBrand — már kész Phase 1-ből) → `0012` (HashSink) → `0013` (RefreshTokens)

> ⚠️ A teljes specifikáció: `/opt/spaceos/docs/SpaceOS_Sprint_D_Phase1_5_v4.md` — MINDIG hivatkozd az eredeti dokumentumot implementáció közben!

---

## T-01 — Race Condition Load Teszt + Advisory Lock (P0, 1 nap)

**Párhuzamos T-02-vel.**

### Feladat
- 50 concurrent `AuditEvent` write → 0 `PreviousHash` duplikátum
- `verify-chain` → `IsValid: true` a 50 rekordra
- Advisory lock single-instance constraint dokumentálva XML doc + ADR-005
- CI pipeline-ban fut

### DoD
- [ ] 50 concurrent `AuditEvent` → 0 `PreviousHash` duplikátum
- [ ] `verify-chain` → `IsValid: true` a 50 rekordra
- [ ] Advisory lock single-instance constraint dokumentálva XML doc + ADR-005
- [ ] CI pipeline-ban fut

---

## T-02 — PostgreSQL Role + RLS + AuditDbContext Separation (P0, 2 nap)

**Párhuzamos T-01-gyel.**

### 2.1 Table ownership fix (SEC-P15-07)

**KRITIKUS:** Ha `spaceos_app` az `AuditEvents` owner, a `FORCE ROW LEVEL SECURITY` bypass-olható.

Hozd létre: `scripts/db/fix-audit-ownership.sql`
```sql
-- Futtatás: psql -U postgres — init-roles.sql ELŐTT

DO $$ BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_schema_owner') THEN
    CREATE ROLE spaceos_schema_owner NOLOGIN;
  END IF;
END $$;

ALTER TABLE "AuditEvents" OWNER TO spaceos_schema_owner;
GRANT SELECT, INSERT, UPDATE, DELETE ON "AuditEvents" TO spaceos_app;

ALTER TABLE "AuditEvents" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "AuditEvents" FORCE ROW LEVEL SECURITY;
```

### 2.2 RLS policy — missing_ok fix (SEC-P15-08)

`init-roles.sql`-ben javítsd:
```sql
CREATE POLICY audit_tenant_isolation ON "AuditEvents"
  FOR ALL
  USING (
    "TenantId" = COALESCE(
      NULLIF(current_setting('app.current_tenant_id', true), ''),
      '00000000-0000-0000-0000-000000000000'
    )::uuid
  );

CREATE POLICY audit_writer_insert_bypass ON "AuditEvents"
  FOR INSERT TO spaceos_audit_writer
  WITH CHECK (true);
```

### 2.3 AuditDbContext + AppDbContext separation (BE-P15-07)

```csharp
// Infrastructure/Persistence/AuditDbContext.cs
public sealed class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options)
        : base(options) { }
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();
    protected override void OnModelCreating(ModelBuilder mb)
        => mb.ApplyConfiguration(new AuditEventConfiguration());
}

// AppDbContext.cs — AuditEvents KIZÁRVA
protected override void OnModelCreating(ModelBuilder mb)
{
    // ... többi entity konfig ...
    mb.Ignore<AuditEvent>(); // BE-P15-07
}
```

Migration: `dotnet ef migrations add Migration_0012_AuditRls --context AuditDbContext --project SpaceOS.Infrastructure --startup-project SpaceOS.Kernel.Api`

### DoD
- [ ] `spaceos_schema_owner` NOLOGIN role — `AuditEvents` owner (nem `spaceos_app`)
- [ ] `FORCE RLS` tényleges hatása ellenőrizve: `spaceos_app` nem owner
- [ ] `current_setting` `missing_ok=true` + `COALESCE` nil-UUID policy
- [ ] `AuditDbContext`: csak `AuditEvents` tábla, saját migration history
- [ ] `AppDbContext`: `AuditEvents` kizárva (`Ignore`)
- [ ] Migration futtatva: `--context AuditDbContext` explicit
- [ ] Integration teszt: cross-context RLS — mindkét context tenant-izolált
- [ ] `AuditEvents DELETE spaceos_app-ként` → `permission denied`

---

## T-03 — JWT ES256 + RefreshToken Teljes Vertikum + OutputCache (P0, 5 nap)

**Blokkolva T-01 + T-02 befejezéséig.**

### 3.1 ES256 keypair

Generálás szerveren (DevOps feladat — már spec-elve), de a kód oldalon:

### 3.2 ISigningKeyProvider + LocalEcKeyProvider (SEC-P15-06)

```csharp
// Infrastructure/Auth/ISigningKeyProvider.cs
public interface ISigningKeyProvider : IDisposable
{
    ECDsaSecurityKey GetPrivateKey();
    ECDsaSecurityKey GetPublicKey();
}

// Infrastructure/Auth/LocalEcKeyProvider.cs
public sealed class LocalEcKeyProvider : ISigningKeyProvider
{
    // ECDsa.Create() + ImportFromPem() from config["Jwt:PrivateKeyPath"] / ["Jwt:PublicKeyPath"]
    // IDisposable: ECDsa.Dispose() — key material törlése memóriából
}
```

### 3.3 IConfigureNamedOptions — BuildServiceProvider anti-pattern csere (BE-P15-01)

```csharp
// Infrastructure/Auth/ConfigureJwtBearerOptions.cs
public sealed class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    // ISigningKeyProvider + IConfiguration injektálva
    // Configure(): TokenValidationParameters ES256 + ClockSkew=Zero
}

// Program.cs
builder.Services.AddSingleton<ISigningKeyProvider, LocalEcKeyProvider>();
builder.Services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
// ⚠️ BuildServiceProvider() TÖRLÉSE!
```

### 3.4 RefreshToken Entity + Service (BE-P15-02, BE-P15-05, BE-P15-12)

```csharp
// Infrastructure/Auth/Persistence/RefreshToken.cs — Entity (Id, UserId, TokenHash, ExpiresAt, CreatedAt, RevokedAt)
// Infrastructure/Auth/Persistence/RefreshTokenConfiguration.cs — EF Config (UQ_TokenHash, IX_ExpiresAt, IX_UserId)
// Infrastructure/Auth/RefreshTokenService.cs — Static: GenerateOpaqueToken (256-bit CSPRNG Base64Url), HashToken (SHA-256 hex), VerifyToken (FixedTimeEquals)
```

Migration: `dotnet ef migrations add Migration_0013_AddRefreshTokens --context AppDbContext --project SpaceOS.Infrastructure --startup-project SpaceOS.Kernel.Api`

### 3.5 RefreshToken CQRS vertikum (BE-P15-04)

```csharp
// Application/Auth/Commands/RefreshTokenCommand.cs — IRequest<Result<TokenPairDto>>
// Application/Auth/Commands/RefreshTokenCommandValidator.cs — NotEmpty, Length(43,43)
// Application/Auth/Commands/RefreshTokenCommandHandler.cs — Hash → lookup → revoke old → create new → generate AT → commit
// Application/Auth/Commands/RevokeTokenCommand.cs — logout endpoint
// Application/Auth/Commands/RevokeTokenCommandHandler.cs — BE-P15-11: idempotens — nem létező RT → 200 OK
```

### 3.6 Endpointok

- `POST /api/auth/refresh` — `RefreshTokenCommand`
- `POST /api/auth/logout` — `RevokeTokenCommand` (idempotens)

### 3.7 JWKS endpoint — OutputCache (BE-P15-06)

```csharp
builder.Services.AddOutputCache(opt =>
    opt.AddPolicy("jwks-cache", policy =>
        policy.Expire(TimeSpan.FromHours(1)).SetVaryByHeader("Accept").Tag("jwks")));

// GET /.well-known/jwks.json — AllowAnonymous + RequireRateLimiting("fixed") + CacheOutput("jwks-cache")
// ⚠️ AddMemoryCache() ELTÁVOLÍTÁSA — OutputCache egységesen
```

### 3.8 Token lifetime

| Token | Lifetime | Tárolás | Revokálható? |
|---|---|---|---|
| Access Token (JWT) | **15 perc** | Kliens memória | Nem |
| Refresh Token (opaque) | **8 óra** | DB: `RefreshTokens.TokenHash` (SHA-256) | Igen |

### DoD
- [ ] `JWT_SIGNING_KEY` env var törölve — `grep 0 találat`
- [ ] EC P-256 keypair: `jwt_ec_private.pem` (640) + `jwt_ec_public.pem` (644)
- [ ] `ConfigureJwtBearerOptions : IConfigureNamedOptions` — `BuildServiceProvider()` eltávolítva
- [ ] `LocalEcKeyProvider : IDisposable` — `ECDsa.Dispose()` implementálva
- [ ] Access token lifetime: **15 perc**, ES256
- [ ] `RefreshToken` entity + config + Migration 0013
- [ ] `RefreshTokenService`: GenerateOpaqueToken + HashToken + VerifyToken (FixedTimeEquals)
- [ ] `POST /api/auth/refresh`: Command + Validator + Handler + test
- [ ] `POST /api/auth/logout`: RevokeTokenCommand + idempotens handler + test
- [ ] `/.well-known/jwks.json`: CacheOutput + RequireRateLimiting
- [ ] `AddOutputCache()` regisztrálva — `AddMemoryCache()` **nincs**
- [ ] `ClockSkew = TimeSpan.Zero`
- [ ] Összes `Api.Tests` ES256 token-nel fut — 0 fail

---

## T-04 — TenantId JWT Claim + TenantSessionInterceptor (P1, 2 nap)

**Párhuzamos T-05-tel. Blokkolva T-03-ig.**

### 4.1 TenantSessionInterceptor (BE-P15-03)

**KRITIKUS:** `set_config(is_local=true)` csak az aktuális tranzakcióban él. Cross-context RLS bypass lehetséges.

```csharp
// Infrastructure/Persistence/TenantSessionInterceptor.cs : DbConnectionInterceptor
// ConnectionOpenedAsync: set_config('app.current_tenant_id', tenantId, false) — SESSION szintű
// ConnectionClosingAsync: set_config reset — pool leak megelőzve
```

### 4.2 DI regisztráció

```csharp
services.AddSingleton<TenantSessionInterceptor>();
// AppDbContext + AuditDbContext: .AddInterceptors(sp.GetRequiredService<TenantSessionInterceptor>())
```

### 4.3 Middleware regisztrációs sorrend

```csharp
app.UseAuthentication();      // 1. JWT parse
app.UseAuthorization();       // 2. Policy check
// TenantContextMiddleware TÖRÖLVE — TenantSessionInterceptor váltja
```

### DoD
- [ ] `TenantSessionInterceptor : DbConnectionInterceptor` implementálva
- [ ] `ConnectionOpened`: `set_config is_local=false`
- [ ] `ConnectionClosing`: `set_config` reset — pool leak megelőzve
- [ ] `AppDbContext` + `AuditDbContext`: interceptor regisztrálva
- [ ] `TenantContextMiddleware` törölve
- [ ] Teszt: cross-context tenant isolation — mindkét context ugyanazt a TenantId-t látja
- [ ] Header manipulation: `X-Tenant-Id` header nem változtatja a TenantId-t
- [ ] Hiányzó `tenant_id` claim → 401

---

## T-05 — Hash Sink PostgreSQL + DbContextFactory (P0, 3 nap)

**Párhuzamos T-04-gyel. Blokkolva T-03-ig.**

### 5.1 IDbContextFactory (BE-P15-09)

```csharp
// AddDbContext HELYETT:
services.AddDbContextFactory<HashSinkDbContext>(opt =>
    opt.UseNpgsql(config["ConnectionStrings:AuditSink"], npg => npg.EnableRetryOnFailure()));

// BE-P15-10: startup fail-fast
services.AddOptions<ConnectionStringOptions>()
    .BindConfiguration("ConnectionStrings")
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### 5.2 PostgresHashSink refactor

```csharp
// Infrastructure/Audit/PostgresHashSink.cs
// IDbContextFactory<HashSinkDbContext> _factory
// using var ctx = await _factory.CreateDbContextAsync(ct) — context azonnal dispose
// Prometheus: spaceos_hashsink_write_total + spaceos_hashsink_write_failures_total
// Sink hiba → NEM throw — fő audit folyamat nem szakad meg
```

### 5.3 Migration

```bash
dotnet ef database update \
  --context HashSinkDbContext \
  --connection "Host=127.0.0.1;Database=spaceos_audit_sink;Username=spaceos_sink_writer;Password=..."
```

### DoD
- [ ] `spaceos_audit_sink` DB + `hash_chain_records` tábla létezik
- [ ] `spaceos_sink_writer`: INSERT only — SELECT/UPDATE/DELETE → `permission denied`
- [ ] `AddDbContextFactory<HashSinkDbContext>()` — Scoped lifetime eliminated
- [ ] `ConnectionStringOptions.ValidateOnStart()` — startup fail-fast
- [ ] Migration: `--context HashSinkDbContext --connection` explicit
- [ ] Prometheus: `spaceos_hashsink_write_total` + `spaceos_hashsink_write_failures_total`
- [ ] `verify-chain` divergencia detektál manipuláció után
- [ ] Sink hiba → fő audit folyamat nem szakad meg
- [ ] Escrow feature flag: **OFF** — upgrade gate dokumentálva

---

## Összesített DoD

- [ ] Meglévő **1027 teszt** zöld
- [ ] Phase 1.5 v4 új tesztek: **≥ 45 db**
- [ ] 0 build warning (xUnit1051 kivételével)
- [ ] `dotnet list package --vulnerable` → 0 high/critical

---

## ⚠️ Fontos kontextus

- **DB architektúra:** Két DB — `spaceos` (AppDbContext + AuditDbContext) + `spaceos_audit_sink` (HashSinkDbContext)
- **PostgreSQL port:** 5433 (nem 5432!)
- **Kernel port:** 5001 (production)
- **RS256 → ES256 migráció:** Az Orchestrator és Portal is frissül ES256-ra — a publikus kulcsot a `/.well-known/jwks.json`-ből olvassák
- **Escrow feature flag:** OFF marad Phase 1.5 után is — S3/Azure sink upgrade kell a GA-hoz
