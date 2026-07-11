---
id: MSG-ARCHITECT-061
from: conductor
to: architect
type: task
priority: high
status: READ
model: opus
epic_id: EPIC-JT-HR
ref: MSG-ARCHITECT-046
created: 2026-07-04
estimated_nwt: 120
content_hash: 3971606d718225dc8c86e89e61ddd7ca488aa492ddb289c437e7c95a0b665edc
---

# JoineryTech HR & Kapacitás — Week 0 Contract-First OpenAPI Spec

**Epic:** EPIC-JT-HR (HR & Kapacitás Modul)
**Priority:** HIGH (JoineryTech parallel development wave 2)
**Estimated:** 120 NWT (~4 hours)
**Dependency Status:** ✅ UNBLOCKED (EPIC-KERNEL-STABLE + EPIC-IDENTITY-V1 DONE)

---

## Context

EPIC-JT-HR (HR & Kapacitás modul) **párhuzamosan futhat** EPIC-JT-CRM-mel és EPIC-JT-CTRL-lel. Domain model kész (MSG-ARCHITECT-046), most a Contract-First Week 0 OpenAPI spec következik.

**Parallel Development Wave 2:**
- 🔄 **CRM**: Backend API implementáció (MSG-BACKEND-103 in progress)
- 🔄 **Dashboard**: Frontend widgets (MSG-FRONTEND-105 in progress)
- ✅ **Kontrolling**: Week 0 DONE (OpenAPI spec locked), Week 1 queued
- 🆕 **HR**: Week 0 OpenAPI spec (THIS TASK) → Backend Week 1 (queued) → Frontend Week 1.5 (MSW)

**Goal:** Lock OpenAPI contract BEFORE implementation starts → prevent integration rework

---

## Task: HR Module OpenAPI 3.1 Specification

**Reference:** MSG-ARCHITECT-046 (JoineryTech HR Domain Model)

**Contract-First Workflow:**
- **Day 1-2:** Endpoint inventory + data model definition
- **Day 3:** Draft OpenAPI spec (Employee CRUD + Absence FSM + Capacity queries)
- **Day 4:** Spec lock + validation + code-gen test

**ROI:** $4k Week 0 → $11-16k savings (2 weeks integration rework prevented)

---

## Deliverables

### 1. OpenAPI 3.1 Spec File

**Location:** `/opt/spaceos/docs/api/joinerytech-hr-v1.yaml`

**Sections:**

#### A. Employee Management (CRUD + Skills + Pay Grade)

Based on MSG-ARCHITECT-046 "Employee Aggregate":

```yaml
# Employee CRUD endpoints:
GET    /api/hr/employees
GET    /api/hr/employees/{employeeId}
POST   /api/hr/employees
PUT    /api/hr/employees/{employeeId}
DELETE /api/hr/employees/{employeeId}

# Employee Skills management:
POST   /api/hr/employees/{employeeId}/skills
PUT    /api/hr/employees/{employeeId}/skills/{skill}
DELETE /api/hr/employees/{employeeId}/skills/{skill}

# Employee Pay Grade promotion:
POST   /api/hr/employees/{employeeId}/promote
```

**Request/Response Models:**
- `CreateEmployeeCommand` (name, role, department, facilityId, weeklyHours, employmentType)
- `UpdateEmployeeCommand` (partial updates allowed)
- `EmployeeDto` (full employee data with skills, payGrade, personal data)
- `AddSkillCommand` (skill, proficiencyLevel)
- `PromoteEmployeeCommand` (newPayGrade, effectiveDate)

#### B. Absence Management (FSM + Approval Workflow)

Based on MSG-ARCHITECT-046 "Absence Aggregate":

```yaml
# Absence CRUD + FSM transitions:
GET    /api/hr/absences
GET    /api/hr/absences/{absenceId}
POST   /api/hr/absences
PUT    /api/hr/absences/{absenceId}
DELETE /api/hr/absences/{absenceId}

# FSM state transitions:
POST   /api/hr/absences/{absenceId}/approve
POST   /api/hr/absences/{absenceId}/reject
POST   /api/hr/absences/{absenceId}/start
POST   /api/hr/absences/{absenceId}/complete
POST   /api/hr/absences/{absenceId}/reopen
```

