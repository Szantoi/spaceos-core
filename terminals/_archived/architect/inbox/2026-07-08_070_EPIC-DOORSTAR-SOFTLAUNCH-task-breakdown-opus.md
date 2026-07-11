---
id: MSG-ARCHITECT-070
from: conductor
to: architect
type: task
priority: critical
status: READ
model: opus
ref: MSG-ROOT-004-RESPONSE, EPIC-DOORSTAR-SOFTLAUNCH
created: 2026-07-08
completed: 2026-07-08
epic_id: EPIC-DOORSTAR-SOFTLAUNCH
estimated_nwt: 240
actual_nwt: 180
---

# EPIC-DOORSTAR-SOFTLAUNCH: Task Breakdown + TASKS.yaml Creation (Opus)

**Epic:** EPIC-DOORSTAR-SOFTLAUNCH (Első éles ügyfél: Doorstar Kft.)
**Status:** ACTIVE (planning phase, activated: 2026-07-08)
**Parallel Track:** EPIC-JT-EHS development (MSG-ARCHITECT-069)
**Timeline:** 6-8 hours (180-240 NWT)
**Model:** **OPUS** (complex integration planning)

---

## Context: Aggressive Parallel Execution Strategy

**VPS Capacity Upgrade:**
- CPU: 6 cores, RAM: 15GB → **AGGRESSIVE PARALLEL** authorized
- **Phase 1A:** EPIC-JT-EHS development (Backend/Frontend/Architect)
- **Phase 1B:** EPIC-DOORSTAR-SOFTLAUNCH planning (THIS TASK - Architect Opus)

**Your Role:** Lead Doorstar epic planning (Week 1-2: 2026-07-08 → 2026-07-22)

**Resource Allocation:** 1-2 CPU cores, ~1GB RAM (sustainable with EHS parallel)

---

## Task: Detailed TASKS.yaml Creation + Planning

**Scope:** Doorstar Kft. Soft Launch (First Paying Customer)
**Complexity:** MEDIUM-HIGH (Keycloak, Joinery, B2B, VPS deployment)
**Timeline:** 2.5 months (2026-07-22 → 2026-09-30) for execution
**Estimated NWT:** 1200 (~40 hours for full execution)

**Deliverables:**
1. ✅ Detailed `spaceos/doorstar/TASKS.yaml` (6 checkpoints, task dependencies)
2. ✅ Keycloak tenant configuration spec
3. ✅ Modules.Joinery integration plan
4. ✅ B2B handshake design (TenantHandshakeAllowlist)
5. ✅ VPS deployment plan
6. ✅ DONE outbox summary

---

## Reference Documentation

**Primary:**
- `docs/knowledge/architecture/SpaceOS_Doorstar_Onboarding_v4.md` (2026-04-08 - MAY BE OUTDATED)
- EPICS.yaml: EPIC-CUTTING-Q3 completion context (6 modules DONE)
- EPICS.yaml: EPIC-DOORSTAR-SOFTLAUNCH description

**Context Updates Since v4 (April 2026):**
- ✅ EPIC-CUTTING-Q3 DONE (6 modules: DMS, HR, Maintenance, QA, CRM, Kontrolling)
- ✅ 75+ API endpoints ready (Week 1-4 pattern validated)
- ✅ Multi-tenancy RLS architecture proven
- ✅ Testcontainers integration tests pattern established
- ✅ ADR-054 (CRM), ADR-055 (Kontrolling) compliance verified

**Assumption:** v4 doc may reference outdated statuses (Phase 3C+, ProdReady Sprint, Modules.Joinery). **UPDATE BASED ON CURRENT STATE.**

---

## Epic Scope: Doorstar Soft Launch

**Business Goal:** First paying customer (Doorstar Kft. ajtógyártó) on production SpaceOS

**Integration Requirements:**
1. **Keycloak Integration** — Multi-tenant auth, Doorstar tenant seed
2. **Modules.Joinery Integration** — Gyártólap PDF, batch anyaglista, order conversion
3. **B2B Handshake** — TenantHandshakeAllowlist for inter-tenant communication
4. **VPS Deployment** — Production infrastructure (PostgreSQL, .NET 8, Node.js 22)
5. **UAT (User Acceptance Testing)** — Doorstar stakeholder validation
6. **Soft Launch** — Production go-live (limited feature set)

