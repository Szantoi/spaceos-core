---
id: MSG-BACKEND-181
from: backend
to: conductor
type: blocked
status: READ
ref: MSG-BACKEND-176
created: 2026-07-07
content_hash: 17d406af94ee2983a324339ca09f63bff4f41b88bb6f158e3f26bd51b7e6915e
---

## Blocker: HR Week 2 Specification Mismatch

**MSG-BACKEND-176** blokkolt — **specification conflicts** a meglévő domain modellel és implementációval.

### Probléma Összefoglalás

Az inbox specification **elavult generikus sablont** használ, amely **NEM egyezik** az HR domain modellel (ADR-056) és a **már implementált** Application Layer-rel (MSG-169-DONE).

### Conflict #1: Contract Scope Error

**Inbox követelmény:**
```csharp
// Contract Commands (MSG-176 szerint)
CreateContractCommand.cs
RenewContractCommand.cs
TerminateContractCommand.cs
UpdateContractCommand.cs

// Contract Queries
GetContractByIdQuery.cs
GetContractsByEmployeeQuery.cs
GetExpiringContractsQuery.cs
```

**ADR-056 szerinti architektúra:**
- **Contract NEM része az HR modulnak** — nincs Contract aggregate az ADR-056-ban!
- HR aggregátumok: **Employee**, **Absence**, **Assignment** (entity), **TimeLog** (entity)
- Contract valószínűleg külön modul vagy későbbi phase

**Fájl:** `/opt/spaceos/docs/architecture/decisions/ADR-056-joinerytech-hr-domain-model.md`
- Line 33-35: "The HR domain has **THREE primary aggregates**" (Employee, Absence, Assignment)
- Line 113-131: Assignment entity (NOT Aggregate Root) — employee workload tracking
- Line 140-162: TimeLog entity — work hour tracking
- **Contract NINCS említve!**

**Eredmény:**
```bash
$ find /opt/spaceos/spaceos-modules-hr/src/Domain/Aggregates -name "*.cs"
Employee.cs         ✅
Absence.cs          ✅
Contract.cs         ❌ NINCS (nem HR scope)
```

### Conflict #2: TimeLog/Clock Implementation Gap

**Inbox követelmény:**
```csharp
RecordClockInCommand.cs          // Daily time tracking
RecordClockOutCommand.cs         // Daily time tracking
```

**ADR-056 szerinti design:**
```csharp
// TimeLog Entity (ADR-056 Line 140-162)
TimeLog
├── TimeLogId (Guid)
├── EmployeeId (Guid)
├── Date (DateTime)
├── Hours (decimal)
├── PushedToControlling (bool)
└── ...
```

**Implementált állapot:**
```bash
$ find /opt/spaceos/spaceos-modules-hr/src/Domain -name "*TimeLog*"
# (nincs output)

$ ls /opt/spaceos/spaceos-modules-hr/src/Application/Commands/
CreateEmployeeCommand.cs              ✅
UpdateEmployeeSkillsCommand.cs        ✅
DeactivateEmployeeCommand.cs          ✅
RequestAbsenceCommand.cs              ✅
ApproveAbsenceCommand.cs              ✅
RejectAbsenceCommand.cs               ✅
ReopenAbsenceCommand.cs               ✅
RecordClockInCommand.cs               ❌ NINCS
RecordClockOutCommand.cs              ❌ NINCS
```

**TimeLog entity LÉTEZIK az ADR-056-ban, DE még nincs implementálva!**

### Conflict #3: Assignment Entity Missing

**ADR-056 szerinti design (Line 113-131):**
```csharp
Assignment (Entity — NOT Aggregate Root)
├── Source (AssignmentSource: Project, Maintenance, Logistics)
├── ReferenceId (ProjectId, WorkOrderId, ShipmentId)
├── StartDate / EndDate
└── HoursPerDay
```

**Implementált állapot:**
```bash
$ find /opt/spaceos/spaceos-modules-hr/src/Domain -name "*Assignment*"
# (nincs output)
```

**Assignment entity LÉTEZIK az ADR-056-ban, DE még nincs implementálva!**

### Conflict #4: Command Naming Mismatches

**Inbox szerint:**
```csharp
UpdateEmployeeCommand.cs          // Generic CRUD update
TerminateEmployeeCommand.cs       // Employee termination
SuspendEmployeeCommand.cs         // Employee suspension
ReactivateEmployeeCommand.cs      // Reactivate suspended
```

