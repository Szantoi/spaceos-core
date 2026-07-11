# JoineryTech HR & Kapacitás — Endpoint Inventory Matrix

**OpenAPI Spec:** `/opt/spaceos/docs/api/joinerytech-hr-v1.yaml`
**Version:** 1.0.0
**Domain Model Reference:** MSG-ARCHITECT-046
**Epic:** EPIC-JT-HR

---

## Endpoint Overview

| Category | Endpoints | Read/Write |
|----------|-----------|------------|
| **Employee Management** | 8 | 3 GET, 2 POST, 2 PUT, 1 DELETE |
| **Absence Management** | 10 | 2 GET, 6 POST, 1 PUT, 1 DELETE |
| **Capacity Queries** | 3 | 3 GET |
| **Vacation Entitlement** | 1 | 1 GET |
| **TOTAL** | **22** | **9 GET, 8 POST, 3 PUT, 2 DELETE** |

---

## Employee Management Endpoints (8)

### CRUD Operations

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/api/hr/employees` | GET | List all employees with filters | query params (department, facilityId, status) | `EmployeeDto[]` | - | `hr.view` |
| `/api/hr/employees/{employeeId}` | GET | Get employee by ID | - | `EmployeeDto` | - | `hr.view` |
| `/api/hr/employees` | POST | Create new employee | `CreateEmployeeCommand` | `EmployeeDto` | - | `hr.manage` |
| `/api/hr/employees/{employeeId}` | PUT | Update employee data | `UpdateEmployeeCommand` | `EmployeeDto` | - | `hr.manage` |
| `/api/hr/employees/{employeeId}` | DELETE | Soft-delete employee | - | `204 No Content` | - | `hr.admin` |

### Skills Management

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/api/hr/employees/{employeeId}/skills` | POST | Add skill to employee | `AddSkillCommand` | `EmployeeDto` | - | `hr.manage` |
| `/api/hr/employees/{employeeId}/skills/{skill}` | PUT | Update skill proficiency | `UpdateSkillCommand` | `EmployeeDto` | - | `hr.manage` |
| `/api/hr/employees/{employeeId}/skills/{skill}` | DELETE | Remove skill from employee | - | `EmployeeDto` | - | `hr.manage` |

### Pay Grade Promotion

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/api/hr/employees/{employeeId}/promote` | POST | Promote employee to new pay grade | `PromoteEmployeeCommand` | `EmployeeDto` | - | `hr.admin` |

---

## Absence Management Endpoints (10)

### CRUD Operations

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/api/hr/absences` | GET | List absences with filters | query params (employeeId, type, status, startDate, endDate) | `AbsenceDto[]` | - | `hr.view` |
| `/api/hr/absences/{absenceId}` | GET | Get absence by ID | - | `AbsenceDto` | - | `hr.view` |
| `/api/hr/absences` | POST | Create absence request | `CreateAbsenceCommand` | `AbsenceDto` | - | `hr.view` (self) |
| `/api/hr/absences/{absenceId}` | PUT | Update absence details | `UpdateAbsenceCommand` | `AbsenceDto` | - | `hr.view` (self, Pending only) |
| `/api/hr/absences/{absenceId}` | DELETE | Delete absence request | - | `204 No Content` | - | `hr.view` (self, Pending only) |

