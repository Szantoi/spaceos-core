# JoineryTech Module Roadmap Template

**Date:** 2026-07-07
**Source:** Explorer Terminal Memory (JoineryTech Research Findings)
**Priority:** Medium
**Complexity:** Low (2-3 days)

---

## Concept

8-module phased delivery roadmap template with week-by-week breakdown, dependency matrix, and risk assessment for clear multi-month visibility.

---

## Problem

- JoineryTech has 8 modules (CRM, Kontrolling, HR, Maintenance, QA, DMS, Assembly, AI)
- No unified roadmap showing dependencies, blockers, and phase progression
- Teams don't know when upstream modules will be ready
- Hard to communicate timeline to stakeholders (Doorstar, investors)

---

## Solution

Create standardized roadmap template that:

### Core Elements
1. **Phase Overview** (1-4 week waves)
2. **Module Dependency Matrix** (which module depends on what)
3. **Week-by-Week Breakdown** (granular task list)
4. **Parallel vs Sequential** (what can run concurrently)
5. **Risk Assessment** (blockers, delays, mitigation)
6. **Success Metrics** (completion criteria per module)

### Template Structure
```markdown
# JoineryTech Module Roadmap — 2026 Q2-Q4

## Overview

**Total Duration:** 20 weeks (2026-07-01 → 2026-11-15)
**Modules:** 8 (CRM → Kontrolling → HR → Maintenance → QA → DMS → Assembly → AI)
**Phases:** 3 (Foundation, Transactions, Operations)

---

## Phase 1: Foundation (Weeks 1-4)
**Goal:** Auth + Catalog + CRM core (no transactional state)

| Week | Module | Tasks | Dependencies | Status |
|------|--------|-------|--------------|--------|
| 1 | Auth | Login, JWT, RBAC | None | ✅ DONE |
| 2 | Catalog | Product catalog API | Auth | 🔄 IN PROGRESS |
| 3 | CRM | Lead + Opportunity FSM | Auth, Catalog | ⏳ PENDING |
| 4 | CRM | Lead → Opportunity conversion | Phase 1 Week 3 | ⏳ PENDING |

**Deliverable:** Auth working + Catalog browsable + CRM Lead→Opportunity flow
**Success Metric:** Doorstar can log in + view products + track 1 lead
**Risk:** Auth token refresh issues (MITIGATED: JWT best practices)

---

## Phase 2: Transactions (Weeks 5-12)
**Goal:** Quotes, Orders, Invoices, Warehouse

| Week | Module | Tasks | Dependencies | Status |
|------|--------|-------|--------------|--------|
| 5 | CRM | Quote Request API | Phase 1 CRM | ⏳ PENDING |
| 6 | Sales | Quote → Order conversion | CRM Quote | ⏳ PENDING |
| 7 | Warehouse | Inventory allocation | Sales Order | ⏳ PENDING |
| 8 | Invoicing | Order → Invoice generation | Sales Order | ⏳ PENDING |
| 9-12 | **Kontrolling** | Cost tracking + Dashboards | All Phase 2 | ⏳ PENDING |

**Deliverable:** Full quote-to-cash cycle working
**Success Metric:** Doorstar can create quote → order → invoice
**Risk:** State management complexity (MITIGATED: FSM + Event Sourcing)

---

## Phase 3: Operations (Weeks 13-20)
**Goal:** Production, HR, EHS, QA, DMS, Assembly, AI

| Week | Module | Tasks | Dependencies | Status |
|------|--------|-------|--------------|--------|
| 13-14 | **HR** | Attendance, Time Tracking | Auth | ⏳ PENDING |
| 15-16 | **Maintenance** | Equipment + Service Logs | Warehouse | ⏳ PENDING |
| 17-18 | **QA** | Inspection + Non-Conformance | Production | ⏳ PENDING |
| 19 | **DMS** | Document storage + versioning | All modules | ⏳ PENDING |
| 20 | **Assembly** | BOM + Production Schedule | Catalog, Warehouse | ⏳ PENDING |
| 21+ | **AI** | Predictive analytics | All Phase 3 | ⏳ PLANNED |

**Deliverable:** Full operational visibility (HR + Maintenance + QA)
**Success Metric:** Doorstar manages employees + equipment + quality
**Risk:** Integration testing surface area (MITIGATED: Incremental E2E)

---

## Dependency Matrix

```
CRM ━━━━┓
        ┣━━→ Sales ━━┓
