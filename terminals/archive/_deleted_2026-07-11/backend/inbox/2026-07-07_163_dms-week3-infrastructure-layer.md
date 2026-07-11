---
id: MSG-BACKEND-163
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-CONDUCTOR-100
epic_id: EPIC-JOINERYTECH-MIGRATION
checkpoint_id: CP-JOINERYTECH-WEEK3-INFRA
estimated_nwt: 90
expected_nwt: 30
created: 2026-07-07
content_hash: 83f20fb1b22a3644f627f0b1db5b3fb447018ff2350c79dd4d8c4f40d57bf8da
---

# DMS Week 3 — Infrastructure Layer (EF Core + RLS)

## 🎯 Mission

**Implementáld az Infrastructure Layer-t a DMS modulhoz:**
- EF Core DbContext konfigurációval
- Repository implementációkkal
- RLS (Row-Level Security) multi-tenancy támogatással
- Database migrations-ökkel

**Miért DMS első?** Pattern establishment — Legkisebb modul (3 aggregate), itt építjük ki az Infrastructure Layer mintákat, amit HR/Maintenance/QA fog újrahasznosítani.

---

## Context — Phase 2 COMPLETE ✅

**Elért eredmények:**
- Week 2 Application Layer: 4/4 modul DONE (DMS, HR, Maintenance, QA)
- 232 fájl, 103 handler, ~724 test PASS
- Pattern reuse: **75% átlag (96% csúcs!)** 🚀
- Timeline: 11 óra (vs 19h becsült) = **42% gyorsabb**

**Week 3 Infrastructure Layer Scope:**
- **4 modul:** DMS → HR → Maintenance → QA (szekvenciális cascade)
- **Becsült:** 480 NWT (~16 óra) → **Várt:** ~160 NWT (~5-6 óra) = **67% gyorsabb**
- **Confidence:** HIGH (80%) — Pattern reuse bizonyított

**Te vagy az első (DMS)** — Establish Infrastructure patterns!

---

## Scope — DMS Week 3 Infrastructure Layer

### 1. EF Core DbContext (DMSDbContext)

**Lokáció:** `spaceos-modules-dms/Infrastructure/Persistence/DMSDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.DMS.Domain.Documents;
using SpaceOS.Modules.DMS.Domain.DocumentCategories;
using SpaceOS.Modules.DMS.Domain.Tags;

namespace SpaceOS.Modules.DMS.Infrastructure.Persistence;

public class DMSDbContext : DbContext
{
    public DMSDbContext(DbContextOptions<DMSDbContext> options) : base(options) { }

    public DbSet<Document> Documents { get; set; }
    public DbSet<DocumentCategory> DocumentCategories { get; set; }
    public DbSet<Tag> Tags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("dms");

        modelBuilder.ApplyConfiguration(new DocumentEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new DocumentCategoryEntityTypeConfiguration());
        modelBuilder.ApplyConfiguration(new TagEntityTypeConfiguration());
    }
}
```

**Connection string konfig:** `appsettings.json` vagy DI extension

### 2. Entity Type Configurations (3 fájl)

**Lokáció:** `spaceos-modules-dms/Infrastructure/Persistence/Configurations/`

**DocumentEntityTypeConfiguration.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.DMS.Domain.Documents;

namespace SpaceOS.Modules.DMS.Infrastructure.Persistence.Configurations;

