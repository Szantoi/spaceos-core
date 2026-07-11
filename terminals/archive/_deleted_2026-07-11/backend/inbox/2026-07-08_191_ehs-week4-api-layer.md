---
id: MSG-BACKEND-191
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-BACKEND-190-DONE
epic_id: EPIC-JT-EHS
estimated_nwt: 150
created: 2026-07-08
content_hash: ee11dce9ac05e673c1bb70428285f2cdb75653929b1fddc279b685c34dca5fca
---

# EHS Week 4: API Layer + Integration Tests

## Context

Week 3 Infrastructure Layer DONE ✅ (MSG-BACKEND-190-DONE):
- EF Core DbContext + RLS interceptor
- 4 Repository implementations (17 files, ~2255 LOC)
- 3 EntityTypeConfigurations with owned entities
- Initial migration (7 tables)
- Build SUCCESS (0 errors)

**Next:** Week 4 API Layer implementálása — az EHS modul utolsó fázisa a production readiness eléréséhez.

---

## 🎯 Feladat: API Project + 15 Endpoints + Integration Tests

### Scope

1. **API Project** (`SpaceOS.Modules.Ehs.Api.csproj`)
2. **15 Minimal API Endpoints** (Incident: 7, RiskAssessment: 5, TrainingRecord: 3)
3. **DI Registration** (DbContext + Repositories + Interceptor + MediatR + AutoMapper + Validators)
4. **Infrastructure.Tests Project** (Testcontainers PostgreSQL)
5. **30-40 Repository Integration Tests**
6. **API Integration Tests** (E2E smoke tests)
7. **AutoMapper NuGet Vulnerability Fix** (13.0.1 → 13.0.2+)

---

## 1. API Project Setup

### SpaceOS.Modules.Ehs.Api.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <!-- ASP.NET Core -->
    <PackageReference Include="Microsoft.AspNetCore.OpenApi" Version="8.0.*" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.8.*" />

    <!-- Project References -->
    <ProjectReference Include="..\Domain\SpaceOS.Modules.Ehs.Domain.csproj" />
    <ProjectReference Include="..\Application\SpaceOS.Modules.Ehs.Application.csproj" />
    <ProjectReference Include="..\Infrastructure\SpaceOS.Modules.Ehs.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

**Kötelező:** Program.cs startup configuration:
- AddDbContext + TenantDbConnectionInterceptor
- AddMediatR (Application assembly scan)
- AddAutoMapper (Application assembly scan)
- AddFluentValidation (Application assembly scan)
- Repository DI registration (Scoped)
- Minimal API endpoint mapping

### Program.cs példa struktúra

```csharp
var builder = WebApplication.CreateBuilder(args);

// DbContext + RLS
builder.Services.AddSingleton<ITenantContext, HttpTenantContext>();
builder.Services.AddSingleton<TenantDbConnectionInterceptor>();
builder.Services.AddDbContext<EhsDbContext>((sp, options) =>
{
    var interceptor = sp.GetRequiredService<TenantDbConnectionInterceptor>();
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("EhsDatabase"),
        npgsqlOptions => npgsqlOptions.MigrationsAssembly("SpaceOS.Modules.Ehs.Infrastructure")
    ).AddInterceptors(interceptor);
});

// Application Layer
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(CreateIncidentCommand).Assembly));
builder.Services.AddAutoMapper(typeof(EhsMappingProfile).Assembly);
builder.Services.AddValidatorsFromAssembly(typeof(CreateIncidentCommandValidator).Assembly);

// Repositories
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<IRiskAssessmentRepository, RiskAssessmentRepository>();
builder.Services.AddScoped<ITrainingRecordRepository, TrainingRecordRepository>();
builder.Services.AddScoped<IEhsNotificationService, EhsNotificationService>();

var app = builder.Build();

// Minimal API endpoint mapping
app.MapIncidentEndpoints();
app.MapRiskAssessmentEndpoints();
app.MapTrainingRecordEndpoints();

app.Run();
```

