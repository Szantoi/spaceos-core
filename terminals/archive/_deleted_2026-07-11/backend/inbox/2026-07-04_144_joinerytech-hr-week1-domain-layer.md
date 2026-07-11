---
id: MSG-BACKEND-144
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
epic_id: EPIC-JT-HR
checkpoint_id: CP-HR-BACKEND
ref: MSG-ARCHITECT-061-DONE
created: 2026-07-04
unblocked_at: 2026-07-06
unblocked_by: conductor
# blocked_by: MSG-BACKEND-143 (DONE 2026-07-06 14:20, unblocked)
estimated_nwt: 120
content_hash: 84ed34963a9f733bc353ddba561ca7a6a66c6e6a5a96417d678e18ab3c3bebfd
---

# JoineryTech HR Week 1 — Domain Layer Implementation

**Epic:** EPIC-JT-HR (Human Resources & Capacity Management)
**Checkpoint:** CP-HR-BACKEND
**Estimated:** 120 NWT (~4 hours)
**Blocked by:** MSG-BACKEND-143 (Kontrolling Week 2 — finish first to avoid context switching)

---

## Context

Az Architect elkészítette a HR & Capacity Management Week 0 OpenAPI specifikációt ✅:
- **File:** `/opt/spaceos/docs/api/joinerytech-hr-v1.yaml` (52KB, 25 endpoints)
- **Validation:** Redocly lint 0 errors, Orval code-gen test passed ✅
- **MSG:** MSG-ARCHITECT-061-DONE

A Domain Model specifikáció készen áll:
- **File:** `/opt/spaceos/docs/joinerytech/domain/HR_DOMAIN_MODEL.md`
- **Aggregates:** Employee, Absence
- **Domain Services:** CapacityCalculationService, VacationEntitlementService
- **FSM:** Absence (5 transitions: approve, reject, start, complete, reopen)

**Most a Week 1 Domain Layer-t kell implementálni.**

---

## Deliverables

### 1. Projekt struktúra
```
spaceos-modules-hr/
  Domain/
    Aggregates/
      Employee.cs
      Absence.cs
    ValueObjects/
      EmployeeId.cs
      AbsenceId.cs
      PersonalData.cs
      Skill.cs
      PayGrade.cs
      Email.cs
      Color.cs
      Address.cs
    Enums/
      AbsenceStatus.cs
      AbsenceType.cs
      EmploymentType.cs
      Department.cs
      SkillKey.cs
      SkillLevel.cs
      MaritalStatus.cs
    Services/
      CapacityCalculationService.cs
      VacationEntitlementService.cs
      ICapacityCalculationService.cs
      IVacationEntitlementService.cs
    Events/
      EmployeeCreatedEvent.cs
      EmployeeSkillAddedEvent.cs
      EmployeeSkillUpdatedEvent.cs
      EmployeeSkillRemovedEvent.cs
      EmployeePersonalDataUpdatedEvent.cs
      EmployeePromotedEvent.cs
      EmployeeDeactivatedEvent.cs
      EmployeeReactivatedEvent.cs
      AbsenceRequestedEvent.cs
      AbsenceApprovedEvent.cs
      AbsenceRejectedEvent.cs
      AbsenceStartedEvent.cs
      AbsenceCompletedEvent.cs
      AbsenceReopenedEvent.cs
    Repositories/
      IEmployeeRepository.cs
      IAbsenceRepository.cs
    FSM/
      AbsenceStatusTransitions.cs
  Tests/
    Domain/
      EmployeeTests.cs
      AbsenceTests.cs
      CapacityCalculationServiceTests.cs
      VacationEntitlementServiceTests.cs
      AbsenceFsmTests.cs
```

### 2. Employee Aggregate (Employee.cs)

**Invariants:**
- Name must not be empty
- WeeklyHours must be 0-168
- Email must be valid and unique per tenant
- VacationBase ≥ 0 (default 20 for Hungary)
- Active employees can have assignments; inactive cannot

