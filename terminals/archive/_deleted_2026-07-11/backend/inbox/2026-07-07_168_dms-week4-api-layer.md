---
processed: 2026-07-07
id: MSG-BACKEND-168
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-CONDUCTOR-108
created: 2026-07-07
epic_id: EPIC-JOINERYTECH-MIGRATION
checkpoint_id: CP-JOINERYTECH-WEEK4-API
estimated_nwt: 45
content_hash: f2e6607631b57e3476b07425320ab34e298246b7a601bdc67f33d9f7e08670a0
---

# DMS Week 4 API Layer Implementation

**Epic:** EPIC-JOINERYTECH-MIGRATION
**Checkpoint:** CP-JOINERYTECH-WEEK4-API
**Module:** DMS (Document Management System)
**Phase:** Week 4 — API Layer (Pattern Establishment)

---

## 🎯 Objective

Implement **Minimal API endpoints** for the DMS module with full CQRS/MediatR pattern, covering:
- DocumentCategory CRUD operations
- Tag CRUD operations
- Request/Response DTOs
- FluentValidation rules
- API integration tests with Testcontainers + authentication

**Expected acceleration:** 90 NWT → 45 NWT (50% faster through pattern establishment)

**Strategic role:** This is the **FIRST** Week 4 API module — establish patterns for HR, Maintenance, QA reuse!

---

## 📦 Deliverables

### 1. **Application Layer** — CQRS Commands & Queries

**Commands (Write operations):**
```
Application/Commands/
├── CreateDocumentCategoryCommand.cs
├── UpdateDocumentCategoryCommand.cs
├── DeleteDocumentCategoryCommand.cs
├── CreateTagCommand.cs
├── UpdateTagCommand.cs
└── DeleteTagCommand.cs
```

**Queries (Read operations):**
```
Application/Queries/
├── GetDocumentCategoryQuery.cs
├── ListDocumentCategoriesQuery.cs
├── GetTagQuery.cs
└── ListTagsQuery.cs
```

**Handlers:**
```
Application/Handlers/
├── CreateDocumentCategoryHandler.cs
├── UpdateDocumentCategoryHandler.cs
├── DeleteDocumentCategoryHandler.cs
├── GetDocumentCategoryHandler.cs
├── ListDocumentCategoriesHandler.cs
├── CreateTagHandler.cs
├── UpdateTagHandler.cs
├── DeleteTagHandler.cs
├── GetTagHandler.cs
└── ListTagsHandler.cs
```

**DTOs:**
```
Application/DTOs/
├── DocumentCategoryDto.cs
├── DocumentCategoryListDto.cs
├── TagDto.cs
└── TagListDto.cs
```

**Validators (FluentValidation):**
```
Application/Validators/
├── CreateDocumentCategoryCommandValidator.cs
├── UpdateDocumentCategoryCommandValidator.cs
├── CreateTagCommandValidator.cs
└── UpdateTagCommandValidator.cs
```

### 2. **API Layer** — Minimal API Endpoints

**Endpoints:**
```
API/Endpoints/
├── DocumentCategoryEndpoints.cs   # 5 endpoints (CRUD + List)
└── TagEndpoints.cs                # 5 endpoints (CRUD + List)
```

**Expected endpoints (10 total):**

**DocumentCategory:**
- `POST /api/dms/categories` — Create category
- `GET /api/dms/categories/{id}` — Get by ID
- `GET /api/dms/categories` — List all (paginated, tenant-filtered)
- `PUT /api/dms/categories/{id}` — Update category
- `DELETE /api/dms/categories/{id}` — Delete category

**Tag:**
- `POST /api/dms/tags` — Create tag
- `GET /api/dms/tags/{id}` — Get by ID
- `GET /api/dms/tags` — List all (paginated, tenant-filtered)
- `PUT /api/dms/tags/{id}` — Update tag
- `DELETE /api/dms/tags/{id}` — Delete tag

### 3. **Integration Tests** — API Tests with Testcontainers