---

## 2. API Endpoints (15 total)

### 2.1 Incident Endpoints (7)

**Fájl:** `Endpoints/IncidentEndpoints.cs`

```csharp
public static class IncidentEndpoints
{
    public static void MapIncidentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/ehs/incidents").WithTags("Incidents");

        // 1. POST /api/ehs/incidents — Create incident
        group.MapPost("/", async (CreateIncidentCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Created($"/api/ehs/incidents/{result.Value}", result.Value)
                                    : Results.BadRequest(result.Errors);
        });

        // 2. GET /api/ehs/incidents/{id} — Get by ID
        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetIncidentByIdQuery { IncidentId = id });
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        });

        // 3. GET /api/ehs/incidents — List with filters
        group.MapGet("/", async (
            [FromQuery] string? type,
            [FromQuery] string? status,
            [FromQuery] DateTime? dateFrom,
            [FromQuery] DateTime? dateTo,
            [FromQuery] int? minSeverity,
            IMediator mediator) =>
        {
            var query = new ListIncidentsQuery
            {
                Type = type,
                Status = status,
                DateFrom = dateFrom,
                DateTo = dateTo,
                MinSeverity = minSeverity
            };
            var result = await mediator.Send(query);
            return Results.Ok(result.Value);
        });

        // 4. POST /api/ehs/incidents/{id}/investigation — Start investigation
        group.MapPost("/{id:guid}/investigation", async (Guid id, StartInvestigationCommand cmd, IMediator mediator) =>
        {
            cmd = cmd with { IncidentId = id };
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
        });

        // 5. POST /api/ehs/incidents/{id}/findings — Add findings
        group.MapPost("/{id:guid}/findings", async (Guid id, AddInvestigationFindingsCommand cmd, IMediator mediator) =>
        {
            cmd = cmd with { IncidentId = id };
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
        });

        // 6. POST /api/ehs/incidents/{id}/corrective-actions — Add action
        group.MapPost("/{id:guid}/corrective-actions", async (Guid id, AddCorrectiveActionCommand cmd, IMediator mediator) =>
        {
            cmd = cmd with { IncidentId = id };
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
        });

        // 7. POST /api/ehs/incidents/{id}/close — Close incident
        group.MapPost("/{id:guid}/close", async (Guid id, CloseIncidentCommand cmd, IMediator mediator) =>
        {
            cmd = cmd with { IncidentId = id };
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
        });
    }
}
```

### 2.2 RiskAssessment Endpoints (5)

**Fájl:** `Endpoints/RiskAssessmentEndpoints.cs`

```csharp
public static class RiskAssessmentEndpoints
{
    public static void MapRiskAssessmentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/ehs/risk-assessments").WithTags("Risk Assessments");

        // 1. POST /api/ehs/risk-assessments — Create assessment
        group.MapPost("/", async (CreateRiskAssessmentCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Created($"/api/ehs/risk-assessments/{result.Value}", result.Value)
                                    : Results.BadRequest(result.Errors);
        });

        // 2. GET /api/ehs/risk-assessments/{id} — Get by ID
        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetRiskAssessmentByIdQuery { RiskAssessmentId = id });
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        });

        // 3. GET /api/ehs/risk-assessments — List with filters
        group.MapGet("/", async (
            [FromQuery] string? riskLevel,
            [FromQuery] string? status,
            [FromQuery] DateTime? reviewDueBefore,
            IMediator mediator) =>
        {
            var query = new ListRiskAssessmentsQuery
            {
                RiskLevel = riskLevel,
                Status = status,
                ReviewDueBefore = reviewDueBefore
            };
            var result = await mediator.Send(query);
            return Results.Ok(result.Value);
        });

        // 4. GET /api/ehs/risk-assessments/matrix — Get risk matrix summary
        group.MapGet("/matrix", async (IMediator mediator) =>
        {
            var result = await mediator.Send(new GetRiskMatrixSummaryQuery());
            return Results.Ok(result.Value);
        });

        // 5. POST /api/ehs/risk-assessments/{id}/controls — Add control
        group.MapPost("/{id:guid}/controls", async (Guid id, AddControlMeasureCommand cmd, IMediator mediator) =>
        {
            cmd = cmd with { RiskAssessmentId = id };
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Ok() : Results.BadRequest(result.Errors);
        });
    }
}
```