**Factory method:**
```csharp
public static Employee Create(
    TenantId tenantId,
    string name,
    string role,
    Department department,
    FacilityId facilityId,
    PayGrade payGrade,
    decimal weeklyHours,
    string email)
```

**Methods:**
- `AddSkill(SkillKey key, SkillLevel level)`
- `UpdateSkill(SkillKey key, SkillLevel level)`
- `RemoveSkill(SkillKey key)`
- `UpdatePersonal(PersonalData personalData)`
- `PromoteToPayGrade(PayGrade newGrade)`
- `Deactivate()` — soft delete
- `Reactivate()`
- `GenerateInitials(string name)` — private helper

**Properties:**
- Skill list: `private readonly List<Skill> _skills = new();`
- ReadOnly exposure: `public IReadOnlyList<Skill> Skills => _skills.AsReadOnly();`
- PersonalData (sensitive): `public PersonalData Personal { get; private set; }`

**Domain Events:**
- EmployeeCreatedEvent
- EmployeeSkillAddedEvent, EmployeeSkillUpdatedEvent, EmployeeSkillRemovedEvent
- EmployeePersonalDataUpdatedEvent
- EmployeePromotedEvent
- EmployeeDeactivatedEvent, EmployeeReactivatedEvent

### 3. Absence Aggregate (Absence.cs)

**Invariants:**
- EndDate ≥ StartDate
- Rejection reason required when rejecting
- WorkDays calculated excluding weekends (Sat/Sun)
- FSM transition validation (AbsenceStatusTransitions)

**Factory method:**
```csharp
public static Absence Create(
    TenantId tenantId,
    EmployeeId employeeId,
    AbsenceType type,
    DateOnly startDate,
    DateOnly endDate,
    string reason)
```

**FSM Transitions (5):**
- `Approve(UserId approvedBy)` — Pending → Approved
- `Reject(UserId rejectedBy, string rejectionReason)` — Pending → Rejected
- `StartAbsence()` — Approved → InProgress
- `CompleteAbsence()` — InProgress → Completed
- `Reopen()` — Rejected → Pending

**Helper:**
- `CalculateWorkDays(DateOnly start, DateOnly end)` — private, excludes weekends

**Domain Events:**
- AbsenceRequestedEvent
- AbsenceApprovedEvent, AbsenceRejectedEvent
- AbsenceStartedEvent, AbsenceCompletedEvent
- AbsenceReopenedEvent

### 4. Value Objects

**PersonalData (sensitive data - requires hr.manage permission):**
- Children (0-10)
- MaritalStatus
- BirthDate, BirthName, BirthPlace, MotherName (Hungarian legal requirement)
- Nationality (default: "HU")
- Address, PrivatePhone, PrivateEmail
- EmergencyContactName, EmergencyContactPhone
- TajNumber (social security), TaxId, IdCardNumber, BankAccount (IBAN)

**Skill:**
- SkillKey (enum)
- SkillLevel (enum)

**PayGrade:**
- Name (string)
- HourlyRate (decimal)

**Email:**
- Value (string)
- Validation: regex for email format

**Color:**
- Hex (string, e.g., "#FF5733")
- Random() static method for avatar color generation

**Address:**
- Street, City, PostalCode, Country

### 5. Enums

**AbsenceStatus:**
- Pending
- Approved
- Rejected
- InProgress
- Completed

**AbsenceType:**
- Vacation
- SickLeave
- UnpaidLeave
- Other

**EmploymentType:**
- FullTime
- PartTime
- Contractor

**Department:**
- Production
- Logistics
- Sales
- Administration
- IT
- Maintenance

**SkillKey:**
- CNCProgramming
- ManualLathe
- Welding
- Painting
- Assembly
- QualityControl
- ForklifDriver
- ElectricalMaintenance

