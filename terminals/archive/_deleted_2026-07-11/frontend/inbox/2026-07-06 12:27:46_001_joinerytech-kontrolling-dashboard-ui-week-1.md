---
id: MSG-FRONTEND-001
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: null
epic_id: null
project_id: null
created: 2026-07-06 12:27:46
completed: 2026-07-06 14:40:00
content_hash: cdcde05bd5b268d0bbf254f675729882566196eeeda85100d5a198e2f9e4ae89
---

# JoineryTech Kontrolling Dashboard UI — Week 1

# JoineryTech Kontrolling Dashboard UI — Week 1

**Epic:** EPIC-JT-CTRL
**Checkpoint:** CP-CTRL-FRONTEND
**Estimated:** 60 NWT (~2 hours)

## Context

Kontrolling Backend API **ready** ✅:
- 115/115 tests PASS
- OpenAPI spec: `/opt/spaceos/docs/api/joinerytech-kontrolling-v1.yaml`
- 5 Query Handlers + 3 Command Handlers implemented

## Deliverables

### 1. Dashboard Components
- **EACCalculationWidget** — EAC kalkuláció kategóriánkénti bontással
- **CostBreakdownChart** — Költség breakdown (planned/actual/projected)
- **VarianceAnalysisPanel** — Költségeltérés analízis, worst performing kategória
- **PortfolioSummaryCard** — Portfolio szintű aggregált összefoglaló

### 2. API Integration
- Use Orval-generated React Query hooks
- Connect to Kontrolling Backend endpoints
- Real data (NOT mock)

### 3. Quality Gates
- [ ] Forms validate + submit to real endpoints
- [ ] Dashboard fetches real data
- [ ] Error handling implemented
- [ ] RBAC permissions enforced (UI-level)

## Reference
- OpenAPI: `/opt/spaceos/docs/api/joinerytech-kontrolling-v1.yaml`
- Backend DONE: MSG-BACKEND-148-DONE (115 tests)
