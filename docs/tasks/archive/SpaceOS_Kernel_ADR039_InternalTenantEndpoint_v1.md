# SpaceOS Kernel — ADR-039 Internal Tenant Endpoint
## `GET /api/internal/tenants/{id}` — Minimal-Info Actor Directory

**Státusz:** IMPLEMENTÁCIÓRA KÉSZ  
**Scope:** `spaceos-kernel` repo — önálló PR  
**Blokkolja:** Sales `LinkCustomerToActorCommandHandler` (ADR-039 read path)  
**Referencia:** `SpaceOS_Modules_Sales_Architecture_v4.md` §7.3 · SEC-S-09 · SEC-S-10

---

## 1. Kontextus

A Sales modul Customer→Platform Actor link validálásához (ADR-039 read-ág, sync loopback) a Kernel egy internal-only endpointot biztosít. A Sales `KernelActorDirectoryClient` (`IActorDirectoryPort` impl) ezt az endpointot hívja szinkron HTTP loopback-on (`http://127.0.0.1:5000/api/internal/tenants/{id}`).

**Auth / Integritás:**
- `X-SpaceOS-Internal` shared secret header kötelező — hiányzó/rossz → **401**
- `X-SpaceOS-TenantId` header = a requester tenant azonosítója
- `{id}` route param = a target tenant azonosítója
- nginx **NEM** exponálja kifelé (loopback-only)

**Válasz (SEC-S-09 — minimal-info, SEMMI kontakt/billing adat):**

```json
{
  "tenantId": "...",
  "tenantType": "Manufacturer",
  "displayName": "Partner Kft.",
  "hasVerifiedHandshakeWithRequester": true
}
```

- `hasVerifiedHandshakeWithRequester` = van-e bejegyzés a `TenantHandshakeAllowlist`-ben a requester és target között (bármely irányban)
- **404** ha a target tenant nem létezik (PII-mentes hibaválasz — SEC-S-10)
- Minden hívás audit-logba: `(requesterTenantId → targetTenantId)` pár + timestamp + eredmény

---

## 2. Architekturális döntések

| Döntés | Választás | Indok |
|---|---|---|
| `InternalHeaderMiddleware` scope | `app.UseWhen(path.StartsWithSegments("/api/internal"), ...)` | Kernel JWT auth megmarad az összes többi `/api/*` route-on |
| `hasVerifiedHandshakeWithRequester` forrása | `TenantHandshakeAllowlist` — `(A→B) OR (B→A)` bármelyik irány | Bidirektív jelenlét = ismert platform-kapcsolat |
| Audit tábla | Új `InternalAccessAuditLog` entitás, RLS nélkül, GRANT INSERT+SELECT only | Cross-tenant rekord — tenant_isolation RLS nem alkalmazható |
| Migration szám | `0030` — **Claude Code verify-elje az aktuális legutolsó migrációt** | Kernel 0029-ig dokumentált; közte lehetnek újak |
| Audit failure kezelése | Swallowed + `LogCritical` — soha nem blokkolja a read response-t | Availability > audit completeness az olvasási ágon |

---

## 3. Solution struktúra (új fájlok)

```
SpaceOS.Kernel.Application/
  Internal/
    Queries/
      GetTenantActorQuery.cs
      GetTenantActorQueryHandler.cs
    Dtos/
      TenantActorResponse.cs
    Ports/
      IInternalAccessAuditWriter.cs
      IB2BHandshakeVerifier.cs
    Specifications/
      TenantByIdForInternalSpec.cs

SpaceOS.Infrastructure/
  Internal/
    InternalAccessAuditEntry.cs
    InternalAccessAuditWriter.cs
    B2BHandshakeVerifier.cs
  Security/
    InternalHeaderMiddleware.cs       ← ha még nincs a Kernel-ben
  Persistence/
    Configurations/
      InternalAccessAuditEntryConfiguration.cs
    Migrations/
      0030_AddInternalAccessAuditLog.cs
    KernelDbContext.cs                ← diff: DbSet hozzáadva

SpaceOS.Kernel.Api/
  Endpoints/
    InternalEndpoints.cs
  Program.cs                          ← diff: UseWhen + MapInternalEndpoints + DI

SpaceOS.Kernel.Tests/
  Internal/
    GetTenantActorQueryHandlerTests.cs
    InternalHeaderMiddlewareTests.cs
```