---

## 6 Checkpoints to Define (Root Guidance)

### CP-1: KEYCLOAK-SETUP
**Goal:** Keycloak tenant configuration + seed data
**Tasks:**
- Keycloak realm configuration (SpaceOS realm)
- Doorstar tenant creation (TenantId, metadata)
- User/role seed data (admin, manager, operator)
- JWT token configuration (ES256 ECDSA P-256)
- Client registration (Portal, Orchestrator)

**Estimated NWT:** ~120 (4 hours)
**Terminal:** Backend (Keycloak admin API) + Infra (VPS Keycloak setup)

---

### CP-2: JOINERY-INTEGRATION
**Goal:** Modules.Joinery deployment + integration testing
**Tasks:**
- Modules.Joinery codebase review (current status unknown)
- Integration with Kernel (auth, audit, FSM)
- Database migration (Joinery schema)
- API endpoint testing (Gyártólap PDF, batch anyaglista)
- E2E test: Order → Gyártólap PDF generation

**Estimated NWT:** ~240 (8 hours)
**Terminal:** Backend (Joinery module integration) + E2E (Playwright tests)

**Blocker Risk:** Modules.Joinery status unknown (may need implementation if not in EPIC-CUTTING-Q3)

---

### CP-3: B2B-HANDSHAKE
**Goal:** TenantHandshakeAllowlist for inter-tenant communication
**Tasks:**
- Design B2B handshake protocol (tenant-to-tenant API calls)
- TenantHandshakeAllowlist aggregate (Domain/Application/Infrastructure/API)
- Seed data: Doorstar ↔ Suppliers allowlist
- Security validation (JWT tenant claim verification)
- Integration test: Doorstar → Supplier quote request

**Estimated NWT:** ~180 (6 hours)
**Terminal:** Backend (B2B handshake module) + Architect (protocol design)

**Blocker Risk:** B2B handshake may not exist (check if in EPIC-CUTTING-Q3)

---

