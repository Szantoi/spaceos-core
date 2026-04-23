# SpaceOS — Database Patterns

> PostgreSQL + EF Core mintái. Forrás: Sprint D Phase 1.5 + modulok implementációja.

---

## 1. RLS policy szerkezet (.NET + PostgreSQL)

### Standard tenant isolation policy

```sql
-- Minden tenant-izolált táblán kötelező:
ALTER TABLE "TableName" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "TableName" FORCE ROW LEVEL SECURITY;
ALTER TABLE "TableName" OWNER TO spaceos_schema_owner;  -- nem spaceos_app!

CREATE POLICY tenant_isolation ON "TableName"
  FOR ALL
  USING (
    "TenantId" = COALESCE(
      NULLIF(current_setting('app.current_tenant_id', true), ''),
      '00000000-0000-0000-0000-000000000000'
    )::uuid
  );
```

**Magyarázat:**
- `current_setting('app.current_tenant_id', true)` — `true` = missing_ok, nem dob exception ha nincs beállítva
- `NULLIF(..., '')` — üres string esetén NULL-ra konvertál
- `COALESCE(..., '000...000')` — NULL esetén nil-UUID → policy soha nem teljesül → üres eredmény
- `FORCE ROW LEVEL SECURITY` — még a tábla ownerére is érvényes (DE csak ha nem `spaceos_app` az owner!)

[MSG-K021 T-02]

### AuditWriter bypass (audit-specifikus)

```sql
-- Audit write service bypass (csak INSERT, nincs tenant filter)
CREATE POLICY audit_writer_insert_bypass ON "AuditEvents"
  FOR INSERT TO spaceos_audit_writer
  WITH CHECK (true);
```

---

## 2. GUC set_config DbConnectionInterceptor minta (Kernel)

```csharp
// Infrastructure/Persistence/TenantSessionInterceptor.cs
public sealed class TenantSessionInterceptor : DbConnectionInterceptor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TenantSessionInterceptor(IHttpContextAccessor httpContextAccessor)
        => _httpContextAccessor = httpContextAccessor;

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        var tenantId = ResolveTenantId();
        if (tenantId is not null)
        {
            // SESSION szintű GUC (false = not transaction-local)
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT set_config('app.current_tenant_id', @val, false)";
            cmd.Parameters.Add(new NpgsqlParameter("@val", tenantId.ToString()));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public override async Task ConnectionClosingAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        CancellationToken cancellationToken = default)
    {
        // Pool leak megelőzés: reset minden connection-close-kor
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT set_config('app.current_tenant_id', '', false)";
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private Guid? ResolveTenantId()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context is null) return null;  // háttérfolyamat — bypass
        
        // spaceos_tenants claim (Keycloak) elsőbbsége
        var claims = context.User.FindAll("spaceos_tenants");
        foreach (var claim in claims)
        {
            try
            {
                using var doc = JsonDocument.Parse(claim.Value);
                if (doc.RootElement.TryGetProperty("tenant_id", out var idEl))
                    if (Guid.TryParse(idEl.GetString(), out var g)) return g;
            }
            catch { /* continue */ }
        }
        
        // tid claim fallback
        var tidClaim = context.User.FindFirst("tid")?.Value;
        if (Guid.TryParse(tidClaim, out var tidGuid)) return tidGuid;
        
        return null;
    }
}
```

**DI regisztráció:**
```csharp
services.AddSingleton<TenantSessionInterceptor>();
services.AddDbContext<AppDbContext>((sp, opt) =>
    opt.UseNpgsql(connStr)
       .AddInterceptors(sp.GetRequiredService<TenantSessionInterceptor>()));
```

[MSG-K021 T-04]

---

## 3. Migration naming konvenció SpaceOS-ban

**Format:** `YYYYMMDDHHMMSS_Migration_NNNN_SlugNeve`