---

## 4. Application Layer

### 4.1 Query

```csharp
// Application/Internal/Queries/GetTenantActorQuery.cs

using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Internal.Dtos;

namespace SpaceOS.Kernel.Application.Internal.Queries;

/// <summary>
/// Internal cross-module read — returns minimal tenant actor info for ADR-039 read path.
/// SEC-S-09: response contains no contact, billing, or PII fields.
/// </summary>
/// <param name="RequesterTenantId">Tenant performing the lookup (X-SpaceOS-TenantId header).</param>
/// <param name="TargetTenantId">Tenant being looked up (route {id}).</param>
public sealed record GetTenantActorQuery(
    Guid RequesterTenantId,
    Guid TargetTenantId) : IRequest<Result<TenantActorResponse>>;
```

### 4.2 DTO

```csharp
// Application/Internal/Dtos/TenantActorResponse.cs

namespace SpaceOS.Kernel.Application.Internal.Dtos;

/// <summary>
/// Minimal tenant actor info for cross-module directory lookups (ADR-039 read path).
/// SEC-S-09 INVARIANT: This DTO MUST NOT contain contact, billing, address,
/// tax number, email, phone, or any other PII field.
/// Review gate on every PR that touches this type.
/// </summary>
public sealed record TenantActorResponse(
    Guid TenantId,
    string TenantType,
    string DisplayName,
    bool HasVerifiedHandshakeWithRequester);
```

### 4.3 Ports

```csharp
// Application/Internal/Ports/IInternalAccessAuditWriter.cs

namespace SpaceOS.Kernel.Application.Internal.Ports;

/// <summary>
/// Append-only audit writer for internal actor directory lookups (SEC-S-09).
/// Every call to GET /api/internal/tenants/{id} is recorded regardless of outcome.
/// </summary>
public interface IInternalAccessAuditWriter
{
    /// <param name="result">"Found" | "NotFound"</param>
    Task RecordAsync(
        Guid requesterTenantId,
        Guid targetTenantId,
        string result,
        CancellationToken ct);
}
```

```csharp
// Application/Internal/Ports/IB2BHandshakeVerifier.cs

namespace SpaceOS.Kernel.Application.Internal.Ports;

/// <summary>
/// Queries the Kernel handshake registry for a verified platform-level
/// relationship between two tenants (ADR-039 read path, SEC-S-09).
/// </summary>
public interface IB2BHandshakeVerifier
{
    /// <summary>
    /// Returns true if any allowlisted entry exists in either direction
    /// (tenantA → tenantB or tenantB → tenantA).
    /// </summary>
    Task<bool> HasVerifiedHandshakeAsync(
        Guid tenantA,
        Guid tenantB,
        CancellationToken ct);
}
```

### 4.4 Specification

```csharp
// Application/Internal/Specifications/TenantByIdForInternalSpec.cs

using Ardalis.Specification;
using SpaceOS.Kernel.Domain.Aggregates;

namespace SpaceOS.Kernel.Application.Internal.Specifications;

/// <summary>
/// Read-only single-tenant spec for the internal actor directory endpoint.
/// AsNoTracking — mutation is never performed via this path.
/// </summary>
public sealed class TenantByIdForInternalSpec : SingleResultSpecification<Tenant>
{
    public TenantByIdForInternalSpec(Guid tenantId)
    {
        Query
            .Where(t => t.Id == tenantId && !t.IsArchived)
            .AsNoTracking();
    }
}
```

### 4.5 Handler

