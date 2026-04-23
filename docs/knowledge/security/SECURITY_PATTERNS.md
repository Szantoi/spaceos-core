# SpaceOS — Security Patterns

> Forrás: Sprint D Phase 1.5 security hardening + Sprint 6 (Q2 Pre-launch) security review eredmények.
> Referencia üzenetek: [MSG-K021], [MSG-K031], [MSG-KC01], [MSG-KERNEL-081], [MSG-KERNEL-082],
> [MSG-ABSTRACTIONS-006], [MSG-ABSTRACTIONS-007], [MSG-CUTTING-008], [MSG-E2E-028]

---

## 1. JWT / RBAC minták

### 1.1 MapInboundClaims = false (KRITIKUS)

**Találat:** ASP.NET Core JwtBearer alapértelmezetten átnevezi a JWT claim-eket SOAP URI ekvivalensekre.
`"tid"` → `http://schemas.microsoft.com/identity/claims/tenantid` → `ClaimsTenantResolver.TryResolve()` null-t kap. [MSG-KERNEL-082]

**Fix:**
```csharp
// Program.cs — AddJwtBearer konfiguráláskor
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;  // ← KÖTELEZŐ
        options.Authority = builder.Configuration["JWT_AUTHORITY"] ?? "https://spaceos-kernel";
        options.Audience  = builder.Configuration["JWT_AUDIENCE"]  ?? "spaceos-kernel-api";
    });
```

**Szabály:** Minden .NET szolgáltatásban, ahol JWT-t validálunk, `MapInboundClaims = false` kötelező.

### 1.2 ValidateAudience = true

**Találat (MEDIUM-01):** Abstractions modul `ValidateAudience = false` volt — más audience-re kiadott érvényes token (pl. portal, orchestrator) is elfogadásra kerülhetett. [MSG-ABSTRACTIONS-006]

**Fix:**
```csharp
opts.TokenValidationParameters = new TokenValidationParameters
{
    ValidateAudience = true,
    ValidAudience = builder.Configuration["Jwt:Audience"]
        ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
        ?? "kernel-api",
};
```

**appsettings.json kiegészítés:**
```json
{
  "Jwt": {
    "Authority": "https://joinerytech.hu/auth/realms/spaceos",
    "Audience": "kernel-api"
  }
}
```

### 1.3 tid claim értelmezés

A Keycloak `spaceos_tenants` claim **double-serialized** JSON string formátumban érkezik:
```json
"spaceos_tenants": "[{\"tenant_id\":\"a1b2c3d4-...\",\"tenant_type\":\"Manufacturer\",\"enabled_modules\":[\"door\"],\"brand_skin\":\"doorstar\"}]"
```

**snake_case kulcsok** (nem camelCase!): `tenant_id`, `tenant_type`, `enabled_modules`, `brand_skin`.

`ClaimsTenantResolver` prioritás-sorrend (Kernel): [MSG-KERNEL-062]
1. `spaceos_tenants` JSON array claim → `tenant_id` kulcs
2. fallback: `tid` flat claim
3. ha egyik sem → `DenyWebRequestSentinel`

### 1.4 DenyWebRequestSentinel minta

**Probléma (security gap):** Ha a `tid` / `spaceos_tenants` claim hiányzik, a régi kód `null`-t adott vissza, az EF Core query filter null esetén mindent átengedett. [MSG-KERNEL-081]

**Fix:**
```csharp
internal static readonly TenantId DenyWebRequestSentinel =
    TenantId.From(Guid.Parse("00000000-0000-0000-0000-000000000002"));

// TryResolve() logika:
// HttpContext != null + claim hiányzik → DenyWebRequestSentinel (nem null!)
// HttpContext == null (háttérfolyamat) → null (bypass — migration, job)
```

**AppDbContext query filter (3 eset):**
- `CurrentTenantGuid == null` → bypass (háttér)
- `CurrentTenantGuid == DenySentinel` → üres eredmény (deny)
- `CurrentTenantGuid == realGuid` → tenant-szűrés (helyes)

---

## 2. Row-Level Security (RLS)

### 2.1 RLS beállítás minta