**Példák:**
- `20260406000000_Migration_0011_SourceBrand`
- `20260407000000_Migration_0012_HashSink`
- `20260407000000_Migration_0013_RefreshTokens`
- `20260410130000_Migration_0028_StageRegistry`

**Raw SQL migration minta (suppressTransaction):**

Ha a migration nem futhat tranzakcióban (pl. CREATE INDEX CONCURRENTLY, RLS policy-k):
```csharp
[DbContext(typeof(AppDbContext))]
[Migration("20260410130000_Migration_0028_StageRegistry")]
public partial class Migration_0028_StageRegistry : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE IF NOT EXISTS "StageDefinitions" (
                ...
            );
            ALTER TABLE "StageDefinitions" ENABLE ROW LEVEL SECURITY;
            ALTER TABLE "StageDefinitions" FORCE ROW LEVEL SECURITY;
            ...
        """, suppressTransaction: true);  // ← fontos
    }
}
```

**Designer.cs minta raw SQL migration esetén:**
```csharp
// Raw SQL migration-öknél a Designer.cs stub lehet (0025-0027 minta):
[DbContext(typeof(AppDbContext))]
[Migration("20260410130000_Migration_0028_StageRegistry")]
partial class Migration_0028_StageRegistry
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder) { }
}
```

[MSG-KERNEL-061-DONE]

---

## 4. Testcontainers integráció (SEC-01/SEC-02 minta)

```csharp
// Integration teszt base class
public abstract class IntegrationTestBase : IAsyncLifetime
{
    protected PostgreSqlContainer _db = new PostgreSqlBuilder()
        .WithImage("postgres:16")
        .WithDatabase("spaceos_test")
        .WithUsername("spaceos_test")
        .WithPassword("test_password")
        .Build();

    public async Task InitializeAsync()
    {
        await _db.StartAsync();
        // EF Core migration futtatás
        var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
                builder.ConfigureAppConfiguration((_, config) =>
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:DefaultConnection"] = _db.GetConnectionString()
                    })));
        var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}

// SEC-01 teszt minta (RLS tenant isolation)
[Fact]
public async Task Tenant_A_Cannot_See_Tenant_B_Data()
{
    // Arrange: Tenant A adatot hoz létre
    var tenantAToken = CreateJwt(tenantId: _tenantA);
    var client = _factory.CreateClientWithToken(tenantAToken);
    
    // Act: Tenant B tokennel lekérdezi
    var tenantBClient = _factory.CreateClientWithToken(CreateJwt(tenantId: _tenantB));
    var response = await tenantBClient.GetAsync($"/api/facilities/{facilityId}");
    
    // Assert
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
}
```

[MSG-K021 T-02 integration tests]

---

## 5. DEFAULT PRIVILEGES PostgreSQL séma grantoknál

Új sémában a future objektumokra is szükséges a grant:

```sql
-- Egyszeri init minden új DB-nél:
GRANT USAGE ON SCHEMA public TO spaceos;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO spaceos;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO spaceos;

-- Jövőbeli objektumok (EF migrations által létrehozandók):
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO spaceos;
ALTER DEFAULT PRIVILEGES IN SCHEMA public
    GRANT USAGE, SELECT ON SEQUENCES TO spaceos;
```

Modul-specifikus sémák (pl. `spaceos_modules`):
```sql
GRANT USAGE ON SCHEMA spaceos_modules TO spaceos;
-- ... (ugyanaz a minta)
ALTER DEFAULT PRIVILEGES IN SCHEMA spaceos_modules
    GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO spaceos;
```

[MSG-INFRA-112 pattern]

---

## 6. WORM hash sink tábla minta