### FSM State Transitions

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/api/hr/absences/{absenceId}/approve` | POST | Approve absence request | `ApproveAbsenceCommand` | `AbsenceDto` | **Pending → Approved** | `hr.manage` |
| `/api/hr/absences/{absenceId}/reject` | POST | Reject absence request | `RejectAbsenceCommand` | `AbsenceDto` | **Pending → Rejected** | `hr.manage` |
| `/api/hr/absences/{absenceId}/start` | POST | Start absence period | - | `AbsenceDto` | **Approved → InProgress** | `hr.manage` |
| `/api/hr/absences/{absenceId}/complete` | POST | Complete absence period | - | `AbsenceDto` | **InProgress → Completed** | `hr.manage` |
| `/api/hr/absences/{absenceId}/reopen` | POST | Reopen rejected absence | - | `AbsenceDto` | **Rejected → Pending** | `hr.view` (self) |

---

## Capacity Queries (3)

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/api/hr/capacity/employees/{employeeId}/daily` | GET | Get daily capacity for employee | query param: `date` | `DailyCapacityDto` | - | `hr.view` |
| `/api/hr/capacity/employees/{employeeId}/weekly` | GET | Get weekly capacity for employee | query params: `weekStart`, `weekEnd` | `WeeklyCapacityDto` | - | `hr.view` |
| `/api/hr/capacity/team/{departmentId}` | GET | Get team capacity for department | query param: `date` | `TeamCapacityDto` | - | `hr.view` |

---

## Vacation Entitlement (1)

| Endpoint | Method | Purpose | Request DTO | Response DTO | FSM Transition | Permission |
|----------|--------|---------|-------------|--------------|----------------|------------|
| `/api/hr/vacation-entitlement/{employeeId}` | GET | Get vacation entitlement (Hungarian Labor Code Mt. §118) | - | `VacationEntitlementDto` | - | `hr.view` |

---

## Data Models

### Request DTOs (Commands)

| DTO | Used By | Key Fields |
|-----|---------|------------|
| **CreateEmployeeCommand** | POST /employees | name, role, department, facilityId, weeklyHours, employmentType, personalData |
| **UpdateEmployeeCommand** | PUT /employees/{id} | name, role, department, weeklyHours, employmentType (partial updates) |
| **AddSkillCommand** | POST /employees/{id}/skills | skill (string), proficiencyLevel (1-3) |
| **UpdateSkillCommand** | PUT /employees/{id}/skills/{skill} | proficiencyLevel (1-3) |
| **PromoteEmployeeCommand** | POST /employees/{id}/promote | newPayGrade, effectiveDate |
| **CreateAbsenceCommand** | POST /absences | employeeId, type, startDate, endDate, reason |
| **UpdateAbsenceCommand** | PUT /absences/{id} | type, startDate, endDate, reason (Pending only) |
| **ApproveAbsenceCommand** | POST /absences/{id}/approve | approverId, notes |
| **RejectAbsenceCommand** | POST /absences/{id}/reject | approverId, rejectionReason |

### Response DTOs

| DTO | Used By | Key Fields |
|-----|---------|------------|
| **EmployeeDto** | GET /employees, POST/PUT /employees | employeeId, name, role, department, facilityId, skills, payGrade, personalData, weeklyHours, employmentType, maritalStatus, childrenCount |
| **AbsenceDto** | GET /absences, POST/PUT /absences, FSM transitions | absenceId, employeeId, type, status, startDate, endDate, reason, approverId, approvalDate, rejectionReason |
| **DailyCapacityDto** | GET /capacity/employees/{id}/daily | date, totalCapacity, totalLoad, availableCapacity, isOverloaded, assignments, absences |
| **WeeklyCapacityDto** | GET /capacity/employees/{id}/weekly | weekStart, weekEnd, totalHours, daysAbsent, daysOverloaded, dailyBreakdown |
| **TeamCapacityDto** | GET /capacity/team/{departmentId} | departmentId, date, employees, aggregatedCapacity |
| **VacationEntitlementDto** | GET /vacation-entitlement/{employeeId} | employeeId, vacationBase (20), childExtra (0/2/4/7), totalEntitlement, used, remaining |

### Value Objects

| Value Object | Purpose | Fields |
|--------------|---------|--------|
| **PersonalData** | Sensitive employee data | taj (TAJ number), taxId, bankAccount, emergencyContact |
| **Skill** | Employee skill with proficiency | name (string), proficiencyLevel (Basic=1, Intermediate=2, Expert=3) |

