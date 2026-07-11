# CLAUDE.md — SpaceOS.Kernel.Tests

> Test layer rules. Read root CLAUDE.md first for global rules.
> Unit tests only. No EF Core. No HTTP. No real I/O.

---

## PURPOSE

Verify handler logic, domain invariants, and validation rules in isolation.
Every external dependency is mocked.

> **Note:** Integration tests exist in separate projects:
> - `SpaceOS.Kernel.IntegrationTests` — repository + pipeline tests (SQLite in-memory)
> - `SpaceOS.Kernel.Api.Tests` — API endpoint tests (WebApplicationFactory)

---

## TEST FILE STRUCTURE

```
Tests/
  Entities/
    TenantTests.cs
    FacilityTests.cs
    WorkStationTests.cs
    SpaceLayerTests.cs
    FlowEpicTests.cs (state + delegation)
  ValueObjects/
    TenantNameTests.cs
    TenantIdTests.cs
    FacilityIdTests.cs
    FacilityNameTests.cs
    WorkStationNameTests.cs
    WorkStationTypeTests.cs
    WorkStationIdTests.cs
    FlowEpicIdTests.cs
    FlowEpicTitleTests.cs
    SpaceLayerIdTests.cs
  Application/                          ← flat — one file per handler/validator
    CreateTenantCommandHandlerTests.cs
    CreateFacilityCommandHandlerTests.cs
    RegisterWorkStationCommandHandlerTests.cs
    GetTenantByIdQueryHandlerTests.cs
    GetAllTenantsQueryHandlerTests.cs
    ValidationBehaviorTests.cs
  Validators/                           ← flat — one file per validator
    CreateTenantCommandValidatorTests.cs
    CreateFacilityCommandValidatorTests.cs
    RegisterWorkStationCommandValidatorTests.cs
    RegisterSpaceLayerCommandValidatorTests.cs
    UpdateFlowEpicTitleCommandValidatorTests.cs
```

**Rule:** `Application/` and `Validators/` are **flat** — no feature subfolders.

One test file per production class. File name = `{ClassName}Tests.cs`.

---

## HANDLER TEST TEMPLATE

```csharp
// Tests/Application/Tenants/Commands/CreateTenantCommandHandlerTests.cs

public sealed class CreateTenantCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _repository = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcher = new();
    private readonly CreateTenantCommandHandler _handler;

    public CreateTenantCommandHandlerTests() =>
        _handler = new CreateTenantCommandHandler(_repository.Object, _dispatcher.Object);

    [Fact]
    public async Task Handle_ValidCommand_ReturnsTenantDto()
    {
        // Arrange
        var command = new CreateTenantCommand("Kovács Kft.");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Kovács Kft.", result.Value.Name);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsAddAsync_Once()
    {
        var command = new CreateTenantCommand("Kovács Kft.");

        await _handler.Handle(command, CancellationToken.None);

        // ✅ Verify Times.Once on every repository/dispatcher call
        _repository.Verify(r => r.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_DispatchesDomainEvents_Once()
    {
        var command = new CreateTenantCommand("Kovács Kft.");

        await _handler.Handle(command, CancellationToken.None);

        _dispatcher.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NotFound_ReturnsNotFoundResult()
    {
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var query = new GetTenantByIdQuery(Guid.NewGuid());
        // ... (query handler test)
    }
}
```

---

## VALUE OBJECT TEST TEMPLATE

```csharp
// Tests/Domain/Tenants/TenantNameTests.cs

public sealed class TenantNameTests
{
    [Theory]
    [InlineData("Kovács Kft.")]
    [InlineData("A")]
    public void From_ValidInput_CreatesValue(string value)
    {
        var name = TenantName.From(value);
        Assert.Equal(value, name.Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void From_EmptyOrNull_ThrowsDomainException(string? value)
    {
        Assert.Throws<DomainException>(() => TenantName.From(value!));
    }

    [Fact]
    public void From_ExceedsMaxLength_ThrowsDomainException()
    {
        var tooLong = new string('A', 101);
        Assert.Throws<DomainException>(() => TenantName.From(tooLong));
    }
}
```

---

## VALIDATOR TEST TEMPLATE

```csharp
// Tests/Application/Tenants/Commands/CreateTenantCommandValidatorTests.cs

public sealed class CreateTenantCommandValidatorTests
{
    private readonly CreateTenantCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_PassesValidation()
    {
        var result = _validator.Validate(new CreateTenantCommand("Kovács Kft."));
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_EmptyName_FailsValidation(string name)
    {
        var result = _validator.Validate(new CreateTenantCommand(name));
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateTenantCommand.Name));
    }
}
```

---

## NAMING CONVENTIONS

| What | Convention |
|---|---|
| Test class | `{ClassName}Tests` |
| Test method | `{Method}_{Scenario}_{ExpectedOutcome}` |
| Subject under test | `_handler` (or `_validator`, `_behavior` — matches the class type) |
| Mocks | `_camelCase` field, `new Mock<T>()` in field initializer |

---

## MANDATORY CHECKLIST — every new handler test file

- [ ] One test file per handler — created alongside the handler, not after
- [ ] Happy path: success result verified
- [ ] `repository.Verify(Times.Once)` on every mock call
- [ ] `dispatcher.Verify(Times.Once)` on mutating handlers
- [ ] Not-found path tested where applicable
- [ ] VO invariant tests cover: empty, null, boundary length, valid input
- [ ] Validator tests cover: valid input, each invalid case separately

---

## TEST RUNNER

```bash
# Run all tests across all 3 test projects
dotnet test

# Run only unit tests
dotnet test --project SpaceOS.Kernel.Tests

# Run specific test class
dotnet test --filter "FullyQualifiedName~CreateTenantCommandHandlerTests"
```

Target: **0 failed, 0 skipped** before any commit. Current: **1077 passing** (883 unit + 101 integration + 93 API · 4 skipped · 2026-04-14).

---

## ANTI-PATTERNS

```csharp
// ❌ Real EF Core / real DB in unit tests
var context = new AppDbContext(new DbContextOptionsBuilder()
    .UseSqlite("Data Source=test.db").Options);

// ❌ Missing Verify — mock was called but not asserted
_repository.Setup(...).ReturnsAsync(tenant);
// ... handler called, but no Verify → silent false positive

// ❌ Multiple assertions in one test without clear separation
// Keep each test focused on one behavior

// ❌ Skipped tests committed
[Fact(Skip = "TODO")]

// ❌ Magic strings in InlineData without explanation
[InlineData("xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx")] // use named const instead
```
