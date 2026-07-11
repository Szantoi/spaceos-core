---
id: MSG-BACKEND-011
from: conductor
to: backend
type: task
priority: critical
status: READ
model: sonnet
ref: SpaceOS_CatalogEHS_Hybrid_Architecture_FINAL.md
created: 2026-06-23
content_hash: 6b44661daa12f93fec0bc4ea915a43fe053d02a0a36e0e6dc401298b9da79bc8
---

# EHS Backend Week 1 — Security Infrastructure (BE-EHS-001→005)

## Kontextus

Az Architect elkészítette a **Catalog + EHS Hybrid Architecture** teljes review pipeline-ját (v1→v4). Most a **Week 1 Backend Track** első 5 taskját kapod meg, amelyek a **KRITIKUS security infrastruktúrát** építik ki.

**Architektúra dokumentumok:** `/opt/spaceos/docs/tasks/new/SpaceOS_CatalogEHS_Hybrid_Architecture_FINAL.md`

## 🔴 KRITIKUS Security Fixek

Ezek a taskok 2 CRITICAL security issue-t is kezelnek:

- **C1 (v3-Security):** RLS policy bypass — `organizationId` JWT-ből, NEM kliensből
- **C2 (v3-Security):** Mass assignment — audit mezők SOHA nem jöhetnek a kliensből

## Taskok (5.5 óra becsült munka)

### BE-EHS-001: Create EHS Module Structure (0.5h)

**Acceptance Criteria:**
- [ ] Create `SpaceOS.Modules.EHS` project structure
- [ ] Folders: `Domain/Entities`, `Application/Commands`, `Application/Queries`, `Infrastructure/Persistence`
- [ ] Add project reference to main solution
- [ ] Write `README.md` with module overview

**Files to create:**
- `spaceos-modules-ehs/Domain/Entities/`
- `spaceos-modules-ehs/Application/Commands/`
- `spaceos-modules-ehs/Application/Queries/`
- `spaceos-modules-ehs/Infrastructure/Persistence/`

---

### BE-EHS-002: Implement RiskAssessment Entity + Factory (1.5h)

**Acceptance Criteria:**
- [ ] Create `RiskAssessment.cs` entity
- [ ] Implement factory method `Create(...)` with domain validation
- [ ] Validate likelihood/severity range (1-5)
- [ ] **DOMAIN VALIDATION (v4-M3):** Enforce "high-risk (score > 15) requires notes"
- [ ] Compute `DataHash` using SHA256
- [ ] Add calculated properties: `RiskScoreBefore`, `RiskScoreAfter`, `ImprovementScore`
- [ ] Write unit test: Factory method throws exception for invalid likelihood
- [ ] Write unit test: High-risk without notes throws `DomainException`

**Files to create:**
- `spaceos-modules-ehs/Domain/Entities/RiskAssessment.cs`
- `spaceos-modules-ehs/Domain/Exceptions/DomainException.cs`

**Code snippet:**
```csharp
public static RiskAssessment Create(...)
{
    ValidateLikelihood(likelihoodBefore);
    ValidateSeverity(severityBefore);

    var riskScore = likelihoodBefore * severityBefore;
    if (riskScore > 15 && string.IsNullOrWhiteSpace(notes))
    {
        throw new DomainException("High-risk assessments (score > 15) require mitigation notes");
    }

    var assessment = new RiskAssessment { ... };
    assessment.DataHash = assessment.ComputeHash();
    return assessment;
}
```

---

### BE-EHS-003: Create DB Migration (v2 Fixes Applied) (1h)

**Acceptance Criteria:**
- [ ] Create `ehs` schema
- [ ] Create `ehs.risk_category` ENUM type (v2-M3 fix)
- [ ] Create `ehs.assessments` table (v2-M1 fix: skeleton table)
- [ ] Create `ehs.risk_assessments` table with:
  - [ ] Soft delete columns: `deleted_at`, `deleted_by`, `version` (v2-H1 fix)
  - [ ] FK constraint to `kernel.organizations` with `ON DELETE RESTRICT` (v2-H2 fix)
  - [ ] FK constraint to `ehs.assessments` (v2-M1 fix)
  - [ ] Composite index on `(organization_id, created_at DESC)` (v2-M4 fix)
- [ ] Enable RLS policy
- [ ] Write migration rollback script

**Files to create:**
- `spaceos-modules-ehs/Infrastructure/Persistence/Migrations/0001_create_ehs_module.sql`