### Enums

| Enum | Values | Usage |
|------|--------|-------|
| **PayGrade** | Trainee (2500 HUF/h), Junior (3000), Skilled (3800), Master (4500), Lead (5500) | Employee pay calculation |
| **Department** | Production, Assembly, Logistics, Sales, Design, Admin, Maintenance, Quality | Employee categorization |
| **AbsenceStatus** | Pending, Approved, Rejected, InProgress, Completed | FSM state machine |
| **AbsenceType** | Vacation, SickLeave, UnpaidLeave, Other | Absence categorization |
| **EmploymentType** | FullTime, PartTime, Contractor | Employee type |
| **ProficiencyLevel** | Basic (1), Intermediate (2), Expert (3) | Skill proficiency |
| **MaritalStatus** | Single, Married, Divorced, Widowed | Personal data |

---

## FSM Transition Rules (Absence Workflow)

| From | To | Trigger Endpoint | Permission | Validation Rules |
|------|-----|------------------|------------|------------------|
| **Pending** | **Approved** | POST /absences/{id}/approve | `hr.manage` | Dates not in past, employee exists, no overlapping absences |
| **Pending** | **Rejected** | POST /absences/{id}/reject | `hr.manage` | Rejection reason required (min 10 chars) |
| **Approved** | **InProgress** | POST /absences/{id}/start | `hr.manage` | Start date reached, employee active |
| **InProgress** | **Completed** | POST /absences/{id}/complete | `hr.manage` | End date reached |
| **Rejected** | **Pending** | POST /absences/{id}/reopen | `hr.view` (self) | Employee can reopen own rejected request |

**Blocking Statuses** (remove capacity):
- Approved
- InProgress
- Completed

**Non-Blocking Status:**
- Pending (capacity not yet reserved)
- Rejected (capacity not reserved)

---

## Integration Contracts

### Exported to Kontrolling Module

| DTO | Purpose | Key Fields |
|-----|---------|------------|
| **TimeLogCostData** | Actual labor costs for Kontrolling module | projectId, employeeId, hoursWorked, hourlyRate (from PayGrade), costTotal (hours × rate), periodStart, periodEnd |

**Integration Pattern:**
- Kontrolling module reads HR data via **direct DB queries** (read-only, RLS-aware)
- Alternative: REST API calls (slower, more decoupled)

---

## Database Schema (HR owns)

| Table | Purpose |
|-------|---------|
| `hr.employees` | Employee master data (name, role, department, facilityId, skills, payGrade, personalData, etc.) |
| `hr.absences` | Absence requests with FSM status (employeeId, type, status, dates, approverId, rejectionReason) |

**Readonly joins to other modules:**
- Production module (for job assignments)
- Warehouse module (for material tracking)
- Finance module (for payroll integration - future)

---

## Security & Permissions

### Authentication
- **Bearer JWT** (HttpOnly cookie in production)
- **Token expiry:** 1 hour (access token), 7 days (refresh token)

### RBAC Permissions

| Permission | Scope | Allowed Operations |
|------------|-------|-------------------|
| **hr.view** | Read-only access to employees, absences, capacity | GET endpoints, self-service absence CRUD (Pending only) |
| **hr.manage** | Manager access | Approve/reject absences, create/update employees, manage skills |
| **hr.admin** | HR admin access | Promote employees, delete employees, configure pay grades |

### Row Level Security (RLS)

- **Employees can view/edit their own absences** (Pending state only)
- **Managers can approve/reject absences** for their department
- **HR admins can manage all employees** across all facilities
- **Personal data (TAJ, tax ID, bank account)** — only visible to HR admins and self

---

## Error Responses (Standardized)

