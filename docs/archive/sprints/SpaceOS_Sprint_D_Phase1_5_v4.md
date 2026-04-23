# SpaceOS — Sprint D · Phase 1.5
## Application Security Hardening — v4.0

> `/senior-backend` review alapján frissítve · 2026-04-06  
> Kumulált review: `/database-designer` + `/database-schema-designer` → v2 · `/senior-security` → v3 · `/senior-backend` → v4

| Attribútum | Érték |
|---|---|
| Verzió | v4.0 — 2026-04-06 |
| Előzmény | v3.0 felülvizsgálva `/senior-backend`: 3 CRITICAL + 4 HIGH + 4 MEDIUM finding |
| Státusz | **IMPLEMENTÁCIÓRA KÉSZ — végleges tervdokumentum** |
| Becsült időtartam | 12 fejlesztői nap (v1: 9 nap → v4: 12 nap) |
| Migration sorrend | `0011` (Phase 1 SourceBrand) → `0012` (HashSink) → `0013` (RefreshTokens) |

---

## 1. Kumulált Finding Összesítő (v1 → v4)

| Review | Finding-ek | Legfontosabb javítás | Effort delta |
|---|---|---|---|
| v1 → `/database-designer` + `/schema-designer` → v2 | 5 DB hiányosság | File sink → PostgreSQL separate DB; RLS hiányzott; ERD; dual context | +1 nap |
| v2 → `/senior-security` → v3 | 3 CRITICAL + 6 HIGH + 5 MEDIUM | RS256→ES256; 8h→15min AT; RLS owner bypass; `missing_ok`; RefreshToken bevezetve | +1 nap |
| v3 → `/senior-backend` → v4 | 3 CRITICAL + 4 HIGH + 4 MEDIUM | `BuildServiceProvider` anti-pattern; RT teljes vertikum; `is_local=false` interceptor; `OutputCache`; `DbContextFactory` | +1 nap |
| **Összesen** | **22 finding (9 CRITICAL, 13 HIGH/MEDIUM)** | **12 fejlesztői nap végleges becslés** | |

### /senior-backend Findings — v3 → v4

| ID | Súly | Terület | Probléma | v4 javítás |
|---|---|---|---|---|
| BE-P15-01 | 🔴 CRITICAL | T-03 DI | `BuildServiceProvider()` Program.cs-ben — 2. DI container, Singleton/Scoped conflict, memory leak | `IConfigureNamedOptions<JwtBearerOptions>` pattern |
| BE-P15-02 | 🔴 CRITICAL | T-03 RT | RefreshToken: nincs EF entity, Migration 0013, Repository — Clean Architecture violation | Teljes vertikum: entity + config + migration + repo |
| BE-P15-03 | 🔴 CRITICAL | T-04 EF | `set_config(is_local=true)` elvész cross-context tranzakcióban — RLS bypass | `TenantSessionInterceptor : DbConnectionInterceptor` + `is_local=false` + connection reset |
| BE-P15-04 | 🟠 HIGH | T-03 RT | `/api/auth/refresh` endpoint spec hiányzik: DTO, Command, Handler, Validator, Test | Teljes CQRS vertikum specifikálva |
| BE-P15-05 | 🟠 HIGH | T-03 RT | Opaque RT formátum és hash nincs specifikálva | 256-bit CSPRNG + hex encoding + `FixedTimeEquals` |
| BE-P15-06 | 🟠 HIGH | T-03 Cache | `AddMemoryCache()` + `AddOutputCache()` kettős caching — redundáns, regisztráció hiányzik | `OutputCache` egységesen — `IMemoryCache` eltávolítva |
| BE-P15-07 | 🟠 HIGH | T-02 EF | Két context → egy tábla (`AuditEvents`) → migration conflict | `AppDbContext` kizárja; `AuditDbContext` az egyedüli owner |
| BE-P15-08 | 🟡 MEDIUM | T-05 Migration | `HashSinkDbContext` migration `--context` nincs specifikálva | Explicit `--context HashSinkDbContext --connection` |
| BE-P15-09 | 🟡 MEDIUM | T-05 DI | `HashSinkDbContext` Scoped — fire-and-forget dispose kockázat | `AddDbContextFactory<HashSinkDbContext>()` pattern |
| BE-P15-10 | 🟡 MEDIUM | T-05 Config | `ConnectionStrings:AuditSink` hiány runtime-ban derül ki | `ValidateOnStart()` — startup fail-fast |
| BE-P15-11 | 🟡 MEDIUM | T-03 API | `/api/auth/logout` idempotens RT revocation nincs kidolgozva | Nem létező RT → 200 OK (nem 404) |
| BE-P15-12 | 🟡 MEDIUM | T-03 RT | Timing-safe összehasonlítás hiányzik RT lookup-nál | `CryptographicOperations.FixedTimeEquals` |

