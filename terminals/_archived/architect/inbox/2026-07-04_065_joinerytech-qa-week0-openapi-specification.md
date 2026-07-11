---
id: MSG-ARCHITECT-065
from: conductor
to: architect
type: task
priority: high
status: READ
model: opus
epic_id: EPIC-JT-QA
checkpoint_id: CP-QA-BACKEND
ref: MSG-ARCHITECT-063-DONE
created: 2026-07-04
estimated_nwt: 120
content_hash: a9a533a67f7a3e4fc470d21837995fb8a7c74c16b589525f0c85eaba262fe25e
---

# JoineryTech QA Week 0 — OpenAPI Contract Specification

**Epic:** EPIC-JT-QA (Minőségbiztosítás)
**Estimated:** 120 NWT (~4 hours)
**Priority:** High (domain model elkészült, OpenAPI spec következik)

---

## Context

A QA Domain Model elkészült (MSG-ARCHITECT-063-DONE). Most a Contract-First development workflow Week 0 fázisa következik: **OpenAPI 3.1 Contract specifikáció**.

**Domain Model File:** `/opt/spaceos/docs/joinerytech/domain/QA_DOMAIN_MODEL.md` (1832 lines, 3 aggregates, 2 FSMs, 3 domain services)

**Prototípus:** JoineryTech prototípus volt QA/reklamáció modul (migrálandó)
**Critical Integration:** Production blocking (failed critical inspections block production)

---

## Deliverables

**File:** `/opt/spaceos/spaceos-modules-qa/docs/openapi.yaml`

**Minimum tartalom (referencia: CRM, Kontrolling OpenAPI specs):**

### 1. API Metadata
```yaml
openapi: 3.1.0
info:
  title: SpaceOS JoineryTech QA API
  version: 1.0.0
  description: |
    Quality Assurance API — Inspection workflow, Ticket management, Production blocking.
servers:
  - url: https://qa.joinerytech.hu
    description: Production (multi-tenant)
  - url: http://localhost:5011
    description: Local development
```

### 2. Endpoint Groups (20-30 endpoints expected)

#### QACheckpoint Endpoints (CRUD)
```yaml
GET    /api/qa/checkpoints                    # List checkpoints (filter by type, active)
GET    /api/qa/checkpoints/{id}                # Get checkpoint by ID
POST   /api/qa/checkpoints                     # Create checkpoint
PUT    /api/qa/checkpoints/{id}                # Update checkpoint
DELETE /api/qa/checkpoints/{id}                # Deactivate checkpoint (soft delete)
POST   /api/qa/checkpoints/{id}/reactivate     # Reactivate checkpoint
POST   /api/qa/checkpoints/{id}/criteria       # Add inspection criteria
```

#### Inspection Endpoints (FSM + Query)
```yaml
GET    /api/qa/inspections                     # List inspections (filter by status, checkpoint, date)
GET    /api/qa/inspections/{id}                # Get inspection by ID
POST   /api/qa/inspections                     # Create inspection (Planned state)
POST   /api/qa/inspections/{id}/start          # Start inspection (→ InProgress)
POST   /api/qa/inspections/{id}/complete       # Complete inspection (→ Completed, Pass result)
POST   /api/qa/inspections/{id}/fail           # Fail inspection (→ Completed, Fail result)
GET    /api/qa/inspections/blocking            # Production integration — blocking inspections query ⚠️ CRITICAL
```

#### Ticket Endpoints (FSM + Query)
```yaml
GET    /api/qa/tickets                         # List tickets (filter by status, type, priority)
GET    /api/qa/tickets/{id}                    # Get ticket by ID
POST   /api/qa/tickets                         # Create ticket (Reported state)
POST   /api/qa/tickets/{id}/assign             # Assign ticket (→ Assigned)
POST   /api/qa/tickets/{id}/start              # Start work (→ InProgress)
POST   /api/qa/tickets/{id}/resolve            # Resolve ticket (→ Resolved)
POST   /api/qa/tickets/{id}/reject             # Reject ticket (→ Rejected)
POST   /api/qa/tickets/{id}/reopen             # Reopen ticket (Rejected → Reported)
```

#### Metrics & Analytics Endpoints
```yaml
GET    /api/qa/metrics/pareto                  # Pareto analysis (root cause, date range) ⚠️ Kontrolling integration
GET    /api/qa/metrics/summary                 # QA dashboard summary (pass rate, fail count, etc.)
```

### 3. Schema Definitions

**From Domain Model → OpenAPI DTOs:**

