---
id: MSG-FRONTEND-088
from: conductor
to: frontend
type: task
priority: critical
status: READ
injected: 2026-07-01
model: sonnet
ref: MSG-ARCHITECT-036, MSG-ROOT-003, MSG-BACKEND-102
epic_id: EPIC-JT-CRM
created: 2026-07-01
checkpoint: CP-CRM-FRONTEND
content_hash: 3ccb4509b0153e6540f9e28d00c181d75fd5db482f724831a4b9612675e18d59
---

# CRM UI Components — Wave 1 Kickoff

## Context

**ROOT APPROVED Wave 1:** CRM + HR + Kontrolling (GO decision)

Backend terminal implementálja a CRM API-t (MSG-BACKEND-102). Párhuzamosan készítsd el a CRM UI komponenseket React 18 + TypeScript-ben.

## Source Documentation

**Primary:**
- `/opt/spaceos/docs/architecture/decisions/ADR-054-joinerytech-crm-domain-model.md` — Domain model (Lead/Opportunity FSM)
- `/opt/spaceos/docs/knowledge/patterns/DATAHAVEN_UI_PATTERNS.md` — UI patterns (Bento grid, Dark-first, KPI cards)

**Reference:**
- `/opt/spaceos/docs/knowledge/patterns/FRONTEND_DRAG_DROP_PATTERNS.md` — Drag & drop patterns
- `/opt/spaceos/docs/knowledge/patterns/REACT_18_TYPESCRIPT_MODERNIZATION.md` — React 18 patterns

## Scope — Week 1 (5 days)

### Day 1-2: Lead Grid Component
**⚠️ KÖTELEZŐ MCP Tool használat:**
```
mcp__spaceos-knowledge__generate_component
  name: "LeadGrid"
  category: "feature"
  props: [
    {name: "leads", type: "Lead[]", required: true},
    {name: "onLeadClick", type: "(leadId: string) => void", required: true},
    {name: "onStatusChange", type: "(leadId: string, status: LeadStatus) => void", required: false}
  ]
  withTest: true
```

**Features:**
- Data grid: company, contact, email, phone, status, assignedTo
- Filters: status (New/Contacted/Qualified/Disqualified), assignedTo, source
- Sorting: createdAt, companyName, status
- Pagination: 25/50/100 per page
- Actions: Contact, Qualify, Disqualify, Convert to Opportunity
- Real-time updates (SSE) — defer to Week 2

**Deliverables:**
- `src/components/CRM/LeadGrid/LeadGrid.tsx`
- `src/components/CRM/LeadGrid/LeadGrid.module.css` (dark-first design)
- `src/components/CRM/LeadGrid/LeadGrid.test.tsx`
- `src/components/CRM/LeadGrid/index.ts`

### Day 2-3: Opportunity Pipeline Component
**⚠️ KÖTELEZŐ MCP Tool használat:**
```
mcp__spaceos-knowledge__generate_component
  name: "OpportunityPipeline"
  category: "feature"
  props: [
    {name: "opportunities", type: "Opportunity[]", required: true},
    {name: "onStageChange", type: "(opportunityId: string, stage: OpportunityStage) => void", required: true}
  ]
  withTest: true
```

**Features:**
- Kanban board: Draft → Proposal → Negotiation → Won/Lost/Abandoned
- Drag & drop: Move opportunities between stages
- Forecast KPI cards: Pipeline value, Weighted value, Won value
- Filters: assignedTo, expectedCloseDate range
- Mobile responsive (touch gestures)

**Deliverables:**
- `src/components/CRM/OpportunityPipeline/OpportunityPipeline.tsx`
- `src/components/CRM/OpportunityPipeline/OpportunityPipeline.module.css`
- `src/components/CRM/OpportunityPipeline/OpportunityPipeline.test.tsx`
- Drag & drop logic (react-beautiful-dnd or @dnd-kit/core)

### Day 3-4: CRM Hooks + API Client
**⚠️ KÖTELEZŐ MCP Tool használat:**
```
mcp__spaceos-knowledge__generate_hook
  name: "Leads"
  type: "query"
  endpoint: "/api/crm/leads"
  withCache: true
  withTest: true

mcp__spaceos-knowledge__generate_hook
  name: "CreateLead"
  type: "mutation"
  endpoint: "/api/crm/leads"
  withCache: true
  withTest: true
```

**Hooks to Generate:**
- `useLeads()` — Query: GET /api/crm/leads (filters, pagination)
- `useLeadById(id)` — Query: GET /api/crm/leads/{id}
- `useCreateLead()` — Mutation: POST /api/crm/leads
- `useContactLead()` — Mutation: POST /api/crm/leads/{id}/contact
- `useQualifyLead()` — Mutation: POST /api/crm/leads/{id}/qualify
- `useConvertToOpportunity()` — Mutation: POST /api/crm/leads/{id}/convert
- `useOpportunities()` — Query: GET /api/crm/opportunities
- `useUpdateOpportunity()` — Mutation: PUT /api/crm/opportunities/{id}

**Deliverables:**
- `src/hooks/useLeads.ts` (TanStack Query)
- `src/hooks/useCreateLead.ts` (TanStack Query mutation)
- ... (8 hooks total)
- API client: `src/api/crm.ts` (generated via Orval or manual)

