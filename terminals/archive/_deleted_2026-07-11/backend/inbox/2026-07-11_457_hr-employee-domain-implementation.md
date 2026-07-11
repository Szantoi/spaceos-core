---
id: MSG-BACKEND-457
from: conductor
to: backend
type: task
priority: high
status: DONE
model: sonnet
ref: MSG-BACKEND-455
epic_id: EPIC-JT-HR
checkpoint_id: CP-EHS-HR-INTEGRATION
created: 2026-07-11
estimated_nwt: 60
content_hash: 1158e03293c307e214e7edbd7f8eee32d9329c85e5837cba8abccca6b175265e
---

# HR Employee Domain Implementation — Complete Foundation

**Predecessor:** MSG-BACKEND-452 (BLOCKED, foundation salvaged)
**Context:** MSG-BACKEND-455 decision (defer & re-scope, Option B)
**Priority:** HIGH (blocks CP-EHS-HR-INTEGRATION checkpoint)

---

## Background

MSG-BACKEND-452 revealed that HR and EHS domain aggregates don't exist despite being marked "complete". Backend proactively created foundation components:

**Already Created (Salvaged):**
- ✅ `Employee.cs` aggregate
- ✅ `EmployeeCompetency.cs` owned entity
- ✅ `IEmployeeRepository.cs` interface
- ✅ `TrainingCompletedEvent.cs` contract
- ✅ `TrainingCompletedEventHandler.cs`

**Still Missing (This Task):**
- ❌ EF Core EmployeeConfiguration
- ❌ EmployeeRepository implementation
- ❌ Database migration
- ❌ Integration tests
- ❌ Build verification

---

## Scope (60 NWT)

### 1. EF Core Entity Configuration (10 NWT)
**File:** `spaceos-modules-hr/src/Infrastructure/Data/Configuration/EmployeeConfiguration.cs`

```csharp
public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever(); // Guid from aggregate

        builder.Property(e => e.FullName).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Position).HasMaxLength(100);
        builder.Property(e => e.HireDate).IsRequired();
        builder.Property(e => e.Status)
               .HasConversion<string>() // Enum → string
               .HasMaxLength(20)
               .IsRequired();

        // Owned entity: EmployeeCompetency
        builder.OwnsMany(e => e.CompetencyMatrix, comp =>
        {
            comp.ToTable("employee_competencies");
            comp.WithOwner().HasForeignKey("EmployeeId");
            comp.HasKey("EmployeeId", "CompetencyCode"); // Composite PK

            comp.Property("CompetencyCode").HasMaxLength(50).IsRequired();
            comp.Property("Level").IsRequired(); // 1-5
            comp.Property("AcquiredDate").IsRequired();
            comp.Property("ExpiryDate"); // Nullable (some competencies don't expire)
        });

        // RLS support (multi-tenant)
        builder.HasQueryFilter(e => EF.Property<string>(e, "TenantId") == _currentTenant.Id);
    }
}
```

### 2. Repository Implementation (15 NWT)
**File:** `spaceos-modules-hr/src/Infrastructure/Repositories/EmployeeRepository.cs`

```csharp
public class EmployeeRepository : IEmployeeRepository
{
    private readonly HrDbContext _context;

    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct)
    {
        return await _context.Employees
            .Include(e => e.CompetencyMatrix)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<IEnumerable<Employee>> GetAllAsync(CancellationToken ct)
    {
        return await _context.Employees
            .Include(e => e.CompetencyMatrix)
            .ToListAsync(ct);
    }

    public async Task<Employee?> GetByConversionIdAsync(Guid conversionId, CancellationToken ct)
    {
        // Support for EHS→HR integration (TrainingCompletedEvent)
        return await _context.Employees
            .Include(e => e.CompetencyMatrix)
            .FirstOrDefaultAsync(e => e.CompetencyMatrix.Any(c => c.ConversionId == conversionId), ct);
    }

    public async Task SaveAsync(Employee employee, CancellationToken ct)
    {
        if (await _context.Employees.AnyAsync(e => e.Id == employee.Id, ct))
        {
            _context.Employees.Update(employee);
        }
        else
        {
            _context.Employees.Add(employee);
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var employee = await GetByIdAsync(id, ct);
        if (employee != null)
        {
            _context.Employees.Remove(employee);
            await _context.SaveChangesAsync(ct);
        }
    }
}
```