**SQL snippet (v2 fixes):**
```sql
-- ✅ v2-M3 fix: ENUM type
CREATE TYPE ehs.risk_category AS ENUM ('machinery', 'chemical', 'ergonomic', 'psychosocial', 'other');

-- ✅ v2-H1 fix: soft delete columns
ALTER TABLE ehs.risk_assessments
  ADD COLUMN deleted_at TIMESTAMPTZ NULL,
  ADD COLUMN deleted_by VARCHAR(255) NULL,
  ADD COLUMN version INT NOT NULL DEFAULT 1;

-- ✅ v2-H2 fix: ON DELETE RESTRICT
CONSTRAINT fk_organization FOREIGN KEY (organization_id)
  REFERENCES kernel.organizations(id) ON DELETE RESTRICT;

-- ✅ v2-M4 fix: composite index
CREATE INDEX idx_risk_assessments_org_created
  ON ehs.risk_assessments(organization_id, created_at DESC)
  WHERE deleted_at IS NULL;
```

---

### BE-EHS-004: Implement ICurrentUserService (v3-C1 Fix) 🔴 CRITICAL (1h)

**SECURITY FIX:** RLS policy bypass prevention

**Acceptance Criteria:**
- [ ] Create `ICurrentUserService` interface
- [ ] Implement `CurrentUserService` (read from `HttpContext.User.Claims`)
- [ ] Extract `OrganizationId` from JWT claim `"organization_id"`
- [ ] Extract `UserId` from JWT claim `NameIdentifier`
- [ ] Register in DI container
- [ ] Write unit test: Service extracts org ID from mocked ClaimsPrincipal

**Files to create:**
- `spaceos-kernel/Application/Common/Interfaces/ICurrentUserService.cs`
- `spaceos-kernel/Infrastructure/Identity/CurrentUserService.cs`

**Why CRITICAL:** Ez az egyetlen megbízható forrása a `organizationId`-nak. SOHA ne fogadj el org ID-t a kliensből!

---

### BE-EHS-005: Implement TenantIsolationInterceptor (v3-C1 Fix) 🔴 CRITICAL (1h)

**SECURITY FIX:** RLS policy bypass prevention

**Acceptance Criteria:**
- [ ] Create `TenantIsolationInterceptor` class
- [ ] Override `ReaderExecutingAsync` method
- [ ] Set `app.current_organization_id` GUC parameter before every query
- [ ] Get org ID from `ICurrentUserService`
- [ ] Register interceptor in `DbContext` options
- [ ] Write integration test: RLS policy filters org B's data when org A queries

**Files to create:**
- `spaceos-kernel/Infrastructure/Persistence/Interceptors/TenantIsolationInterceptor.cs`

**Code snippet:**
```csharp
public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(...)
{
    var orgId = _currentUser.OrganizationId;
    command.CommandText = $"SET LOCAL app.current_organization_id = {orgId}; {command.CommandText}";
    return await base.ReaderExecutingAsync(...);
}
```

**Why CRITICAL:** Ez garantálja hogy MINDEN query RLS policy-val fut. Security perimeter!

---

## Definition of Done (KRITIKUS!)

- [ ] Minden task unit test-je PASS
- [ ] BE-EHS-004 + BE-EHS-005 integration teszt PASS (RLS isolation)
- [ ] Build 0 hiba, 0 warning
- [ ] Migration SQL rollback script működik
- [ ] Code formázva (`dotnet format`)
- [ ] README.md frissítve EHS modul leírással

## Tesztelési útmutató

### Unit Test példa (BE-EHS-002):
```csharp
[Fact]
public void Create_HighRiskWithoutNotes_ThrowsDomainException()
{
    // Arrange
    var likelihood = 4;
    var severity = 5;  // score = 20 (> 15)
    var notes = "";

    // Act & Assert
    Assert.Throws<DomainException>(() =>
        RiskAssessment.Create(
            organizationId: Guid.NewGuid(),
            assessmentId: 1,
            likelihoodBefore: likelihood,
            severityBefore: severity,
            category: "machinery",
            notes: notes,
            createdBy: "test-user"
        )
    );
}
```

### Integration Test példa (BE-EHS-005):
```csharp
[Fact]
public async Task Query_OrgA_CannotSeeOrgB_Data()
{
    // Arrange
    var orgA = Guid.NewGuid();
    var orgB = Guid.NewGuid();

    await SeedRiskAssessment(organizationId: orgB, ...);

    // Act (simulate user from Org A)
    _currentUserService.Setup(x => x.OrganizationId).Returns(orgA);
    var results = await _dbContext.RiskAssessments.ToListAsync();

    // Assert (RLS should filter out Org B's data)
    Assert.Empty(results);
}
```

---

## Következő lépések

Ha ezek a taskok **DONE**:
1. Conductor kiadja a következő backend bundle-t: **BE-EHS-006→011** (POST/GET endpoints + validation + security)
2. Frontend párhuzamosan implementálja a **FE-CAT-001→007** (Catalog Filter UI)
3. Week 1 végére: **Catalog MVP + EHS Backend DEPLOYED**

---

**Prioritás:** CRITICAL
**Becsült idő:** ~5.5 óra
**Review:** Automatikus (TypeScript reviewer pipeline)
**Deploy blocker:** Igen — ezek nélkül **NEM lehet Week 1 deployt csinálni**

**Files changed:** (majd a DONE outbox-ban)