### 2.3 TrainingRecord Endpoints (3)

**Fájl:** `Endpoints/TrainingRecordEndpoints.cs`

```csharp
public static class TrainingRecordEndpoints
{
    public static void MapTrainingRecordEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/ehs/training-records").WithTags("Training Records");

        // 1. POST /api/ehs/training-records — Create record
        group.MapPost("/", async (CreateTrainingRecordCommand cmd, IMediator mediator) =>
        {
            var result = await mediator.Send(cmd);
            return result.IsSuccess ? Results.Created($"/api/ehs/training-records/{result.Value}", result.Value)
                                    : Results.BadRequest(result.Errors);
        });

        // 2. GET /api/ehs/training-records/{id} — Get by ID
        group.MapGet("/{id:guid}", async (Guid id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetTrainingRecordByIdQuery { TrainingRecordId = id });
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        });

        // 3. GET /api/ehs/training-records/expiring — Get expiring trainings
        group.MapGet("/expiring", async ([FromQuery] int daysAhead, IMediator mediator) =>
        {
            var query = new GetExpiringTrainingsQuery { DaysAhead = daysAhead };
            var result = await mediator.Send(query);
            return Results.Ok(result.Value);
        });
    }
}
```

---

## 3. DI Registration Helper (opcionális)

**Fájl:** `Extensions/EhsServiceCollectionExtensions.cs`

```csharp
public static class EhsServiceCollectionExtensions
{
    public static IServiceCollection AddEhsModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext + RLS
        services.AddSingleton<ITenantContext, HttpTenantContext>();
        services.AddSingleton<TenantDbConnectionInterceptor>();
        services.AddDbContext<EhsDbContext>((sp, options) =>
        {
            var interceptor = sp.GetRequiredService<TenantDbConnectionInterceptor>();
            options.UseNpgsql(
                configuration.GetConnectionString("EhsDatabase"),
                npgsql => npgsql.MigrationsAssembly("SpaceOS.Modules.Ehs.Infrastructure")
            ).AddInterceptors(interceptor);
        });

        // Application Layer
        services.AddMediatR(cfg =>
            cfg.RegisterServicesFromAssembly(typeof(CreateIncidentCommand).Assembly));
        services.AddAutoMapper(typeof(EhsMappingProfile).Assembly);
        services.AddValidatorsFromAssembly(typeof(CreateIncidentCommandValidator).Assembly);

        // Repositories
        services.AddScoped<IIncidentRepository, IncidentRepository>();
        services.AddScoped<IRiskAssessmentRepository, RiskAssessmentRepository>();
        services.AddScoped<ITrainingRecordRepository, TrainingRecordRepository>();
        services.AddScoped<IEhsNotificationService, EhsNotificationService>();

        return services;
    }
}
```

**Usage:**
```csharp
builder.Services.AddEhsModule(builder.Configuration);
```

---

## 4. Infrastructure.Tests Project

### SpaceOS.Modules.Ehs.Infrastructure.Tests.csproj

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <!-- Testing Framework -->
    <PackageReference Include="xunit" Version="2.9.*" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.*" />
    <PackageReference Include="FluentAssertions" Version="6.12.*" />

    <!-- Testcontainers -->
    <PackageReference Include="Testcontainers.PostgreSql" Version="4.1.*" />

    <!-- EF Core Testing -->
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="8.0.*" />

    <!-- Project Reference -->
    <ProjectReference Include="..\Infrastructure\SpaceOS.Modules.Ehs.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### Test Structure