#### QACheckpointDto
```yaml
type: object
properties:
  id: { type: string, format: uuid }
  tenantId: { type: string, format: uuid }
  name: { type: string, maxLength: 200 }
  type: { $ref: '#/components/schemas/CheckpointType' }
  criticalLevel: { $ref: '#/components/schemas/CriticalLevel' }
  criteria:
    type: array
    items: { $ref: '#/components/schemas/InspectionCriteriaDto' }
  isActive: { type: boolean }
  createdAt: { type: string, format: date-time }
required: [id, tenantId, name, type, criticalLevel]
```

#### InspectionDto
```yaml
type: object
properties:
  id: { type: string, format: uuid }
  tenantId: { type: string, format: uuid }
  checkpointId: { type: string, format: uuid }
  orderId: { type: string, format: uuid, nullable: true }
  projectId: { type: string, format: uuid, nullable: true }
  inspectorEmployeeId: { type: string, format: uuid }
  status: { $ref: '#/components/schemas/InspectionStatus' }
  result: { $ref: '#/components/schemas/InspectionResult' }
  inspectionDate: { type: string, format: date-time }
  failureNotes:
    type: array
    items: { $ref: '#/components/schemas/FailureNoteDto' }
  notes: { type: string, maxLength: 1000, nullable: true }
  createdAt: { type: string, format: date-time }
  updatedAt: { type: string, format: date-time }
required: [id, tenantId, checkpointId, status]
```

#### TicketDto
```yaml
type: object
properties:
  id: { type: string, format: uuid }
  tenantId: { type: string, format: uuid }
  ticketNumber: { type: string }  # Auto-generated (e.g., "REK-2026-001")
  type: { $ref: '#/components/schemas/TicketType' }
  status: { $ref: '#/components/schemas/TicketStatus' }
  priority: { $ref: '#/components/schemas/TicketPriority' }
  orderId: { type: string, format: uuid, nullable: true }
  projectId: { type: string, format: uuid, nullable: true }
  customerDescription: { type: string, maxLength: 2000 }
  assignedToEmployeeId: { type: string, format: uuid, nullable: true }
  rootCause: { type: string, maxLength: 1000, nullable: true }
  resolutionAction: { $ref: '#/components/schemas/ResolutionActionDto', nullable: true }
  rejectionReason: { type: string, maxLength: 500, nullable: true }
  reportedAt: { type: string, format: date-time }
  resolvedAt: { type: string, format: date-time, nullable: true }
required: [id, tenantId, ticketNumber, type, status, priority, customerDescription]
```

#### Enums
```yaml
CheckpointType:
  type: string
  enum: [Incoming, InProcess, Final]

CriticalLevel:
  type: string
  enum: [Critical, Major, Minor]

InspectionStatus:
  type: string
  enum: [Planned, InProgress, Completed]

InspectionResult:
  type: string
  enum: [Pass, Fail, Conditional]

TicketType:
  type: string
  enum: [Warranty, Repair, Missing]

TicketStatus:
  type: string
  enum: [Reported, Assigned, InProgress, Resolved, Rejected]

TicketPriority:
  type: string
  enum: [Critical, High, Medium, Low]
```

### 4. Command DTOs (Create/Update)

```yaml
CreateInspectionCommand:
  type: object
  properties:
    checkpointId: { type: string, format: uuid }
    orderId: { type: string, format: uuid, nullable: true }
    projectId: { type: string, format: uuid, nullable: true }
    inspectorEmployeeId: { type: string, format: uuid }
    notes: { type: string, maxLength: 1000, nullable: true }
  required: [checkpointId, inspectorEmployeeId]

FailInspectionCommand:
  type: object
  properties:
    failureNotes:
      type: array
      items: { $ref: '#/components/schemas/FailureNoteDto' }
      minItems: 1
    notes: { type: string, maxLength: 1000, nullable: true }
  required: [failureNotes]

CreateTicketCommand:
  type: object
  properties:
    type: { $ref: '#/components/schemas/TicketType' }
    priority: { $ref: '#/components/schemas/TicketPriority' }
    orderId: { type: string, format: uuid, nullable: true }
    projectId: { type: string, format: uuid, nullable: true }
    customerDescription: { type: string, maxLength: 2000 }
  required: [type, priority, customerDescription]

ResolveTicketCommand:
  type: object
  properties:
    rootCause: { type: string, maxLength: 1000 }
    resolutionAction: { $ref: '#/components/schemas/ResolutionActionDto' }
  required: [rootCause, resolutionAction]
```

### 5. Production Blocking Query Response ⚠️ CRITICAL

```yaml
BlockingInspectionsResponse:
  type: object
  properties:
    isBlocked: { type: boolean }
    blockingInspections:
      type: array
      items:
        type: object
        properties:
          inspectionId: { type: string, format: uuid }
          checkpointName: { type: string }
          failureNotes:
            type: array
            items: { $ref: '#/components/schemas/FailureNoteDto' }
          inspectionDate: { type: string, format: date-time }
  required: [isBlocked, blockingInspections]

# Query endpoint
GET /api/qa/inspections/blocking?orderId={orderId}
  parameters:
    - name: orderId
      in: query
      required: true
      schema: { type: string, format: uuid }
  responses:
    '200':
      description: Blocking inspections for order
      content:
        application/json:
          schema: { $ref: '#/components/schemas/BlockingInspectionsResponse' }
```

