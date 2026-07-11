---
id: MSG-FRONTEND-007
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-BACKEND-191-DONE
epic_id: EPIC-JT-EHS
estimated_nwt: 180
created: 2026-07-08
completed: 2026-07-08
content_hash: auto
---

# EHS Dashboard UI — Environment, Health & Safety Module

## Context

Backend EHS API Layer COMPLETE ✅ (MSG-BACKEND-191-DONE):
- 15 REST endpoints (Incident: 7, RiskAssessment: 5, TrainingRecord: 3)
- 37 integration tests GREEN (100% pass rate)
- ISO 45001 compliance patterns
- Multi-tenant RLS support
- Production ready

**Next:** Frontend Dashboard UI implementation — last phase before EHS Module production ready (7/7 JoineryTech modules complete).

---

## 🎯 Feladat: EHS Dashboard + 3 Main Features

### Scope

1. **EHS Dashboard Page** (main landing page)
2. **Incident Reporting & Management** (7-step FSM workflow)
3. **Risk Assessment Matrix** (5×5 visualization + CRUD)
4. **Training Expiry Tracking** (employee training compliance)
5. **API Integration** (15 endpoints via TanStack Query)

---

## 1. EHS Dashboard Page

**Route:** `/ehs` vagy `/ehs/dashboard`

**Layout:**
```
┌─────────────────────────────────────────────────┐
│ EHS Dashboard — ISO 45001 Compliance            │
├─────────────────────────────────────────────────┤
│  KPI Strip:                                     │
│  ┌───────┐ ┌───────┐ ┌───────┐ ┌───────┐       │
│  │ Open  │ │ High  │ │Expiring│ │Critical│      │
│  │Incdts │ │ Risk  │ │Trainng│ │ Actions│      │
│  │  12   │ │   8   │ │  15   │ │    3   │      │
│  └───────┘ └───────┘ └───────┘ └───────┘       │
├─────────────────────────────────────────────────┤
│  Quick Actions:                                 │
│  [+ Report Incident] [+ Risk Assessment]        │
│  [View Training Calendar] [Export Reports]      │
├─────────────────────────────────────────────────┤
│  Recent Activity Feed (Last 10 incidents/risks) │
│  ┌─────────────────────────────────────────┐   │
│  │ 🟠 Near-miss at machinery (1h ago)       │   │
│  │ 🔴 High risk: Chemical storage (2h ago)  │   │
│  │ 🟢 Training completed: Forklift (5h ago) │   │
│  └─────────────────────────────────────────┘   │
└─────────────────────────────────────────────────┘
```

**Components:**
- `EhsDashboard.tsx` — main page
- `EhsKpiStrip.tsx` — 4 KPI cards (incident count, high risk count, expiring trainings, critical actions)
- `EhsQuickActions.tsx` — action buttons
- `EhsActivityFeed.tsx` — recent activity list

**API Calls:**
- `GET /api/ehs/incidents` (filter: status=Open)
- `GET /api/ehs/risk-assessments` (filter: riskLevel=High)
- `GET /api/ehs/training-records` (filter: status=Expiring, daysAhead=30)

---

## 2. Incident Reporting & Management

**Route:** `/ehs/incidents`

### 2.1 Incident List View

**Layout:**
```
┌─────────────────────────────────────────────────┐
│ Incidents                       [+ Report New]  │
├─────────────────────────────────────────────────┤
│  Filters: [Type▾] [Status▾] [Severity▾]        │
│           [Date Range]                          │
├─────────────────────────────────────────────────┤
│  │ID │Title          │Type    │Status  │Svrty│ │
│  ├───┼───────────────┼────────┼────────┼─────┤ │
│  │123│Machinery near │NearMiss│Open    │Med  │ │
│  │122│Chemical spill │Accident│InvInPrg│High │ │
│  │121│Forklift near  │NearMiss│Closed  │Low  │ │
└─────────────────────────────────────────────────┘
```

**Components:**
- `IncidentListPage.tsx` — main list view
- `IncidentFilters.tsx` — filter bar (Type, Status, Severity, Date range)
- `IncidentTable.tsx` — data table with sorting

**API Calls:**
- `GET /api/ehs/incidents?type={type}&status={status}&minSeverity={severity}&dateFrom={from}&dateTo={to}`

### 2.2 Incident Report Form (Create)

**Route:** `/ehs/incidents/new`

**Form Fields:**
```typescript
interface IncidentReportForm {
  title: string;                    // required
  type: 'Accident' | 'Injury' | 'NearMiss' | 'EnvironmentalIncident' | 'PropertyDamage';
  incidentDate: Date;               // required
  location: string;                 // required
  description: string;              // required, textarea
  severity: 'Low' | 'Medium' | 'High' | 'Critical';
  reportedBy: Guid;                 // user ID (from auth context)
  witnesses?: string[];             // optional multi-select
}
```