```
Infrastructure.Tests/
  Repositories/
    IncidentRepositoryTests.cs       (~10-12 tests)
    RiskAssessmentRepositoryTests.cs (~10-12 tests)
    TrainingRecordRepositoryTests.cs (~8-10 tests)
  Data/
    EhsDbContextTests.cs              (~5 tests - RLS, migrations)
```

### Testcontainers Base Class

**Fájl:** `TestBase/PostgresTestBase.cs`

```csharp
public abstract class PostgresTestBase : IAsyncLifetime
{
    private PostgreSqlContainer? _postgresContainer;
    protected EhsDbContext DbContext { get; private set; } = null!;
    protected Guid TestTenantId { get; } = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("ehs_test")
            .Build();

        await _postgresContainer.StartAsync();

        var options = new DbContextOptionsBuilder<EhsDbContext>()
            .UseNpgsql(_postgresContainer.GetConnectionString())
            .Options;

        DbContext = new EhsDbContext(options);
        await DbContext.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await DbContext.DisposeAsync();
        if (_postgresContainer != null)
            await _postgresContainer.DisposeAsync();
    }
}
```

### IncidentRepositoryTests példa (10-12 tests)

**Fájl:** `Repositories/IncidentRepositoryTests.cs`

```csharp
public class IncidentRepositoryTests : PostgresTestBase
{
    private IncidentRepository CreateRepository()
        => new IncidentRepository(DbContext);

    [Fact]
    public async Task AddAsync_ShouldPersistIncident()
    {
        // Arrange
        var repo = CreateRepository();
        var incident = Incident.Create(
            TestTenantId,
            "Near-miss at machinery",
            IncidentType.NearMiss,
            DateTime.UtcNow,
            "Production floor",
            IncidentSeverity.Medium
        );

        // Act
        await repo.AddAsync(incident, TestTenantId);
        await DbContext.SaveChangesAsync();

        // Assert
        var retrieved = await repo.GetByIdAsync(incident.Id, TestTenantId);
        retrieved.Should().NotBeNull();
        retrieved!.Title.Should().Be("Near-miss at machinery");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenWrongTenant()
    {
        // Arrange
        var repo = CreateRepository();
        var incident = Incident.Create(TestTenantId, "Test", IncidentType.Accident, DateTime.UtcNow, "Location", IncidentSeverity.Low);
        await repo.AddAsync(incident, TestTenantId);
        await DbContext.SaveChangesAsync();

        // Act
        var wrongTenant = Guid.NewGuid();
        var retrieved = await repo.GetByIdAsync(incident.Id, wrongTenant);

        // Assert
        retrieved.Should().BeNull();  // RLS isolation check
    }

    [Fact]
    public async Task ListAsync_ShouldFilterByStatus()
    {
        // Arrange
        var repo = CreateRepository();
        var incident1 = Incident.Create(TestTenantId, "Open", IncidentType.Accident, DateTime.UtcNow, "A", IncidentSeverity.Low);
        var incident2 = Incident.Create(TestTenantId, "Closed", IncidentType.Accident, DateTime.UtcNow, "B", IncidentSeverity.Low);
        incident2.StartInvestigation(Guid.NewGuid(), DateTime.UtcNow.AddDays(1));
        incident2.Investigation!.AddFindings("Test findings");
        incident2.Close("Done");

        await repo.AddAsync(incident1, TestTenantId);
        await repo.AddAsync(incident2, TestTenantId);
        await DbContext.SaveChangesAsync();

        // Act
        var filter = new IncidentFilter { Status = IncidentStatus.Closed };
        var results = await repo.ListAsync(filter, TestTenantId);

        // Assert
        results.Should().HaveCount(1);
        results.First().Id.Should().Be(incident2.Id);
    }

    // Add ~8-10 more tests:
    // - ListAsync_FilterByType
    // - ListAsync_FilterByDateRange
    // - ListAsync_FilterByMinSeverity
    // - GetSummaryAsync_ShouldAggregateCorrectly
    // - GetTrendsAsync_ShouldGroupByMonth
    // - UpdateAsync_ShouldPersistChanges
    // - ExistsAsync_ShouldReturnTrue_WhenExists
    // - ExistsAsync_ShouldReturnFalse_WhenNotExists
}
```