| Status Code | Error Code | Description |
|-------------|------------|-------------|
| **400** | VALIDATION_FAILED | Validation error (e.g., invalid date range, missing required field) |
| **401** | UNAUTHORIZED | Token expired (refresh token required) |
| **403** | FORBIDDEN | Permission denied (e.g., non-manager trying to approve absence) |
| **404** | NOT_FOUND | Resource not found (employee, absence) |
| **409** | CONFLICT | Overlapping absence exists |
| **422** | STATE_INVALID | Invalid FSM transition (e.g., approve already approved absence) |
| **500** | INTERNAL_ERROR | Server error |

### Special: StateError (422)

**When:** Invalid FSM transition attempted

**Response Body:**
```json
{
  "code": "STATE_INVALID",
  "message": "Cannot transition from Approved to Pending",
  "currentStatus": "Approved",
  "allowedTransitions": ["InProgress"],
  "timestamp": "2026-07-04T10:00:00Z"
}
```

---

## Hungarian Labor Code Compliance

### Vacation Entitlement (Mt. §118)

- **Base:** 20 days/year
- **Child Extra:**
  - 1 child → +2 days
  - 2 children → +4 days
  - 3+ children → +7 days

**Implementation:**
- `GET /api/hr/vacation-entitlement/{employeeId}` calculates entitlement based on `childrenCount` field
- `used` = count of Completed absences with `type: Vacation`
- `remaining` = `totalEntitlement - used`

### Sick Leave (Mt. §123)

- **15 days/year** paid sick leave
- Type: `AbsenceType.SickLeave`
- No separate entitlement endpoint (tracked via absences)

---

## Capacity Calculation Logic

### Daily Capacity Formula

```
totalCapacity = employee.weeklyHours / 5  (e.g., 40h/week → 8h/day)
totalLoad = SUM(assignments.estimatedHours)
availableCapacity = totalCapacity - totalLoad
isOverloaded = totalLoad > totalCapacity

Absences (Approved, InProgress, Completed):
  totalCapacity = 0
  availableCapacity = 0
```

### Weekly Capacity Formula

```
totalHours = SUM(dailyCapacity.totalCapacity) for 5 days
daysAbsent = COUNT(days with blocking absence)
daysOverloaded = COUNT(days where isOverloaded = true)
```

### Team Capacity Aggregation

```
aggregatedCapacity = SUM(employee.dailyCapacity) for all employees in department
```

---

## Next Steps (After Week 0)

### Backend Implementation (Week 1-5)

**Week 1: Domain Layer**
- Employee aggregate (CRUD, skills, pay grade)
- Absence aggregate (FSM state machine)
- CapacityCalculationService, VacationEntitlementService

**Week 2: Application Layer**
- CQRS command handlers (Create, Update, Promote, ApproveAbsence, etc.)
- CQRS query handlers (ListEmployees, GetDailyCapacity, etc.)
- FSM transition validation

**Week 3: Infrastructure Layer**
- EF Core configuration (employees, absences tables)
- RLS policies (self-service, manager, admin)
- Database migrations

**Week 4: API Layer**
- ASP.NET Core controllers (22 endpoints)
- Validation attributes (dates, overlaps, FSM transitions)
- Authorization policies (hr.view, hr.manage, hr.admin)

**Week 5: Testing**
- Unit tests (FSM state machine, capacity calculation, vacation entitlement)
- Integration tests (CRUD, FSM transitions)
- E2E test: Create employee → Request absence → Approve → Complete → Check vacation entitlement

### Frontend Implementation (Week 1.5+)

**Week 1.5: MSW Mock API Setup**
- Mock API handlers (22 endpoints)
- Feature flag for mock/real API swap
- React Query hooks integration (Orval generated)

**Week 2.5: UI Components**
- Employee list & detail view
- Absence request form with FSM status indicator
- Daily/weekly capacity dashboard
- Vacation entitlement widget

---

**Status:** Endpoint inventory matrix completed
**Validation:** Pending (redocly lint)
**Code Generation:** Pending (Orval test)
**Next:** Spec validation and code-gen test

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Opus 4.5 <noreply@anthropic.com>