Catalog ┛            ┣━━→ Warehouse ━━→ Invoicing
                     ┃
                     ┗━━→ Kontrolling (depends on ALL)

HR ━━━━━━━━━━━━━━━━━━━━┓
Maintenance ━━━━━━━━━━━━┫
QA ━━━━━━━━━━━━━━━━━━━━┣━━→ DMS ━━→ Assembly ━━→ AI
Production ━━━━━━━━━━━━┛
```

**Critical Path:** Auth → Catalog → CRM → Sales → Warehouse (longest dependency chain)

---

## Parallel Work Streams

**Week 1-4 (Phase 1):**
- Backend: Auth + Catalog API (sequential)
- Frontend: Login UI + Catalog UI (parallel after Auth)
- Design: All UI screens upfront (parallel)

**Week 5-12 (Phase 2):**
- Backend Team A: CRM Quote API
- Backend Team B: Sales Order API (parallel after Quote)
- Frontend: Mock API development (parallel)

**Week 13-20 (Phase 3):**
- Backend: HR + Maintenance + QA (parallel, independent modules)
- Frontend: 3 parallel tracks (HR UI, Maintenance UI, QA UI)

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| **Auth token issues** | Medium | High | JWT best practices, refresh token flow tested Week 1 |
| **State management bugs** | High | High | FSM + Event Sourcing + integration spike Week 2 |
| **Integration surface area** | Medium | Medium | Incremental E2E tests, not big-bang at end |
| **Module delay cascade** | High | High | Parallel work streams (HR/Maintenance/QA independent) |
| **Infrastructure blocker** | Medium | High | 24h resolution SLA, fallback to manual override |

---

## Success Metrics

| Phase | Metric | Target |
|-------|--------|--------|
| **Phase 1** | Auth success rate | 99.5% |
| **Phase 1** | Catalog API response time | <200ms (p95) |
| **Phase 2** | Quote-to-Order conversion | <2 min end-to-end |
| **Phase 3** | HR attendance tracking | 100% employees |
| **Overall** | Test coverage | ≥80% |

---

## Communication Cadence

**Weekly:**
- Monday: Phase status update (DONE, IN PROGRESS, BLOCKED)
- Friday: Next week planning + dependency check

**Monthly:**
- Doorstar stakeholder demo (show working features)
- Roadmap adjustment (if delays or new requirements)

---
```

---

## Benefits

- **Visibility:** All teams know dependencies, blockers, timeline
- **Stakeholder Communication:** Simple timeline for Doorstar/investors
- **Risk Management:** Explicit mitigation strategies
- **Parallel Work:** Clear when modules can run concurrently

---

## Implementation

**Template Location:** `/opt/spaceos/docs/knowledge/patterns/MODULE_DELIVERY_ROADMAP_TEMPLATE.md`

**Usage:**
1. Copy template for new multi-module project
2. Fill in modules, dependencies, week-by-week tasks
3. Update weekly status (markdown table)
4. Communicate to stakeholders monthly

**Integration:**
- Conductor uses template for epic planning
- Root uses template for business roadmap
- Architect uses template for dependency validation

---

## Example Projects That Could Use Template

1. **JoineryTech Full Stack** (current use case)
2. **SpaceOS Portal v2** (dashboard + planning + kanban + projects)
3. **Doorstar Soft Launch** (CRM + Sales + Warehouse + Production)
4. **Multi-Tenant Expansion** (Auth + Tenant Isolation + RBAC + Audit)

---

## ROI Estimate

- **Time Savings:** 2-3 hours/week roadmap sync × 20 weeks = **40-60 hours/project**
- **Stakeholder Confidence:** Clear timeline → faster approval
- **Risk Reduction:** Early blocker identification → fewer delays

---

## Next Steps

1. Create template in `docs/knowledge/patterns/MODULE_DELIVERY_ROADMAP_TEMPLATE.md`
2. Apply to JoineryTech project (test with real data)
3. Refine based on Conductor feedback
4. Add to Conductor workflow (epic planning)

---

**Status:** Pending Implementation
**Assigned:** Librarian (template creation)
**Estimated Effort:** 2-3 days (includes JoineryTech example)