---

## 2. Feladatok — v4

| # | Feladat | Prioritás | Effort | Sorrend |
|---|---|---|---|---|
| T-01 | Race condition load teszt + advisory lock constraint doc | P0 blocker | 1 nap | 1. (párhuzamos T-02-vel) |
| T-02 | PG role + RLS + AuditDbContext context separation | P0 blocker | 2 nap | 1. (párhuzamos T-01-gyel) |
| T-03 | JWT ES256 + `IConfigureOptions` + RefreshToken teljes vertikum + `OutputCache` | P0 blocker | 5 nap | 2. |
| T-04 | TenantId JWT + `TenantSessionInterceptor` (`is_local=false`) | P1 magas | 2 nap | 3. (párhuzamos T-05-tel) |
| T-05 | HashSink PG + `DbContextFactory` + startup validation + migration spec | P0 blocker | 3 nap | 3. (párhuzamos T-04-gyel) |

**Implementációs sorrend:** `T-01 ‖ T-02` → `T-03` → `T-04 ‖ T-05`

---

## 3. T-02 · PostgreSQL Role + RLS + AuditDbContext Separation

### 3.1 Table ownership fix (SEC-P15-07)

> **KRITIKUS:** Ha `spaceos_app` az `AuditEvents` owner, a `FORCE ROW LEVEL SECURITY` bypass-olható.

```sql
-- scripts/db/fix-audit-ownership.sql
-- Futtatás: psql -U postgres — init-roles.sql ELŐTT

-- 1. Dedikált owner role — NOLOGIN, csak ownership céljára
DO $$ BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_schema_owner') THEN
    CREATE ROLE spaceos_schema_owner NOLOGIN;
  END IF;
END $$;

-- 2. AuditEvents ownership átadása
ALTER TABLE "AuditEvents" OWNER TO spaceos_schema_owner;

-- 3. spaceos_app explicit jogok visszaállítása
GRANT SELECT, INSERT, UPDATE, DELETE ON "AuditEvents" TO spaceos_app;

-- 4. FORCE RLS most már tényleg véd mindenki ellen
ALTER TABLE "AuditEvents" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "AuditEvents" FORCE ROW LEVEL SECURITY;

-- 5. Ellenőrzés — elvárt: spaceos_schema_owner
SELECT tableowner FROM pg_tables WHERE tablename = 'AuditEvents';
```

### 3.2 RLS policy — missing_ok fix (SEC-P15-08)

```sql
-- init-roles.sql — RLS policy javítva
-- RÉGI (hibás): current_setting('app.current_tenant_id')::uuid
--   → exception-t dob ha nincs beállítva (background job, healthz)

-- ÚJ (helyes): missing_ok=true + COALESCE nil-UUID fallback
CREATE POLICY audit_tenant_isolation ON "AuditEvents"
  FOR ALL
  USING (
    "TenantId" = COALESCE(
      NULLIF(current_setting('app.current_tenant_id', true), ''),
      '00000000-0000-0000-0000-000000000000'
    )::uuid
  );

-- spaceos_audit_writer bypass (INSERT-nél nem kell USING)
CREATE POLICY audit_writer_insert_bypass ON "AuditEvents"
  FOR INSERT TO spaceos_audit_writer
  WITH CHECK (true);
```

### 3.3 AuditDbContext + AppDbContext separation (BE-P15-07)

```csharp
// Infrastructure/Persistence/AuditDbContext.cs
/// <summary>
/// Restricted DbContext for audit writes only.
/// Uses spaceos_audit_writer role — INSERT + SELECT on AuditEvents.
/// OWNS the AuditEvents table migration — AppDbContext excludes it.
/// </summary>
public sealed class AuditDbContext : DbContext
{
    public AuditDbContext(DbContextOptions<AuditDbContext> options)
        : base(options) { }

    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder mb)
        => mb.ApplyConfiguration(new AuditEventConfiguration());
}

// Infrastructure/Persistence/AppDbContext.cs — AuditEvents KIZÁRVA
protected override void OnModelCreating(ModelBuilder mb)
{
    // ... többi entity konfig ...

    // BE-P15-07: AuditEvents kizárva — AuditDbContext kezeli és migrálja
    mb.Ignore<AuditEvent>();
}
```

```bash
# Migration futtatás — explicit --context
dotnet ef migrations add Migration_0012_AuditRls \
  --context AuditDbContext \
  --project SpaceOS.Infrastructure \
  --startup-project SpaceOS.Kernel.Api
```

---

## 4. T-03 · JWT ES256 + RefreshToken Teljes Vertikum

### 4.1 ES256 keypair generálás