### RiskAssessmentRepositoryTests példa (10-12 tests)

**Fájl:** `Repositories/RiskAssessmentRepositoryTests.cs`

```csharp
public class RiskAssessmentRepositoryTests : PostgresTestBase
{
    private RiskAssessmentRepository CreateRepository()
        => new RiskAssessmentRepository(DbContext);

    [Fact]
    public async Task AddAsync_ShouldPersistRiskAssessment()
    {
        // Arrange
        var repo = CreateRepository();
        var assessment = RiskAssessment.Create(
            TestTenantId,
            "Machinery hazard",
            "Description",
            Severity.High,
            Likelihood.Likely
        );

        // Act
        await repo.AddAsync(assessment, TestTenantId);
        await DbContext.SaveChangesAsync();

        // Assert
        var retrieved = await repo.GetByIdAsync(assessment.Id, TestTenantId);
        retrieved.Should().NotBeNull();
        retrieved!.Hazard.Should().Be("Machinery hazard");
        retrieved.RiskLevel.Should().Be(RiskLevel.High);  // High * Likely = High
    }

    [Fact]
    public async Task GetRiskMatrixSummaryAsync_ShouldReturn25Cells()
    {
        // Arrange
        var repo = CreateRepository();
        var a1 = RiskAssessment.Create(TestTenantId, "H1", "D", Severity.Low, Likelihood.Rare);
        var a2 = RiskAssessment.Create(TestTenantId, "H2", "D", Severity.Critical, Likelihood.AlmostCertain);
        await repo.AddAsync(a1, TestTenantId);
        await repo.AddAsync(a2, TestTenantId);
        await DbContext.SaveChangesAsync();

        // Act
        var matrix = await repo.GetRiskMatrixSummaryAsync(TestTenantId);

        // Assert
        matrix.Should().NotBeNull();
        matrix.Cells.Should().HaveCount(25);  // 5×5
        matrix.TotalAssessments.Should().Be(2);
    }

    // Add ~8-10 more tests:
    // - ListAsync_FilterByRiskLevel
    // - ListAsync_FilterByStatus
    // - ListAsync_FilterByReviewDueBefore
    // - GetRiskMatrixAsync_ShouldReturnCorrectCounts
    // - UpdateAsync_ShouldPersistControlMeasures
    // - ExistsAsync_Tests
}
```

### TrainingRecordRepositoryTests példa (8-10 tests)

**Fájl:** `Repositories/TrainingRecordRepositoryTests.cs`

```csharp
public class TrainingRecordRepositoryTests : PostgresTestBase
{
    private TrainingRecordRepository CreateRepository()
        => new TrainingRecordRepository(DbContext);

    [Fact]
    public async Task AddAsync_ShouldPersistTrainingRecord()
    {
        // Arrange
        var repo = CreateRepository();
        var training = TrainingRecord.Create(
            TestTenantId,
            Guid.NewGuid(),
            "Forklift Safety",
            DateTime.UtcNow.AddMonths(-1),
            DateTime.UtcNow.AddYears(1),
            "Instructor XYZ",
            "CERT-12345"
        );

        // Act
        await repo.AddAsync(training, TestTenantId);
        await DbContext.SaveChangesAsync();

        // Assert
        var retrieved = await repo.GetByIdAsync(training.Id, TestTenantId);
        retrieved.Should().NotBeNull();
        retrieved!.TrainingType.Should().Be("Forklift Safety");
        retrieved.Status.Should().Be(TrainingStatus.Valid);
    }

    [Fact]
    public async Task GetExpiringAsync_ShouldReturnOnlyExpiringSoon()
    {
        // Arrange
        var repo = CreateRepository();
        var t1 = TrainingRecord.Create(TestTenantId, Guid.NewGuid(), "T1", DateTime.UtcNow, DateTime.UtcNow.AddDays(10), "I", null);
        var t2 = TrainingRecord.Create(TestTenantId, Guid.NewGuid(), "T2", DateTime.UtcNow, DateTime.UtcNow.AddDays(60), "I", null);
        await repo.AddAsync(t1, TestTenantId);
        await repo.AddAsync(t2, TestTenantId);
        await DbContext.SaveChangesAsync();

        // Act
        var expiring = await repo.GetExpiringAsync(30, TestTenantId);

        // Assert
        expiring.Should().HaveCount(1);
        expiring.First().Id.Should().Be(t1.Id);
    }

    // Add ~6-8 more tests:
    // - ListAsync_FilterByEmployeeId
    // - ListAsync_FilterByStatus (computed property check)
    // - GetExpiringTrainingsAsync_ShouldReturnDTOs
    // - ExistsAsync_Tests
}
```