**Components:**
- `IncidentReportForm.tsx` — form with validation (React Hook Form + Zod)
- `SeveritySelector.tsx` — severity picker (visual color coding)
- `WitnessMultiSelect.tsx` — employee multi-select

**API Call:**
- `POST /api/ehs/incidents` (CreateIncidentCommand)

### 2.3 Incident Detail View (FSM Workflow)

**Route:** `/ehs/incidents/{id}`

**Layout:**
```
┌─────────────────────────────────────────────────┐
│ Incident #123 — Machinery near-miss             │
│ Status: Open → InvestigationInProgress → Closed │
│                     ▲ YOU ARE HERE              │
├─────────────────────────────────────────────────┤
│  Details:                                       │
│  Type: Near-miss | Severity: Medium             │
│  Date: 2026-07-08 14:30 | Location: Production  │
│  Reported by: John Doe (Operator)               │
├─────────────────────────────────────────────────┤
│  Actions (based on current status):             │
│  [Start Investigation]  ← if status = Open      │
│  [Add Findings]         ← if Investigation      │
│  [Add Corrective Action] ← if Investigation     │
│  [Close Incident]       ← if Investigation      │
├─────────────────────────────────────────────────┤
│  Timeline:                                      │
│  📌 Reported: 2026-07-08 14:30 (John Doe)       │
│  🔍 Investigation started: 2026-07-08 15:00     │
│  📝 Findings added: 2026-07-08 16:00            │
│  ✅ Corrective action: 2026-07-08 17:00         │
│  🔒 Closed: 2026-07-08 18:00                    │
└─────────────────────────────────────────────────┘
```

**Components:**
- `IncidentDetailPage.tsx` — main detail view
- `IncidentFsmStatus.tsx` — FSM status indicator (visual progress bar)
- `IncidentActions.tsx` — action buttons (conditional based on status)
- `IncidentTimeline.tsx` — event timeline

**API Calls:**
- `GET /api/ehs/incidents/{id}`
- `POST /api/ehs/incidents/{id}/start-investigation` (StartInvestigationCommand)
- `POST /api/ehs/incidents/{id}/add-findings` (AddInvestigationFindingsCommand)
- `POST /api/ehs/incidents/{id}/add-corrective-action` (AddCorrectiveActionCommand)
- `POST /api/ehs/incidents/{id}/close` (CloseIncidentCommand)

---

## 3. Risk Assessment Matrix

**Route:** `/ehs/risk-assessments`

### 3.1 Risk Matrix Visualization (5×5 Grid)

**Layout:**
```
┌─────────────────────────────────────────────────┐
│ Risk Assessment Matrix (5×5)                    │
├─────────────────────────────────────────────────┤
│        Rare  Unlikely  Possible  Likely  AlmostC│
│Crit  │  🟢  │  🟡   │  🟠    │  🔴   │  🔴    │
│High  │  🟢  │  🟡   │  🟠    │  🔴   │  🔴    │
│Med   │  🟢  │  🟡   │  🟡    │  🟠   │  🔴    │
│Low   │  🟢  │  🟢   │  🟡    │  🟡   │  🟠    │
│VLow  │  🟢  │  🟢   │  🟢    │  🟡   │  🟡    │
│                                                 │
│  Legend: 🟢 Low  🟡 Medium  🟠 High  🔴 Critical│
│  Click cell to see assessments in that category │
└─────────────────────────────────────────────────┘
```

**Components:**
- `RiskMatrixPage.tsx` — main page
- `RiskMatrixGrid.tsx` — 5×5 interactive grid
- `RiskMatrixCell.tsx` — clickable cell (shows count badge)
- `RiskMatrixLegend.tsx` — color legend

**API Calls:**
- `GET /api/ehs/risk-assessments/risk-matrix` (GetRiskMatrixSummaryQuery)

### 3.2 Risk Assessment List

**Layout:**
```
┌─────────────────────────────────────────────────┐
│ Risk Assessments                [+ New]         │
├─────────────────────────────────────────────────┤
│  Filters: [Risk Level▾] [Status▾]              │
├─────────────────────────────────────────────────┤
│  │Hazard          │Severity│Likhd│RiskLvl│Sts │ │
│  ├────────────────┼────────┼─────┼───────┼────┤ │
│  │Chemical storage│Critical│Likely│🔴High │Open│ │
│  │Machinery hazard│High    │Poss │🟠Med  │Open│ │
│  │Slip hazard     │Low     │Rare │🟢Low  │Done│ │
└─────────────────────────────────────────────────┘
```

**Components:**
- `RiskAssessmentListPage.tsx`
- `RiskAssessmentFilters.tsx` (RiskLevel, Status, ReviewDueBefore)
- `RiskAssessmentTable.tsx`