```csharp
// Application/Internal/Queries/GetTenantActorQueryHandler.cs

using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Application.Internal.Dtos;
using SpaceOS.Kernel.Application.Internal.Ports;
using SpaceOS.Kernel.Application.Internal.Specifications;
using SpaceOS.Kernel.Domain.Interfaces;

namespace SpaceOS.Kernel.Application.Internal.Queries;

/// <summary>
/// Handles GET /api/internal/tenants/{id} — ADR-039 read path.
/// SEC-S-09: returns minimal-info DTO; every call is audited.
/// </summary>
public sealed class GetTenantActorQueryHandler(
    ITenantRepository tenants,
    IB2BHandshakeVerifier handshakeVerifier,
    IInternalAccessAuditWriter auditWriter,
    ILogger<GetTenantActorQueryHandler> log)
    : IRequestHandler<GetTenantActorQuery, Result<TenantActorResponse>>
{
    public async Task<Result<TenantActorResponse>> Handle(
        GetTenantActorQuery query, CancellationToken ct)
    {
        var spec = new TenantByIdForInternalSpec(query.TargetTenantId);
        var tenant = await tenants.FirstOrDefaultAsync(spec, ct).ConfigureAwait(false);

        if (tenant is null)
        {
            // SEC-S-10: PII-free audit — no tenant name or details in log/response
            await RecordAndLogAsync(query, "NotFound", ct).ConfigureAwait(false);
            return Result.NotFound();
        }

        var hasHandshake = await handshakeVerifier
            .HasVerifiedHandshakeAsync(query.RequesterTenantId, query.TargetTenantId, ct)
            .ConfigureAwait(false);

        await RecordAndLogAsync(query, "Found", ct).ConfigureAwait(false);

        // SEC-S-09 review gate: only TenantId, TenantType, Name, handshake flag — no PII
        return Result.Success(new TenantActorResponse(
            TenantId:                          tenant.Id,
            TenantType:                        tenant.TenantType.ToString(),
            DisplayName:                       tenant.Name,
            HasVerifiedHandshakeWithRequester: hasHandshake));
    }

    private async Task RecordAndLogAsync(
        GetTenantActorQuery query, string result, CancellationToken ct)
    {
        // SEC-S-10: structured log — no tenant name, no payload
        log.LogInformation(
            "InternalActorLookup requester={RequesterTenantId} target={TargetTenantId} result={Result}",
            query.RequesterTenantId, query.TargetTenantId, result);

        try
        {
            await auditWriter
                .RecordAsync(query.RequesterTenantId, query.TargetTenantId, result, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Audit failure must NOT block the read response — log critical, continue
            log.LogCritical(ex,
                "AUDIT WRITE FAILED — InternalActorLookup requester={RequesterTenantId} target={TargetTenantId}",
                query.RequesterTenantId, query.TargetTenantId);
        }
    }
}
```

---

## 5. Infrastructure Layer

### 5.1 Audit Entity

```csharp
// Infrastructure/Internal/InternalAccessAuditEntry.cs

namespace SpaceOS.Kernel.Infrastructure.Internal;

/// <summary>
/// Append-only record for every GET /api/internal/tenants/{id} call (SEC-S-09).
/// No UPDATE, no DELETE — enforced via DB GRANT and EF configuration.
/// RLS disabled: this table records cross-tenant pairs by design.
/// </summary>
public sealed class InternalAccessAuditEntry
{
    public long Id { get; private init; }                    // GENERATED ALWAYS AS IDENTITY
    public Guid RequesterTenantId { get; private init; }
    public Guid TargetTenantId { get; private init; }
    public string Result { get; private init; } = default!;  // "Found" | "NotFound"
    public DateTimeOffset OccurredAt { get; private init; }

    // EF Core
    private InternalAccessAuditEntry() { }

    public static InternalAccessAuditEntry Create(
        Guid requesterTenantId,
        Guid targetTenantId,
        string result,
        DateTimeOffset occurredAt)
        => new()
        {
            RequesterTenantId = requesterTenantId,
            TargetTenantId    = targetTenantId,
            Result            = result,
            OccurredAt        = occurredAt,
        };
}
```

### 5.2 Audit Writer

