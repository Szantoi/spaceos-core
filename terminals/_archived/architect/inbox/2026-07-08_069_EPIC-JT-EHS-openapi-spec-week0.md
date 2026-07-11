---
id: MSG-ARCHITECT-069
from: conductor
to: architect
type: task
priority: high
status: READ
model: sonnet
ref: MSG-ROOT-004-RESPONSE, EPIC-JT-EHS
created: 2026-07-08
completed: 2026-07-08
epic_id: EPIC-JT-EHS
estimated_nwt: 60
actual_nwt: 60
content_hash: 504f9f1f1e99523ef2dc34404f90ee6bb4418690dfc3e0d51989ce389e68ef8b
---

# EPIC-JT-EHS: OpenAPI Spec (Week 0) — Munkavédelem/Safety Module

**Epic:** EPIC-JT-EHS (JoineryTech Munkavédelem/Safety)
**Status:** ACTIVE (activated: 2026-07-08)
**Parallel Track:** EPIC-DOORSTAR-SOFTLAUNCH planning (MSG-ARCHITECT-070)
**Timeline:** 2-3 hours (60 NWT)

---

## Context: Aggressive Parallel Execution

**VPS Capacity Upgrade:**
- CPU: 6 cores (41% headroom)
- RAM: 15GB (60% headroom)
- **Strategy:** EPIC-JT-EHS + EPIC-DOORSTAR-SOFTLAUNCH parallel development

**Your Role:**
- Track A: EHS OpenAPI spec (THIS TASK)
- Track B: Doorstar task breakdown (MSG-ARCHITECT-070, Opus model)

**Resource Allocation:** 1 CPU core, ~1GB RAM per track → sustainable

---

## Task: EHS OpenAPI Specification (Week 0)

**Scope:** Munkavédelem (Occupational Health & Safety) világ
**Pattern:** Proven Week 1-4 pattern (like DMS, HR, Maintenance, QA, CRM, Kontrolling)
**Deliverable:** OpenAPI 3.1 spec for EHS module API endpoints

---

## Domain Overview

**EHS Module (ISO 45001 Compliance):**

1. **Incident FSM** (Baleset/Kvázibaleset workflow)
   - States: Reported → Investigated → CorrectiveActionPlanned → Closed
   - Optional: Reopened state
   - Event types: Accident, NearMiss, HazardousCondition

2. **5×5 Risk Matrix** (Kockázatértékelés)
   - Severity (1-5): Negligible → Catastrophic
   - Likelihood (1-5): Rare → Almost Certain
   - Risk Score = Severity × Likelihood (1-25)
   - Risk Level: Low (1-5), Medium (6-12), High (15-25)

3. **Training/Competency Management** (Oktatás/Kompetencia lejárat-figyelés)
   - Training records linked to employees (HR integration)
   - Competency expiration tracking
   - Mandatory training alerts

---

## Aggregates & Entities

### 1. Incident Aggregate
**Root:** Incident
**Owned Entities:**
- IncidentInvestigation (0-1)
- CorrectiveAction (0-n)
- IncidentWitness (0-n)

**Properties:**
- IncidentId (Guid)
- TenantId (Guid)
- IncidentType (enum: Accident, NearMiss, HazardousCondition)
- IncidentDate (DateTimeOffset)
- Location (string)
- Description (string, markdown)
- Severity (enum: 1-5)
- Status (FSM state)
- ReportedBy (EmployeeId, FK to HR module)
- ReportedAt (DateTimeOffset)
- InvestigatedBy (EmployeeId, nullable)
- InvestigatedAt (DateTimeOffset, nullable)
- ClosedAt (DateTimeOffset, nullable)

### 2. RiskAssessment Aggregate
**Root:** RiskAssessment
**Owned Entities:**
- RiskControl (0-n) — mitigation measures

**Properties:**
- RiskAssessmentId (Guid)
- TenantId (Guid)
- HazardDescription (string)
- Severity (1-5)
- Likelihood (1-5)
- RiskScore (calculated: Severity × Likelihood)
- RiskLevel (calculated: Low/Medium/High)
- AssessedBy (EmployeeId)
- AssessedAt (DateTimeOffset)
- ReviewDueDate (DateTimeOffset)
- Status (enum: Active, Archived)

### 3. TrainingRecord Aggregate
**Root:** TrainingRecord

**Properties:**
- TrainingRecordId (Guid)
- TenantId (Guid)
- EmployeeId (Guid, FK to HR module)
- TrainingType (string, e.g., "First Aid", "Fire Safety")
- CompletedAt (DateTimeOffset)
- ExpiresAt (DateTimeOffset, nullable)
- IssuedBy (string, certifying authority)
- CertificateNumber (string, nullable)
- Status (enum: Valid, Expired, Expiring)

---

## API Endpoint Requirements

### Incident Management (12-15 endpoints)

**Commands (CQRS):**
1. POST /api/ehs/incidents — CreateIncident
2. PUT /api/ehs/incidents/{id}/investigate — StartInvestigation
3. PUT /api/ehs/incidents/{id}/corrective-actions — AddCorrectiveAction
4. PUT /api/ehs/incidents/{id}/close — CloseIncident
5. PUT /api/ehs/incidents/{id}/reopen — ReopenIncident
6. PUT /api/ehs/incidents/{id}/witnesses — AddWitness