public class DocumentEntityTypeConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("documents", "dms");

        builder.HasKey(d => d.Id);

        // StronglyTypedId conversion
        builder.Property(d => d.Id)
            .HasConversion(
                id => id.Value,
                value => new DocumentId(value)
            );

        // TenantId index for RLS performance
        builder.Property(d => d.TenantId).IsRequired();
        builder.HasIndex(d => d.TenantId);

        // Owned entity: DocumentMetadata
        builder.OwnsOne(d => d.Metadata, metadata =>
        {
            metadata.Property(m => m.Title).HasMaxLength(500).IsRequired();
            metadata.Property(m => m.Description).HasMaxLength(2000);
            metadata.Property(m => m.FileExtension).HasMaxLength(10);
            metadata.Property(m => m.FileSizeBytes).IsRequired();
            metadata.Property(m => m.StoragePath).HasMaxLength(1000).IsRequired();
        });

        // DocumentStatus enum
        builder.Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        // DocumentType enum
        builder.Property(d => d.Type)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Timestamps
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.UpdatedAt).IsRequired();
        builder.Property(d => d.ArchivedAt);
    }
}
```

**DocumentCategoryEntityTypeConfiguration.cs** és **TagEntityTypeConfiguration.cs** hasonló mintával.

### 3. Repository Implementations (3 fájl)

**Lokáció:** `spaceos-modules-dms/Infrastructure/Persistence/Repositories/`

**DocumentRepository.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.DMS.Application.Contracts;
using SpaceOS.Modules.DMS.Domain.Documents;

namespace SpaceOS.Modules.DMS.Infrastructure.Persistence.Repositories;

public class DocumentRepository : IDocumentRepository
{
    private readonly DMSDbContext _context;

    public DocumentRepository(DMSDbContext context)
    {
        _context = context;
    }

    // ⚠️ FONTOS: TenantId NEM kell a method signature-ben!
    // RLS automatikusan szűr tenant alapján

    public async Task<Document?> GetByIdAsync(DocumentId id, CancellationToken ct = default)
    {
        return await _context.Documents
            .FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<IEnumerable<Document>> GetAllAsync(CancellationToken ct = default)
    {
        return await _context.Documents.ToListAsync(ct);
    }

    public async Task<IEnumerable<Document>> GetByStatusAsync(DocumentStatus status, CancellationToken ct = default)
    {
        return await _context.Documents
            .Where(d => d.Status == status)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Document document, CancellationToken ct = default)
    {
        await _context.Documents.AddAsync(document, ct);
        await _context.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Document document, CancellationToken ct = default)
    {
        _context.Documents.Update(document);
        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(DocumentId id, CancellationToken ct = default)
    {
        var document = await GetByIdAsync(id, ct);
        if (document != null)
        {
            _context.Documents.Remove(document);
            await _context.SaveChangesAsync(ct);
        }
    }
}
```

**DocumentCategoryRepository.cs** és **TagRepository.cs** hasonló mintával.

### 4. Database Migrations (2 migration)

**Lokáció:** `spaceos-modules-dms/Infrastructure/Persistence/Migrations/`

**Migration 1: InitialCreate**

```bash
cd spaceos-modules-dms
dotnet ef migrations add InitialCreate --context DMSDbContext --output-dir Infrastructure/Persistence/Migrations
```

**Migration 2: EnableRLS**

```bash
dotnet ef migrations add EnableRLS --context DMSDbContext --output-dir Infrastructure/Persistence/Migrations
```

**RLS Migration Up() metódusban:**
```csharp
migrationBuilder.Sql(@"
    -- Tenant context setter function
    CREATE OR REPLACE FUNCTION dms.set_tenant_context(p_tenant_id UUID)
    RETURNS VOID AS $$
    BEGIN
        PERFORM set_config('app.tenant_id', p_tenant_id::text, false);
    END;
    $$ LANGUAGE plpgsql;

    -- RLS Policy: documents
    ALTER TABLE dms.documents ENABLE ROW LEVEL SECURITY;
    CREATE POLICY tenant_isolation_documents ON dms.documents
        USING (tenant_id = current_setting('app.tenant_id')::uuid);

    -- RLS Policy: document_categories
    ALTER TABLE dms.document_categories ENABLE ROW LEVEL SECURITY;
    CREATE POLICY tenant_isolation_document_categories ON dms.document_categories
        USING (tenant_id = current_setting('app.tenant_id')::uuid);

    -- RLS Policy: tags
    ALTER TABLE dms.tags ENABLE ROW LEVEL SECURITY;
    CREATE POLICY tenant_isolation_tags ON dms.tags
        USING (tenant_id = current_setting('app.tenant_id')::uuid);
");
```

### 5. TenantDbConnectionInterceptor

**Lokáció:** `spaceos-modules-dms/Infrastructure/Persistence/TenantDbConnectionInterceptor.cs`

```csharp
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;

namespace SpaceOS.Modules.DMS.Infrastructure.Persistence;

public class TenantDbConnectionInterceptor : DbConnectionInterceptor
{
    private readonly ITenantContext _tenantContext;

    public TenantDbConnectionInterceptor(ITenantContext tenantContext)
    {
        _tenantContext = tenantContext;
    }

    public override InterceptionResult ConnectionOpening(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId != Guid.Empty)
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"SELECT dms.set_tenant_context('{tenantId}')";
            command.ExecuteNonQuery();
        }

        return base.ConnectionOpening(connection, eventData, result);
    }

    public override async ValueTask<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        if (tenantId != Guid.Empty)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"SELECT dms.set_tenant_context('{tenantId}')";
            await command.ExecuteNonQueryAsync(ct);
        }

        return await base.ConnectionOpeningAsync(connection, eventData, result, ct);
    }
}
```

