---
id: MSG-BACKEND-103
from: conductor
to: backend
type: task
priority: high
status: INJECTED
injected: 2026-07-03
model: sonnet
epic_id: EPIC-JT-CRM
project_id: joinerytech-prod
created: 2026-07-01
content_hash: 342076e201afc3535ada453f0444a9a5893cb7d89a9c80e2862fa6038d74bc3d
---

# JT-CRM-002: CRM Backend API Implementation

# CRM Backend API Implementation

Implementáld a JoineryTech CRM modul backend API-ját az ADR-054 alapján.

## ADR Forrás
`/opt/spaceos/docs/architecture/decisions/ADR-054-joinerytech-crm-domain-model.md`

## Scope

### Domain Layer
- `Lead` aggregate (FSM: New → Contacted → Qualified → Opportunity)
- `Opportunity` aggregate (FSM: Draft → Proposal → Negotiation → Won/Lost/Abandoned)
- Activities & Tasks entity (polymorphic: Lead/Opportunity)
- 18 domain events (LeadCreated, OpportunityWon, stb.)

### Application Layer (CQRS)
- **15 Commands:** CreateLead, ContactLead, QualifyLead, ConvertToOpportunity, CreateOpportunity, ProposeOpportunity, NegotiateOpportunity, WinOpportunity, LoseOpportunity, AbandonOpportunity, ReviseToProposal, AddActivity, AddTask, CompleteTask, DeleteLead
- **9 Queries:** GetLeads, GetLeadById, GetOpportunities, GetOpportunityById, GetActivities, GetTasks, GetPipelineForecast, GetLeadsByStatus, GetOpportunitiesForQuoteConversion

### Infrastructure Layer
- Database schema: `crm.leads`, `crm.opportunities`, `crm.activities`, `crm.tasks`
- RLS policies (tenant isolation + role-based: crm.manage, crm.view)
- Indexes: (tenant_id, status), assigned_to, created_at DESC, expected_close_date
- Partial index on tasks WHERE completed = FALSE
- Repositories: ILeadRepository, IOpportunityRepository

### API Layer (19 endpoints)
- Lead Management (9 endpoints)
- Opportunity Management (10 endpoints)
- Permissions: crm.view, crm.manage, crm.admin

### Integration Contracts
1. **CRM → Sales:** `IQuoteCreationService.CreateQuoteFromOpportunityAsync()`
2. **CRM → Identity:** `IUserValidationService.UserExistsAsync()`
3. **CRM → Customer:** `ILeadCreationService.CreateLeadFromWebshopInquiryAsync()`

## Codegen Support

Használd az ADR-051 CQRS Handler Generator-t:
```bash
./scripts/codegen/generate-handler.sh CreateLead \
  --type command \
  --module CRM \
  --repository ILeadRepository \
  --aggregate Lead \
  --with-test
```

## Implementation Plan (5 weeks)
- **Week 1:** Domain Layer (Aggregates, Value Objects, FSM)
- **Week 2:** Application Layer (CQRS handlers, MediatR)
- **Week 3:** Infrastructure Layer (Database, Repositories, Event Bus)
- **Week 4:** API Layer (Controllers, OpenAPI, Authorization)
- **Week 5:** Sales Integration (Opportunity → Quote E2E)

## Build & Test
- 0 TypeScript/C# errors
- Unit tests (FSM transitions, domain events)
- Integration tests (API endpoints, RLS policies)
- E2E test (Lead → Opportunity → Quote conversion)

## Files to Create
- `src/Modules/CRM/Domain/` (Aggregates, Events)
- `src/Modules/CRM/Application/` (Commands, Queries, Handlers)
- `src/Modules/CRM/Infrastructure/` (Database, Repositories)
- `src/Modules/CRM/API/` (Controllers, DTOs)
- `tests/CRM.Tests/` (Unit, Integration, E2E)

## Acceptance Criteria

- [ ] Domain aggregates (Lead, Opportunity) implemented with FSM validation
- [ ] 18 domain events defined and published
- [ ] 15 command handlers + 9 query handlers implemented (CQRS)
- [ ] Database schema created (4 tables, RLS policies, indexes)
- [ ] 19 REST API endpoints implemented with OpenAPI
- [ ] 3 integration contracts defined (interfaces only)
- [ ] Unit tests for FSM transitions and domain events
- [ ] Integration tests for API endpoints and RLS
- [ ] Build passes with 0 errors
- [ ] E2E test: Lead → Opportunity → Quote conversion (interface stub)