**API Calls:**
- `GET /api/ehs/risk-assessments?riskLevel={level}&status={status}&reviewDueBefore={date}`

### 3.3 Risk Assessment Form (Create)

**Form Fields:**
```typescript
interface RiskAssessmentForm {
  hazard: string;                    // required
  description: string;               // required
  severity: 'VeryLow' | 'Low' | 'Medium' | 'High' | 'Critical';
  likelihood: 'Rare' | 'Unlikely' | 'Possible' | 'Likely' | 'AlmostCertain';
  // RiskScore = Severity × Likelihood (calculated automatically)
  // RiskLevel = Low/Medium/High/Critical (calculated from RiskScore)
  assessedBy: Guid;                  // user ID
  reviewDueDate?: Date;              // optional
}
```

**Components:**
- `RiskAssessmentForm.tsx`
- `RiskScoreCalculator.tsx` — visual calculator (shows live RiskScore + RiskLevel)

**API Call:**
- `POST /api/ehs/risk-assessments` (CreateRiskAssessmentCommand)

---

## 4. Training Expiry Tracking

**Route:** `/ehs/training`

### 4.1 Training Expiry Dashboard

**Layout:**
```
┌─────────────────────────────────────────────────┐
│ Training Compliance Dashboard                   │
├─────────────────────────────────────────────────┤
│  Overview:                                      │
│  Valid: 120 │ Expiring (30d): 15 │ Expired: 3  │
├─────────────────────────────────────────────────┤
│  Expiring Soon (Next 30 Days):                  │
│  │Employee      │Training      │Expires │Days│ │
│  ├──────────────┼──────────────┼────────┼────┤ │
│  │John Doe      │Forklift Sfty │07-15   │ 7  │ │
│  │Jane Smith    │Chemical Hndlg│07-20   │ 12 │ │
│  │Bob Johnson   │Fire Safety   │08-05   │ 28 │ │
└─────────────────────────────────────────────────┘
```

**Components:**
- `TrainingCompliancePage.tsx`
- `TrainingComplianceKpi.tsx` — 3 KPI cards (Valid, Expiring, Expired)
- `ExpiringTrainingsTable.tsx` — table sorted by expiry date

**API Calls:**
- `GET /api/ehs/training-records?status=Expiring&daysAhead=30` (GetExpiringTrainingsQuery)

### 4.2 Training Record Form (Create/Renew)

**Form Fields:**
```typescript
interface TrainingRecordForm {
  employeeId: Guid;                  // required, employee selector
  trainingType: string;              // required (e.g., "Forklift Safety", "Chemical Handling")
  completedDate: Date;               // required
  expiresAt: Date;                   // required
  issuedBy: string;                  // training provider name
  certificateNumber?: string;        // optional
}
```

**Components:**
- `TrainingRecordForm.tsx`
- `EmployeeSelector.tsx` — employee search + select

**API Calls:**
- `POST /api/ehs/training-records` (CreateTrainingRecordCommand)
- `POST /api/ehs/training-records/{id}/renew` (RenewTrainingRecordCommand)

---

## 5. API Integration (TanStack Query)

### 5.1 API Client Generation

**Recommended:** Use Orval for automatic client generation from OpenAPI spec.

```bash
# If EHS API has OpenAPI spec at /swagger/v1/swagger.json
npx orval --config orval.ehs.config.ts
```

**orval.ehs.config.ts:**
```typescript
export default {
  ehs: {
    input: 'http://localhost:5000/swagger/v1/swagger.json',  // EHS API
    output: {
      target: 'src/api/ehs/generated.ts',
      client: 'react-query',
      mode: 'tags-split',
    },
  },
};
```

### 5.2 TanStack Query Hooks

**Custom hooks (if not using Orval):**

```typescript
// src/hooks/useIncidents.ts
export function useIncidents(filters?: IncidentFilter) {
  return useQuery({
    queryKey: ['incidents', filters],
    queryFn: () => fetch(`/api/ehs/incidents?${new URLSearchParams(filters)}`).then(r => r.json()),
  });
}

export function useIncidentById(id: string) {
  return useQuery({
    queryKey: ['incident', id],
    queryFn: () => fetch(`/api/ehs/incidents/${id}`).then(r => r.json()),
  });
}

export function useCreateIncident() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateIncidentCommand) =>
      fetch('/api/ehs/incidents', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(data),
      }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['incidents'] });
    },
  });
}

// Similar for useRiskAssessments, useTrainingRecords...
```

### 5.3 Required Hooks

**Incidents (7 hooks):**
- `useIncidents(filters?)` — list
- `useIncidentById(id)` — get by ID
- `useCreateIncident()` — create
- `useStartInvestigation(id)` — start investigation
- `useAddFindings(id)` — add findings
- `useAddCorrectiveAction(id)` — add action
- `useCloseIncident(id)` — close