Minden tenant-izolált táblán kötelező:
```sql
ALTER TABLE "TableName" ENABLE ROW LEVEL SECURITY;
ALTER TABLE "TableName" FORCE ROW LEVEL SECURITY;

CREATE POLICY tenant_isolation ON "TableName"
  FOR ALL
  USING (
    "TenantId" = COALESCE(
      NULLIF(current_setting('app.current_tenant_id', true), ''),
      '00000000-0000-0000-0000-000000000000'
    )::uuid
  );
```

**Fontos:** `current_setting('app.current_tenant_id', true)` — a `true` paraméter (`missing_ok`) kötelező, különben exception ha a GUC nincs beállítva. [MSG-K021 T-02]

### 2.2 DenyWebRequestSentinel az RLS-ben

Ha a claim hiányzik, a sentinel GUID (`000...002`) kerül a GUC-ba → az RLS filter soha nem igaz → üres eredmény. Ez defense-in-depth: DB-szintű védelmet biztosít app-szintű bug esetén is.

### 2.3 Tábla owner — FORCE RLS bypass megelőzése

**KRITIKUS (SEC-P15-07):** Ha `spaceos_app` az `AuditEvents` owner, a `FORCE ROW LEVEL SECURITY` bypass-olható a PostgreSQL rules szerint. [MSG-K021 T-02]

**Fix:**
```sql
DO $$ BEGIN
  IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = 'spaceos_schema_owner') THEN
    CREATE ROLE spaceos_schema_owner NOLOGIN;
  END IF;
END $$;

ALTER TABLE "AuditEvents" OWNER TO spaceos_schema_owner;
GRANT SELECT, INSERT, UPDATE, DELETE ON "AuditEvents" TO spaceos_app;
```

**Szabály:** Minden új tábla `spaceos_schema_owner` NOLOGIN role tulajdonában kell legyen.

### 2.4 GUC regisztráció — ALTER DATABASE

A `app.current_tenant_id` GUC-ot regisztrálni kell az adatbázisban, különben `42704 unrecognized configuration parameter` hibát kap az interceptor. [MSG-E2E-044]

```sql
ALTER DATABASE spaceos SET "app.current_tenant_id" TO '';
```

Ez egy egyszeri init lépés, nem a migration része.

---

## 3. TenantSessionInterceptor / DbConnectionInterceptor minta

### 3.1 DbConnectionInterceptor (Kernel minta)

```csharp
// Infrastructure/Persistence/TenantSessionInterceptor.cs : DbConnectionInterceptor
public override async Task ConnectionOpenedAsync(
    DbConnection connection, ConnectionEndEventData eventData, CancellationToken ct = default)
{
    var tenantId = _httpContextAccessor.HttpContext?.User
        .FindFirst("spaceos_tenants")?.Value; // ClaimsTenantResolver
    if (!string.IsNullOrEmpty(tenantId))
    {
        await connection.ExecuteAsync(
            "SELECT set_config('app.current_tenant_id', @val, false)",
            new { val = tenantId });
    }
}

public override async Task ConnectionClosingAsync(
    DbConnection connection, ConnectionEventData eventData, CancellationToken ct = default)
{
    await connection.ExecuteAsync(
        "SELECT set_config('app.current_tenant_id', '', false)");
}
```

`false` = session-szintű GUC (nem `is_local=true`, ami csak tranzakcióban él). [MSG-K021 T-04]

### 3.2 Read-path RLS — DbCommandInterceptor

A `SaveChangesInterceptor` csak write-path-en fut. Olvasáskor is kell a GUC: [MSG-ABSTRACTIONS-006 MEDIUM-02]

```csharp
// TenantCommandInterceptor extends DbCommandInterceptor
public override InterceptionResult<DbDataReader> ReaderExecuting(
    DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
{
    SetTenantGuc(command.Connection);
    return result;
}
```

### 3.3 OpenConnectionAsync affinity fix (JOINERY-014 / CUTTING-015 minta)