**ITenantContext interface:** Ha még nincs, hozd létre `Application/Contracts/ITenantContext.cs`-ben:

```csharp
namespace SpaceOS.Modules.DMS.Application.Contracts;

public interface ITenantContext
{
    Guid TenantId { get; }
}
```

### 6. DI Registration Extension

**Lokáció:** `spaceos-modules-dms/Infrastructure/DependencyInjection.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SpaceOS.Modules.DMS.Application.Contracts;
using SpaceOS.Modules.DMS.Infrastructure.Persistence;
using SpaceOS.Modules.DMS.Infrastructure.Persistence.Repositories;

namespace SpaceOS.Modules.DMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddDMSInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext with interceptor
        services.AddDbContext<DMSDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DMSDatabase");
            options.UseNpgsql(connectionString);
            options.AddInterceptors(sp.GetRequiredService<TenantDbConnectionInterceptor>());
        });

        // Repositories
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IDocumentCategoryRepository, DocumentCategoryRepository>();
        services.AddScoped<ITagRepository, TagRepository>();

        // Interceptor
        services.AddScoped<TenantDbConnectionInterceptor>();

        return services;
    }
}
```

### 7. Integration Tests (Testcontainers)

**Lokáció:** `spaceos-modules-dms/tests/Infrastructure.Tests/Persistence/`

**DocumentRepositoryTests.cs:**
```csharp
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.DMS.Domain.Documents;
using SpaceOS.Modules.DMS.Infrastructure.Persistence;
using SpaceOS.Modules.DMS.Infrastructure.Persistence.Repositories;
using Testcontainers.PostgreSql;
using Xunit;

namespace SpaceOS.Modules.DMS.Infrastructure.Tests.Persistence;

public class DocumentRepositoryTests : IAsyncLifetime
{
    private PostgreSqlContainer _postgresContainer;
    private DMSDbContext _context;
    private DocumentRepository _repository;

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .Build();

        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<DMSDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        _context = new DMSDbContext(options);
        await _context.Database.MigrateAsync();

        _repository = new DocumentRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldPersistDocument()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var document = Document.Create(
            tenantId,
            new DocumentMetadata(
                "Test Document",
                "Test description",
                "pdf",
                1024,
                "/storage/test.pdf"
            ),
            DocumentType.Contract
        );

        // Act
        await _repository.AddAsync(document);

        // Assert
        var retrieved = await _repository.GetByIdAsync(document.Id);
        Assert.NotNull(retrieved);
        Assert.Equal(document.Id, retrieved.Id);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        // Arrange
        var nonExistentId = new DocumentId(Guid.NewGuid());

        // Act
        var result = await _repository.GetByIdAsync(nonExistentId);

        // Assert
        Assert.Null(result);
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }
}
```

**RLS Tests:** Tesztelj multi-tenant isolation-t (két különböző tenantId-vel)

---

## Acceptance Criteria

### Build Gate ✅

- [ ] `dotnet build` — **0 errors**
- [ ] Warnings: csak non-critical nullable reference warnings (CS8618, CS8602) megengedettek

### Migration Gate ✅

- [ ] `dotnet ef migrations add InitialCreate` — sikeres
- [ ] `dotnet ef migrations add EnableRLS` — sikeres
- [ ] `dotnet ef database update` — sikeres (Testcontainers PostgreSQL)
- [ ] RLS policies active: `SELECT * FROM pg_policies WHERE schemaname = 'dms';`

### Test Gate ✅

- [ ] Integration tests PASS (Testcontainers)
- [ ] Repository CRUD operations work
- [ ] RLS tenant isolation verified (multi-tenant tests)
- [ ] Migration Up/Down tested

### Security Gate ✅

- [ ] RLS enabled on all tables (documents, document_categories, tags)
- [ ] TenantId indexed on all tables
- [ ] DbConnectionInterceptor registered in DI
- [ ] No SQL injection vectors (EF Core parameterized queries only)

---

## Critical Patterns (Week 3 Infrastructure)

### 🔒 RLS Implementation Pattern

**Miért fontos:** Multi-tenancy biztonsági garancia — tenantok SOHA nem látják egymás adatait.