### 6. Pareto Analysis Response (Kontrolling Integration)

```yaml
ParetoAnalysisResponse:
  type: object
  properties:
    totalInspections: { type: integer }
    passedInspections: { type: integer }
    failedInspections: { type: integer }
    passRate: { type: number, format: double }
    failureCategories:
      type: array
      items:
        type: object
        properties:
          failureType: { type: string }
          count: { type: integer }
          percentage: { type: number, format: double }
          cumulativePercentage: { type: number, format: double }
  required: [totalInspections, passedInspections, failedInspections, passRate, failureCategories]

# Query endpoint
GET /api/qa/metrics/pareto?startDate={start}&endDate={end}
  parameters:
    - name: startDate
      in: query
      required: true
      schema: { type: string, format: date }
    - name: endDate
      in: query
      required: true
      schema: { type: string, format: date }
  responses:
    '200':
      description: Pareto analysis for date range
      content:
        application/json:
          schema: { $ref: '#/components/schemas/ParetoAnalysisResponse' }
```

### 7. Security & RLS

```yaml
securitySchemes:
  BearerAuth:
    type: http
    scheme: bearer
    bearerFormat: JWT

security:
  - BearerAuth: []
```

**RLS Enforcement:**
- All endpoints filter by `TenantId` (extracted from JWT token)
- PostgreSQL RLS policies enforce tenant isolation
- User can ONLY access QA data from their tenant

### 8. Validation Rules

**Inspection:**
- Cannot complete/fail Planned inspection (must start first)
- Failed inspections require ≥1 FailureNote
- InspectorEmployeeId must reference existing Employee (HR module)

**Ticket:**
- Resolve requires rootCause + resolutionAction
- Reject requires rejectionReason (min 10 chars)
- Cannot resolve Reported ticket (must assign first)

**Checkpoint:**
- Name unique per tenant
- Critical checkpoints require explicit CriticalLevel = Critical

---

## Acceptance Criteria

- [ ] `openapi.yaml` created in `/opt/spaceos/spaceos-modules-qa/docs/`
- [ ] 20-30 endpoints defined (Checkpoint CRUD, Inspection FSM, Ticket FSM, Blocking query, Pareto query)
- [ ] All DTOs mapped from Domain Model (QACheckpoint, Inspection, Ticket, FailureNote, ResolutionAction, etc.)
- [ ] Enums defined (CheckpointType, CriticalLevel, InspectionStatus, InspectionResult, TicketType, TicketStatus, TicketPriority)
- [ ] Command DTOs for all FSM transitions (CreateInspection, StartInspection, CompleteInspection, FailInspection, CreateTicket, AssignTicket, etc.)
- [ ] Production blocking query endpoint documented (GET /api/qa/inspections/blocking)
- [ ] Pareto analysis endpoint documented (GET /api/qa/metrics/pareto)
- [ ] Security (JWT BearerAuth) documented
- [ ] RLS enforcement documented (tenant isolation)
- [ ] Validation rules documented
- [ ] Redocly lint PASS (zero errors)
- [ ] Orval code-gen ready (TypeScript client generation)

---

## Reference Documents

- **QA Domain Model:** `/opt/spaceos/docs/joinerytech/domain/QA_DOMAIN_MODEL.md` ← PRIMARY SOURCE
- **CRM OpenAPI:** `/opt/spaceos/spaceos-modules-crm/docs/openapi.yaml` (structure template)
- **Kontrolling OpenAPI:** `/opt/spaceos/spaceos-modules-kontrolling/docs/openapi.yaml` (metrics endpoint pattern)
- **Contract-First Workflow:** ADR-050 (Orval code-gen, MSW mock, domain-first)

---

## DONE Outbox Format

**File:** `2026-07-04_NNN_joinerytech-qa-week0-openapi-done.md`

**Frontmatter:**
```yaml
---
id: MSG-ARCHITECT-065-DONE
from: architect
to: conductor
type: done
status: READ
ref: MSG-ARCHITECT-065
epic_id: EPIC-JT-QA
checkpoint_id: CP-QA-BACKEND
created: YYYY-MM-DD
---
```

**Tartalom:**
- OpenAPI spec summary (endpoint count, schema count)
- Redocly lint result
- Production blocking query validation
- Pareto analysis endpoint validation
- Files created: openapi.yaml location
- Következő lépés: Backend Week 1 Domain Layer implementation

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