```csharp
// Infrastructure/Internal/InternalAccessAuditWriter.cs

using SpaceOS.Kernel.Application.Internal.Ports;
using SpaceOS.Kernel.Infrastructure.Persistence;

namespace SpaceOS.Kernel.Infrastructure.Internal;

/// <summary>
/// Writes append-only audit rows for internal tenant actor lookups.
/// Uses a dedicated lightweight DbContext scope to decouple from the main request context.
/// </summary>
public sealed class InternalAccessAuditWriter(IKernelDbContextFactory dbFactory)
    : IInternalAccessAuditWriter
{
    public async Task RecordAsync(
        Guid requesterTenantId,
        Guid targetTenantId,
        string result,
        CancellationToken ct)
    {
        await using var db = dbFactory.CreateAuditContext();

        var entry = InternalAccessAuditEntry.Create(
            requesterTenantId,
            targetTenantId,
            result,
            DateTimeOffset.UtcNow);

        db.InternalAccessAuditLog.Add(entry);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
```

> **Megjegyzés:** Ha a Kernel már rendelkezik `IDbContextFactory<KernelDbContext>`-szel (DI-regisztrálva),
> cseréld le `IKernelDbContextFactory`-t `IDbContextFactory<KernelDbContext>`-re és hívj
> `CreateDbContextAsync(ct)`.

### 5.3 Handshake Verifier

```csharp
// Infrastructure/Internal/B2BHandshakeVerifier.cs

using Microsoft.EntityFrameworkCore;
using SpaceOS.Kernel.Application.Internal.Ports;
using SpaceOS.Kernel.Infrastructure.Persistence;

namespace SpaceOS.Kernel.Infrastructure.Internal;

/// <summary>
/// Queries TenantHandshakeAllowlist (Migration 0029) for a bidirectional
/// platform-level relationship between two tenants.
/// Bidirectional: either A→B or B→A entry counts as a verified connection.
/// </summary>
public sealed class B2BHandshakeVerifier(KernelDbContext db) : IB2BHandshakeVerifier
{
    public async Task<bool> HasVerifiedHandshakeAsync(
        Guid tenantA,
        Guid tenantB,
        CancellationToken ct)
        => await db.TenantHandshakeAllowlist
            .AsNoTracking()
            .AnyAsync(
                h => (h.TenantId == tenantA && h.AllowedTenantId == tenantB)
                  || (h.TenantId == tenantB && h.AllowedTenantId == tenantA),
                ct)
            .ConfigureAwait(false);
}
```

> **Megjegyzés:** Ha a Kernel `TenantHandshakeAllowlist` DbSet neve eltér, a Claude Code agent
> cserélje le. A Migration 0029 DDL-je alapján a tábla neve `TenantHandshakeAllowlist`.

### 5.4 Internal Header Middleware

```csharp
// Infrastructure/Security/InternalHeaderMiddleware.cs
// Ha a Kernel-ben már létezik ilyen fájl, CSAK a Program.cs UseWhen bekötése szükséges.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace SpaceOS.Kernel.Infrastructure.Security;

/// <summary>
/// Guards /api/internal/* routes with a shared secret header (SEC-S-09).
/// Applied via UseWhen — does NOT affect JWT-authenticated /api/* routes.
/// TIMING NOTE: uses string Equals (Ordinal). For full timing-safe comparison
/// upgrade to HMAC-verified secret comparison if threat model requires it.
/// </summary>
public sealed class InternalHeaderMiddleware(RequestDelegate next, IConfiguration config)
{
    private readonly string _expectedSecret =
        config["SpaceOS:InternalSecret"]
        ?? throw new InvalidOperationException("SpaceOS:InternalSecret not configured.");

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-SpaceOS-Internal", out var header)
            || !string.Equals(header.FirstOrDefault(), _expectedSecret, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(
                """{"error":"Unauthorized","detail":"Missing or invalid X-SpaceOS-Internal header."}""",
                context.RequestAborted).ConfigureAwait(false);
            return;
        }

        await next(context).ConfigureAwait(false);
    }
}
```

### 5.5 EF Core Configuration