**SQL Pattern:**
```sql
-- Function
CREATE OR REPLACE FUNCTION dms.set_tenant_context(p_tenant_id UUID)
RETURNS VOID AS $$ BEGIN
    PERFORM set_config('app.tenant_id', p_tenant_id::text, false);
END; $$ LANGUAGE plpgsql;

-- Policy
ALTER TABLE dms.documents ENABLE ROW LEVEL SECURITY;
CREATE POLICY tenant_isolation_documents ON dms.documents
    USING (tenant_id = current_setting('app.tenant_id')::uuid);
```

**C# Interceptor Pattern:**
```csharp
public override InterceptionResult ConnectionOpening(...)
{
    var tenantId = _tenantContext.TenantId;
    connection.ExecuteNonQuery($"SELECT dms.set_tenant_context('{tenantId}')");
    return base.ConnectionOpening(...);
}
```

### 🗄️ Repository Pattern (RLS-aware)

**Kulcs szabály:** TenantId NEM method signature-ben! RLS automatikusan szűr.

```csharp
// ✅ CORRECT
Task<Document?> GetByIdAsync(DocumentId id, CancellationToken ct);

// ❌ WRONG
Task<Document?> GetByIdAsync(DocumentId id, Guid tenantId, CancellationToken ct);
```

**Miért?** RLS PostgreSQL szinten garantálja a tenant isolation-t. Explicit tenantId paraméter felesleges ÉS kockázatos (bypass lehetőség).

**Kivétel:** Ha RLS NINCS (pl. admin query), akkor explicit tenantId szükséges.

### 🏗️ StronglyTypedId EF Core Conversion

**Value Conversion Pattern:**
```csharp
builder.Property(d => d.Id)
    .HasConversion(
        id => id.Value,        // To DB: Guid
        value => new DocumentId(value)  // From DB: DocumentId
    );
```

**Miért fontos:** Domain-Driven Design — StronglyTypedId type safety megőrzése persistence layer-ben is.

### 🧪 Testcontainers Integration Tests

**Pattern:**
```csharp
private PostgreSqlContainer _postgresContainer = new PostgreSqlBuilder()
    .WithImage("postgres:16-alpine")
    .Build();

await _postgresContainer.StartAsync();
var connectionString = _postgresContainer.GetConnectionString();
```

**Miért fontos:** Real PostgreSQL instance (nem mock) → RLS policies, migrations, queries teljes tesztelése.

---

## Risks & Mitigations

### 🟡 MEDIUM: EF Core Configuration Complexity

**Risk:** Entity Type Configuration hibák (value objects, owned entities, StronglyTypedIds).

**Mitigation:**
- Referencia: `docs/knowledge/patterns/DATABASE_PATTERNS.md`
- QA module hasonló pattern (Week 1 Domain Layer)
- Build error iteráció (autonomous problem-solving)

**Impact if delayed:** +0.5-1 hour (debugging, retries)

### 🟡 MEDIUM: Migration Errors

**Risk:** Migration Up/Down constraint violations, RLS policy syntax errors.

**Mitigation:**
- Testcontainers ephemeral PostgreSQL
- RLS SQL templates from `docs/knowledge/deployment/KNOWN_GOTCHAS.md`
- Migration rollback tested (Down method)

**Impact if delayed:** +0.5-1 hour

### 🟢 LOW: Repository Interface Inconsistency

**Risk:** Eltérő GetByIdAsync signature-ök (2-param vs 3-param).

**Mitigation:**
- DMS establishes pattern: **2-param (id, ct)** — RLS handles tenantId
- Document in DONE outbox → HR/Maintenance/QA follows

**Impact if delayed:** Minimal (pattern clarification needed)

---

## Timeline Estimate

| Fase | NWT | Idő | Aktivitás |
|------|-----|-----|-----------|
| **DbContext + Configs** | 15 | 30 perc | DMSDbContext + 3 EntityTypeConfiguration |
| **Repositories** | 10 | 20 perc | 3 Repository implementation (pattern reuse) |
| **Migrations** | 20 | 40 perc | InitialCreate + EnableRLS (RLS SQL) |
| **DI Extension** | 5 | 10 perc | AddDMSInfrastructure registration |
| **Integration Tests** | 15 | 30 perc | Testcontainers setup + CRUD tests |
| **Migration Testing** | 10 | 20 perc | Up/Down + RLS verification |
| **Build + Debugging** | 15 | 30 perc | Error fixes, warnings cleanup |
| **TOTAL** | **90** | **~3h** | Original estimate |
| **Expected (Pattern Reuse)** | **30** | **~1h** | **67% faster** 🚀 |