**Request/Response Models:**
- `CreateAbsenceCommand` (employeeId, type, startDate, endDate, reason)
- `AbsenceDto` (absenceId, employeeId, type, status, dates, approver, rejectionReason)
- `ApproveAbsenceCommand` (approverId, notes)
- `RejectAbsenceCommand` (approverId, rejectionReason)

**FSM Status Enum:**
```typescript
enum AbsenceStatus {
  Pending,      // Initial state
  Approved,     // Manager approved (blocking)
  Rejected,     // Manager rejected (can reopen)
  InProgress,   // Absence started (blocking)
  Completed     // Absence ended (blocking, terminal state)
}
```

#### C. Capacity Calculation Queries (Read-Heavy)

Based on MSG-ARCHITECT-046 "CapacityCalculationService":

```yaml
# Capacity queries:
GET /api/hr/capacity/employees/{employeeId}/daily
GET /api/hr/capacity/employees/{employeeId}/weekly
GET /api/hr/capacity/team/{departmentId}
GET /api/hr/vacation-entitlement/{employeeId}
```

**Response Models:**
- `DailyCapacityDto` (date, totalCapacity, totalLoad, availableCapacity, isOverloaded, assignments, absences)
- `WeeklyCapacityDto` (weekStart, totalHours, daysAbsent, daysOverloaded, dailyBreakdown)
- `TeamCapacityDto` (departmentId, employees, aggregatedCapacity)
- `VacationEntitlementDto` (employeeId, vacationBase, childExtra, totalEntitlement, used, remaining)

#### D. Data Models (Schemas)

**Domain Entities:**
- `Employee` (stored entity with skills, payGrade, personal data)
- `Absence` (stored entity with FSM status)
- `PersonalData` (sensitive value object: TAJ, tax ID, bank account, emergency contact)
- `Skill` (value object: name + proficiencyLevel)

**Enums:**
```typescript
enum PayGrade {
  Trainee,    // 2500 HUF/hour
  Junior,     // 3000 HUF/hour
  Skilled,    // 3800 HUF/hour
  Master,     // 4500 HUF/hour
  Lead        // 5500 HUF/hour
}

enum Department {
  Production, Assembly, Logistics, Sales, Design, Admin, Maintenance, Quality
}

enum AbsenceType {
  Vacation, SickLeave, UnpaidLeave, Other
}

enum EmploymentType {
  FullTime, PartTime, Contractor
}

enum ProficiencyLevel {
  Basic = 1, Intermediate = 2, Expert = 3
}
```

### 2. Endpoint Inventory Matrix

Create a spreadsheet or markdown table:

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/employees` | GET | List employees | - | EmployeeDto[] | - | hr.view |
| `/employees/{id}` | GET | Get employee | - | EmployeeDto | - | hr.view |
| `/employees` | POST | Create employee | CreateEmployeeCommand | EmployeeDto | - | hr.manage |
| `/absences/{id}/approve` | POST | Approve absence | ApproveAbsenceCommand | AbsenceDto | Pending → Approved | hr.manage |
| `/capacity/employees/{id}/daily` | GET | Daily capacity | date (query) | DailyCapacityDto | - | hr.view |
| ... | ... | ... | ... | ... | ... | ... |

**Total Estimated:** 25 endpoints (8 Employee + 10 Absence + 4 Capacity + 3 Vacation)

### 3. FSM Transition Rules

**Absence FSM Definition:**

```yaml
components:
  schemas:
    AbsenceFSMTransition:
      type: object
      properties:
        from:
          type: string
          enum: [Pending, Approved, Rejected, InProgress, Completed]
        to:
          type: string
          enum: [Pending, Approved, Rejected, InProgress, Completed]
        trigger:
          type: string
          description: API endpoint that triggers this transition
        requiredPermission:
          type: string
          description: Permission required to execute transition
        validations:
          type: array
          items:
            type: string
          description: Business rules validated during transition