```csharp
// Infrastructure/Persistence/Configurations/InternalAccessAuditEntryConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Kernel.Infrastructure.Internal;

namespace SpaceOS.Kernel.Infrastructure.Persistence.Configurations;

public sealed class InternalAccessAuditEntryConfiguration
    : IEntityTypeConfiguration<InternalAccessAuditEntry>
{
    public void Configure(EntityTypeBuilder<InternalAccessAuditEntry> builder)
    {
        builder.ToTable("InternalAccessAuditLog");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .UseIdentityAlwaysColumn();
        builder.Property(e => e.RequesterTenantId).IsRequired();
        builder.Property(e => e.TargetTenantId).IsRequired();
        builder.Property(e => e.Result).HasMaxLength(20).IsRequired();
        builder.Property(e => e.OccurredAt).IsRequired();

        builder.HasIndex(e => new { e.RequesterTenantId, e.OccurredAt })
            .HasDatabaseName("IX_InternalAudit_Requester_OccurredAt");
        builder.HasIndex(e => new { e.TargetTenantId, e.OccurredAt })
            .HasDatabaseName("IX_InternalAudit_Target_OccurredAt");
    }
}
```

**KernelDbContext diff — DbSet hozzáadása:**

```csharp
// Infrastructure/Persistence/KernelDbContext.cs — diff
public DbSet<InternalAccessAuditEntry> InternalAccessAuditLog => Set<InternalAccessAuditEntry>();
```

---

## 6. Migration

> ⚠️ **Claude Code**: verify the latest existing migration number before creating this file.
> `0030` feltételezi, hogy `0029` a legutolsó.

```csharp
// Infrastructure/Persistence/Migrations/0030_AddInternalAccessAuditLog.cs

using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SpaceOS.Kernel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddInternalAccessAuditLog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "InternalAccessAuditLog",
            columns: table => new
            {
                Id = table.Column<long>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy",
                        NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                RequesterTenantId = table.Column<Guid>(nullable: false),
                TargetTenantId    = table.Column<Guid>(nullable: false),
                Result            = table.Column<string>(maxLength: 20, nullable: false),
                OccurredAt        = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InternalAccessAuditLog", x => x.Id);
                table.CheckConstraint("CK_InternalAudit_Result",
                    "\"Result\" IN ('Found', 'NotFound')");
            });

        migrationBuilder.CreateIndex(
            name: "IX_InternalAudit_Requester_OccurredAt",
            table: "InternalAccessAuditLog",
            columns: new[] { "RequesterTenantId", "OccurredAt" });

        migrationBuilder.CreateIndex(
            name: "IX_InternalAudit_Target_OccurredAt",
            table: "InternalAccessAuditLog",
            columns: new[] { "TargetTenantId", "OccurredAt" });

        // Append-only enforcement: no UPDATE, no DELETE for the application role.
        // Adjust role name to match the Kernel's production DB role (e.g. spaceos_app).
        migrationBuilder.Sql("""
            REVOKE UPDATE, DELETE ON "InternalAccessAuditLog" FROM spaceos_app;
            GRANT  SELECT, INSERT  ON "InternalAccessAuditLog" TO   spaceos_app;
            """);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "InternalAccessAuditLog");
    }
}
```

---

## 7. API Layer

### 7.1 Endpoint

```csharp
// Api/Endpoints/InternalEndpoints.cs

using Ardalis.Result.AspNetCore;
using MediatR;
using SpaceOS.Kernel.Application.Internal.Queries;

namespace SpaceOS.Kernel.Api.Endpoints;

internal static class InternalEndpoints
{
    /// <summary>
    /// Registers /api/internal/* endpoints.
    /// NOTE: InternalHeaderMiddleware is applied via UseWhen in Program.cs — not here.
    /// nginx MUST NOT expose /api/internal/* to the public internet.
    /// </summary>
    internal static IEndpointRouteBuilder MapInternalEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/internal")
            .WithTags("Internal");

        // ADR-039 read path — Sales Customer→Platform Actor link validation
        group.MapGet("/tenants/{id:guid}", async (
            Guid id,
            HttpContext httpCtx,
            ISender mediator,
            CancellationToken ct) =>
        {
            var requesterHeader = httpCtx.Request.Headers["X-SpaceOS-TenantId"]
                .FirstOrDefault();

            if (!Guid.TryParse(requesterHeader, out var requesterTenantId))
            {
                return Results.Json(
                    new { error = "X-SpaceOS-TenantId header is missing or not a valid GUID." },
                    statusCode: StatusCodes.Status400BadRequest);
            }

            var result = await mediator
                .Send(new GetTenantActorQuery(requesterTenantId, id), ct)
                .ConfigureAwait(false);

            return result.ToMinimalApiResult();
        })
        .WithName("GetTenantActorInternal")
        .Produces<Application.Internal.Dtos.TenantActorResponse>(200)
        .Produces(400)
        .Produces(401)
        .Produces(404)
        .ExcludeFromDescription(); // Do not expose in public Swagger

        return app;
    }
}
```

