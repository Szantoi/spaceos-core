---
id: MSG-ARCHITECT-039
ref: MSG-ARCHITECT-036
from: architect
to: conductor
type: done
priority: high
status: READ
created: 2026-07-01
---

# CRM Domain Model Design — DONE

## Summary

Elkészítettem a JoineryTech CRM modul teljes domain modelljét ADR-054 formátumban.

## Deliverables

**ADR Dokumentáció:** `/opt/spaceos/docs/architecture/decisions/ADR-054-joinerytech-crm-domain-model.md`

**Tartalom:**
- ✅ Aggregate boundaries (Lead, Opportunity)
- ✅ FSM diagramok (Mermaid formátumban)
- ✅ Domain events katalógus (18 esemény)
- ✅ Integration contracts (CRM → Sales, Identity, Customer)
- ✅ Adatbázis séma (RLS policies, indexek, constraints)
- ✅ CQRS command/query handlers (15 command, 9 query)
- ✅ REST API endpoints (19 endpoint)
- ✅ Testing strategy (unit tests, integration tests, E2E)
- ✅ Performance & scalability considerations
- ✅ 5-week implementation plan

## Architecture Highlights

### 1. Aggregate Boundaries

**Lead Aggregate:**
- FSM: New → Contacted → Qualified → Opportunity
- Activities & Tasks kezelése SLA monitoring-gal
- Webshop inquiry auto-lead creation

**Opportunity Aggregate:**
- FSM: Draft → Proposal → Negotiation → Won/Lost/Abandoned
- Forecast kalkuláció (pipeline, weighted, won value)
- Quote konverzió integráció

### 2. Domain Events

**Lead Events (5):**
- LeadCreated, LeadContacted, LeadQualified, LeadDisqualified, LeadConvertedToOpportunity

**Opportunity Events (7):**
- OpportunityCreated, OpportunityProposed, OpportunityNegotiated, OpportunityWon, OpportunityLost, OpportunityAbandoned, OpportunityRevisedToProposal

**Activity/Task Events (6):**
- LeadActivityAdded, LeadTaskAdded, LeadTaskCompleted
- OpportunityActivityAdded, OpportunityTaskAdded, OpportunityTaskCompleted

### 3. Integration Contracts

**CRM → Sales:**
```csharp
IQuoteCreationService.CreateQuoteFromOpportunityAsync()
```
- Opportunity (Proposal/Negotiation) → Quote (draft)
- OpportunityRef metadata field
- Permissions: crm.manage + quote.create

**CRM → Identity:**
```csharp
IUserValidationService.UserExistsAsync()
```
- Lead/Opportunity assignment validation
- Activity CreatedBy validation

**CRM → Customer:**
```csharp
ILeadCreationService.CreateLeadFromWebshopInquiryAsync()
```
- Auto-lead creation from webshop
- Permission-less (public endpoint)

### 4. Database Schema

**4 Tables:**
- `crm.leads` — Lead aggregate root
- `crm.opportunities` — Opportunity aggregate root
- `crm.activities` — Activity log (polymorphic: Lead/Opportunity)
- `crm.tasks` — Task management (polymorphic: Lead/Opportunity)

**RLS Policies:**
- Tenant isolation minden táblán
- Role-based access control (crm.manage, crm.view)

**Indexes:**
- `(tenant_id, status)` composite index
- `assigned_to` index (user-based queries)
- `created_at DESC` index (recent items)
- `expected_close_date` index (pipeline reports)
- Partial index on `crm.tasks` WHERE `completed = FALSE` (overdue SLA)

### 5. Performance Considerations

**Caching:**
- Forecast calculations: 5 min cache per tenant
- User info: 1 hour cache
- Activities/Tasks: Real-time, no cache

**Archival:**
- Closed opportunities: Archive after 2 years
- Disqualified leads: Archive after 1 year

## Implementation Plan

**Week 1:** Domain Layer (Aggregates, Value Objects, FSM)
**Week 2:** Application Layer (CQRS handlers, MediatR)
**Week 3:** Infrastructure Layer (Database, Repositories, Event Bus)
**Week 4:** API Layer (Controllers, OpenAPI, Authorization)
**Week 5:** Sales Integration (Opportunity → Quote E2E)

## Acceptance Criteria Status

- [x] Aggregate boundaries definiálva
- [x] FSM diagramok (Lead, Opportunity)
- [x] Domain events catalog
- [x] Integration contract CRM→Sales
- [x] ADR dokumentáció

## Files Changed

- `/opt/spaceos/docs/architecture/decisions/ADR-054-joinerytech-crm-domain-model.md` (NEW)

## Technical Debt

Nincs.

## Next Steps

1. **Backend Terminal:** Implementálja az ADR-054 alapján a CRM modult
2. **Sales Terminal:** Implementálja az `IQuoteCreationService` interface-t
3. **Identity Terminal:** Implementálja az `IUserValidationService` interface-t
4. **Frontend Terminal:** CRM UI komponensek (Lead grid, Opportunity pipeline, Activity log)

## Notes

Az ADR-054 követi a SpaceOS architektúra minden alapelvét:
- Modular Monolith (ADR-002)
- Immutability & Audit Trail (ADR-003)
- RBAC & RLS (ADR-004)
- Walking Skeleton First (ADR-005)
- Event Sourcing Patterns
- CQRS Handler Generator (ADR-051)
- FSM Subscription System (ADR-052)

A dokumentáció készen áll a backend implementációra.
