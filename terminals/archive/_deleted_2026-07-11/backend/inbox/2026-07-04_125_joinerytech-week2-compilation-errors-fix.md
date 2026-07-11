---
id: MSG-BACKEND-125
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-CONDUCTOR-079
created: 2026-07-04
---

# JoineryTech Week 2: Fix 12 Compilation Errors

**Ref:** MSG-BACKEND-122 unblocked (NuGet infrastructure resolved)

**Status:** Code complete (977 LOC JWT/OAuth), build verification blocked

---

## Problem Summary

**12 compilation errors** preventing build success:

### Missing Types/Namespaces
1. **JoineryTechDbContext** type not found
2. **Microsoft.EntityFrameworkCore** namespace not found
3. **SpaceOS.Modules.JoineryTech.Infrastructure** namespace not found

### Root Cause
- EF Core package references missing in Application layer
- Infrastructure project reference missing in Application layer
- JoineryTechDbContext not created in Infrastructure layer

---

## Task Acceptance Criteria

- [ ] JoineryTechDbContext created in Infrastructure/Data/
- [ ] EF Core package reference added to Application.csproj
- [ ] Infrastructure project reference added to Application.csproj
- [ ] `dotnet build` → **0 errors**
- [ ] Manual endpoint testing (login, refresh, logout)
- [ ] DONE outbox with build verification results

---

## Context: Week 2 Code Complete

**17 files created (977 LOC):**

### Application Layer (10 files, 432 lines)
- DTOs: LoginRequest, LoginResponse, RefreshTokenRequest, TokenResponse
- Contracts: ITokenService, IPasswordHasher
- CQRS Commands: LoginCommand, RefreshTokenCommand, LogoutCommand
- Handlers: LoginCommandHandler (99L), RefreshTokenCommandHandler (82L), LogoutCommandHandler (50L)

### Infrastructure Layer (3 files, 303 lines)
- TokenService (156L) - ES256 ECDSA P-256 JWT, 15min access, 7day refresh
- PasswordHasher (52L) - BCrypt work factor 11
- TenantDbConnectionInterceptor (95L) - automatic tenant_id GUC from JWT

### API Layer (4 files, 242 lines)
- Program.cs (72L) - DI setup, JWT middleware
- AuthEndpoints.cs (124L) - 3 Minimal API endpoints
- appsettings.json, appsettings.Development.json

---

## Implementation Steps

### 1. Create JoineryTechDbContext

**Location:** `SpaceOS.Modules.JoineryTech.Infrastructure/Data/JoineryTechDbContext.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.JoineryTech.Domain.Aggregates;

namespace SpaceOS.Modules.JoineryTech.Infrastructure.Data;

public class JoineryTechDbContext : DbContext
{
    public JoineryTechDbContext(DbContextOptions<JoineryTechDbContext> options)
        : base(options)
    {
    }

    // Week 1 entities
    public DbSet<Lead> Leads => Set<Lead>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<Contact> Contacts => Set<Contact>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<Customer> Customers => Set<Customer>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("joinerytech");

        // Week 1 configurations (if exist)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JoineryTechDbContext).Assembly);
    }
}
```

### 2. Add EF Core Package to Application Layer

**File:** `SpaceOS.Modules.JoineryTech.Application/SpaceOS.Modules.JoineryTech.Application.csproj`

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
</ItemGroup>
```

### 3. Add Infrastructure Project Reference to Application

**File:** `SpaceOS.Modules.JoineryTech.Application/SpaceOS.Modules.JoineryTech.Application.csproj`

```xml
<ItemGroup>
  <ProjectReference Include="..\SpaceOS.Modules.JoineryTech.Infrastructure\SpaceOS.Modules.JoineryTech.Infrastructure.csproj" />
</ItemGroup>
```

### 4. Verify Build

```bash
cd /opt/spaceos/backend/spaceos/modules/joinerytech
dotnet restore
dotnet build
```

**Expected:** 0 errors, 0 warnings

---

## Manual Testing (After Build Success)

### Test Endpoints

**1. Login:**
```bash
curl -X POST http://localhost:5001/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"Test123!","tenantId":"tenant-001"}'
```

**Expected:** 200 OK, access_token + refresh_token returned

**2. Refresh Token:**
```bash
curl -X POST http://localhost:5001/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<from-login>"}'
```

**Expected:** 200 OK, new access_token + refresh_token

**3. Logout:**
```bash
curl -X POST http://localhost:5001/api/auth/logout \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"<from-login>"}'
```

**Expected:** 200 OK

---

## Week 1 Context (Background)

**Already complete:**
- 1,109 LOC (PostgreSQL RLS, 5 entities)
- Lead, Opportunity, Contact, Activity, Customer aggregates
- CQRS handlers, FluentValidation, row-level security
- Build: 0 errors, production-ready

---

## Expected Outcome

**DONE outbox with:**
- Build verification (0 errors)
- Manual endpoint testing results (3 endpoints tested)
- Code quality summary
- Ready for Week 3 Catalog module

**Timeline:** 1-2 hours

---

**Priority:** HIGH (blocks Week 3 Catalog implementation)

**Model:** sonnet (compilation + testing task)