### 7.2 Program.cs diff

```csharp
// Program.cs — csak az érintett sorok

// 1. UseWhen — internal middleware scope (UseAuthentication ELŐTT)
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api/internal"),
    inner => inner.UseMiddleware<InternalHeaderMiddleware>());

// 2. Endpoint registration (UseAuthentication / UseAuthorization UTÁN)
app.MapInternalEndpoints();

// 3. DI registration (builder.Services section-ben)
builder.Services.AddScoped<IB2BHandshakeVerifier, B2BHandshakeVerifier>();
builder.Services.AddScoped<IInternalAccessAuditWriter, InternalAccessAuditWriter>();
```

---

## 8. Tests

### 8.1 Handler Tests

```csharp
// Tests/Internal/GetTenantActorQueryHandlerTests.cs

using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Internal.Dtos;
using SpaceOS.Kernel.Application.Internal.Ports;
using SpaceOS.Kernel.Application.Internal.Queries;
using SpaceOS.Kernel.Application.Internal.Specifications;
using SpaceOS.Kernel.Domain.Aggregates;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Interfaces;
using Xunit;

namespace SpaceOS.Kernel.Tests.Internal;

public sealed class GetTenantActorQueryHandlerTests
{
    private static readonly Guid RequesterId = Guid.NewGuid();
    private static readonly Guid TargetId    = Guid.NewGuid();

    private readonly Mock<ITenantRepository>          _tenants   = new();
    private readonly Mock<IB2BHandshakeVerifier>      _handshake = new();
    private readonly Mock<IInternalAccessAuditWriter> _audit     = new();

    private GetTenantActorQueryHandler CreateSut() =>
        new(_tenants.Object, _handshake.Object, _audit.Object,
            Microsoft.Extensions.Logging.Abstractions
                .NullLogger<GetTenantActorQueryHandler>.Instance);

    // Helper — uses domain factory; keeps invariants intact
    private static Tenant MakeTenant(string name, TenantType type)
    {
        var result = Tenant.Register(name, brandSkinId: null, tenantType: type);
        return result.Value;
    }

    // ── 1. Target found, handshake = false ───────────────────────────────────
    [Fact]
    public async Task Handle_ExistingTenant_NoHandshake_ReturnsSucessWithFalseFlag()
    {
        var tenant = MakeTenant("Partner Kft.", TenantType.Manufacturer);
        _tenants.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<TenantByIdForInternalSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _handshake.Setup(h => h.HasVerifiedHandshakeAsync(RequesterId, TargetId, default))
            .ReturnsAsync(false);
        _audit.Setup(a => a.RecordAsync(RequesterId, TargetId, "Found", default))
            .Returns(Task.CompletedTask);

        var result = await CreateSut().Handle(
            new GetTenantActorQuery(RequesterId, TargetId), default);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasVerifiedHandshakeWithRequester);
        Assert.Equal("Manufacturer", result.Value.TenantType);
        _audit.Verify(a => a.RecordAsync(RequesterId, TargetId, "Found", default), Times.Once);
    }

    // ── 2. Target found, handshake = true ────────────────────────────────────
    [Fact]
    public async Task Handle_ExistingTenant_WithHandshake_ReturnsTrueFlag()
    {
        var tenant = MakeTenant("Partner Kft.", TenantType.PanelCutter);
        _tenants.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<TenantByIdForInternalSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _handshake.Setup(h => h.HasVerifiedHandshakeAsync(RequesterId, TargetId, default))
            .ReturnsAsync(true);
        _audit.Setup(a => a.RecordAsync(RequesterId, TargetId, "Found", default))
            .Returns(Task.CompletedTask);

        var result = await CreateSut().Handle(
            new GetTenantActorQuery(RequesterId, TargetId), default);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.HasVerifiedHandshakeWithRequester);
        Assert.Equal("PanelCutter", result.Value.TenantType);
    }

    // ── 3. Target tenant not found → NotFound ────────────────────────────────
    [Fact]
    public async Task Handle_NonExistentTarget_ReturnsNotFound()
    {
        _tenants.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<TenantByIdForInternalSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);
        _handshake.Setup(h => h.HasVerifiedHandshakeAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _audit.Setup(a => a.RecordAsync(RequesterId, TargetId, "NotFound", default))
            .Returns(Task.CompletedTask);

        var result = await CreateSut().Handle(
            new GetTenantActorQuery(RequesterId, TargetId), default);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        // Handshake must NOT be called for non-existent tenants
        _handshake.Verify(
            h => h.HasVerifiedHandshakeAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), default),
            Times.Never);
        _audit.Verify(a => a.RecordAsync(RequesterId, TargetId, "NotFound", default), Times.Once);
    }

    // ── 4. Audit failure does NOT propagate ──────────────────────────────────
    [Fact]
    public async Task Handle_AuditWriteThrows_ResponseStillReturned()
    {
        var tenant = MakeTenant("Stable Kft.", TenantType.Manufacturer);
        _tenants.Setup(r => r.FirstOrDefaultAsync(
                It.IsAny<TenantByIdForInternalSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _handshake.Setup(h => h.HasVerifiedHandshakeAsync(RequesterId, TargetId, default))
            .ReturnsAsync(false);
        _audit.Setup(a => a.RecordAsync(It.IsAny<Guid>(), It.IsAny<Guid>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("DB unavailable"));

        // Must NOT throw — audit failure is swallowed + logged as Critical
        var result = await CreateSut().Handle(
            new GetTenantActorQuery(RequesterId, TargetId), default);

        Assert.True(result.IsSuccess);
    }

    // ── 5. DTO PII-review gate — no sensitive field names ────────────────────
    [Fact]
    public void TenantActorResponse_ContainsNoPiiFields()
    {
        var forbiddenNames = new[]
        {
            "email", "phone", "address", "contact", "tax",
            "billing", "vat", "iban", "zip", "city", "street"
        };

        var properties = typeof(TenantActorResponse)
            .GetProperties()
            .Select(p => p.Name.ToLowerInvariant());

        foreach (var forbidden in forbiddenNames)
            Assert.DoesNotContain(properties, p => p.Contains(forbidden));
    }
}
```