**Implementálva (ADR-056 szerint):**
```csharp
UpdateEmployeeSkillsCommand.cs    ✅ (Specific domain operation, NOT generic Update)
DeactivateEmployeeCommand.cs      ✅ (Immutable domain pattern, NOT generic Terminate)
SuspendEmployeeCommand.cs         ❌ NINCS
ReactivateEmployeeCommand.cs      ❌ NINCS
```

### Meglévő Implementáció Státusza

**Week 2 Application Layer PARTIAL COMPLETE:**

✅ **Commands (7):** CreateEmployee, UpdateEmployeeSkills, DeactivateEmployee, RequestAbsence, ApproveAbsence, RejectAbsence, ReopenAbsence
✅ **Queries (8):** GetEmployee, GetEmployees, GetEmployeesBySkill, GetDepartmentCapacity, GetEmployeeCapacity, GetAbsence, GetEmployeeAbsences, GetPendingAbsences
✅ **Validators (6):** CreateEmployee, UpdateEmployeeSkills, Deactivate, RequestAbsence, Approve, Reject
✅ **DTOs (12):** Employee, Absence, Capacity, Skills, etc.
✅ **Build:** 0 errors, 0 warnings
✅ **MSG-169-DONE:** "Pattern Validation: ✅ 100% Complete" (for Employee + Absence scope)

**Missing from Inbox Requirements:**
❌ **Contract CQRS handlers** (Contract aggregate nincs az ADR-056-ban)
❌ **TimeLog CQRS handlers** (TimeLog entity létezik ADR-ben, DE nincs implementálva)
❌ **Assignment CQRS handlers** (Assignment entity létezik ADR-ben, DE nincs implementálva)
❌ **Clock-in/Clock-out handlers** (TimeLog-hoz kell)
❌ **Suspend/Reactivate Employee handlers** (ADR-056-ban nincs mentioned)

**Verification:**
```bash
$ dotnet build /opt/spaceos/spaceos-modules-hr/src/SpaceOS.Modules.HR.csproj
Build succeeded.
    0 Warning(s)
    0 Error(s)

$ find /opt/spaceos/spaceos-modules-hr/src/Application/Commands -name "*.cs" | wc -l
14  # (7 commands × 2 files each = command + handler)

$ find /opt/spaceos/spaceos-modules-hr/src/Application/Queries -name "*.cs" | wc -l
16  # (8 queries × 2 files each = query + handler)
```

### Root Cause Analysis

**Conductor error:** Dispatched MSG-176 using **generic JoineryTech Week 2 specification template**, amely:
1. Feltételez Contract aggregate-et az HR modulban (SCOPE ERROR — nincs az ADR-056-ban)
2. Feltételez TimeLog/Assignment implementációt (ADR-056-ban VAN, de NINCS IMPLEMENTÁLVA)
3. Feltételez generic Update commands-ot (DESIGN MISMATCH — immutable domain)
4. Nem tükrözi az ADR-056 gazdagabb, de részlegesen implementált domain modellt

**What should have happened:**
- Verification hogy HR Week 2 Application Layer részben implementálva van (Employee + Absence COMPLETE)
- Custom specification based on ADR-056, NOT generic template
- TimeLog + Assignment entities későbbi phase vagy explicit decision needed

### Architecture Confirmation

**ADR-056 domain model CORRECT:**
- 3 aggregátumok/entitások: Employee (aggregate), Absence (aggregate), Assignment (entity), TimeLog (entity)
- Employee + Absence IMPLEMENTED ✅
- Assignment + TimeLog NOT IMPLEMENTED ❌ (design done, code pending)
- Contract NINCS az ADR-ban (külön modul vagy későbbi phase)

**No architecture change needed.** Decision szükséges: implementáljuk-e Assignment + TimeLog entitásokat most vagy később?

### Blocking Reason

Nem implementálhatom az inbox követelményeket, mert:
- ❌ **Contract Commands/Queries:** SCOPE ERROR — Contract nincs az HR domain-ben (ADR-056)
- ❌ **Clock-in/Clock-out Commands:** IMPLEMENTATION GAP — TimeLog entity létezik ADR-ben, DE nincs implementálva
- ❌ **Assignment Commands:** IMPLEMENTATION GAP — Assignment entity létezik ADR-ben, DE nincs implementálva
- ⚠️ **Naming mismatches:** DeactivateEmployee vs TerminateEmployee (már implementálva, csak naming eltér)
- ⚠️ **Suspend/Reactivate:** Minor gap, nincs explicit ADR support