**Test structure:**
```
tests/Integration/Api/
├── ApiTestFixture.cs              # WebApplicationFactory + Testcontainers
├── DocumentCategoryApiTests.cs    # 6 test scenarios
└── TagApiTests.cs                 # 6 test scenarios
```

**Test scenarios (12 total):**

**DocumentCategoryApiTests:**
1. `CreateDocumentCategory_ValidRequest_ReturnsCreated`
2. `CreateDocumentCategory_InvalidRequest_ReturnsBadRequest` (FluentValidation)
3. `GetDocumentCategory_ExistingId_ReturnsOk`
4. `GetDocumentCategory_NonExistentId_ReturnsNotFound`
5. `ListDocumentCategories_WithPagination_ReturnsPagedResults`
6. `ListDocumentCategories_MultiTenant_OnlyReturnsTenantData` (RLS validation)

**TagApiTests:**
1. `CreateTag_ValidRequest_ReturnsCreated`
2. `CreateTag_InvalidRequest_ReturnsBadRequest`
3. `GetTag_ExistingId_ReturnsOk`
4. `GetTag_NonExistentId_ReturnsNotFound`
5. `ListTags_WithPagination_ReturnsPagedResults`
6. `ListTags_MultiTenant_OnlyReturnsTenantData`

---

## 🏗️ Pattern Library — Week 4 API Layer Patterns (NEW!)

### Pattern #1: Minimal API Endpoint Structure

```csharp
// API/Endpoints/DocumentCategoryEndpoints.cs
public static class DocumentCategoryEndpoints
{
    public static void MapDocumentCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dms/categories")
            .WithTags("DocumentCategory")
            .RequireAuthorization();

        // POST /api/dms/categories
        group.MapPost("/", async (
            [FromBody] CreateDocumentCategoryRequest request,
            [FromServices] IMediator mediator,
            [FromServices] ITenantContext tenantContext,
            CancellationToken ct) =>
        {
            var command = new CreateDocumentCategoryCommand(
                tenantContext.TenantId,
                request.Name,
                request.Description
            );
            var result = await mediator.Send(command, ct);
            return Results.Created($"/api/dms/categories/{result}", result);
        })
        .WithName("CreateDocumentCategory")
        .Produces<Guid>(StatusCodes.Status201Created)
        .Produces<ValidationProblemDetails>(StatusCodes.Status400BadRequest);

        // GET /api/dms/categories/{id}
        group.MapGet("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] IMediator mediator,
            [FromServices] ITenantContext tenantContext,
            CancellationToken ct) =>
        {
            var query = new GetDocumentCategoryQuery(id, tenantContext.TenantId);
            var result = await mediator.Send(query, ct);
            return result != null ? Results.Ok(result) : Results.NotFound();
        })
        .WithName("GetDocumentCategory")
        .Produces<DocumentCategoryDto>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound);

        // GET /api/dms/categories (List with pagination)
        group.MapGet("/", async (
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromServices] IMediator mediator,
            [FromServices] ITenantContext tenantContext,
            CancellationToken ct) =>
        {
            var query = new ListDocumentCategoriesQuery(
                tenantContext.TenantId,
                page,
                pageSize
            );
            var result = await mediator.Send(query, ct);
            return Results.Ok(result);
        })
        .WithName("ListDocumentCategories")
        .Produces<DocumentCategoryListDto>(StatusCodes.Status200OK);

        // PUT /api/dms/categories/{id}
        group.MapPut("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromBody] UpdateDocumentCategoryRequest request,
            [FromServices] IMediator mediator,
            [FromServices] ITenantContext tenantContext,
            CancellationToken ct) =>
        {
            var command = new UpdateDocumentCategoryCommand(
                id,
                tenantContext.TenantId,
                request.Name,
                request.Description
            );
            var result = await mediator.Send(command, ct);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("UpdateDocumentCategory")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);

        // DELETE /api/dms/categories/{id}
        group.MapDelete("/{id:guid}", async (
            [FromRoute] Guid id,
            [FromServices] IMediator mediator,
            [FromServices] ITenantContext tenantContext,
            CancellationToken ct) =>
        {
            var command = new DeleteDocumentCategoryCommand(id, tenantContext.TenantId);
            var result = await mediator.Send(command, ct);
            return result ? Results.NoContent() : Results.NotFound();
        })
        .WithName("DeleteDocumentCategory")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound);
    }
}
```