### 8.2 Middleware Tests

```csharp
// Tests/Internal/InternalHeaderMiddlewareTests.cs

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SpaceOS.Kernel.Infrastructure.Security;
using Xunit;

namespace SpaceOS.Kernel.Tests.Internal;

public sealed class InternalHeaderMiddlewareTests
{
    private static InternalHeaderMiddleware CreateSut(
        RequestDelegate next, string secret = "test-secret")
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SpaceOS:InternalSecret"] = secret
            })
            .Build();
        return new InternalHeaderMiddleware(next, config);
    }

    // ── Missing header → 401 ─────────────────────────────────────────────────
    [Fact]
    public async Task InvokeAsync_MissingHeader_Returns401()
    {
        var nextCalled = false;
        var sut = CreateSut(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new System.IO.MemoryStream();

        await sut.InvokeAsync(ctx);

        Assert.Equal(401, ctx.Response.StatusCode);
        Assert.False(nextCalled);
    }

    // ── Wrong secret → 401 ───────────────────────────────────────────────────
    [Fact]
    public async Task InvokeAsync_WrongSecret_Returns401()
    {
        var nextCalled = false;
        var sut = CreateSut(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new System.IO.MemoryStream();
        ctx.Request.Headers["X-SpaceOS-Internal"] = "wrong-secret";

        await sut.InvokeAsync(ctx);

        Assert.Equal(401, ctx.Response.StatusCode);
        Assert.False(nextCalled);
    }

    // ── Correct secret → next called ─────────────────────────────────────────
    [Fact]
    public async Task InvokeAsync_CorrectSecret_CallsNext()
    {
        var nextCalled = false;
        var sut = CreateSut(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = new DefaultHttpContext();
        ctx.Request.Headers["X-SpaceOS-Internal"] = "test-secret";

        await sut.InvokeAsync(ctx);

        Assert.True(nextCalled);
    }
}
```