**SkillLevel:**
- Beginner
- Intermediate
- Advanced
- Expert

**MaritalStatus:**
- Unknown
- Single
- Married
- Divorced
- Widowed

### 6. Domain Services

**CapacityCalculationService:**
```csharp
public interface ICapacityCalculationService
{
    decimal CalculateDailyCapacity(Employee employee);
    DailyLoad CalculateDailyLoad(EmployeeId employeeId, DateOnly date, IEnumerable<Assignment> assignments, IEnumerable<Absence> absences);
    WeekSummary CalculateWeekSummary(EmployeeId employeeId, DateOnly monday, IEnumerable<Assignment> assignments, IEnumerable<Absence> absences);
    HashSet<(EmployeeId, DateOnly)> DetectOverloads(IEnumerable<Employee> employees, DateOnly startDate, DateOnly endDate, IEnumerable<Assignment> assignments, IEnumerable<Absence> absences);
}
```

**Implementation:**
- DailyCapacity = WeeklyHours / 5 (assume 5-day work week)
- Absence blocks capacity if status is Approved/InProgress/Completed
- WeekSummary calculates Mon-Fri (excludes weekends)
- Overload detection: load > capacity

**Helper types:**
- `DailyLoad` (Hours, IsAbsent, IsOverloaded)
- `WeekSummary` (EmployeeId, WeekStart, TotalHours, DaysAbsent, DaysOverloaded)

**VacationEntitlementService:**
```csharp
public interface IVacationEntitlementService
{
    VacationEntitlement CalculateEntitlement(Employee employee);
    VacationBalance CalculateBalance(Employee employee, int year, IEnumerable<Absence> absences);
    SickLeaveBalance CalculateSickLeaveBalance(int year, IEnumerable<Absence> absences);
}
```

**Implementation:**
- Hungarian Labor Code (Mt. §118) child vacation days:
  - 1 child: +2 days
  - 2 children: +4 days
  - 3+ children: +7 days
- Base vacation: 20 days (default)
- Sick leave: 15 days/year (Mt. §123)

**Helper types:**
- `VacationEntitlement` (Base, ChildExtra, Total)
- `VacationBalance` (EmployeeId, Year, Entitlement, Base, ChildExtra, Used, Remaining)
- `SickLeaveBalance` (Year, Entitlement, Used, Remaining)

### 7. FSM Transition Validator

**AbsenceStatusTransitions:**
```csharp
public static class AbsenceStatusTransitions
{
    public static bool IsValidTransition(AbsenceStatus from, AbsenceStatus to);
    public static IReadOnlyList<AbsenceStatus> GetAllowedTransitions(AbsenceStatus from);
}
```

**Transition Rules:**
- Pending → Approved, Rejected
- Approved → InProgress
- Rejected → Pending (reopen)
- InProgress → Completed
- No backwards transitions (immutable audit trail)

### 8. Repository Contracts

**IEmployeeRepository:**
```csharp
public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(EmployeeId id, CancellationToken ct = default);
    Task<Employee?> GetByEmailAsync(TenantId tenantId, Email email, CancellationToken ct = default);
    Task<IEnumerable<Employee>> GetActiveByDepartmentAsync(TenantId tenantId, Department department, CancellationToken ct = default);
    Task<IEnumerable<Employee>> GetActiveBySkillAsync(TenantId tenantId, SkillKey skill, CancellationToken ct = default);
    Task AddAsync(Employee employee, CancellationToken ct = default);
    Task UpdateAsync(Employee employee, CancellationToken ct = default);
}
```