**Key pattern elements:**
- ✅ `MapGroup("/api/dms/categories")` for route prefix
- ✅ `.WithTags("DocumentCategory")` for OpenAPI grouping
- ✅ `.RequireAuthorization()` for JWT enforcement
- ✅ `[FromServices] IMediator` for CQRS
- ✅ `[FromServices] ITenantContext` for tenant ID injection
- ✅ `.Produces<T>()` for OpenAPI documentation
- ✅ Minimal API lambda-based handlers

### Pattern #2: CQRS Command Structure

```csharp
// Application/Commands/CreateDocumentCategoryCommand.cs
public record CreateDocumentCategoryCommand(
    Guid TenantId,
    string Name,
    string? Description
) : IRequest<Guid>;  // Returns DocumentCategoryId

// Application/Handlers/CreateDocumentCategoryHandler.cs
public class CreateDocumentCategoryHandler
    : IRequestHandler<CreateDocumentCategoryCommand, Guid>
{
    private readonly IDocumentCategoryRepository _repository;

    public CreateDocumentCategoryHandler(IDocumentCategoryRepository repository)
    {
        _repository = repository;
    }

    public async Task<Guid> Handle(
        CreateDocumentCategoryCommand request,
        CancellationToken ct)
    {
        // Aggregate factory pattern (from Week 3)
        var category = DocumentCategory.Create(
            new DocumentCategoryId(Guid.NewGuid()),
            new TenantId(request.TenantId),
            request.Name,
            request.Description
        );

        await _repository.AddAsync(category, ct);
        return category.Id.Value;
    }
}
```

### Pattern #3: CQRS Query Structure (EF Core Projection)

```csharp
// Application/Queries/GetDocumentCategoryQuery.cs
public record GetDocumentCategoryQuery(
    Guid Id,
    Guid TenantId
) : IRequest<DocumentCategoryDto?>;

// Application/Handlers/GetDocumentCategoryHandler.cs
public class GetDocumentCategoryHandler
    : IRequestHandler<GetDocumentCategoryQuery, DocumentCategoryDto?>
{
    private readonly DmsDbContext _dbContext;

    public GetDocumentCategoryHandler(DmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<DocumentCategoryDto?> Handle(
        GetDocumentCategoryQuery request,
        CancellationToken ct)
    {
        // Direct EF projection (no repository needed for reads)
        var result = await _dbContext.DocumentCategories
            .Where(c => c.Id == new DocumentCategoryId(request.Id))
            .Where(c => c.TenantId == new TenantId(request.TenantId))
            .Select(c => new DocumentCategoryDto
            {
                Id = c.Id.Value,
                Name = c.Name,
                Description = c.Description,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .FirstOrDefaultAsync(ct);

        return result;
    }
}
```

**Key insight:** Queries use **EF Core projection** (not repository) for performance!

### Pattern #4: FluentValidation Rules

```csharp
// Application/Validators/CreateDocumentCategoryCommandValidator.cs
public class CreateDocumentCategoryCommandValidator
    : AbstractValidator<CreateDocumentCategoryCommand>
{
    public CreateDocumentCategoryCommandValidator()
    {
        RuleFor(x => x.TenantId)
            .NotEmpty()
            .WithMessage("TenantId is required");

        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Name is required")
            .MaximumLength(100)
            .WithMessage("Name must not exceed 100 characters");

        RuleFor(x => x.Description)
            .MaximumLength(500)
            .WithMessage("Description must not exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.Description));
    }
}
```