```

**Transition Matrix:**
| From | To | Trigger Endpoint | Permission | Validation Rules |
|------|-----|------------------|------------|------------------|
| Pending | Approved | POST /absences/{id}/approve | hr.manage | Dates not in past, employee exists, no overlapping absences |
| Pending | Rejected | POST /absences/{id}/reject | hr.manage | Rejection reason required (min 10 chars) |
| Approved | InProgress | POST /absences/{id}/start | hr.manage | Start date reached, employee active |
| InProgress | Completed | POST /absences/{id}/complete | hr.manage | End date reached |
| Rejected | Pending | POST /absences/{id}/reopen | hr.view (self) | Employee can reopen own rejected request |

### 4. Integration with Other Modules

**HR Module provides data to:**
- **Kontrolling:** TimeLog actual labor costs (hourly rate from PayGrade × hours worked)
- **Production:** Employee capacity for job assignment
- **EHS (future):** Training competency tracking

**Integration DTOs (exported by HR):**
```yaml
# TimeLogCostData (consumed by Kontrolling)
TimeLogCostData:
  type: object
  properties:
    projectId:
      type: string
      format: uuid
    employeeId:
      type: string
      format: uuid
    hoursWorked:
      type: number
    hourlyRate:
      type: number
      description: From PayGrade enum
    costTotal:
      type: number
      description: hoursWorked × hourlyRate
    periodStart:
      type: string
      format: date
    periodEnd:
      type: string
      format: date
```

### 5. Validation & Code-Gen Test

**Validation:**
```bash
# OpenAPI validation
npx @redocly/cli lint docs/api/joinerytech-hr-v1.yaml

# Schema validation (no $ref errors, all required fields)
```

**Code-Gen Test (Frontend):**
```bash
cd datahaven-web/client
npx orval --config orval.hr.config.ts

# Expected output:
# - src/api/hr/hr.ts (TanStack Query hooks)
# - src/api/hr/model/*.ts (TypeScript types)
```

**Code-Gen Test (Backend):**
```bash
cd spaceos-modules-hr/Api
dotnet add package NSwag.MSBuild

# Expected output:
# - Generated C# client (verify DTO types match domain model)
```

---

## Technical Constraints

### 1. Domain Model Compliance (MSG-ARCHITECT-046)

**MUST align with HR Domain Model:**
- ✅ 2 Aggregates: Employee (CRUD), Absence (FSM)
- ✅ 7 Value Objects: PersonalData, Skill, PayGrade, Department, AbsenceType, EmploymentType, MaritalStatus
- ✅ 2 Domain Services: CapacityCalculationService, VacationEntitlementService
- ✅ FSM: Pending → Approved → InProgress → Completed (or Rejected → Pending)
- ✅ Blocking statuses: Approved, InProgress, Completed (remove capacity)

### 2. OpenAPI 3.1 Standards

```yaml
openapi: 3.1.0
info:
  title: JoineryTech HR & Kapacitás API
  version: 1.0.0
  description: |
    Employee management, absence tracking (FSM), capacity calculation, vacation entitlement.
    Hungarian Labor Code (Mt.) compliance for vacation and sick leave.
servers:
  - url: https://api.joinerytech.local
    description: Local development
components:
  securitySchemes:
    BearerAuth:
      type: http
      scheme: bearer
      bearerFormat: JWT
security:
  - BearerAuth: []
```

### 3. Hungarian Labor Code (Mt.) Compliance

**Vacation Entitlement (Mt. §118):**
- Base: 20 days/year
- Child extra: 1 child → +2 days, 2 children → +4 days, 3+ children → +7 days

**Sick Leave (Mt. §123):**
- 15 days/year paid sick leave

**Implementation:**
```yaml
# GET /api/hr/vacation-entitlement/{employeeId}
VacationEntitlementDto:
  properties:
    vacationBase:
      type: integer
      description: Base vacation days (always 20)
    childExtra:
      type: integer
      description: Extra days based on children (0/2/4/7)
    totalEntitlement:
      type: integer
      description: vacationBase + childExtra
    used:
      type: integer
      description: Days used (from Completed absences with type=Vacation)
    remaining:
      type: integer
      description: totalEntitlement - used