**Queries:**
7. GET /api/ehs/incidents — ListIncidents (filter: status, type, date range)
8. GET /api/ehs/incidents/{id} — GetIncident
9. GET /api/ehs/incidents/summary — GetIncidentSummary (count by type/severity)
10. GET /api/ehs/incidents/trends — GetIncidentTrends (monthly aggregation)

### Risk Assessment (8-10 endpoints)

**Commands:**
11. POST /api/ehs/risk-assessments — CreateRiskAssessment
12. PUT /api/ehs/risk-assessments/{id} — UpdateRiskAssessment
13. PUT /api/ehs/risk-assessments/{id}/controls — AddRiskControl
14. PUT /api/ehs/risk-assessments/{id}/archive — ArchiveRiskAssessment

**Queries:**
15. GET /api/ehs/risk-assessments — ListRiskAssessments (filter: risk level, review due)
16. GET /api/ehs/risk-assessments/{id} — GetRiskAssessment
17. GET /api/ehs/risk-assessments/matrix — GetRiskMatrix (5×5 grid visualization)

### Training/Competency (8-10 endpoints)

**Commands:**
18. POST /api/ehs/training-records — CreateTrainingRecord
19. PUT /api/ehs/training-records/{id}/renew — RenewTrainingRecord

**Queries:**
20. GET /api/ehs/training-records — ListTrainingRecords (filter: employee, status, expiring)
21. GET /api/ehs/training-records/{id} — GetTrainingRecord
22. GET /api/ehs/training-records/expiring — GetExpiringTrainings (alert dashboard)
23. GET /api/ehs/training-records/by-employee/{employeeId} — GetEmployeeTrainings

**Total:** ~25-30 endpoints

---

## Success Criteria

**OpenAPI Spec Quality:**
- ✅ All 3 aggregates covered (Incident, RiskAssessment, TrainingRecord)
- ✅ FSM state transitions documented (Incident workflow)
- ✅ 5×5 risk matrix calculation logic documented
- ✅ HR module integration (EmployeeId FK) documented
- ✅ Redocly linter PASS (zero errors/warnings)
- ✅ Response/Request DTOs defined (reusable schemas)
- ✅ Multi-tenancy support (TenantId in all aggregates)

**Pattern Consistency:**
- ✅ Same structure as DMS, HR, Maintenance, QA, CRM, Kontrolling specs
- ✅ CQRS command/query separation clear
- ✅ FluentValidation requirements annotated
- ✅ Testcontainers test scenarios suggested

---

## Reference Specs (Pattern Examples)

**Previous Week 0 OpenAPI specs:**
- DMS: MSG-ARCHITECT-066 (1866 lines, 36 endpoints)
- HR: MSG-ARCHITECT-061 (25 endpoints)
- Maintenance: MSG-ARCHITECT-062 (31 endpoints)
- QA: MSG-ARCHITECT-065

**ADR Compliance:**
- ADR-054: CRM domain (NO Customer in CRM) — NOT applicable to EHS
- ADR-055: Kontrolling calculated layer — NOT applicable to EHS
- Standard multi-tenancy RLS: APPLICABLE

---

## Acceptance Criteria

1. ✅ OpenAPI 3.1 YAML file created: `spaceos-modules-joinerytech/ehs/openapi.yaml`
2. ✅ 25-30 endpoints defined (Incident, RiskAssessment, TrainingRecord)
3. ✅ FSM state machine documented (Incident workflow)
4. ✅ 5×5 risk matrix calculation logic (Severity × Likelihood → RiskScore/RiskLevel)
5. ✅ HR integration FK documented (EmployeeId references)
6. ✅ Redocly linter validation PASS
7. ✅ Multi-tenancy support (TenantId in all aggregates)
8. ✅ Response/Request DTOs reusable schemas
9. ✅ DONE outbox written with spec summary

---

## Next Steps (After This Task DONE)

**Backend Week 1 (Domain Layer):**
- MSG-BACKEND-XXX: EPIC-JT-EHS Week 1 Domain Layer
- Aggregates: Incident, RiskAssessment, TrainingRecord
- FSM: Incident state machine
- Value Objects: IncidentType, Severity, RiskLevel
- Domain Methods: CalculateRiskScore, CheckTrainingExpiry
- ETA: 4-6 hours (120 NWT)

**Parallel Track (Doorstar Planning):**
- MSG-ARCHITECT-070: Doorstar task breakdown + TASKS.yaml (Opus model)
- ETA: 6-8 hours (180-240 NWT)

---

**Priority:** HIGH (Track A of parallel execution)
**Model:** Sonnet (proven pattern, low complexity)
**Estimated Time:** 2-3 hours (60 NWT)
**Resource Allocation:** 1 CPU core, ~1GB RAM
**Parallel Task:** MSG-ARCHITECT-070 (Doorstar planning, Opus)

🤖 Generated by Conductor — Aggressive Parallel Execution (VPS Capacity Upgrade)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