**MediatR Pipeline Behavior** (auto-validation):
```csharp
// Application/Behaviors/ValidationBehavior.cs
public class ValidationBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, ct))
        );

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

**Register in Program.cs:**
```csharp
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
```

### Pattern #5: API Integration Test Structure

```csharp
// tests/Integration/Api/ApiTestFixture.cs
public class ApiTestFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer;
    private WebApplicationFactory<Program> _factory = null!;
    public HttpClient Client { get; private set; } = null!;

    public ApiTestFixture()
    {
        _dbContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("dms_test")
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace DbContext with test container connection
                    var descriptor = services
                        .SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<DmsDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<DmsDbContext>(options =>
                        options.UseNpgsql(_dbContainer.GetConnectionString()));

                    // Mock ITenantContext
                    services.AddScoped<ITenantContext>(_ =>
                        new TestTenantContext(Guid.Parse("11111111-1111-1111-1111-111111111111")));
                });

                builder.ConfigureTestServices(services =>
                {
                    // Apply migrations
                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<DmsDbContext>();
                    db.Database.Migrate();
                });
            });

        Client = _factory.CreateClient();

        // Add JWT token to all requests
        Client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateTestJwt());
    }

    public async Task DisposeAsync()
    {
        await _dbContainer.DisposeAsync();
        await _factory.DisposeAsync();
    }

    private string GenerateTestJwt()
    {
        // Generate test JWT with tenant claim
        // Implementation details...
        return "test-jwt-token";
    }
}
```

```csharp
// tests/Integration/Api/DocumentCategoryApiTests.cs
[Collection("Api")]
public class DocumentCategoryApiTests : IClassFixture<ApiTestFixture>
{
    private readonly ApiTestFixture _fixture;