---

## 5. AutoMapper NuGet Vulnerability Fix

**Probléma:** NU1903 — GHSA-rvv3-g6hj-g44x (AutoMapper 13.0.1 high severity)

**Fix:** Upgrade AutoMapper 13.0.1 → 13.0.2 (vagy újabb)

**Fájlok:**
- `src/Application/SpaceOS.Modules.Ehs.Application.csproj`

```xml
<!-- BEFORE -->
<PackageReference Include="AutoMapper" Version="13.0.1" />

<!-- AFTER -->
<PackageReference Include="AutoMapper" Version="13.0.2" />
```

**Verification:**
```bash
dotnet restore
dotnet build  # 0 warnings expected
```

---

## Acceptance Criteria

- [ ] API projekt létrehozva (Program.cs + DI registration)
- [ ] 15 endpoint implementálva (Incident: 7, RiskAssessment: 5, TrainingRecord: 3)
- [ ] Infrastructure.Tests projekt létrehozva (Testcontainers)
- [ ] 30-40 repository integration test implementálva
- [ ] AutoMapper 13.0.2+ upgrade
- [ ] Build SUCCESS (0 errors, 0 warnings)
- [ ] All tests GREEN (dotnet test)
- [ ] Swagger UI működik (`/swagger`)
- [ ] RLS tesztek sikeresek (tenant isolation)

---

## Build & Test parancsok

```bash
# 1. API build
cd /opt/spaceos/spaceos-modules-ehs/src/Api
dotnet build

# 2. Infrastructure.Tests build
cd /opt/spaceos/spaceos-modules-ehs/tests/Infrastructure.Tests
dotnet build

# 3. Run tests
cd /opt/spaceos/spaceos-modules-ehs
dotnet test

# 4. Run API (dev)
cd /opt/spaceos/spaceos-modules-ehs/src/Api
dotnet run
# Open: http://localhost:5000/swagger
```

---

## Security Review Checklist

- [ ] Tenant context injection minden endpoint-ra (ITenantContext)
- [ ] MediatR command validation (FluentValidation)
- [ ] No SQL injection (EF Core LINQ queries)
- [ ] Authorization placeholder (JWT bearer token ha kell)
- [ ] Sensitive data sanitization (notification logs)

---

## Következő Lépések

Ha Week 4 DONE:
1. **CP-EHS-BACKEND checkpoint** → DONE (EPICS.yaml frissítés)
2. **Frontend dispatch** → MSG-FRONTEND-XXX (EHS Dashboard UI)
3. **JoineryTech Phase 1 COMPLETE** → 7/7 modules production ready

---

## Referenciák

- Week 1 Domain Layer: MSG-BACKEND-188
- Week 2 Application Layer: MSG-BACKEND-189
- Week 3 Infrastructure Layer: MSG-BACKEND-190
- Proven Week 1-4 pattern: CRM, Kontrolling, HR, Maintenance, QA, DMS modules

---

**Estimated NWT:** 150 (~5-6 hours)
**Priority:** High (EHS module completion)
**Model:** Sonnet

---

📋 Generated by Conductor — EHS Week 4 API Layer + Integration Tests Dispatch

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
