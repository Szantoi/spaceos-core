# EF Core Configuration Plan — CRM Module Week 3

> **Document Type:** Planning (Phase 1 — No Build Required)
> **Status:** DRAFT → Ready for review
> **Created:** 2026-07-02 16:15 UTC
> **Task Reference:** MSG-BACKEND-116
> **Framework:** EF Core 8 (.NET 8)

---

## Executive Summary

EF Core entity type configuration strategy for CRM module. Covers entity configurations, value object mappings, owned entity relationships, discriminator setup, and DbContext initialization.

**Entities:** Lead, Opportunity, Activity, Task (4 aggregate types)
**Value Objects:** ContactInfo, Money (mapped as owned types)
**DbContext:** CrmDbContext with DbSet for each aggregate root
**Migrations:** InitialCreate migration strategy

---

## 1. Project Structure

```
SpaceOS.Modules.CRM/
├── src/
│   ├── Domain/
│   │   ├── Aggregates/
│   │   │   ├── Lead.cs
│   │   │   └── Opportunity.cs
│   │   ├── ValueObjects/
│   │   │   ├── ContactInfo.cs
│   │   │   └── Money.cs
│   │   ├── Entities/
│   │   │   ├── Activity.cs
│   │   │   └── Task.cs
│   │   └── Events/
│   │       └── [18 domain events]
│   │
│   ├── Application/
│   │   ├── Commands/
│   │   ├── Queries/
│   │   └── DTOs/
│   │
│   ├── Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── CrmDbContext.cs
│   │   │   ├── Configurations/
│   │   │   │   ├── LeadConfiguration.cs
│   │   │   │   ├── OpportunityConfiguration.cs
│   │   │   │   ├── ActivityConfiguration.cs
│   │   │   │   └── TaskConfiguration.cs
│   │   │   ├── Migrations/
│   │   │   │   └── 20260702_InitialCreate.cs
│   │   │   └── DbConnectionInterceptor.cs
│   │   └── Repositories/
│   │       ├── LeadRepository.cs
│   │       └── OpportunityRepository.cs
│   │
│   └── Api/
│       └── CrmEndpoints.cs
│
└── tests/
    ├── LeadFsmTests.cs
    ├── OpportunityFsmTests.cs
    └── CrmIntegrationTests.cs
```

---

## 2. DbContext Configuration

### 2.1 CrmDbContext Class

```csharp
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.CRM.Domain.Aggregates;
using SpaceOS.Modules.CRM.Domain.Entities;
using SpaceOS.Modules.CRM.Infrastructure.Persistence.Configurations;

namespace SpaceOS.Modules.CRM.Infrastructure.Persistence;

public class CrmDbContext : DbContext
{
    // Aggregate Roots
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();

    // Child Entities (accessed via aggregates in normal flow)
    // public DbSet<Activity> Activities => Set<Activity>(); // if needed for direct queries
    // public DbSet<Task> Tasks => Set<Task>(); // if needed for direct queries

    private readonly string _tenantId;

    public CrmDbContext(DbContextOptions<CrmDbContext> options, ITenantProvider tenantProvider)
        : base(options)
    {
        _tenantId = tenantProvider.TenantId;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Set default schema
        modelBuilder.HasDefaultSchema("crm");

        // Apply configurations
        modelBuilder.ApplyConfiguration(new LeadConfiguration());
        modelBuilder.ApplyConfiguration(new OpportunityConfiguration());
        modelBuilder.ApplyConfiguration(new ActivityConfiguration());
        modelBuilder.ApplyConfiguration(new TaskConfiguration());

        // Global query filters for tenant isolation
        modelBuilder.Entity<Lead>().HasQueryFilter(l => l.TenantId == Guid.Parse(_tenantId));
        modelBuilder.Entity<Opportunity>().HasQueryFilter(o => o.TenantId == Guid.Parse(_tenantId));
        modelBuilder.Entity<Activity>().HasQueryFilter(a => a.TenantId == Guid.Parse(_tenantId));
        modelBuilder.Entity<Task>().HasQueryFilter(t => t.TenantId == Guid.Parse(_tenantId));
    }

    // Override SaveChanges to set audit timestamps
    public override int SaveChanges()
    {
        UpdateAuditFields();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateAuditFields();
        return await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is not null)
            .ToList();

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                if (entry.Entity is IAuditable auditable)
                {
                    auditable.CreatedAt = DateTime.UtcNow;
                    auditable.UpdatedAt = DateTime.UtcNow;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                if (entry.Entity is IAuditable auditable)
                {
                    auditable.UpdatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}

// Marker interface for audit tracking
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    DateTime UpdatedAt { get; set; }
}
```