**IAbsenceRepository:**
```csharp
public interface IAbsenceRepository
{
    Task<Absence?> GetByIdAsync(AbsenceId id, CancellationToken ct = default);
    Task<IEnumerable<Absence>> GetByEmployeeAndYearAsync(EmployeeId employeeId, int year, CancellationToken ct = default);
    Task<IEnumerable<Absence>> GetPendingAsync(TenantId tenantId, CancellationToken ct = default);
    Task<IEnumerable<Absence>> GetActiveAbsencesAsync(TenantId tenantId, DateOnly date, CancellationToken ct = default);
    Task AddAsync(Absence absence, CancellationToken ct = default);
    Task UpdateAsync(Absence absence, CancellationToken ct = default);
}
```

### 9. Unit Tests

**Test coverage minimum: 80%**

**EmployeeTests.cs (15+ tests):**
- Employee creation validation (name, weeklyHours, email)
- Skill add/update/remove (duplicate detection, not found errors)
- Personal data update
- Pay grade promotion
- Deactivate/Reactivate
- Initials generation (1 word, 2 words, empty)
- Domain event assertions

**AbsenceTests.cs (20+ tests):**
- Absence creation validation (endDate >= startDate)
- WorkDays calculation (weekends excluded)
- FSM transitions: Approve, Reject, Start, Complete, Reopen
- Invalid transitions (exception assertions)
- Rejection reason validation
- Domain event assertions

**CapacityCalculationServiceTests.cs (10+ tests):**
- DailyCapacity calculation (weeklyHours / 5)
- DailyLoad with blocking absences (Approved, InProgress, Completed)
- DailyLoad ignores non-blocking absences (Pending, Rejected)
- WeekSummary aggregation (Mon-Fri only)
- Overload detection (load > capacity)

**VacationEntitlementServiceTests.cs (12+ tests):**
- Child vacation days calculation (0, 1, 2, 3+ children)
- Vacation balance calculation (entitlement - used)
- Vacation usage only counts blocking absences (Approved, InProgress, Completed)
- Sick leave balance (15 days/year)
- Edge cases: no personal data, 0 children, 10 children

**AbsenceFsmTests.cs (10+ tests):**
- IsValidTransition for all valid paths
- IsValidTransition rejects invalid paths
- GetAllowedTransitions for each status
- Edge case: Reopen only from Rejected

**Test pattern (EXAMPLE):**
```csharp
[Fact]
public void Create_ValidEmployee_ShouldSucceed()
{
    // Arrange
    var tenantId = TenantId.New();
    var facilityId = FacilityId.New();
    var payGrade = PayGrade.Create("Grade 5", 2500);

    // Act
    var employee = Employee.Create(
        tenantId,
        "János Kovács",
        "CNC Operator",
        Department.Production,
        facilityId,
        payGrade,
        40.0m,
        "janos.kovacs@example.com");

    // Assert
    employee.Should().NotBeNull();
    employee.Name.Should().Be("János Kovács");
    employee.Initials.Should().Be("JK");
    employee.WeeklyHours.Should().Be(40.0m);
    employee.Active.Should().BeTrue();
    employee.VacationBase.Should().Be(20); // Hungarian default
    employee.DomainEvents.Should().ContainSingle(e => e is EmployeeCreatedEvent);
}

[Fact]
public void Approve_FromPending_ShouldTransitionToApproved()
{
    // Arrange
    var absence = Absence.Create(
        TenantId.New(),
        EmployeeId.New(),
        AbsenceType.Vacation,
        new DateOnly(2026, 8, 1),
        new DateOnly(2026, 8, 5),
        "Summer vacation");
    var approvedBy = UserId.New();

    // Act
    absence.Approve(approvedBy);

    // Assert
    absence.Status.Should().Be(AbsenceStatus.Approved);
    absence.ApprovedByUserId.Should().Be(approvedBy);
    absence.ApprovedAt.Should().NotBeNull();
    absence.DomainEvents.Should().Contain(e => e is AbsenceApprovedEvent);
}

[Fact]
public void CalculateEntitlement_ThreeChildren_ShouldReturn27Days()
{
    // Arrange
    var service = new VacationEntitlementService();
    var employee = CreateEmployeeWithChildren(3);

    // Act
    var entitlement = service.CalculateEntitlement(employee);

    // Assert
    entitlement.Base.Should().Be(20);
    entitlement.ChildExtra.Should().Be(7); // 3+ children = +7 days
    entitlement.Total.Should().Be(27);
}
```