```bash
sudo openssl ecparam -name prime256v1 -genkey -noout \
  -out /etc/spaceos/keys/jwt_ec_private.pem
sudo openssl ec -in /etc/spaceos/keys/jwt_ec_private.pem \
  -pubout -out /etc/spaceos/keys/jwt_ec_public.pem

sudo chown root:spaceos /etc/spaceos/keys/jwt_ec_private.pem
sudo chmod 640 /etc/spaceos/keys/jwt_ec_private.pem   # root:spaceos
sudo chmod 644 /etc/spaceos/keys/jwt_ec_public.pem
```

### 4.2 ISigningKeyProvider : IDisposable (SEC-P15-06)

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
    private readonly ECDsa _privateEcdsa;
    private readonly ECDsa _publicEcdsa;
    private readonly ECDsaSecurityKey _privateKey;
    private readonly ECDsaSecurityKey _publicKey;
    private bool _disposed;

    public LocalEcKeyProvider(IConfiguration config)
    {
        _privateEcdsa = ECDsa.Create();
        _privateEcdsa.ImportFromPem(
            File.ReadAllText(config["Jwt:PrivateKeyPath"]
                ?? throw new InvalidOperationException("Jwt:PrivateKeyPath not configured")));
        _privateKey = new ECDsaSecurityKey(_privateEcdsa);

        _publicEcdsa = ECDsa.Create();
        _publicEcdsa.ImportFromPem(
            File.ReadAllText(config["Jwt:PublicKeyPath"]
                ?? throw new InvalidOperationException("Jwt:PublicKeyPath not configured")));
        _publicKey = new ECDsaSecurityKey(_publicEcdsa);
    }

    public ECDsaSecurityKey GetPrivateKey() => _privateKey;
    public ECDsaSecurityKey GetPublicKey()  => _publicKey;

    public void Dispose()
    {
        if (_disposed) return;
        _privateEcdsa.Dispose(); // key material törlése memóriából
        _publicEcdsa.Dispose();
        _disposed = true;
    }
}
```

### 4.3 IConfigureNamedOptions — BuildServiceProvider anti-pattern csere (BE-P15-01)

```csharp
// Infrastructure/Auth/ConfigureJwtBearerOptions.cs
/// <summary>
/// Configures JwtBearer options via DI — no BuildServiceProvider() anti-pattern.
/// </summary>
public sealed class ConfigureJwtBearerOptions
    : IConfigureNamedOptions<JwtBearerOptions>
{
    private readonly ISigningKeyProvider _keyProvider;
    private readonly IConfiguration _config;

    public ConfigureJwtBearerOptions(
        ISigningKeyProvider keyProvider,
        IConfiguration config)
    {
        _keyProvider = keyProvider;
        _config      = config;
    }

    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name != JwtBearerDefaults.AuthenticationScheme) return;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = _keyProvider.GetPublicKey(),
            ValidIssuer              = _config["Jwt:Issuer"],
            ValidAudience            = _config["Jwt:Audience"],
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.Zero   // SEC-P15-04
        };
    }

    public void Configure(JwtBearerOptions options)
        => Configure(JwtBearerDefaults.AuthenticationScheme, options);
}

// Api/Program.cs
builder.Services.AddSingleton<ISigningKeyProvider, LocalEcKeyProvider>();
builder.Services.AddSingleton<
    IConfigureOptions<JwtBearerOptions>,
    ConfigureJwtBearerOptions>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(); // options-t a ConfigureJwtBearerOptions tölti fel — nincs BuildServiceProvider()
```

### 4.4 RefreshToken Entity + Service (BE-P15-02 + BE-P15-05 + BE-P15-12)

```csharp
// Infrastructure/Auth/Persistence/RefreshToken.cs
public sealed class RefreshToken
{
    public Guid Id            { get; private set; }
    public Guid UserId        { get; private set; }
    public string TokenHash   { get; private set; } = default!; // SHA-256 hex, 64 chars
    public DateTimeOffset ExpiresAt  { get; private set; }
    public DateTimeOffset CreatedAt  { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsActive  => !IsRevoked && !IsExpired;

    private RefreshToken() { } // EF Core ctor

    public static RefreshToken Create(
        Guid userId, string tokenHash, DateTimeOffset expiresAt)
    => new()
    {
        Id        = Guid.NewGuid(),
        UserId    = userId,
        TokenHash = tokenHash,
        ExpiresAt = expiresAt,
        CreatedAt = DateTimeOffset.UtcNow
    };

    public void Revoke() => RevokedAt = DateTimeOffset.UtcNow;
}
```

```csharp
// Infrastructure/Auth/RefreshTokenService.cs
public static class RefreshTokenService
{
    /// <summary>256-bit CSPRNG token, Base64Url encoded (43 chars).</summary>
    public static string GenerateOpaqueToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncoder.Encode(bytes.ToArray());
    }