**Confidence:** MEDIUM-HIGH (70%) — Infrastructure Layer új terület, de RLS pattern dokumentált.

**First module:** Pattern establishment fázis — debug early, document well!

---

## DONE Outbox Requirements

**Fájlnév:** `2026-07-07_163_dms-week3-infrastructure-layer-done.md`

```yaml
---
id: MSG-BACKEND-163-DONE
from: backend
to: conductor
type: done
status: UNREAD
priority: high
ref: MSG-BACKEND-163
created: YYYY-MM-DD
---

# DMS Week 3 Infrastructure Layer — DONE

## Összefoglaló

**✅ Infrastructure Layer teljes mértékben implementálva** a DMS modulhoz EF Core + RLS mintával.

### Implementált komponensek

**DbContext:**
- DMSDbContext with schema "dms"
- 3 DbSet (Documents, DocumentCategories, Tags)
- 3 EntityTypeConfiguration applied

**Repositories:**
- DocumentRepository (GetByIdAsync, GetAllAsync, GetByStatusAsync, Add, Update, Delete)
- DocumentCategoryRepository
- TagRepository

**Migrations:**
- InitialCreate — Tables + indexes
- EnableRLS — RLS policies for multi-tenancy

**DI Extension:**
- AddDMSInfrastructure registration
- TenantDbConnectionInterceptor

**Tests:**
- Testcontainers integration tests
- Repository CRUD operations
- RLS tenant isolation verified

### Build eredmény

Build succeeded.
    X Warning(s) - NON-CRITICAL
    0 Error(s)

### Test eredmény

X passing tests (Testcontainers PostgreSQL)

### Fájlok

- DMSDbContext.cs
- DocumentEntityTypeConfiguration.cs
- DocumentCategoryEntityTypeConfiguration.cs
- TagEntityTypeConfiguration.cs
- DocumentRepository.cs
- DocumentCategoryRepository.cs
- TagRepository.cs
- TenantDbConnectionInterceptor.cs
- ITenantContext.cs
- DependencyInjection.cs
- Migrations/YYYYMMDDHHMMSS_InitialCreate.cs
- Migrations/YYYYMMDDHHMMSS_EnableRLS.cs
- Tests/Infrastructure.Tests/Persistence/DocumentRepositoryTests.cs

**Total: ~15 files**

## Critical Patterns Established

### 1. RLS Implementation ✅

**PostgreSQL Function:**
CREATE FUNCTION dms.set_tenant_context(p_tenant_id UUID) ...

**Policies created:**
- tenant_isolation_documents
- tenant_isolation_document_categories
- tenant_isolation_tags

**Interceptor registered:** TenantDbConnectionInterceptor in DI

### 2. Repository Pattern (2-param signature) ✅

GetByIdAsync(DocumentId id, CancellationToken ct)
— NO TenantId parameter (RLS handles it)

This pattern established for HR/Maintenance/QA reuse!

### 3. StronglyTypedId Conversion ✅

HasConversion(id => id.Value, value => new DocumentId(value))

Applied to all aggregate IDs.

### 4. Testcontainers Integration ✅

PostgreSQL 16 Alpine container
Real migration testing (not mocked)

## Következő lépések javaslat

HR Week 3 Infrastructure dispatch — Reuse DMS patterns (67% gyorsabb várható).
```

---

## Koordináció

**Conductor-ra vár:**
- DMS Week 3 DONE → HR Week 3 dispatch
- Pattern validation (2-param Repository signature established?)
- Timeline tényleges (30 NWT teljesült?)

**Next cascade:** HR Week 3 Infrastructure (2 aggregates: Employee, Absence)

---

## Referenciák

| Dokumentum | Tartalom |
|---|---|
| `docs/knowledge/patterns/DATABASE_PATTERNS.md` | RLS SQL, DbConnectionInterceptor, Testcontainers |
| `docs/knowledge/deployment/KNOWN_GOTCHAS.md` | RLS GUC pattern (app.tenant_id) |
| `docs/knowledge/patterns/TESTING_PATTERNS.md` | Testcontainers setup, integration test structure |
| `MSG-CONDUCTOR-100` | Week 3 Infrastructure planning (full cascade scope) |
| `MSG-CONDUCTOR-099` | Phase 2 COMPLETE report (pattern reuse validation) |

---

**Conductor note:** DMS Week 3 első Infrastructure modul — pattern establishment kritikus! Document well, test thoroughly, establish 2-param Repository signature as standard.

🚀 Good luck! Week 3 Infrastructure cascade starts here!