**RiskAssessments (5 hooks):**
- `useRiskAssessments(filters?)` — list
- `useRiskAssessmentById(id)` — get by ID
- `useRiskMatrix()` — get matrix summary
- `useCreateRiskAssessment()` — create
- `useAddControlMeasure(id)` — add control

**TrainingRecords (3 hooks):**
- `useTrainingRecords(filters?)` — list
- `useTrainingRecordById(id)` — get by ID
- `useCreateTrainingRecord()` — create

---

## Acceptance Criteria

- [ ] EHS Dashboard page with KPI strip + activity feed
- [ ] Incident reporting form (create)
- [ ] Incident detail view with FSM workflow actions
- [ ] Incident list with filters (Type, Status, Severity, Date)
- [ ] Risk Assessment 5×5 matrix visualization
- [ ] Risk Assessment form (create)
- [ ] Risk Assessment list with filters
- [ ] Training expiry dashboard (expiring in 30 days)
- [ ] Training record form (create/renew)
- [ ] All 15 API endpoints integrated (TanStack Query hooks)
- [ ] Responsive design (mobile + desktop)
- [ ] Dark mode support
- [ ] Loading states + error handling
- [ ] Build SUCCESS (0 errors, 0 warnings)

---

## Technical Stack

**Frontend:**
- React 18 + TypeScript
- TanStack Query v5 (API state management)
- React Hook Form + Zod (form validation)
- Shadcn UI components (Button, Card, Table, Form, Select, DatePicker)
- Tailwind CSS (styling)
- Lucide React (icons)

**API Client:**
- Orval (recommended) OR manual TanStack Query hooks
- Axios or native fetch

---

## File Structure

```
src/
  pages/
    EhsDashboardPage.tsx          # Main dashboard
    IncidentListPage.tsx          # Incident list
    IncidentDetailPage.tsx        # Incident detail + FSM
    IncidentReportPage.tsx        # New incident form
    RiskMatrixPage.tsx            # Risk matrix 5×5
    RiskAssessmentListPage.tsx    # Risk list
    RiskAssessmentFormPage.tsx    # New risk form
    TrainingCompliancePage.tsx    # Training expiry
    TrainingRecordFormPage.tsx    # New training form
  components/
    ehs/
      EhsKpiStrip.tsx
      EhsActivityFeed.tsx
      IncidentTable.tsx
      IncidentFsmStatus.tsx
      RiskMatrixGrid.tsx
      RiskScoreCalculator.tsx
      ExpiringTrainingsTable.tsx
  hooks/
    useIncidents.ts
    useRiskAssessments.ts
    useTrainingRecords.ts
  api/
    ehs/
      generated.ts              # Orval-generated client
```

---

## Design Patterns

**ISO 45001 Compliance Visual Language:**
- **Green** — Low risk, Valid, Closed
- **Yellow** — Medium risk, Expiring, Under investigation
- **Orange** — High risk, Needs attention
- **Red** — Critical risk, Expired, Immediate action

**FSM Workflow Pattern:**
- Visual progress indicator (stepper or status badge)
- Conditional action buttons (only show available transitions)
- Timeline view (event history)

**5×5 Risk Matrix:**
- Interactive grid (click cell to filter assessments)
- Color-coded cells (green → yellow → orange → red)
- Count badges on cells (number of assessments in each category)

---

## Security Considerations

**Authorization:**
- Check user role before showing "Create" buttons (only EHS Coordinators can create)
- API endpoints will enforce authorization server-side

**Data Validation:**
- Zod schemas for all forms (match backend FluentValidation rules)
- Client-side validation before API call
- Server-side validation errors displayed

**Multi-Tenancy:**
- Tenant context automatically added by API (no UI changes needed)
- User can only see incidents/risks/trainings from their tenant

---

## Next Steps After Frontend DONE

1. **CP-EHS-FRONTEND checkpoint** → DONE (EPICS.yaml)
2. **JoineryTech Phase 1 COMPLETE** → 7/7 modules production ready
3. **Integration testing** → E2E tests for EHS workflow
4. **Production deployment** → EHS Module live

---

## Referenciák

- Backend API DONE: MSG-BACKEND-191
- Week 1 Domain Layer: MSG-BACKEND-188
- Week 2 Application Layer: MSG-BACKEND-189
- Week 3 Infrastructure Layer: MSG-BACKEND-190
- Week 4 API Layer: MSG-BACKEND-191

---

**Estimated NWT:** 180 (~6-8 hours)
**Priority:** High (EHS Module completion)
**Model:** Sonnet

---

📋 Generated by Conductor — EHS Dashboard UI Dispatch (Final Phase)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