    /// <summary>SHA-256 hex for DB storage — plaintext never persisted.</summary>
    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    /// <summary>Timing-safe comparison (BE-P15-12).</summary>
    public static bool VerifyToken(string incoming, string storedHash)
        => CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(HashToken(incoming)),
            Encoding.UTF8.GetBytes(storedHash));
}
```

```csharp
// Infrastructure/Auth/Persistence/RefreshTokenConfiguration.cs
public sealed class RefreshTokenConfiguration
    : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> b)
    {
        b.ToTable("RefreshTokens");
        b.HasKey(x => x.Id);

        b.Property(x => x.TokenHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.ExpiresAt).IsRequired();
        b.Property(x => x.CreatedAt).IsRequired();
        b.Property(x => x.RevokedAt);

        b.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("UQ_RefreshTokens_TokenHash");
        b.HasIndex(x => x.ExpiresAt)
            .HasDatabaseName("IX_RefreshTokens_ExpiresAt");   // cleanup job
        b.HasIndex(x => x.UserId)
            .HasDatabaseName("IX_RefreshTokens_UserId");
    }
}
```

```bash
# Migration 0013 — AppDbContext-ben
dotnet ef migrations add Migration_0013_AddRefreshTokens \
  --context AppDbContext \
  --project SpaceOS.Infrastructure \
  --startup-project SpaceOS.Kernel.Api
```

### 4.5 RefreshToken CQRS vertikum (BE-P15-04)

```csharp
// Application/Auth/Commands/RefreshTokenCommand.cs
public sealed record RefreshTokenCommand(string RefreshToken)
    : IRequest<Result<TokenPairDto>>;

// Application/Auth/Commands/RefreshTokenCommandValidator.cs
public sealed class RefreshTokenCommandValidator
    : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
        => RuleFor(x => x.RefreshToken).NotEmpty().Length(43, 43);
}

// Application/Auth/Commands/RefreshTokenCommandHandler.cs
public sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, Result<TokenPairDto>>
{
    public async Task<Result<TokenPairDto>> Handle(
        RefreshTokenCommand cmd, CancellationToken ct)
    {
        var hash   = RefreshTokenService.HashToken(cmd.RefreshToken);
        var stored = await _rtRepo.GetActiveByHashAsync(hash, ct)
                                  .ConfigureAwait(false);

        if (stored is null || !stored.IsActive)
            return Result.Unauthorized();

        // Token rotation: revokolja a régit
        stored.Revoke();
        await _rtRepo.UpdateAsync(stored, ct).ConfigureAwait(false);

        // Új AT + RT pár
        var newRaw    = RefreshTokenService.GenerateOpaqueToken();
        var newHash   = RefreshTokenService.HashToken(newRaw);
        var newEntity = RefreshToken.Create(
            stored.UserId, newHash,
            DateTimeOffset.UtcNow.AddHours(8));
        await _rtRepo.AddAsync(newEntity, ct).ConfigureAwait(false);

        var at = _tokenService.GenerateAccessToken(stored.UserId);
        await _uow.CommitAsync(ct).ConfigureAwait(false);

        return Result.Success(new TokenPairDto(at, newRaw));
    }
}

// Application/Auth/Commands/RevokeTokenCommandHandler.cs
// BE-P15-11: idempotens — nem létező RT → 200 OK, nem 404
public async Task<Result> Handle(
    RevokeTokenCommand cmd, CancellationToken ct)
{
    var hash   = RefreshTokenService.HashToken(cmd.RefreshToken);
    var stored = await _rtRepo.GetByHashAsync(hash, ct).ConfigureAwait(false);

    if (stored is not null && !stored.IsRevoked)
        stored.Revoke();

    await _uow.CommitAsync(ct).ConfigureAwait(false);
    return Result.Success();
}
```

### 4.6 JWKS endpoint — OutputCache (BE-P15-06 + SEC-P15-03 + SEC-P15-14)

```csharp
// Api/Program.cs
builder.Services.AddOutputCache(opt =>
    opt.AddPolicy("jwks-cache", policy =>
        policy.Expire(TimeSpan.FromHours(1))
              .SetVaryByHeader("Accept")
              .Tag("jwks")));

app.UseOutputCache();