**Key Patterns:**
- `ITenantProvider` injected to set tenant context automatically
- Global query filters for tenant isolation (application-level + RLS)
- SaveChanges override to set audit timestamps automatically
- `ConfigureAwait(false)` on async operations

---

## 3. Entity Configurations

### 3.1 LeadConfiguration

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.CRM.Domain.Aggregates;
using SpaceOS.Modules.CRM.Domain.ValueObjects;

namespace SpaceOS.Modules.CRM.Infrastructure.Persistence.Configurations;

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        // Table mapping
        builder.ToTable("leads", "crm");

        // Primary key
        builder.HasKey(l => l.Id);

        // Properties (non-value objects)
        builder.Property(l => l.TenantId).IsRequired();
        builder.Property(l => l.Status).IsRequired().HasMaxLength(50);
        builder.Property(l => l.CompanyName).HasMaxLength(255);
        builder.Property(l => l.JobTitle).HasMaxLength(100);
        builder.Property(l => l.Industry).HasMaxLength(100);
        builder.Property(l => l.LeadScore).HasDefaultValue(0);
        builder.Property(l => l.Source).HasMaxLength(50);
        builder.Property(l => l.AssignedToUserId);
        builder.Property(l => l.CreatedByUserId).IsRequired();

        // Timestamps
        builder.Property(l => l.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(l => l.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(l => l.ContactedAt);
        builder.Property(l => l.QualifiedAt);
        builder.Property(l => l.ConvertedAt);
        builder.Property(l => l.LastActivityAt);
        builder.Property(l => l.DeletedAt);

        // Version (for optimistic concurrency)
        builder.Property(l => l.Version).IsConcurrencyToken().HasDefaultValue(0);

        // Value Object: ContactInfo (owned type)
        builder.OwnsOne(l => l.ContactInfo, ci =>
        {
            ci.Property(c => c.FirstName).HasColumnName("first_name").IsRequired().HasMaxLength(100);
            ci.Property(c => c.LastName).HasColumnName("last_name").IsRequired().HasMaxLength(100);
            ci.Property(c => c.EmailAddress).HasColumnName("email_address").HasMaxLength(255);
            ci.Property(c => c.EmailVerified).HasColumnName("email_verified").HasDefaultValue(false);
            ci.Property(c => c.PhoneNumber).HasColumnName("phone_number").HasMaxLength(20);
            ci.Property(c => c.CountryCode).HasColumnName("country_code").HasMaxLength(2);
        });

        // Relationships
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(l => l.AssignedToUserId)
            .IsRequired(false);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(l => l.CreatedByUserId)
            .IsRequired();

        // Child collections (Activities and Tasks)
        builder.HasMany<Activity>()
            .WithOne()
            .HasForeignKey(a => a.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<Task>()
            .WithOne()
            .HasForeignKey(t => t.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(l => new { l.TenantId, l.Status })
            .HasName("idx_leads_tenant_status")
            .HasFilter("[DeletedAt] IS NULL");

        builder.HasIndex(l => new { l.AssignedToUserId, l.TenantId })
            .HasName("idx_leads_assigned_to")
            .HasFilter("[DeletedAt] IS NULL");

        builder.HasIndex(l => l.CreatedAt)
            .HasName("idx_leads_created_at");

        // Implicit value object indexes (inherited from composite key)
    }
}
```

**Key Patterns:**
- `OwnsOne()` for value objects (ContactInfo stored as composite columns)
- `HasMaxLength()` for string validation at EF level + DB constraint
- `IsConcurrencyToken()` for optimistic locking (Version column)
- HasOne/WithMany for relationships to external aggregates
- HasMany for child collections with CASCADE delete
- Index configurations matching INFRASTRUCTURE_SCHEMA_DESIGN.md

### 3.2 OpportunityConfiguration

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.CRM.Domain.Aggregates;
using SpaceOS.Modules.CRM.Domain.ValueObjects;

namespace SpaceOS.Modules.CRM.Infrastructure.Persistence.Configurations;

public class OpportunityConfiguration : IEntityTypeConfiguration<Opportunity>
{
    public void Configure(EntityTypeBuilder<Opportunity> builder)
    {
        // Table mapping
        builder.ToTable("opportunities", "crm");

        // Primary key
        builder.HasKey(o => o.Id);

        // Properties (non-value objects)
        builder.Property(o => o.TenantId).IsRequired();
        builder.Property(o => o.LeadId); // Optional: can be standalone
        builder.Property(o => o.Status).IsRequired().HasMaxLength(50);
        builder.Property(o => o.WinProbability).HasDefaultValue(50);
        builder.Property(o => o.Title).IsRequired().HasMaxLength(255);
        builder.Property(o => o.Description);
        builder.Property(o => o.ExpectedCloseDate);

        // Loss/Abandonment context
        builder.Property(o => o.LossReason).HasMaxLength(100);
        builder.Property(o => o.CompetitorName).HasMaxLength(255);
        builder.Property(o => o.AbandonmentReason);

        // Relationships
        builder.Property(o => o.AssignedToUserId);
        builder.Property(o => o.CreatedByUserId).IsRequired();

        // Timestamps
        builder.Property(o => o.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(o => o.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(o => o.WonAt);
        builder.Property(o => o.LostAt);
        builder.Property(o => o.AbandonedAt);
        builder.Property(o => o.LastActivityAt);
        builder.Property(o => o.DeletedAt);

        // Version (for optimistic concurrency)
        builder.Property(o => o.Version).IsConcurrencyToken().HasDefaultValue(0);

        // Value Object: EstimatedValue (Money)
        builder.OwnsOne(o => o.EstimatedValue, mv =>
        {
            mv.Property(m => m.Amount)
                .HasColumnName("estimated_value_amount")
                .HasPrecision(15, 2);
            mv.Property(m => m.Currency)
                .HasColumnName("estimated_value_currency")
                .HasMaxLength(3)
                .HasDefaultValue("EUR");
        });

        // Value Object: FinalValue (Money)
        builder.OwnsOne(o => o.FinalValue, mv =>
        {
            mv.Property(m => m.Amount)
                .HasColumnName("final_value_amount")
                .HasPrecision(15, 2);
            mv.Property(m => m.Currency)
                .HasColumnName("final_value_currency")
                .HasMaxLength(3)
                .HasDefaultValue("EUR");
        });

        // Relationships to Lead (optional)
        builder.HasOne<Lead>()
            .WithMany()
            .HasForeignKey(o => o.LeadId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull); // Can outlive Lead

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(o => o.AssignedToUserId)
            .IsRequired(false);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(o => o.CreatedByUserId)
            .IsRequired();

        // Child collections (Activities and Tasks)
        builder.HasMany<Activity>()
            .WithOne()
            .HasForeignKey(a => a.OpportunityId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany<Task>()
            .WithOne()
            .HasForeignKey(t => t.OpportunityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(o => new { o.TenantId, o.Status })
            .HasName("idx_opportunities_tenant_status")
            .HasFilter("[DeletedAt] IS NULL");

        builder.HasIndex(o => new { o.AssignedToUserId, o.TenantId })
            .HasName("idx_opportunities_assigned_to")
            .HasFilter("[DeletedAt] IS NULL");

        builder.HasIndex(o => new { o.ExpectedCloseDate, o.TenantId })
            .HasName("idx_opportunities_expected_close")
            .HasFilter("[Status] IN ('Open', 'Proposal', 'Negotiation') AND [DeletedAt] IS NULL");

        builder.HasIndex(o => o.CreatedAt)
            .HasName("idx_opportunities_created_at");
    }
}
```

**Key Patterns:**
- **Multiple Value Objects:** EstimatedValue and FinalValue (both Money type)
- **Precision on Decimals:** `HasPrecision(15, 2)` for money fields
- **Optional Relationships:** Lead can be null, opportunity can outlive lead
- **Filter Expressions:** Filtered indexes for performance

### 3.3 ActivityConfiguration

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.CRM.Domain.Entities;

namespace SpaceOS.Modules.CRM.Infrastructure.Persistence.Configurations;

public class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        // Table mapping
        builder.ToTable("activities", "crm");

        // Primary key
        builder.HasKey(a => a.Id);

        // Properties
        builder.Property(a => a.TenantId).IsRequired();
        builder.Property(a => a.ParentType).IsRequired().HasMaxLength(50);
        builder.Property(a => a.LeadId);
        builder.Property(a => a.OpportunityId);
        builder.Property(a => a.ActivityType).IsRequired().HasMaxLength(50);
        builder.Property(a => a.Subject).IsRequired().HasMaxLength(255);
        builder.Property(a => a.Description);
        builder.Property(a => a.Outcome).HasMaxLength(100);
        builder.Property(a => a.ScheduledFor);
        builder.Property(a => a.CompletedAt);
        builder.Property(a => a.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(a => a.CreatedByUserId).IsRequired();
        builder.Property(a => a.AssignedToUserId);

        // Relationships
        builder.HasOne<Lead>()
            .WithMany()
            .HasForeignKey(a => a.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Opportunity>()
            .WithMany()
            .HasForeignKey(a => a.OpportunityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(a => new { a.LeadId, a.CreatedAt })
            .HasName("idx_activities_lead");

        builder.HasIndex(a => new { a.OpportunityId, a.CreatedAt })
            .HasName("idx_activities_opportunity");
    }
}
```

**Key Patterns:**
- **Polymorphic Discriminator:** ParentType + FK constraint (enforced at application layer)
- **Double FK:** LeadId OR OpportunityId (one must be null, enforced via check)
- **Indexes on CreatedAt DESC:** For recent activity queries

### 3.4 TaskConfiguration

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SpaceOS.Modules.CRM.Domain.Entities;

namespace SpaceOS.Modules.CRM.Infrastructure.Persistence.Configurations;

public class TaskConfiguration : IEntityTypeConfiguration<Task>
{
    public void Configure(EntityTypeBuilder<Task> builder)
    {
        // Table mapping
        builder.ToTable("tasks", "crm");

        // Primary key
        builder.HasKey(t => t.Id);

        // Properties
        builder.Property(t => t.TenantId).IsRequired();
        builder.Property(t => t.ParentType).IsRequired().HasMaxLength(50);
        builder.Property(t => t.LeadId);
        builder.Property(t => t.OpportunityId);
        builder.Property(t => t.Title).IsRequired().HasMaxLength(255);
        builder.Property(t => t.Description);
        builder.Property(t => t.Priority).HasMaxLength(20).HasDefaultValue("Medium");
        builder.Property(t => t.IsCompleted).HasDefaultValue(false);
        builder.Property(t => t.DueDate).IsRequired();
        builder.Property(t => t.CompletedAt);
        builder.Property(t => t.AssignedToUserId).IsRequired();
        builder.Property(t => t.CreatedByUserId).IsRequired();
        builder.Property(t => t.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();
        builder.Property(t => t.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP").IsRequired();

        // Relationships
        builder.HasOne<Lead>()
            .WithMany()
            .HasForeignKey(t => t.LeadId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Opportunity>()
            .WithMany()
            .HasForeignKey(t => t.OpportunityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(t => new { t.AssignedToUserId, t.DueDate })
            .HasName("idx_tasks_open")
            .HasFilter("[IsCompleted] = 0");

        builder.HasIndex(t => new { t.LeadId, t.DueDate })
            .HasName("idx_tasks_lead");

        builder.HasIndex(t => new { t.OpportunityId, t.DueDate })
            .HasName("idx_tasks_opportunity");
    }
}
```

**Key Patterns:**
- **Partial Index:** idx_tasks_open (only incomplete tasks)
- **Default Values:** Priority='Medium', IsCompleted=false
- **Required Relationships:** AssignedToUserId and CreatedByUserId always set

---

## 4. Value Object Definitions

### 4.1 ContactInfo Value Object

```csharp
using System.Text.RegularExpressions;
using Ardalis.Result;

namespace SpaceOS.Modules.CRM.Domain.ValueObjects;

public record ContactInfo
{
    public string FirstName { get; }
    public string LastName { get; }
    public string? EmailAddress { get; }
    public bool EmailVerified { get; }
    public string? PhoneNumber { get; }
    public string? CountryCode { get; }

    private ContactInfo(string firstName, string lastName, string? emailAddress, bool emailVerified, string? phoneNumber, string? countryCode)
    {
        FirstName = firstName;
        LastName = lastName;
        EmailAddress = emailAddress;
        EmailVerified = emailVerified;
        PhoneNumber = phoneNumber;
        CountryCode = countryCode;
    }

    public static Result<ContactInfo> Create(string firstName, string lastName, string? emailAddress = null, string? phoneNumber = null, string? countryCode = null)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(firstName) || firstName.Length > 100)
            errors.Add("FirstName required and max 100 characters");

        if (string.IsNullOrWhiteSpace(lastName) || lastName.Length > 100)
            errors.Add("LastName required and max 100 characters");

        if (!string.IsNullOrEmpty(emailAddress))
        {
            if (!Regex.IsMatch(emailAddress, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors.Add("Invalid email format");
            if (emailAddress.Length > 255)
                errors.Add("Email max 255 characters");
        }

        if (!string.IsNullOrEmpty(phoneNumber) && phoneNumber.Length > 20)
            errors.Add("Phone max 20 characters");

        if (!string.IsNullOrEmpty(countryCode) && countryCode.Length != 2)
            errors.Add("CountryCode must be 2 characters");

        if (errors.Any())
            return Result<ContactInfo>.Invalid(errors);

        return Result<ContactInfo>.Success(new ContactInfo(
            firstName.Trim(),
            lastName.Trim(),
            emailAddress?.Trim(),
            false,
            phoneNumber?.Trim(),
            countryCode?.ToUpperInvariant()
        ));
    }

    public string FullName => $"{FirstName} {LastName}".Trim();

    public Result<ContactInfo> VerifyEmail()
    {
        if (string.IsNullOrEmpty(EmailAddress))
            return Result<ContactInfo>.Error("No email to verify");

        return Result<ContactInfo>.Success(new ContactInfo(
            FirstName,
            LastName,
            EmailAddress,
            true,
            PhoneNumber,
            CountryCode
        ));
    }
}
```

**Key Patterns:**
- **Static Factory:** Create() method returns Result<T> (no exceptions)
- **Immutable Record:** Value object is a record with init-only properties
- **Validation:** Email regex, length limits, required fields
- **Derived Properties:** FullName computed from FirstName + LastName

### 4.2 Money Value Object

```csharp
using Ardalis.Result;

namespace SpaceOS.Modules.CRM.Domain.ValueObjects;

public record Money
{
    public decimal Amount { get; }
    public string Currency { get; } // ISO 4217 code (EUR, USD, GBP, etc.)

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, string currency = "EUR")
    {
        if (amount < 0)
            return Result<Money>.Invalid("Amount cannot be negative");

        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            return Result<Money>.Invalid("Currency must be 3-character ISO code");

        if (amount != 0 && amount < 0.01m)
            return Result<Money>.Invalid("Amount must be >= 0.01 if non-zero");

        return Result<Money>.Success(new Money(Math.Round(amount, 2), currency.ToUpperInvariant()));
    }

    public static Result<Money> Zero(string currency = "EUR") => Create(0, currency);

    // Business operations
    public static Result<Money> operator +(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            return Result<Money>.Error($"Cannot add different currencies: {left.Currency} + {right.Currency}");

        return Create(left.Amount + right.Amount, left.Currency);
    }

    public static Result<Money> operator -(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            return Result<Money>.Error($"Cannot subtract different currencies: {left.Currency} - {right.Currency}");

        return Create(left.Amount - right.Amount, left.Currency);
    }

    public string FormattedValue => $"{Amount:F2} {Currency}";

    public override string ToString() => FormattedValue;
}
```

**Key Patterns:**
- **Type-Safe Currency:** Prevents mixing EUR + USD
- **Precision:** 2 decimal places (no rounding errors)
- **Operator Overloading:** + and - check currency match
- **Formatted Display:** FormattedValue for UI presentation

---

## 5. Migration Strategy

### 5.1 Initial Migration File Structure

**File:** `Infrastructure/Persistence/Migrations/20260702_InitialCreate.cs`

**Key Elements:**

```csharp
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 1. Create schema
        migrationBuilder.EnsureSchema(name: "crm");

        // 2. Create tables
        // - leads
        // - opportunities
        // - activities
        // - tasks

        // 3. Create indexes

        // 4. Create RLS policies (via raw SQL)

        // 5. Grant role permissions (via raw SQL)
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // 1. Drop RLS policies
        // 2. Drop indexes
        // 3. Drop tables (reverse order)
        // 4. Drop schema
    }
}
```

**Snapshot File:** `Infrastructure/Persistence/CrmDbContextModelSnapshot.cs`
- Auto-generated by EF Core
- Documents current schema state

### 5.2 Raw SQL for RLS Policies

```csharp
migrationBuilder.Sql("""
    ALTER TABLE crm.leads ENABLE ROW LEVEL SECURITY;
    ALTER TABLE crm.opportunities ENABLE ROW LEVEL SECURITY;
    ALTER TABLE crm.activities ENABLE ROW LEVEL SECURITY;
    ALTER TABLE crm.tasks ENABLE ROW LEVEL SECURITY;
""");

migrationBuilder.Sql("""
    CREATE POLICY tenant_isolation_leads ON crm.leads
      USING (tenant_id = current_setting('app.tenant_id')::uuid)
      WITH CHECK (tenant_id = current_setting('app.tenant_id')::uuid);

    CREATE POLICY tenant_isolation_opportunities ON crm.opportunities
      USING (tenant_id = current_setting('app.tenant_id')::uuid)
      WITH CHECK (tenant_id = current_setting('app.tenant_id')::uuid);

    -- ... (activities, tasks)
""");
```

---

## 6. DbConnection Interceptor (Tenant Context)

### DbConnectionInterceptor Pattern

```csharp
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace SpaceOS.Modules.CRM.Infrastructure.Persistence;

public class TenantContextInterceptor : DbConnectionInterceptor
{
    private readonly ITenantProvider _tenantProvider;

    public TenantContextInterceptor(ITenantProvider tenantProvider)
    {
        _tenantProvider = tenantProvider;
    }

    public override async Task<InterceptionResult> ConnectionOpeningAsync(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result,
        CancellationToken cancellationToken = default)
    {
        await SetTenantContextAsync(connection, cancellationToken).ConfigureAwait(false);
        return result;
    }

    public override InterceptionResult ConnectionOpening(
        DbConnection connection,
        ConnectionEventData eventData,
        InterceptionResult result)
    {
        SetTenantContext(connection);
        return result;
    }

    private async Task SetTenantContextAsync(DbConnection connection, CancellationToken ct)
    {
        var tenantId = _tenantProvider.TenantId;
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT set_config('app.tenant_id', '{tenantId}', false);";
        await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
    }

    private void SetTenantContext(DbConnection connection)
    {
        var tenantId = _tenantProvider.TenantId;
        using var command = connection.CreateCommand();
        command.CommandText = $"SELECT set_config('app.tenant_id', '{tenantId}', false);";
        command.ExecuteNonQuery();
    }
}
```

**Registration in Dependency Injection:**

```csharp
public static IServiceCollection AddCrmInfrastructure(this IServiceCollection services)
{
    services.AddDbContext<CrmDbContext>((sp, options) =>
    {
        var connectionString = sp.GetRequiredService<IConfiguration>()
            .GetConnectionString("DefaultConnection");

        options
            .UseNpgsql(connectionString, npgOptions =>
            {
                npgOptions.EnableRetryOnFailure(maxRetryCount: 3);
            })
            .AddInterceptors(sp.GetRequiredService<TenantContextInterceptor>());
    });

    services.AddScoped<TenantContextInterceptor>();

    return services;
}
```

---

## 7. EF Core Query Patterns

### 7.1 Typical Query Pattern (AsNoTracking)

```csharp
// Get all open leads for current user
public async Task<IEnumerable<LeadDto>> GetMyOpenLeadsAsync(Guid userId, CancellationToken ct)
{
    var leads = await _context.Leads
        .AsNoTracking()
        .Where(l => l.Status == LeadStatus.New && l.AssignedToUserId == userId)
        .OrderByDescending(l => l.CreatedAt)
        .Take(50)
        .ToListAsync(ct)
        .ConfigureAwait(false);

    return leads.Select(l => new LeadDto
    {
        Id = l.Id,
        FullName = l.ContactInfo.FullName,
        Email = l.ContactInfo.EmailAddress,
        CreatedAt = l.CreatedAt
    });
}
```

**Key Patterns:**
- `AsNoTracking()` for read-only queries (5-10% performance gain)
- `Select()` to project to DTOs (no unnecessary fields)
- `ConfigureAwait(false)` for async

### 7.2 Aggregate Root Query Pattern

```csharp
// Get Lead with activities for editing
public async Task<Lead?> GetLeadWithActivityAsync(Guid leadId, CancellationToken ct)
{
    return await _context.Leads
        .Include(l => l.Activities)  // Eager load child entities
        .FirstOrDefaultAsync(l => l.Id == leadId, ct)
        .ConfigureAwait(false);
}
```

**Key Patterns:**
- `Include()` to fetch child entities (Activities)
- Not using `AsNoTracking()` because we'll modify the aggregate

---

## 8. Dependency Injection Setup

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace SpaceOS.Modules.CRM.Infrastructure.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddCrmPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<CrmDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found");

            var interceptor = sp.GetRequiredService<TenantContextInterceptor>();

            options
                .UseNpgsql(connectionString, npgSqlOptions =>
                {
                    npgSqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelaySeconds: 30);
                    npgSqlOptions.CommandTimeout(60);
                })
                .AddInterceptors(interceptor);
        });

        // Interceptor
        services.AddScoped<TenantContextInterceptor>();

        // Repositories
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<IOpportunityRepository, OpportunityRepository>();

        return services;
    }
}
```

---

## 9. Testing Strategy

### Unit Tests (No DB)

```csharp
[TestFixture]
public class ContactInfoTests
{
    [Test]
    public void Create_ValidInput_ReturnsSuccess()
    {
        var result = ContactInfo.Create("John", "Doe", "john@example.com");
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.FullName, Is.EqualTo("John Doe"));
    }

    [Test]
    public void Create_InvalidEmail_ReturnsError()
    {
        var result = ContactInfo.Create("John", "Doe", "invalid-email");
        Assert.That(result.IsSuccess, Is.False);
    }
}
```

### Integration Tests (With DB)

```csharp
[TestFixture]
public class LeadRepositoryIntegrationTests
{
    private CrmDbContext _context;