```sql
CREATE TABLE IF NOT EXISTS "AuditHashes" (
    "TenantId"   uuid        NOT NULL,
    "BlockIndex" bigint      NOT NULL,
    "Hash"       varchar(64) NOT NULL,
    "CreatedAt"  timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT "PK_AuditHashes" PRIMARY KEY ("TenantId", "BlockIndex")
);

ALTER TABLE "AuditHashes" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "AuditHashes" FORCE ROW LEVEL SECURITY;

CREATE POLICY "rls_audit_hashes_tenant"
    ON "AuditHashes" USING (
        "TenantId" = COALESCE(
            NULLIF(current_setting('app.current_tenant_id', TRUE), '')::uuid,
            '00000000-0000-0000-0000-000000000001'::uuid));

-- spaceos_audit_worm role: CSAK INSERT
CREATE ROLE spaceos_audit_worm NOLOGIN;
REVOKE ALL ON "AuditHashes" FROM spaceos_audit_worm;
GRANT INSERT ON "AuditHashes" TO spaceos_audit_worm;
```

Migration neve: `Migration_0027_AuditHashesWorm`, `suppressTransaction: true`. [MSG-K031 Track C]

---

## 7. Advisory lock MD5 int64 key minta

**Probléma (SEC-06):** `pg_advisory_xact_lock(hashtext(@tenantId))` — `hashtext()` int4 értéket ad → 50% kollízió esély 10k+ tenant felett.

**Fix:**
```csharp
// PostgresAdvisoryAuditWriteLock.cs
var md5Bytes = MD5.HashData(Encoding.UTF8.GetBytes(tenantId.ToString()));
var lockKey  = BitConverter.ToInt64(md5Bytes, 0);
await connection.ExecuteAsync(
    "SELECT pg_advisory_xact_lock(@lockKey)", 
    new { lockKey });
```

[MSG-K031 Track B]

---

## 8. IDbContextFactory minta (HashSink)

```csharp
// AddDbContext helyett AddDbContextFactory eliminates Scoped lifetime issues:
services.AddDbContextFactory<HashSinkDbContext>(opt =>
    opt.UseNpgsql(
        config["ConnectionStrings:AuditSink"],
        npg => npg.EnableRetryOnFailure()));

// Használat (PostgresHashSink service):
public sealed class PostgresHashSink : IWormStorageService
{
    private readonly IDbContextFactory<HashSinkDbContext> _factory;
    
    public async Task AppendAsync(Guid tenantId, long blockIndex, string hash, CancellationToken ct)
    {
        using var ctx = await _factory.CreateDbContextAsync(ct);
        ctx.AuditHashes.Add(new AuditHash { TenantId = tenantId, BlockIndex = blockIndex, Hash = hash });
        await ctx.SaveChangesAsync(ct);
    }
}
```

[MSG-K021 T-05]

---

## 9. try_cast_uuid PostgreSQL immutable függvény

```sql
-- Migration 0019 tartalmazza (Spatial BIM):
CREATE OR REPLACE FUNCTION try_cast_uuid(p_input TEXT)
RETURNS uuid LANGUAGE plpgsql IMMUTABLE AS $$
BEGIN
  BEGIN
    RETURN p_input::uuid;
  EXCEPTION WHEN others THEN
    RETURN NULL;
  END;
END;
$$;
```

Minden raw SQL query-ban malformed UUID-ok kezelésére. [MSG-K025 SEC-P3A-01]

---

## 10. EF Core query filter minta (TenantId)

```csharp
// AppDbContext.OnModelCreating — minden tenant-izolált entitáson:
modelBuilder.Entity<Facility>()
    .HasQueryFilter(f => f.TenantId == CurrentTenantGuid 
                         || CurrentTenantGuid == null);  // háttérfolyamat bypass
```

**DenyWebRequestSentinel logikával:**
```csharp
// Háromféle visszatérési érték ClaimsTenantResolver-től:
private TenantId? CurrentTenantId => _tenantResolver.TryResolve();

// AppDbContext belső logika:
// null     → CurrentTenantGuid = null  → filter bypass (háttérfolyamat)
// sentinel → CurrentTenantGuid = 000...002 → filter mindig false → 0 sor
// valódi   → CurrentTenantGuid = valódi GUID → tenant-szűrés
```

[MSG-KERNEL-081-DONE]