---

## 9. Definition of Done

### Domain / Application gates
- [ ] `GetTenantActorQuery` + Handler + DTO létezik — 0 build warning
- [ ] `TenantActorResponse` DTO: **nincs** email, phone, address, tax, billing, contact mező (PII review gate)
- [ ] `IB2BHandshakeVerifier` + `IInternalAccessAuditWriter` interfészek az Application/Ports-ban

### Infrastructure gates
- [ ] `B2BHandshakeVerifier` — `TenantHandshakeAllowlist` bidirektív `AnyAsync` (AsNoTracking)
- [ ] `InternalAccessAuditEntry` — private init ctor, EF config, két index
- [ ] `InternalAccessAuditWriter` — DbFactory-alapú, audit hiba nem propagál
- [ ] `InternalHeaderMiddleware` — Kernel-ben létezik (ha még nem volt)
- [ ] Migration `0030` alkalmazva: `InternalAccessAuditLog` tábla létezik a DB-ben
- [ ] `REVOKE UPDATE, DELETE ON "InternalAccessAuditLog"` érvényes

### API gates
- [ ] `app.UseWhen("/api/internal", ...)` — JWT auth **nem** érintett
- [ ] `MapInternalEndpoints()` regisztrálva
- [ ] `ExcludeFromDescription()` — endpoint nem jelenik meg a public Swagger-ben
- [ ] `GET /api/internal/tenants/{id}` — `X-SpaceOS-Internal` hiányzó/rossz → **401**
- [ ] `GET /api/internal/tenants/{id}` — `X-SpaceOS-TenantId` hiányzó → **400**
- [ ] `GET /api/internal/tenants/{id}` — target nem létezik → **404** (PII-mentes)
- [ ] `GET /api/internal/tenants/{id}` — target létezik → **200** + helyes DTO

### Test gates
- [ ] `GetTenantActorQueryHandlerTests` — 5 teszt zöld
- [ ] `InternalHeaderMiddlewareTests` — 3 teszt zöld
- [ ] Meglévő **1178** Kernel teszt zöld
- [ ] 0 build warning

### Security gates
- [ ] nginx config: `/api/internal/*` **nincs** exponálva kifelé (loopback-only)
- [ ] `SpaceOS:InternalSecret` env-varból jön — **nem** appsettings.json-ban
- [ ] Audit log tartalmaz minden hívást (Found + NotFound eset)

---

## 10. Implementációs megjegyzések Claude Code agentnek

```
Implementáld a SpaceOS_Kernel_ADR039_InternalTenantEndpoint_v1.md alapján.

ELLENŐRZÉSEK INDÍTÁS ELŐTT:
1. `ls Infrastructure/Persistence/Migrations/` → legutolsó migration szám (0030 helyett a következőt használd)
2. `grep -r "InternalHeaderMiddleware" --include="*.cs"` → ha létezik, skip §5.4; csak Program.cs UseWhen kell
3. `grep -r "TenantHandshakeAllowlist" Infrastructure/Persistence/KernelDbContext.cs` → DbSet neve
4. `grep -r "IDbContextFactory" --include="*.cs"` → ha már van factory, InternalAccessAuditWriter-ben
   cseréld IKernelDbContextFactory → IDbContextFactory<KernelDbContext> + CreateDbContextAsync(ct)

TRACK SORREND:
A) Domain/Application (Query + DTO + Ports + Spec) — nincs függőség
B) Infrastructure/Persistence (Entity + Config + Migration) — A után
C) Infrastructure/Internal (AuditWriter + HandshakeVerifier) — B után
D) Infrastructure/Security (Middleware, ha nincs) — A után
E) API (Endpoints + Program.cs diff + DI) — A+B+C+D után
F) Tests — párhuzamosan A-E-vel

TILOS:
- Audit hiba exception propagálása a handler-ből
- PII mező hozzáadása TenantActorResponse-hoz
- InternalHeaderMiddleware globális alkalmazása (csak UseWhen!)
- /api/internal/* endpoint megjelenése a public Swagger-ben
```