    [SetUp]
    public async Task SetUpAsync()
    {
        // Use TestcontainerSQL for PostgreSQL test container
        var container = /* PostgreSQL test container */;
        var connectionString = container.GetConnectionString();

        var options = new DbContextOptionsBuilder<CrmDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        _context = new CrmDbContext(options, new MockTenantProvider());
        await _context.Database.MigrateAsync();
    }

    [Test]
    public async Task GetLeadById_ExistingId_ReturnsLeadAsync()
    {
        var lead = /* create test lead */;
        _context.Leads.Add(lead);
        await _context.SaveChangesAsync();

        var repository = new LeadRepository(_context);
        var result = await repository.GetByIdAsync(lead.Id, CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Id, Is.EqualTo(lead.Id));
    }
}
```

---

## 10. Common Pitfalls & Solutions

| Pitfall | Symptom | Solution |
|---------|---------|----------|
| **Missing AsNoTracking()** | Slow queries, high memory | Add `AsNoTracking()` to all read-only queries |
| **Forgetting ConfigureAwait(false)** | Deadlocks in console apps | Always add `.ConfigureAwait(false)` after await |
| **Eager loading everything** | N+1 queries | Use `Include()` only for needed relationships |
| **Querying without tenant filter** | Data leak across tenants | RLS enforces, but app-layer filter recommended |
| **Soft delete logic missing** | Deleted records still visible | `HasQueryFilter()` in DbContext |
| **Concurrency conflicts** | Unhandled optimistic locking | Implement `Version` concurrency token |

---

## 11. Next Steps

1. ✅ **INFRASTRUCTURE_SCHEMA_DESIGN.md** (schema + indexes + RLS)
2. ✅ **EF_CORE_CONFIGURATION_PLAN.md** (this document)
3. → **REPOSITORY_IMPLEMENTATION_PLAN.md** (repository interfaces + implementations)

---

**Status:** READY FOR REVIEW
**Generated:** 2026-07-02 16:15 UTC
**Task:** MSG-BACKEND-116 Phase 1
**Next:** Repository Implementation Planning