---

## .csproj Dependencies

**Add to spaceos-modules-hr.csproj:**
```xml
<PackageReference Include="FluentAssertions" Version="6.12.0" />
<PackageReference Include="xunit" Version="2.9.0" />
<PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
```

---

## Security & Validation Considerations

1. **PersonalData Protection:**
   - Sensitive fields (TAJ, TaxId, BankAccount) require `hr.manage` permission
   - Never log or expose in DTOs without authorization check
   - Application layer must enforce this (Week 2)

2. **Email Uniqueness:**
   - Repository must enforce unique constraint per tenant
   - Application layer validates before calling Create

3. **FSM Immutability:**
   - No backwards transitions (except Rejected → Pending reopen)
   - Domain Events create audit trail (never delete events)

4. **Input Validation:**
   - All factory methods validate inputs (ArgumentException on invalid)
   - WeeklyHours: 0-168 (max theoretical hours/week)
   - Children: 0-10
   - Rejection reason: required when rejecting

---

## Build & Test Gate

**Before submitting DONE:**
```bash
cd /opt/spaceos/spaceos-modules-hr
dotnet build
dotnet test --verbosity normal

# Expected:
# Build: 0 warnings, 0 errors
# Tests: 67+ tests, all green ✅
```

**Test coverage minimum:** 80% for domain layer

---

## Acceptance Criteria

- [ ] Employee aggregate implemented with all methods (Create, AddSkill, UpdateSkill, RemoveSkill, UpdatePersonal, PromoteToPayGrade, Deactivate, Reactivate)
- [ ] Absence aggregate implemented with all FSM transitions (Approve, Reject, StartAbsence, CompleteAbsence, Reopen)
- [ ] 7 enums implemented (AbsenceStatus, AbsenceType, EmploymentType, Department, SkillKey, SkillLevel, MaritalStatus)
- [ ] 6 value objects implemented (PersonalData, Skill, PayGrade, Email, Color, Address)
- [ ] CapacityCalculationService implemented with DailyLoad, WeekSummary, overload detection
- [ ] VacationEntitlementService implemented with Hungarian Labor Code compliance (Mt. §118, §123)
- [ ] AbsenceStatusTransitions FSM validator implemented
- [ ] 14 domain events implemented
- [ ] 2 repository contracts implemented (IEmployeeRepository, IAbsenceRepository)
- [ ] **67+ unit tests** — all green ✅
- [ ] Build: 0 warnings, 0 errors
- [ ] Test coverage: ≥80%

---

## DONE Outbox Format

**File:** `2026-07-04_NNN_msg-144-hr-week1-domain-done.md`

**Frontmatter:**
```yaml
---
id: MSG-BACKEND-144-DONE
from: backend
to: conductor
type: done
status: READ
ref: MSG-BACKEND-144
epic_id: EPIC-JT-HR
checkpoint_id: CP-HR-BACKEND
created: YYYY-MM-DD
---
```

**Tartalom:**
- Összefoglaló: Mi készült el?
- Unit test eredmények (hány teszt, coverage %)
- Build státusz (warnings/errors)
- Files changed lista
- Következő lépés: Week 2 Application Layer (DTOs, Query/Command handlers, Integration Data Provider)

---

## References

- **Domain Model:** `/opt/spaceos/docs/joinerytech/domain/HR_DOMAIN_MODEL.md`
- **OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-hr-v1.yaml`
- **Architect DONE:** MSG-ARCHITECT-061-DONE
- **Hungarian Labor Code:**
  - Mt. §118: Vacation entitlement (20 days + child extra)
  - Mt. §123: Sick leave (15 days/year paid)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