### 3. DbContext Integration (5 NWT)
**File:** `spaceos-modules-hr/src/Infrastructure/Data/HrDbContext.cs`

```csharp
public class HrDbContext : DbContext
{
    private readonly ICurrentTenant _currentTenant;

    public DbSet<Employee> Employees { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfiguration(new EmployeeConfiguration(_currentTenant));

        // Global RLS filter
        modelBuilder.Entity<Employee>().HasQueryFilter(e =>
            EF.Property<string>(e, "TenantId") == _currentTenant.Id
        );
    }
}
```

### 4. Database Migration (10 NWT)
**Command:**
```bash
cd /opt/spaceos/backend/spaceos-modules/spaceos-modules-hr
dotnet ef migrations add AddEmployeeDomain --output-dir Infrastructure/Data/Migrations
```

**Expected Migration:**
```csharp
public partial class AddEmployeeDomain : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "employees",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                FullName = table.Column<string>(maxLength: 200, nullable: false),
                Position = table.Column<string>(maxLength: 100, nullable: true),
                HireDate = table.Column<DateTime>(nullable: false),
                Status = table.Column<string>(maxLength: 20, nullable: false),
                TenantId = table.Column<string>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_employees", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "employee_competencies",
            columns: table => new
            {
                EmployeeId = table.Column<Guid>(nullable: false),
                CompetencyCode = table.Column<string>(maxLength: 50, nullable: false),
                Level = table.Column<int>(nullable: false),
                AcquiredDate = table.Column<DateTime>(nullable: false),
                ExpiryDate = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_employee_competencies", x => new { x.EmployeeId, x.CompetencyCode });
                table.ForeignKey(
                    name: "FK_employee_competencies_employees_EmployeeId",
                    column: x => x.EmployeeId,
                    principalTable: "employees",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });
    }
}
```

### 5. Integration Tests (15 NWT)
**File:** `spaceos-modules-hr/tests/Integration/EmployeeRepository_Tests.cs`

```csharp
public class EmployeeRepository_Tests : IClassFixture<HrTestContainerFixture>
{
    private readonly HrDbContext _context;
    private readonly EmployeeRepository _repository;

    [Fact]
    public async Task SaveAsync_NewEmployee_CreatesInDatabase()
    {
        // Arrange
        var employee = new Employee(Guid.NewGuid(), "John Doe", "Developer", DateTime.UtcNow);

        // Act
        await _repository.SaveAsync(employee, CancellationToken.None);

        // Assert
        var saved = await _repository.GetByIdAsync(employee.Id, CancellationToken.None);
        Assert.NotNull(saved);
        Assert.Equal("John Doe", saved.FullName);
    }

    [Fact]
    public async Task AddCompetency_UpdatesEmployee_PersistsCorrectly()
    {
        // Arrange
        var employee = new Employee(Guid.NewGuid(), "Jane Smith", "Engineer", DateTime.UtcNow);
        await _repository.SaveAsync(employee, CancellationToken.None);

        // Act
        employee.AddCompetency("WELDING_CERT", 3, DateTime.UtcNow, DateTime.UtcNow.AddYears(3));
        await _repository.SaveAsync(employee, CancellationToken.None);

        // Assert
        var updated = await _repository.GetByIdAsync(employee.Id, CancellationToken.None);
        Assert.Single(updated.CompetencyMatrix);
        Assert.Equal("WELDING_CERT", updated.CompetencyMatrix.First().CompetencyCode);
        Assert.Equal(3, updated.CompetencyMatrix.First().Level);
    }

    [Fact]
    public async Task GetByIdAsync_WithCompetencies_LoadsFullAggregate()
    {
        // Arrange
        var employee = new Employee(Guid.NewGuid(), "Mike Johnson", "Supervisor", DateTime.UtcNow);
        employee.AddCompetency("SAFETY_CERT", 5, DateTime.UtcNow, null);
        employee.AddCompetency("QUALITY_CERT", 4, DateTime.UtcNow, DateTime.UtcNow.AddYears(2));
        await _repository.SaveAsync(employee, CancellationToken.None);

        // Act
        var loaded = await _repository.GetByIdAsync(employee.Id, CancellationToken.None);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(2, loaded.CompetencyMatrix.Count());
    }

    [Fact]
    public async Task DeleteAsync_RemovesEmployee_AndCompetencies()
    {
        // Arrange
        var employee = new Employee(Guid.NewGuid(), "Bob Wilson", "Technician", DateTime.UtcNow);
        employee.AddCompetency("FORKLIFT_CERT", 2, DateTime.UtcNow, null);
        await _repository.SaveAsync(employee, CancellationToken.None);

        // Act
        await _repository.DeleteAsync(employee.Id, CancellationToken.None);

        // Assert
        var deleted = await _repository.GetByIdAsync(employee.Id, CancellationToken.None);
        Assert.Null(deleted);
    }
}
```