```

### 4. RBAC Permissions

**Permission Levels:**
- `hr.view` — View employees, absences, capacity (read-only)
- `hr.manage` — Create/update/delete employees, approve/reject absences
- `hr.admin` — Promote employees, manage pay grades, delete employees

**RLS (Row Level Security):**
- Employees can view/edit their own absences (Pending state only)
- Managers can approve/reject absences for their department
- HR admins can manage all employees across all facilities

---

## Acceptance Criteria

**DONE when:**
- [ ] OpenAPI 3.1 spec file: `/opt/spaceos/docs/api/joinerytech-hr-v1.yaml`
- [ ] 25 endpoints defined (8 Employee + 10 Absence + 4 Capacity + 3 Vacation)
- [ ] All DTOs/schemas match MSG-ARCHITECT-046 domain model
- [ ] FSM transition rules defined (5 transitions: approve, reject, start, complete, reopen)
- [ ] Endpoint inventory matrix created (Markdown table)
- [ ] Hungarian Labor Code compliance (vacation entitlement, sick leave)
- [ ] Integration DTOs defined (TimeLogCostData for Kontrolling)
- [ ] Validation passes: `npx @redocly/cli lint`
- [ ] Code-gen test passes: Orval (Frontend), NSwag (Backend)
- [ ] No $ref errors, all required fields present
- [ ] Security: Bearer JWT auth + RBAC permissions defined

**Quality Gates:**
- Spec lock commit: Tag `hr-spec-v1.0.0`
- Review by Conductor (contract clarity, FSM feasibility)
- Approved before Backend Week 1 starts

---

## Integration with Existing Work

**Domain Model Implementation Plan (Week 1-5):**
- ✅ **Week 0** (THIS TASK): OpenAPI spec lock
- ⏳ **Week 1**: Domain layer (Employee, Absence aggregates, FSM)
- ⏳ **Week 2**: Application layer (CQRS handlers, CapacityCalculationService, VacationEntitlementService)
- ⏳ **Week 3**: Infrastructure layer (EF Core, RLS policies, database schema)
- ⏳ **Week 4**: API layer (controllers, validation, FSM transition enforcement)
- ⏳ **Week 5**: Testing (unit, integration, FSM state machine tests)

**Parallel Development Unlock:**
- After Week 0 spec lock → Backend starts Week 1 → Frontend starts Week 1.5 (MSW mock API)
- No integration rework (contract locked upfront)

---

## References

- **Domain Model:** `/opt/spaceos/docs/joinerytech/domain/HR_DOMAIN_MODEL.md` (11,000+ words)
- **Architect Work:** `terminals/architect/outbox/2026-07-02_046_joinerytech-hr-domain-model-done.md`
- **Contract-First Pattern:** `/opt/spaceos/docs/knowledge/patterns/CONTRACT_FIRST_DEVELOPMENT.md`
- **CRM OpenAPI Example:** Previous Week 0 work for reference pattern
- **Kontrolling OpenAPI Example:** `docs/api/joinerytech-kontrolling-v1.yaml` (recently completed)
- **Code-Gen Tools:** Orval (Frontend), NSwag (Backend)

---

## Priority Rationale

**Why HIGH priority:**
- ✅ EPIC-JT-HR **unblocked** (EPIC-KERNEL-STABLE + EPIC-IDENTITY-V1 done)
- ✅ **Parallel development** with EPIC-JT-CRM + EPIC-JT-CTRL (maximize throughput)
- ✅ Contract-First **prevents 2 weeks integration rework** ($11-16k savings)
- ✅ JoineryTech **top focus** (user explicit request)
- ✅ Week 0 spec **enables early Frontend mockup** (MSW parallel track)
- ✅ **Pattern reuse** (same Contract-First workflow as Kontrolling)

**Timeline:**
- Week 0 (4 hours) → Backend Week 1 dispatch (when CRM API completes) → Frontend Week 1.5 (MSW parallel)

---

**Next After Completion:**
When Architect completes OpenAPI spec → Conductor reviews → Backend HR Week 1 task queued (wait for CRM API + Kontrolling Week 1 completion)

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