### CP-4: VPS-DEPLOY
**Goal:** Production VPS deployment + infrastructure ready
**Tasks:**
- PostgreSQL 16 production instance (RLS, multi-tenancy)
- .NET 8 API deployment (Kernel + 6 modules + Joinery)
- Node.js 22 Orchestrator BFF deployment
- React Portal deployment (Nginx reverse proxy)
- SSL/TLS certificates (Let's Encrypt)
- Monitoring setup (Grafana, Prometheus)
- Backup strategy (PostgreSQL automated backups)

**Estimated NWT:** ~240 (8 hours)
**Terminal:** Infra (VPS operator) + Backend (deployment scripts)

---

### CP-5: DOORSTAR-UAT
**Goal:** User acceptance testing with Doorstar stakeholders
**Tasks:**
- Doorstar user onboarding (tenant admin, 3-5 users)
- UAT scenario execution (Order → Gyártólap PDF → Batch anyaglista)
- Bug fixes from UAT feedback
- Performance testing (API response times, load testing)
- Security audit (JWT validation, RLS isolation)

**Estimated NWT:** ~240 (8 hours)
**Terminal:** Frontend (UAT support) + Backend (bug fixes) + Root (stakeholder coordination)

---

### CP-6: SOFT-LAUNCH
**Goal:** Production go-live (limited feature set)
**Tasks:**
- Final production deployment (version tag)
- Monitoring dashboard setup (real-time alerts)
- Doorstar training session (admin, manager, operator)
- Support SLA agreement (24h response time)
- Post-launch monitoring (first 2 weeks)

**Estimated NWT:** ~180 (6 hours)
**Terminal:** Root (business coordination) + Backend/Frontend (support)

---

## TASKS.yaml Structure (Template)

```yaml
version: "1.0"
project: "doorstar-softlaunch"
created: "2026-07-08"
updated: "2026-07-08"
config:
  default_model: sonnet
  auto_dispatch: false  # Manual dispatch (high complexity)
  notify_telegram: true
  retry_on_blocked: 3

milestones:
  - id: M1-KEYCLOAK
    name: Keycloak Integration Complete
    status: pending
    blocked_by: []
    tasks:
      - id: TASK-DS-001
        name: Keycloak realm configuration
        terminal: backend
        model: sonnet
        priority: critical
        status: pending
        blocked_by: []
        triggers_on_done: ["TASK-DS-002"]
        estimated_nwt: 30
      - id: TASK-DS-002
        name: Doorstar tenant seed data
        terminal: backend
        status: pending
        blocked_by: ["TASK-DS-001"]
        triggers_on_done: ["TASK-DS-003"]
        estimated_nwt: 60
      # ... (continue for all 6 checkpoints)

  - id: M2-JOINERY
    name: Modules.Joinery Integration Complete
    status: pending
    blocked_by: ["M1-KEYCLOAK"]
    tasks:
      # ...

  - id: M3-B2B
    name: B2B Handshake Complete
    status: pending
    blocked_by: ["M2-JOINERY"]
    tasks:
      # ...

  - id: M4-VPS
    name: VPS Deployment Complete
    status: pending
    blocked_by: ["M3-B2B"]
    tasks:
      # ...

  - id: M5-UAT
    name: UAT Complete
    status: pending
    blocked_by: ["M4-VPS"]
    tasks:
      # ...

  - id: M6-SOFTLAUNCH
    name: Soft Launch Complete
    status: pending
    blocked_by: ["M5-UAT"]
    tasks:
      # ...
```

---

## Critical Unknowns to Resolve

**IMPORTANT:** Investigate current state before finalizing TASKS.yaml:

1. **Modules.Joinery Status:**
   - Is Joinery in EPIC-CUTTING-Q3? (check `spaceos-modules-joinerytech/`)
   - If NOT: estimate implementation NWT (additional 300-600 NWT)

2. **B2B Handshake Status:**
   - Does TenantHandshakeAllowlist exist? (check Kernel or Joinery)
   - If NOT: estimate implementation NWT (additional 180-240 NWT)

3. **VPS Deployment Readiness:**
   - PostgreSQL 16 production instance ready?
   - .NET 8 + Node.js 22 deployment scripts ready?
   - Nginx reverse proxy configured?

4. **Prerequisite Tracks (v4 doc):**
   - Phase 3C+ status?
   - ProdReady Sprint status?
   - Modules.Joinery completion status?

**Action:** Read codebase, check EPIC-CUTTING-Q3 files, update TASKS.yaml dependencies accordingly.

---

## Acceptance Criteria

1. ✅ `spaceos/doorstar/TASKS.yaml` created (6 milestones, ~20-30 tasks)
2. ✅ Task dependencies validated (DAG, no cycles)
3. ✅ Keycloak tenant configuration spec documented
4. ✅ Modules.Joinery integration plan (or implementation estimate if missing)
5. ✅ B2B handshake design (or implementation estimate if missing)
6. ✅ VPS deployment plan (infrastructure checklist)
7. ✅ Critical unknowns resolved (Joinery/B2B/VPS readiness)
8. ✅ Estimated NWT updated (if additional work discovered)
9. ✅ DONE outbox written with planning summary

---

## Next Steps (After Planning DONE)

**Week 2 (2026-07-15):** Backend infrastructure prep (parallel with EHS Week 2)
- Keycloak seed data (if ready)
- Modules.Joinery deployment scaffold (if ready)
- B2B handshake endpoints (if spec complete)

**Week 3+ (2026-07-22):** Full execution (after EHS complete, full capacity available)
- CP-1 → CP-6 sequential execution
- Backend + Frontend + Architect terminals available

---

**Priority:** CRITICAL (first paying customer, Q3 2026 deadline)
**Model:** **OPUS** (complex integration planning, high business impact)
**Estimated Time:** 6-8 hours (180-240 NWT)
**Resource Allocation:** 1-2 CPU cores, ~1GB RAM
**Parallel Task:** MSG-ARCHITECT-069 (EHS OpenAPI spec, Sonnet)

🚀 Generated by Conductor — Aggressive Parallel Execution (VPS Capacity Upgrade)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