### 6. Build Verification (5 NWT)
```bash
cd /opt/spaceos/backend/spaceos-modules/spaceos-modules-hr
dotnet build --no-incremental

# Expected output:
# Build succeeded.
# 0 Warning(s)
# 0 Error(s)
```

---

## Acceptance Criteria

- [ ] EmployeeConfiguration created with RLS support
- [ ] EmployeeRepository implemented (IEmployeeRepository interface)
- [ ] HrDbContext updated with Employee DbSet
- [ ] Database migration created (`dotnet ef migrations add`)
- [ ] Integration tests PASS (4 tests: Save, AddCompetency, GetById, Delete)
- [ ] `dotnet build spaceos-modules-hr/` succeeds (0 errors)
- [ ] RLS filter verified (tenant isolation)

---

## Security Checklist

- [ ] RLS filter applied (tenant isolation via `TenantId`)
- [ ] No sensitive data in logs (competency details)
- [ ] Input validation in domain (Employee aggregate)
- [ ] Cascade delete configured (employee → competencies)

---

## Files to Create/Modify

### Create:
1. `Infrastructure/Data/Configuration/EmployeeConfiguration.cs`
2. `Infrastructure/Repositories/EmployeeRepository.cs`
3. `Infrastructure/Data/Migrations/YYYYMMDDHHMMSS_AddEmployeeDomain.cs`
4. `tests/Integration/EmployeeRepository_Tests.cs`

### Modify:
5. `Infrastructure/Data/HrDbContext.cs` (add DbSet<Employee>)
6. `Infrastructure/DependencyInjection.cs` (register EmployeeRepository)

---

## Dependencies

**Pre-existing (from MSG-452 salvage):**
- ✅ Employee.cs aggregate
- ✅ EmployeeCompetency.cs owned entity
- ✅ IEmployeeRepository.cs interface

**External:**
- EF Core 8.0+
- Testcontainers.PostgreSQL (for integration tests)

---

## Build & Test Commands

```bash
cd /opt/spaceos/backend

# Build HR module
dotnet build spaceos-modules/spaceos-modules-hr/

# Create migration
cd spaceos-modules/spaceos-modules-hr
dotnet ef migrations add AddEmployeeDomain --output-dir Infrastructure/Data/Migrations

# Run integration tests
dotnet test spaceos-modules/spaceos-modules-hr/tests/ --filter EmployeeRepository

# Verify repository methods
dotnet test spaceos-modules/spaceos-modules-hr/tests/ --filter EmployeeRepository_Tests
```

---

## Next Steps After Completion

1. **MSG-BACKEND-458:** EHS→HR Integration Event Handlers (30 NWT)
   - TrainingCompletedEventHandler integration
   - Event registration in DI
   - Integration tests (Event → CompetencyMatrix)
   - E2E test (EHS training → HR competency)

2. **CP-EHS-HR-INTEGRATION checkpoint update → DONE**

---

**Estimated Timeline:** 60 NWT (~2 hours)
**Priority:** HIGH (blocks CP-EHS-HR-INTEGRATION)
**Complexity:** MEDIUM (foundation exists, infrastructure implementation)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