// Api/Endpoints/WellKnownEndpoints.cs
app.MapGet("/.well-known/jwks.json", (ISigningKeyProvider kp) =>
{
    var jwk = JsonWebKeyConverter.ConvertFromECDsaSecurityKey(kp.GetPublicKey());
    jwk.Alg = SecurityAlgorithms.EcdsaSha256;
    return Results.Ok(new { keys = new[] { jwk } });
})
.AllowAnonymous()
.RequireRateLimiting("fixed")    // SEC-P15-14: DoS védelem
.CacheOutput("jwks-cache")       // SEC-P15-03: Cache-Control: max-age=3600
.WithTags("Auth");
```

### 4.7 Token lifetime összefoglaló (SEC-P15-05)

| Token | Lifetime | Tárolás | Revokálható? |
|---|---|---|---|
| Access Token (JWT) | **15 perc** | Kliens memóriában / localStorage | Nem (rövid élettartam kompenzál) |
| Refresh Token (opaque) | **8 óra** | DB: `RefreshTokens.TokenHash` (SHA-256) | ✅ igen — `POST /api/auth/logout` |

---

## 5. T-04 · TenantSessionInterceptor — is_local=false (BE-P15-03)

> **KRITIKUS:** `set_config(is_local=true)` csak az aktuális tranzakcióban él. Ha az `AppDbContext` és az `AuditDbContext` különböző tranzakciókat nyitnak ugyanazon kérésen belül, az audit write elveszíti a tenant context-et → RLS bypass lehetséges.

```csharp
// Infrastructure/Persistence/TenantSessionInterceptor.cs
/// <summary>
/// Sets app.current_tenant_id at SESSION level (is_local=false).
/// Resets on connection return to pool — prevents tenant context leak.
/// Replaces TenantContextMiddleware entirely.
/// </summary>
public sealed class TenantSessionInterceptor : DbConnectionInterceptor
{
    private readonly IHttpContextAccessor _http;

    public TenantSessionInterceptor(IHttpContextAccessor http)
        => _http = http;

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken ct)
    {
        var tenantId = _http.HttpContext?
            .User.FindFirst("tenant_id")?.Value;

        if (tenantId is not null && Guid.TryParse(tenantId, out _))
        {
            await using var cmd = connection.CreateCommand();
            // is_local=false → session-szintű, minden tranzakcióban él
            cmd.CommandText =
                $"SELECT set_config('app.current_tenant_id', '{tenantId}', false)";
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
    }

    public override async Task ConnectionClosingAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        CancellationToken ct)
    {
        // Pool visszaadásakor: reset — megelőzi a tenant context szivárgást
        await using var cmd = connection.CreateCommand();
        cmd.CommandText =
            "SELECT set_config('app.current_tenant_id', '', false)";
        await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }
}

// Infrastructure/DependencyInjection.cs
services.AddSingleton<TenantSessionInterceptor>();

services.AddDbContext<AppDbContext>((sp, opt) =>
    opt.UseNpgsql(config["ConnectionStrings:Default"],
        npg => npg.EnableRetryOnFailure())
    .AddInterceptors(sp.GetRequiredService<TenantSessionInterceptor>()));

services.AddDbContext<AuditDbContext>((sp, opt) =>
    opt.UseNpgsql(config["ConnectionStrings:AuditWriter"],
        npg => npg.EnableRetryOnFailure())
    .AddInterceptors(sp.GetRequiredService<TenantSessionInterceptor>()));

// TenantContextMiddleware törölhető — az interceptor veszi át a szerepét
```

### Middleware regisztrációs sorrend (SEC-P15-09)

```csharp
// Api/Program.cs — kötelező sorrend
app.UseAuthentication();      // 1. JWT parse
app.UseAuthorization();       // 2. Policy check
// TenantContextMiddleware TÖRÖLVE — TenantSessionInterceptor váltja
```

---

## 6. T-05 · Hash Sink — DbContextFactory + Startup Validation

### 6.1 IDbContextFactory (BE-P15-09)

```csharp
// Infrastructure/DependencyInjection.cs

// HELYETTE (AddDbContext helyett):
services.AddDbContextFactory<HashSinkDbContext>(opt =>
    opt.UseNpgsql(config["ConnectionStrings:AuditSink"],
        npg => npg.EnableRetryOnFailure()));

// BE-P15-10: startup fail-fast validation
services.AddOptions<ConnectionStringOptions>()
    .BindConfiguration("ConnectionStrings")
    .ValidateDataAnnotations()
    .ValidateOnStart(); // app induláskor derül ki, nem az első kérésnél
```

```csharp
// Infrastructure/Audit/PostgresHashSink.cs
public sealed class PostgresHashSink : IHashSink
{
    private readonly IDbContextFactory<HashSinkDbContext> _factory;
    private static readonly Counter<long> _writeSuccess =
        Metrics.CreateCounter<long>("spaceos_hashsink_write_total", "");
    private static readonly Counter<long> _writeFailure =
        Metrics.CreateCounter<long>("spaceos_hashsink_write_failures_total", "");

    public PostgresHashSink(IDbContextFactory<HashSinkDbContext> factory, ...)
        => _factory = factory;