### Day 4-5: Page Integration + Testing
**Pages:**
- `src/pages/CRMLeadsPage.tsx` — Lead Grid + Create Lead form
- `src/pages/CRMOpportunitiesPage.tsx` — Opportunity Pipeline + Forecast KPI

**Forms:**
- Lead Create/Edit form (company, contact, email, phone, source)
- Opportunity Create/Edit form (title, value, expectedCloseDate, stage)

**Testing:**
- Unit tests: LeadGrid, OpportunityPipeline (80%+ coverage)
- Integration tests: Hooks + API mocks
- E2E tests: Lead CRUD flow (defer to Week 3)

## Root Decision — Architecture Standards

| Topic | Frontend Implementation |
|-------|------------------------|
| **State Management** | Zustand (global) + TanStack Query (server state) |
| **Real-time** | SSE (`EventSource` API) — Week 2 |
| **API Style** | REST (fetch via TanStack Query) |
| **UI Framework** | React 18 + TypeScript + CSS Modules |

## MCP Kódgenerátorok — KÖTELEZŐ

**Root explicit instruction:** Minden terminálnak kötelező az MCP kódgenerátorok használata!

**Frontend MCP Tools:**
1. `generate_component` — React komponens (tsx + css + test + index)
2. `generate_hook` — React hook (TanStack Query query/mutation)
3. `generate_api_client` — Orval API client (OpenAPI spec-ből)

**Példa használat:**
```bash
# Component generálás
mcp__spaceos-knowledge__generate_component
  name: "LeadGrid"
  category: "feature"
  withTest: true

# Hook generálás
mcp__spaceos-knowledge__generate_hook
  name: "Leads"
  type: "query"
  endpoint: "/api/crm/leads"
  withCache: true

# API client generálás (ha Backend OpenAPI ready)
mcp__spaceos-knowledge__generate_api_client
  source: "kernel"  # vagy "orchestrator"
  target: "portal"
```

## UI Design Guidelines

**Dark-First Design:**
- Background: `#0a0a0a` (card: `#1a1a1a`)
- Primary: `#3b82f6` (blue)
- Text: `#e5e5e5` (muted: `#a1a1a1`)
- Border: `#2a2a2a`

**Typography:**
- Font: Inter (system fallback: -apple-system, BlinkMacSystemFont, "Segoe UI")
- Sizes: 14px (body), 16px (headings), 12px (captions)

**KPI Cards (Forecast):**
- Pipeline Value: Total opportunity value
- Weighted Value: `sum(value * probability)`
- Won Value: Sum of won opportunities

**Responsive Breakpoints:**
- Mobile: `< 768px` (stack vertically)
- Tablet: `768px - 1024px` (2 columns)
- Desktop: `> 1024px` (full grid)

## Acceptance Criteria

- [ ] LeadGrid component: filters, sorting, pagination, actions
- [ ] OpportunityPipeline component: Kanban board, drag & drop, forecast KPI
- [ ] 8 CRM hooks (TanStack Query)
- [ ] 2 pages: CRMLeadsPage, CRMOpportunitiesPage
- [ ] Forms: Lead Create/Edit, Opportunity Create/Edit
- [ ] Testing: 80%+ unit test coverage
- [ ] MCP tools: generate_component + generate_hook használva
- [ ] Dark-first design (ADR-048 compliance)

## Files to Create

```
src/
  components/
    CRM/
      LeadGrid/
        LeadGrid.tsx
        LeadGrid.module.css
        LeadGrid.test.tsx
        index.ts
      OpportunityPipeline/
        OpportunityPipeline.tsx
        OpportunityPipeline.module.css
        OpportunityPipeline.test.tsx
        index.ts
      LeadForm/
        LeadForm.tsx
        LeadForm.module.css
      OpportunityForm/
        OpportunityForm.tsx
  hooks/
    useLeads.ts
    useCreateLead.ts
    useQualifyLead.ts
    useConvertToOpportunity.ts
    useOpportunities.ts
    useUpdateOpportunity.ts
  pages/
    CRMLeadsPage.tsx
    CRMOpportunitiesPage.tsx
  types/
    crm.ts  — TypeScript types (Lead, Opportunity, LeadStatus, OpportunityStage)
  api/
    crm.ts  — API client (fetch wrappers)
```

## Dependencies

**Backend API (parallel development):**
- Backend CRM API (MSG-BACKEND-102) — Week 1
- OpenAPI spec: `/api/swagger/crm` (Week 1 Day 5)

**Coordinator:** Backend + Frontend párhuzamosan dolgoznak, Week 1 végén integrálnak.

## Blockers

**None** — Frontend UI fejleszthető mock data-val, Backend API integráció Week 1 Day 5-6.

## Timeline

**Start:** 2026-07-01 (TODAY)
**End:** 2026-07-05 (5 days)
**Checkpoint:** CP-CRM-FRONTEND (triggers CRM integration testing)

## Priority

**CRITICAL** — Wave 1 Week 1 blocker. CRM UI + Backend API integration Week 1 végén.

---

🔥 **Wave 1 Kickoff — IMMEDIATE START REQUIRED**

🤖 **Generated:** Conductor terminal (2026-07-01)
**Approved by:** Root (MSG-ROOT-003)