Ha a GUC-t és az adatlekérést biztosítani kell ugyanazon fizikai connection-ön:
```csharp
if (dbContext.Database.IsRelational())
{
    await dbContext.Database.OpenConnectionAsync(ct);
    await dbContext.Database.ExecuteSqlAsync(
        $"SELECT set_config('app.current_tenant_id', {tenantIdStr}, false)", ct);
}
try
{
    result = await repo.SomeOperationAsync(ct);
}
finally
{
    if (dbContext.Database.IsRelational())
        await dbContext.Database.CloseConnectionAsync();
}
```

**Mikor kell:** Belső (internal) endpointokon ahol a request pipeline nem fut végig (nincs JWT auth → nincs interceptor), de manuálisan kell a tenant kontextus. [MSG-CUTTING-015]

---

## 4. SSRF allowlist regex minta

A `StageDefinition.Register()` a `ModuleEndpoint` URL-t validálja: [MSG-K054-DONE SEC-01]

```csharp
// Domain szintű validáció:
private static readonly Regex ModuleEndpointRegex =
    new(@"^https?://(127\.0\.0\.1|localhost):(50[0-9]{2})$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

// Register() factory metódusban:
if (!ModuleEndpointRegex.IsMatch(endpoint))
    throw new DomainException("ModuleEndpoint csak loopback+port 50xx lehet");
```

---

## 5. err.message szivárgás és NODE_ENV guard (Orchestrator)

**Finding (Sprint 6):** Az Orchestrator error handler-ek `err.message`-t küldhetnek vissza a kliensnek — internal server hiba esetén stack trace szivárog.

**Pattern:**
```typescript
// src/middleware/error.middleware.ts
app.use((err: Error, req: Request, res: Response, next: NextFunction) => {
    const isDev = process.env.NODE_ENV !== 'production';
    res.status(500).json({
        error: 'Internal Server Error',
        message: isDev ? err.message : undefined  // production-ban nem szivárog
    });
});
```

---

## 6. pageSize clamp minta

**Finding:** Pagination paraméter maximuma nincs korlátozva → DoS lehetőség. [MSG-K025 3.]

**FluentValidation:**
```csharp
RuleFor(x => x.PageSize)
    .GreaterThan(0)
    .LessThanOrEqualTo(50);  // max 50
```

**Abstractions Graph Engine limit:** max 200 slot, max 500 connection per template. [MSG-ABSTRACTIONS-006]

---

## 7. Axios CVE fix (1.15.0)

**Finding (Sprint 6 Orchestrator dependency review):** Axios SSRF/request hijacking CVE.

**Fix:** `npm install axios@1.15.0` (vagy latest patch).

---

## 8. Vite CVE — elfogadott kockázat

**CVEs:** GHSA-4w7w-66w2-5vf9, GHSA-v2wj-q39q-566r, GHSA-p9ff-h696-f583 (Vite dev server). [MSG-E2E-028]

**Kockázatelfogadás:** Mindhárom CVE a Vite **dev servert** érinti (WebSocket, `server.fs.deny`, `.map` path traversal). `vitest run` módban dev server nem indul → production kockázat: 0.

**Fix:** `vite@7.3.2`-re frissítve az E2E projektben (npm audit után: 0 vulnerability).

---

## 9. Internal service-to-service auth — X-SpaceOS-Internal guard

**Finding (ORCH Track C, SEC-01):** Service-to-service hívásokhoz (Joinery → Orchestrator `/bff/internal/*`) nem elegendő a JWT — service identity szükséges.

**Pattern:**
```typescript
// src/middleware/internal.middleware.ts
export function requireInternalHeader(req, res, next) {
    if (req.headers['x-spaceos-internal'] !== 'true') {
        console.warn(`[SEC-01] Blocked unauthorized internal access: ${req.method} ${req.path}`);
        return res.status(403).json({ error: 'Forbidden' });
    }
    next();
}
```

**Korlát:** Csak belső hálózaton (loopback) biztonságos. Ha a `/bff/internal` prefix publikus hálózatról is elérhető lesz, shared secret vagy mTLS kell.

---

## 10. Keycloak /auth/admin/ blokkolás (Nginx)

```nginx
location /auth/admin/ {
    allow 127.0.0.1;
    deny all;
    proxy_pass http://127.0.0.1:8080/admin/;
}
```

Az admin interfész csak SSH tunnel-en keresztül érhető el. [MSG-INFRA-KC01]