    public async Task AppendAsync(
        TenantId tenantId, Guid eventId,
        string stateHash, DateTimeOffset occurredAt,
        CancellationToken ct)
    {
        try
        {
            // using: context azonnal dispose-olódik a write után
            await using var ctx = await _factory
                .CreateDbContextAsync(ct)
                .ConfigureAwait(false);

            ctx.HashChainRecords.Add(new HashChainRecord
            {
                TenantId   = tenantId.Value,
                EventId    = eventId,
                StateHash  = stateHash,
                OccurredAt = occurredAt
            });
            await ctx.SaveChangesAsync(ct).ConfigureAwait(false);
            _writeSuccess.Add(1);
        }
        catch (Exception ex)
        {
            _writeFailure.Add(1);
            _logger.LogError(ex,
                "HashSink write FAILED for EventId {EventId}", eventId);
            // Nem throw — fő audit folyamat nem szakad meg
        }
    }
}
```

### 6.2 Migration — explicit context (BE-P15-08)

```bash
# HashSink migration — külön DB, explicit connection
dotnet ef database update \
  --context HashSinkDbContext \
  --connection "Host=127.0.0.1;Database=spaceos_audit_sink;Username=spaceos_sink_writer;Password=..."
```

### 6.3 Prometheus alerting thresholds

| Metrika | Alert küszöb | Severity | Akció |
|---|---|---|---|
| `spaceos_hashsink_write_failures_total` | `rate(5m) > 0` | WARNING | Operátor vizsgálja |
| `spaceos_hashsink_write_failures_total` | `rate(5m) > 10` | CRITICAL | Escrow feature flag OFF |
| failure ratio `> 1%` | `failures/total > 0.01` | CRITICAL | PagerDuty webhook |

> ⚠️ **Fontos:** A PostgreSQL hash sink (két DB, egy instance) Phase 1.5-re elfogadható. **Escrow GA-hoz a sink upgrade kötelező: S3 Object Lock vagy Azure Immutable Blob.** Ez explicit deployment gate — Escrow feature flag OFF marad amíg el nem készül.

---

## 7. Migration Összesítő — Phase 1.5

| Migration | Context | DB | Tartalom |
|---|---|---|---|
| `0011_AddSourceBrandToAuditEvents` | `AuditDbContext` | `spaceos` | Phase 1 — SourceBrand mező |
| `0012_AddHashSinkInfrastructure` | `HashSinkDbContext` | `spaceos_audit_sink` | `hash_chain_records` tábla + indexek |
| `0013_AddRefreshTokens` | `AppDbContext` | `spaceos` | `RefreshTokens` tábla + indexek |

---

## 8. DB Architektúra — Trust Boundary Térkép

```
┌─────────────────────────────────────────────────────────────────┐
│  PostgreSQL 16 — VPS 127.0.0.1                                  │
│                                                                 │
│  ┌─────────────────────────┐   ┌───────────────────────────┐   │
│  │  DB: spaceos            │   │  DB: spaceos_audit_sink   │   │
│  │                         │   │                           │   │
│  │  Táblák:                │   │  Táblák:                  │   │
│  │   Tenants, Facilities   │   │   hash_chain_records      │   │
│  │   WorkStations          │   │                           │   │
│  │   SpaceLayers           │   │  Role-ok:                 │   │
│  │   FlowEpics             │   │   sink_writer (INSERT)    │   │
│  │   SyncSignals           │   │   sink_verifier (SELECT)  │   │
│  │   NodeManifests         │   │                           │   │
│  │   AuditEvents ←owner─┐  │   │  Context:                 │   │
│  │   RefreshTokens       │  │   │   HashSinkDbContext       │   │
│  │                       │  │   └───────────────────────────┘   │
│  │  Role-ok:             │  │                                   │
│  │   spaceos_app (R/W)   │  │   Két külön DB =                  │
│  │   spaceos_schema_owner│◄─┘   Két külön trust boundary        │
│  │    (AuditEvents owner)│      Kompromittált spaceos_app       │
│  │   spaceos_audit_writer│      nem fér a sink DB-hez           │
│  │    (AuditEvents:      │                                       │
│  │     INSERT+SELECT)    │                                       │
│  │                       │                                       │
│  │  Context-ok:          │                                       │
│  │   AppDbContext        │                                       │
│  │   AuditDbContext      │                                       │
│  └─────────────────────────┘                                    │
└─────────────────────────────────────────────────────────────────┘
```

---

## 9. ERD — Phase 1.5 után

```
erDiagram
    Tenants ||--o{ Facilities : owns
    Tenants ||--o{ AuditEvents : "audit scope"
    Tenants ||--o{ SyncSignals : "sync scope"

    AuditEvents {
        uuid        Id           PK
        uuid        TenantId     FK "IX + RLS policy (FORCE)"
        varchar100  EventType    "IX"
        varchar100  ActorId      "GDPR: GUID only"
        varchar45   SourceIp
        varchar50   SourceBrand  "IX partial (0011)"
        jsonb       Payload
        varchar64   PreviousHash "NOT NULL — chain link"
        varchar64   StateHash    "NOT NULL — chain hash"
        timestamptz OccurredAt   "IX composite (TenantId, OccurredAt)"
    }

    RefreshTokens {
        uuid        Id           PK
        uuid        UserId       "IX"
        varchar64   TokenHash    "UNIQUE — SHA-256 hex"
        timestamptz ExpiresAt    "IX — cleanup job"
        timestamptz CreatedAt
        timestamptz RevokedAt    "NULL = aktív"
    }

    hash_chain_records {
        bigserial   Id           PK "tamper indicator: gap = törlés"
        uuid        TenantId     "IX"
        uuid        EventId      "UNIQUE — 1:1 AuditEvent"
        varchar64   StateHash    "NOT NULL — mirror"
        timestamptz OccurredAt   "IX"
        timestamptz InsertedAt   "DEFAULT now()"
    }

    AuditEvents ||--|| hash_chain_records : "StateHash mirror"
    NOTE: hash_chain_records → spaceos_audit_sink (külön DB)
```

### Index stratégia — teljes kép

| Tábla | Index | Oszlopok | Típus |
|---|---|---|---|
| `AuditEvents` | `PK_AuditEvents` | `Id` | PK |
| `AuditEvents` | `IX_AuditEvents_Tenant_Time` | `(TenantId, OccurredAt)` | Composite |
| `AuditEvents` | `IX_AuditEvents_EventType` | `EventType` | BTree |
| `AuditEvents` | `IX_AuditEvents_SourceBrand` | `SourceBrand WHERE NOT NULL` | Partial |
| `RefreshTokens` | `UQ_RefreshTokens_TokenHash` | `TokenHash` | UNIQUE |
| `RefreshTokens` | `IX_RefreshTokens_ExpiresAt` | `ExpiresAt` | BTree (cleanup) |
| `RefreshTokens` | `IX_RefreshTokens_UserId` | `UserId` | BTree |
| `hash_chain_records` | `UQ_hash_chain_records_EventId` | `EventId` | UNIQUE |
| `hash_chain_records` | `IX_hash_chain_records_TenantId` | `TenantId` | BTree |
| `hash_chain_records` | `IX_hash_chain_records_OccurredAt` | `OccurredAt` | BTree |

---

## 10. Sprint Definition of Done — v4 (kumulált)

### T-01 — Race Condition Load Teszt
- [ ] 50 concurrent `AuditEvent` → 0 `PreviousHash` duplikátum
- [ ] `verify-chain` → `IsValid: true` a 50 rekordra
- [ ] Advisory lock single-instance constraint dokumentálva XML doc + ADR-005
- [ ] CI pipeline-ban fut

### T-02 — PostgreSQL Role + RLS + Context Separation
- [ ] `spaceos_schema_owner` NOLOGIN role — `AuditEvents` owner (nem `spaceos_app`)
- [ ] `FORCE RLS` tényleges hatása ellenőrizve: `spaceos_app` nem owner
- [ ] `current_setting` `missing_ok=true` + `COALESCE` nil-UUID policy
- [ ] `AuditDbContext`: csak `AuditEvents` tábla, saját migration history
- [ ] `AppDbContext`: `AuditEvents` kizárva (`Ignore` vagy `ToSqlQuery`)
- [ ] Migration futtatva: `--context AuditDbContext` explicit
- [ ] Integration teszt: cross-context RLS — mindkét context tenant-izolált
- [ ] `AuditEvents DELETE spaceos_app-ként` → `permission denied`

### T-03 — JWT ES256 + RefreshToken + IConfigureOptions
- [ ] `JWT_SIGNING_KEY` env var törölve — `grep 0 találat`
- [ ] EC P-256 keypair: `jwt_ec_private.pem` (640) + `jwt_ec_public.pem` (644)
- [ ] `ConfigureJwtBearerOptions : IConfigureNamedOptions` — `BuildServiceProvider()` eltávolítva
- [ ] `LocalEcKeyProvider : IDisposable` — `ECDsa.Dispose()` implementálva
- [ ] Access token lifetime: **15 perc**, ES256
- [ ] `RefreshToken` entity + `RefreshTokenConfiguration` + Migration 0013
- [ ] `RefreshTokenService`: `GenerateOpaqueToken` + `HashToken` + `VerifyToken` (`FixedTimeEquals`)
- [ ] `POST /api/auth/refresh`: `RefreshTokenCommand` + Validator + Handler + companion test
- [ ] `POST /api/auth/logout`: `RevokeTokenCommand` + idempotens handler + companion test
- [ ] `/.well-known/jwks.json`: `CacheOutput("jwks-cache")` + `RequireRateLimiting("fixed")`
- [ ] `AddOutputCache()` regisztrálva — `AddMemoryCache()` **nincs**
- [ ] `ClockSkew = TimeSpan.Zero`
- [ ] Összes `Api.Tests` ES256 token-nel fut — 0 fail

### T-04 — TenantId JWT Claim + Interceptor
- [ ] `TenantSessionInterceptor : DbConnectionInterceptor` implementálva
- [ ] `ConnectionOpened`: `set_config is_local=false`
- [ ] `ConnectionClosing`: `set_config` reset — pool leak megelőzve
- [ ] `AppDbContext` + `AuditDbContext`: interceptor regisztrálva
- [ ] `TenantContextMiddleware` törölve
- [ ] Teszt: cross-context tenant isolation — mindkét context ugyanazt a `TenantId`-t látja
- [ ] Header manipulation: `X-Tenant-Id` header nem változtatja a `TenantId`-t
- [ ] Hiányzó `tenant_id` claim → 401

### T-05 — Hash Sink
- [ ] `spaceos_audit_sink` DB + `hash_chain_records` tábla létezik
- [ ] `spaceos_sink_writer`: INSERT only — SELECT/UPDATE/DELETE → `permission denied`
- [ ] `AddDbContextFactory<HashSinkDbContext>()` — Scoped lifetime eliminated
- [ ] `ConnectionStringOptions.ValidateOnStart()` — startup fail-fast
- [ ] Migration: `--context HashSinkDbContext --connection` explicit
- [ ] Prometheus: `spaceos_hashsink_write_total` + `spaceos_hashsink_write_failures_total`
- [ ] `verify-chain` divergencia detektál manipuláció után
- [ ] Sink hiba → fő audit folyamat nem szakad meg
- [ ] Escrow feature flag: **OFF** — upgrade gate dokumentálva

### Összesített
- [ ] Meglévő **1027 teszt** zöld
- [ ] Phase 1.5 v4 új tesztek: **≥ 45 db**
- [ ] 0 build warning (xUnit1051 kivételével — ismert)
- [ ] `dotnet list package --vulnerable` → 0 high/critical

---

## 11. Security Adósság — Státusz Phase 1.5 v4 után

| ID | Tétel | Phase 1 | Phase 1.5 v4 | Marad |
|---|---|---|---|---|
| P0-1 | JWT ES256 | ❌ | ✅ T-03 | — |
| P0-2 | Hash Chain Sink (PostgreSQL) | ❌ | ✅ T-05 | — |
| P0-3 | Race condition load teszt | ❌ | ✅ T-01 | — |
| P0-4 | PG audit role + RLS (owner fix + missing_ok) | ❌ | ✅ T-02 | — |
| P1-1 | TenantId JWT claim + interceptor | ❌ | ✅ T-04 | — |
| P1-2 | ExternalAuthToken → KV ref | ❌ | ❌ | Phase 2 |
| P1-3 | AggregateSnapshot | ❌ | ❌ | Phase 2 |
| P1-4 | Outbox Pattern | ❌ | ❌ | Phase 2 |
| P1-5 | IntentDataJson schema | ❌ | ❌ | Phase 2 |
| P1-6 | Identity rate limiting (Redis) | ❌ | ❌ | Phase 2 |
| P1-7 | Threat Model (STRIDE) | ❌ | ❌ | Phase 2 párhuzamos |
| P1-8 | ProofHash + WORM | ❌ | ❌ | Phase 2 |
| Sink upgrade | S3 Object Lock / Azure Immutable Blob | ❌ | ❌ | **Escrow GA gate** |
| P2-x | Chain verifier, Snapshots, GDPR, Alerting | ❌ | ❌ | Phase 3+ |

> ✅ **Phase 1.5 v4 után: Escrow production deployment minimum TELJESÍTVE**  
> P0-1..P0-4 + P1-1 mind lezárva. Escrow feature flag ON-ra kapcsolható **MIUTÁN** a hash sink upgrade (S3 / Azure Immutable Blob) is teljesül. Ez az egyetlen maradék explicit gate az Escrow GA előtt.

---

## 12. Mi Jön Phase 1.5 Után

| Fázis | Tartalom | Blokkoló feltétel |
|---|---|---|
| **Phase 2 — Tool Registry** | AI chat valódi Kernel adatokat kérdez le | Phase 1.5 kész |
| **Phase 2 (párhuzamos) — P1 security** | ExternalAuthToken → KV ref, IntentDataJson schema, Identity rate limiting | Phase 1.5 kész |
| **Phase 3 — Modules.Joinery MVP** | Ajtóméretek, anyaglista, vágási terv. Architecture-first. | Phase 2 kész |
| **Horizon 2 — Sink upgrade** | `PostgresHashSink` → S3 Object Lock / Azure Immutable Blob | Escrow GA előtt |
| **Horizon 2 — RFC 3161 TSA** | DigiCert / GlobalSign EU timestamping | Escrow GA előtt |

---

*SpaceOS · Sprint D · Phase 1.5 v4 · `/database-designer` + `/senior-security` + `/senior-backend` reviewed · 2026-04-06*