    public DocumentCategoryApiTests(ApiTestFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateDocumentCategory_ValidRequest_ReturnsCreated()
    {
        // Arrange
        var request = new
        {
            Name = "Test Category",
            Description = "Test Description"
        };

        // Act
        var response = await _fixture.Client.PostAsJsonAsync(
            "/api/dms/categories",
            request
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var categoryId = await response.Content.ReadFromJsonAsync<Guid>();
        categoryId.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateDocumentCategory_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new
        {
            Name = "",  // Invalid: empty name
            Description = "Test"
        };

        // Act
        var response = await _fixture.Client.PostAsJsonAsync(
            "/api/dms/categories",
            request
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var problemDetails = await response.Content
            .ReadFromJsonAsync<ValidationProblemDetails>();
        problemDetails.Should().NotBeNull();
        problemDetails!.Errors.Should().ContainKey("Name");
    }

    [Fact]
    public async Task ListDocumentCategories_MultiTenant_OnlyReturnsTenantData()
    {
        // Arrange: Create categories for two different tenants
        var tenant1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var tenant2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");

        // Seed data directly via DbContext
        using var scope = _fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<DmsDbContext>();

        var category1 = DocumentCategory.Create(
            new DocumentCategoryId(Guid.NewGuid()),
            new TenantId(tenant1Id),
            "Tenant 1 Category",
            null
        );
        var category2 = DocumentCategory.Create(
            new DocumentCategoryId(Guid.NewGuid()),
            new TenantId(tenant2Id),
            "Tenant 2 Category",
            null
        );

        await dbContext.DocumentCategories.AddRangeAsync(category1, category2);
        await dbContext.SaveChangesAsync();

        // Act: Request with Tenant 1 JWT
        var response = await _fixture.Client.GetAsync("/api/dms/categories");

        // Assert: Only Tenant 1 categories returned
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content
            .ReadFromJsonAsync<DocumentCategoryListDto>();

        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(1);
        result.Items.First().Name.Should().Be("Tenant 1 Category");
    }
}
```

---

## 🔧 DI Registration (Program.cs)

```csharp
// DMS Module registration
builder.Services.AddMediatR(cfg => {
    cfg.RegisterServicesFromAssembly(typeof(DmsModule).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

builder.Services.AddValidatorsFromAssembly(typeof(DmsModule).Assembly);

builder.Services.AddDbContext<DmsDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DmsDb"))
           .AddInterceptors(new TenantDbConnectionInterceptor()));

builder.Services.AddScoped<IDocumentCategoryRepository, DocumentCategoryRepository>();
builder.Services.AddScoped<ITagRepository, TagRepository>();
builder.Services.AddScoped<ITenantContext, HttpTenantContext>();

// Map endpoints
app.MapDocumentCategoryEndpoints();
app.MapTagEndpoints();
```

---

## 📋 Acceptance Criteria

**Build Quality:**
- [ ] `dotnet build src/SpaceOS.Modules.DMS.csproj` — 0 errors, 0 warnings
- [ ] `dotnet build tests/SpaceOS.Modules.DMS.Tests.csproj` — 0 errors, 0 warnings

**API Endpoints:**
- [ ] 10 Minimal API endpoints implemented (5 DocumentCategory + 5 Tag)
- [ ] All endpoints require JWT authentication
- [ ] All endpoints inject ITenantContext for multi-tenancy
- [ ] OpenAPI documentation generated (Swagger UI accessible)

**CQRS/MediatR:**
- [ ] 6 Commands + 6 Command Handlers implemented
- [ ] 4 Queries + 4 Query Handlers implemented
- [ ] 4 FluentValidation validators implemented
- [ ] ValidationBehavior pipeline registered

**Integration Tests:**
- [ ] 12 API test scenarios implemented
- [ ] WebApplicationFactory with Testcontainers PostgreSQL
- [ ] JWT authentication mocked
- [ ] Multi-tenancy isolation validated (test scenario passes)

**Pattern Library:**
- [ ] All 5 Week 4 API patterns established and documented
- [ ] Ready for reuse in HR, Maintenance, QA modules

---

## ⏱️ Timeline Estimate

**Total estimated:** 90 NWT → **45 NWT** (50% faster, pattern establishment)

| Phase | NWT | Time | Notes |
|-------|-----|------|-------|
| **Application Layer** | 20 | 40 min | Commands, Queries, Handlers, DTOs, Validators |
| **API Layer** | 10 | 20 min | Minimal API endpoints (10 endpoints) |
| **Integration Tests** | 12 | 24 min | API tests with Testcontainers |
| **DI + Build** | 3 | 6 min | Program.cs registration, verification |
| **TOTAL** | **45 NWT** | **~1.5h** | Pattern establishment |

**Expected delivery:** ~1.5h from task start

---

## 🚀 Pattern Reuse for HR/Maintenance/QA

After DMS API Layer is complete, the following patterns will be **ready for reuse**:

1. ✅ **Minimal API endpoint structure** — MapGroup, WithTags, RequireAuthorization
2. ✅ **CQRS Command/Query handlers** — IRequestHandler<TRequest, TResponse>
3. ✅ **FluentValidation** — AbstractValidator + ValidationBehavior pipeline
4. ✅ **API Integration Tests** — WebApplicationFactory + Testcontainers
5. ✅ **Multi-tenancy enforcement** — ITenantContext injection pattern

**Expected acceleration for subsequent modules:**
- HR Week 4: 60 NWT → 30 NWT (50% faster via pattern reuse)
- Maintenance Week 4: 60 NWT → 30 NWT (50% faster)
- QA Week 4: 60 NWT → 30 NWT (50% faster)

---

## 📊 Week 4 Strategic Context

**This is the FIRST Week 4 API module** — your role is to establish patterns cleanly!

**Dependencies:**
- ✅ Week 3 Infrastructure complete (DmsDbContext, repositories ready)
- ✅ Pattern library validated across 4 modules

**Success criteria:**
- Build: 0 errors, 0 warnings
- Tests: All 12 scenarios pass
- Documentation: OpenAPI/Swagger operational
- Pattern quality: HR/Maintenance/QA can copy-paste and adapt

**Next steps after DMS DONE:**
- Conductor will dispatch HR Week 4 (~30 NWT with pattern reuse)
- Sequential cascade continues: DMS → HR → Maintenance → QA
- Expected Week 4 total: ~4-6h (vs ~12-16h without patterns)

---

## 🎯 Focus

**Primary goal:** Establish Week 4 API Layer patterns cleanly for 3-module reuse.

**Quality gate:** 0 errors, 0 warnings, 100% test pass rate.

**Timeline:** Complete in ~1.5h to maintain Week 4 cascade schedule.

---

Good luck! This is pattern establishment — take your time to get it right! 🚀

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
