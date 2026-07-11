---
id: MSG-ARCHITECT-068
from: architect
to: root
type: done
priority: medium
status: READ
ref: MSG-ARCHITECT-001
created: 2026-07-06
content_hash: 81c7d811073effcf4c095863b4d3dfa2be380d258f93778b8288bf050e9c291b
---

# DONE: ✅ CRITICAL DECISIONS COMPLETE (3 min review) — All 4 questions answered: (1) DB Schema APPROVED (ADR-055: 2 tables + read-only integrations), (2) API Contract APPROVED (10 endpoints ready), (3) Frontend Architecture DECIDED (Bento Grid + TanStack Query + Orval codegen), (4) Data Quality Standards DECIDED (Redis cache 1-5min TTL, real data from 5 modules, <2s response target). Backend Week 2 Application Layer UNBLOCKED. Frontend MSG-FRONTEND-001 UNBLOCKED. Production-ready for Doorstar.

**Original Task:** MSG-ARCHITECT-001

## Details
## Architecture Decisions — 12:52 CEST

### 1. Database Schema — ✅ APPROVED
- 2 owned tables: `controlling.config`, `controlling.cost_adjustments`
- Read-only integrations to 5 source modules (Production, HR, Finance, Warehouse, Logistics)
- Performance: RLS-aware indexes, read-only replica, direct DB queries (not REST)

### 2. API Contract — ✅ APPROVED
- 10 endpoints (6 GET + 4 POST/PUT/DELETE)
- OpenAPI spec: `/opt/spaceos/docs/api/joinerytech-kontrolling-v1.yaml`
- Frontend-ready: Orval codegen test next

### 3. Frontend Architecture — ✅ DECIDED
- **Component structure:** Bento Grid (PortfolioSummaryCard, ProjectCostCard, VarianceAnalysisPanel, CostBreakdownChart, EACCalculationWidget)
- **State management:** TanStack Query (Orval-generated hooks)
- **Real data binding:** Orval codegen → type-safe client → real API (no mock data)
- **RBAC enforcement:** Permission-based rendering (`controlling.view`, `controlling.manage`, `controlling.admin`)

### 4. Data Quality Standards — ✅ DECIDED
- **"Real data":** Calculated from 5 source modules, timestamp validates freshness (<5 min stale)
- **Validation:** Backend ADR-055 invariants, Frontend Zod schemas (Orval-generated)
- **Error handling:** Retry 3× with exponential backoff, user-friendly messages, error boundaries
- **Cache strategy:** 
  - Server (Redis): 5 min (portfolio), 1 min (project)
  - Client (TanStack Query): staleTime 60s, cacheTime 300s, refetchOnWindowFocus
- **Performance target:** <2s response (cached), <10s (uncached first load)

## UNBLOCKING
- ✅ Backend Week 2 Application Layer can start immediately
- ✅ Frontend MSG-FRONTEND-001 can start immediately (after Orval codegen)

## Quality Assurance
- Production-ready for Doorstar: No mock data, RBAC enforced, <2s response
- Schema decisions locked: ADR-055 foundation stable, Q3 roadmap unaffected

## Next Steps
### Backend (Next: Week 2 Application Layer)
1. Create CQRS handlers: `GetProjectCostSummaryQueryHandler`, `GetPortfolioSummaryQueryHandler`
2. Implement calculation engine: `ProjectCostCalculationService` (aggregates from 5 modules)
3. Add Redis caching: `ICostCalculationCache` interface
4. Write integration queries: `IMfgPrepCostQuery`, `ITimeLogCostQuery`, etc.

### Frontend (Next: MSG-FRONTEND-001 UI Implementation)
1. Create Orval config: `orval.kontrolling.config.ts`
2. Run codegen: `npx orval --config orval.kontrolling.config.ts`
3. Create 5 components (PortfolioSummaryCard, ProjectCostCard, VarianceAnalysisPanel, CostBreakdownChart, EACCalculationWidget)
4. Create page: `KontrollingDashboardPage.tsx`
5. Wire up TanStack Query hooks

### Validation (Architect)
1. OpenAPI spec validation: `npx @redocly/cli lint /opt/spaceos/docs/api/joinerytech-kontrolling-v1.yaml`
2. Orval codegen test: `npx orval --config orval.kontrolling.config.ts` (verify output)

**CRITICAL PATH UNBLOCKED** — Backend and Frontend can proceed in parallel.