### Recommended Next Steps

**Opció A: MSG-176 CANCELLED (RECOMMENDED)**
- HR Week 2 Application Layer **PARTIAL COMPLETE** (Employee + Absence aggregates)
- Specification elavult — ADR-056 domain model gazdagabb, de részlegesen implementált
- Assignment + TimeLog entities későbbi phase (ADR-ben VAN, implementáció NINCS)
- Contract scope ERROR (nincs az HR modulban)
- MSG-176 CANCELLED, inbox marked as duplicate/outdated work

**Opció B: Specification Correction**
- Conductor updates MSG-176 specification to match ADR-056
- Remove Contract scope (külön modul vagy phase)
- Clarify TimeLog + Assignment implementation priority
- Accept existing Command/Query names (DeactivateEmployee, UpdateEmployeeSkills)
- Optional: Add missing queries (GetAllEmployees, GetActiveEmployees, GetEmployeesByDepartment)

**Opció C: Implement Missing Entities (TimeLog + Assignment)**
- TimeLog entity + CQRS handlers (~20 NWT)
  - RecordClockInCommand, RecordClockOutCommand
  - GetTimeLogsByEmployeeQuery, GetTimeLogsForControllingQuery
  - Integration with Controlling module
- Assignment entity + CQRS handlers (~15 NWT)
  - CreateAssignmentCommand (from Projects, Maintenance, Logistics)
  - GetEmployeeAssignmentsQuery, GetCapacityQuery (with assignments)
- Contract scope SKIP (nincs az ADR-056-ban)
- Expected time: ~35 NWT

### Impact Assessment

**Week 2 Application Layer Status:**

| Module | Employee/Absence | TimeLog | Assignment | Contract | Status |
|--------|------------------|---------|------------|----------|--------|
| **HR** | ✅ COMPLETE | ❌ ADR-defined, NOT impl. | ❌ ADR-defined, NOT impl. | ❌ Out of scope | 🟡 PARTIAL |
| **CRM** | ✅ COMPLETE | N/A | N/A | ❌ Separate module | ✅ DONE (MSG-174 CANCELLED) |
| **Kontrolling** | ✅ COMPLETE | N/A | N/A | N/A | ✅ DONE (MSG-175 CANCELLED) |
| **Maintenance** | 📋 PENDING | 📋 PENDING | 📋 PENDING | N/A | 📋 VALID (MSG-177) |
| **QA** | 📋 PENDING | 📋 PENDING | N/A | N/A | 📋 VALID (MSG-178) |

**Corrected Dispatch Count:** 2-3 valid tasks (Maintenance, QA) + 1 partial (HR)

### Verifiable Files

```bash
# Domain Model (ADR-056)
cat /opt/spaceos/docs/architecture/decisions/ADR-056-joinerytech-hr-domain-model.md

# Domain Aggregates (Employee + Absence ONLY)
ls /opt/spaceos/spaceos-modules-hr/src/Domain/Aggregates/

# Implemented Commands (7 handlers)
find /opt/spaceos/spaceos-modules-hr/src/Application/Commands -name "*Handler.cs" | wc -l

# Implemented Queries (8 handlers)
find /opt/spaceos/spaceos-modules-hr/src/Application/Queries -name "*Handler.cs" | wc -l

# Build Status
dotnet build /opt/spaceos/spaceos-modules-hr/src/SpaceOS.Modules.HR.csproj

# MSG-169 DONE outbox (Week 2 Application Layer PARTIAL)
cat /opt/spaceos/terminals/backend/outbox/2026-07-07_169_hr-week4-api-layer-done.md
```

### Kockázatok

| Kockázat | Impact | Likelihood |
|----------|--------|------------|
| **Implementálás spec conflict nélkül** | HIGH — Architectural violation (Contract scope, TimeLog/Assignment gap) | HIGH |
| **Duplicate work** | MEDIUM — Employee + Absence már complete, wasted effort | LOW if Opció A |
| **Incomplete domain model** | MEDIUM — TimeLog + Assignment ADR-ben VAN, implementáció NINCS | MEDIUM |

---

**Status:** 🔴 BLOCKED — Architecture decision needed from Conductor

**Preferred Resolution:** **Opció B** — MSG-176 specification correction (remove Contract, clarify TimeLog/Assignment priority)

**Alternative:** **Opció C** — Implement TimeLog + Assignment entities (~35 NWT)

**Next Action:** Conductor döntés Opció A/B/C közül

---

🤖 Generated by Backend Terminal

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
