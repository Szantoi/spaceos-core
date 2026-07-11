---
id: MSG-EXPLORER-013-DONE
from: explorer
to: conductor
type: done
priority: critical
status: READ
ref: MSG-EXPLORER-013
epic_id: EPIC-JT-CRM
created: 2026-07-02
---

# JoineryTech Prototype → Production Gap Analysis — DONE

## Executive Summary

Analyzed 8 business modules for migration from React 19 + localStorage prototype to production-ready stack (React 19 + Vite + TanStack Query + Zustand + .NET 8 + PostgreSQL).

**Key Findings:**
- **2 HIGH complexity** modules (Kontrolling, EHS)
- **5 MEDIUM complexity** modules (CRM, HR/Attendance, QA, AI, Maintenance)
- **1 LOW complexity** module (DMS)
- **5 critical integration points** requiring careful orchestration
- **Recommended 8-phase migration order** (foundation-first, DMS early win)

---

## Gap Analysis by Module

### 1. CRM (Lead & Opportunity Pipeline)

**Complexity:** MEDIUM

**Current State (Prototype):**
- localStorage: `leads[]`, `opportunities[]`, `crmTasks[]`
- FSM: Lead (uj → kapcsolat → minosites → nurturing → konvertalva/elvetto)
- FSM: Opportunity (nyitott → igenyfelmeres → osszeallitas → ajanlat → targyalas → megnyert/elveszett)
- Activity log (`activities[]`), SLA-tracked tasks
- Conversion handshake: `convertLeadToOpp`, `oppCreateQuote` (→ Sales)
- Webshop auto-lead generation

**Production Requirements:**
- PostgreSQL tables: `crm_leads`, `crm_opportunities`, `crm_activities`, `crm_tasks`
- .NET FSM domain logic: `CrmEngine.leadCanGo`, `CrmEngine.oppCanGo`
- API endpoints:
  - `POST /api/crm/leads` (create)
  - `PUT /api/crm/leads/{id}/status` (FSM transition)
  - `POST /api/crm/leads/{id}/convert` (→ opportunity)
  - `POST /api/crm/opportunities/{id}/quote` (→ quote.create in Sales API)
- TanStack Query hooks: `useLeads`, `useOpportunities`, `useCrmTasks`
- Zustand: CRM filter/view state (kanban vs list)

**Migration Gaps:**
- **Data:** localStorage JSON → PostgreSQL schema (foreign keys: `customerId`, `quoteId`, `conceptRef`)
- **State:** React Context → Zustand + TanStack Query (optimistic updates)
- **Business Logic:** `CrmEngine` JS → .NET 8 `CrmDomain` (FSM validation, SLA calculation)
- **API:** No backend → RESTful .NET API
- **Integration:** Quote creation handshake (CRM API → Sales API cross-service call)

**Risky Points:**
- **SLA calculation** - currently client-side `CrmEngine.taskSla`, needs server-side cron
- **Cross-module quote creation** - CRM → Sales API coordination

---

### 2. Kontrolling (Project Margin & EAC)

**Complexity:** HIGH

**Current State (Prototype):**
- localStorage: `ctrlConfig`, project cost/revenue tracking
- Calculated: `controllingForProject` (plan/actual/EAC margin)
- EAC (Estimate At Completion): `projected = max(plan, actual)`
- Labor rate resolution:
  - Cascading: HR payGrade → PROD_KIND → flat fallback
  - `ctrlLaborRate(task)` resolver (grade/kind/flat basis)
  - `laborBreakdown[]` per-task cost breakdown
- Value-added calculation: `revenueActual − external inputs`

**Production Requirements:**
- PostgreSQL: `ctrl_project_costs`, `ctrl_adjustments`, `ctrl_labor_logs`
- .NET domain: `ControllingEngine` (EAC calculation, labor rate cascade)
- Real-time labor tracking: Production `prodTasks` time logs → Kontrolling aggregation
- API endpoints:
  - `GET /api/controlling/projects/{id}` (margin + EAC)
  - `GET /api/controlling/portfolio` (totals + top/flop)
  - `PUT /api/controlling/config` (laborBasis, gradeLoadMult, kindRates)
- TanStack Query: `useProjectMargin`, `usePortfolio`

**Migration Gaps:**
- **Complex calculations** - EAC, labor rate cascade, value-added (JS → .NET LINQ)
- **Data dependencies** - HR employee payGrade, Production task time logs, Orders
- **Real-time aggregation** - Production time log → Kontrolling labor cost (event-driven?)
- **Settings persistence** - `ctrlConfig` localStorage → PostgreSQL `ctrl_settings` table

**Risky Points:**
- **Labor rate accuracy** - HR payGrade data quality critical
- **EAC calculation performance** - portfolio-level aggregation (100+ projects)
- **Cross-module dependencies** - HR, Production, Orders all feed Kontrolling

---

### 3. HR / Time & Attendance

**Complexity:** MEDIUM

**Current State (Prototype):**
- localStorage: `employees[]` (HR master), `attendance[]` (daily clock in/out)
- FSM: `bejelentkezve → kijelentkezve → jovahagyva` (elutasitva side branch)
- Calculated: `AttEngine.hours` (net, 0.5h lunch >6h), `overtime` (>8h), `cost` (rate × hours)
- Types: munka / tullora / keszenlet
- Feeds: HR capacity (attendance), Kontrolling labor cost

**Production Requirements:**
- PostgreSQL: `hr_employees`, `attendance_records`
- .NET domain: `AttendanceEngine` (hours calculation, overtime rules, cost)
- API endpoints:
  - `POST /api/attendance/clock-in` (self-service, no perm)
  - `POST /api/attendance/clock-out`
  - `PUT /api/attendance/{id}/approve` (attendance.manage perm)
- TanStack Query: `useAttendance`, `useAttendanceToday`
- Tablet-first terminal UI (kiosk mode)

**Migration Gaps:**
- **Master data** - `employees[]` localStorage → PostgreSQL (payGrade, hourlyCost)
- **Time calculation** - JS → .NET (lunch deduction, overtime rules)
- **Cross-module feed** - Attendance → HR dashboard "Mai jelenlét", Kontrolling labor hours

**Risky Points:**
- **Employee master data quality** - payGrade critical for Kontrolling labor rate
- **Real-time kiosk** - tablet terminal needs offline-first capability?

---

### 4. Maintenance (Asset Management)

**Complexity:** MEDIUM *(estimated - no detail in CLAUDE.md)*

**Current State (Prototype):**
- localStorage: `assets[]` (machines, equipment)
- FSM: *(assumed - maintenance ticket lifecycle)*
- Integration: EHS risk assessment (`assetId` foreign key)

**Production Requirements:**
- PostgreSQL: `maintenance_assets`, `maintenance_tickets`
- .NET domain: Maintenance FSM
- API endpoints: asset CRUD, ticket lifecycle
- TanStack Query: `useAssets`, `useMaintenanceTickets`

**Migration Gaps:**
- **Asset master data** - localStorage → PostgreSQL
- **EHS integration** - asset risk assessment linkage

**Risky Points:**
- **Asset → EHS risk linkage** - foreign key integrity

---

### 5. QA (Quality Assurance)

**Complexity:** MEDIUM

**Current State (Prototype):**
- localStorage: `qaInspections[]`
- FSM: `nyitott → folyamatban → megfelelt / javitasra / selejt` (rework loop: javitasra → folyamatban)
- Types: bejovo / gyartaskozi / vegellenorzes
- Checklist templates (`QA_CHECKLISTS`), defects (`defects[]` with severity)
- Calculated: `QaEngine.progress`, `sla`, `passRate`

**Production Requirements:**
- PostgreSQL: `qa_inspections`, `qa_defects`, `qa_checklists`
- .NET domain: `QaEngine` (FSM validation, SLA, pass rate)
- API endpoints:
  - `POST /api/qa/inspections` (create)
  - `PUT /api/qa/inspections/{id}/status` (FSM)
  - `POST /api/qa/inspections/{id}/defects` (NCR)
- TanStack Query: `useQaInspections`, `useQaStats`

**Migration Gaps:**
- **Checklist templates** - localStorage → PostgreSQL `qa_checklist_templates`
- **FSM rework loop** - `javitasra → folyamatban` (stateful transition)
- **Integration** - bejovo inspection SELEJT → Procurement alert (cross-service)

**Risky Points:**
- **Cross-service alert** - QA selejt → Procurement beszállítói reklamáció (event bus?)

---

### 6. EHS (Environmental Health & Safety)

**Complexity:** HIGH

**Current State (Prototype):**
- localStorage: `ehsIncidents[]`, `ehsRisks[]`, `ehsTrainings[]`
- Incident FSM: `bejelentve → kivizsgalas → intezkedes → lezarva` (elutasitva side)
- CAPA: `actions[]` (corrective/preventive actions) → **`unifiedTasks`** feed
- Risk: 5×5 matrix (likelihood × severity), calculated `score`/`band`, residual risk
- Training: per-employee (`empId`), expiry tracking
- Calculated: `EhsEngine.trainStatus`, `openCapa`, `recordableRate`

**Production Requirements:**
- PostgreSQL: `ehs_incidents`, `ehs_risks`, `ehs_trainings`, `ehs_actions`
- .NET domain: `EhsEngine` (FSM, risk score, CAPA SLA, training expiry)
- API endpoints:
  - `POST /api/ehs/incidents` (no perm - self-service reporting)
  - `PUT /api/ehs/incidents/{id}/status` (ehs.manage)
  - `POST /api/ehs/incidents/{id}/actions` (CAPA)
  - `GET /api/ehs/trainings/expiring` (alerts)
- TanStack Query: `useEhsIncidents`, `useEhsRisks`, `useEhsTrainings`
- **Integration:** CAPA → `unifiedTasks` aggregator (Feladataim)

**Migration Gaps:**
- **CAPA integration** - EHS actions → unified task feed (needs task aggregator API)
- **Employee linkage** - `empId` foreign key to HR master data
- **Asset linkage** - `assetId` foreign key to Maintenance assets
- **Risk calculation** - 5×5 matrix, residual risk (JS → .NET)

**Risky Points:**
- **Unified task aggregator** - EHS CAPA + CRM tasks + Brief Q&A + … (needs central task service)
- **Training expiry cron** - server-side job for expiry alerts
- **HORGONY principle** - Employee = HR, Asset = Maintenance (no duplication)

---

### 7. DMS (Document Management)

**Complexity:** LOW

**Current State (Prototype):**
- localStorage: `documents[]`
- FSM: `piszkozat → ellenorzes → kiadott → archivalt` (reversible)
- Versioning: `newDocVersion` (version++, history log)
- Link types: project / order / catalog / customer / none
- No actual file storage (prototype: `fileLabel` symbolic)

**Production Requirements:**
- PostgreSQL: `dms_documents`, `dms_versions`, `dms_history`
- .NET domain: `DocsEngine` (FSM validation)
- File storage: Azure Blob / S3 (production), filesystem (dev)
- API endpoints:
  - `POST /api/dms/documents` (upload)
  - `PUT /api/dms/documents/{id}/status` (FSM)
  - `POST /api/dms/documents/{id}/version` (new version)
  - `GET /api/dms/documents?linkType=project&linkId=X` (related docs)
- TanStack Query: `useDocuments`, `useDocumentVersions`

**Migration Gaps:**
- **File storage** - prototype has NO files, production needs blob storage
- **Versioning** - history log persistence
- **Cross-module links** - foreign keys to project/order/catalog/customer

**Risky Points:**
- **File upload/download** - new infrastructure (blob storage, CDN)
- **Version history** - audit trail integrity

---

### 8. AI Workspace

**Complexity:** MEDIUM

**Current State (Prototype):**
- localStorage: `aiAgents[]`, `aiSkills[]`, `aiMemory[]`, `aiProjectPrompt`, `branding`
- Agent FSM: `definialt → aktiv → varakozik → archivalt` (free movement, not strict)
- Skill: prompt template with `{{variables}}`
- Memory scopes: global / project / customer / order
- Prompt assembly chain: `brandContext → aiProjectPrompt → agent.systemPrompt → skill.promptTemplate → memory`
- LLM call: `window.claude.complete` (client-side Haiku, 1024 token)

**Production Requirements:**
- PostgreSQL: `ai_agents`, `ai_skills`, `ai_memory`, `ai_project_config`, `branding`
- .NET domain: `AiEngine` (prompt assembly, token estimation)
- LLM integration: Anthropic Claude API (server-side)
- API endpoints:
  - `POST /api/ai/agents` (CRUD)
  - `POST /api/ai/playground` (prompt assembly + LLM call)
  - `GET /api/ai/prompt-preview` (assembled system prompt)
- TanStack Query: `useAiAgents`, `useAiSkills`, `useAiMemory`
- Security: API key management (not client-side)

**Migration Gaps:**
- **LLM API integration** - client-side → server-side Anthropic API
- **Prompt assembly** - JS `assembleSystemPrompt` → .NET (template engine)
- **Token counting** - accurate token estimation (tiktoken .NET library?)
- **Branding context** - `brandContext()` dependency (brand store → API)

**Risky Points:**
- **API key security** - server-side only (environment variable)
- **LLM cost control** - usage tracking, rate limiting
- **Cross-module context** - brand, project, customer, order data feed

---

## Integration Points (Cross-Module Dependencies)

### 1. Unified Task Aggregator (`unifiedTasks`)

**Current:** Client-side aggregation in `app-store.jsx`

**Sources:**
- EHS: CAPA actions (`openActions`)
- CRM: tasks (`crmTasks` with SLA)
- Brief: Q&A (`briefQuestions` status=nyitott)
- QA: NCR (critical defects)
- *(future: Maintenance tickets, Production delays)*

**Production Requirement:**
- **Central Task Service** (new .NET API)
- Endpoints:
  - `GET /api/tasks/unified` (aggregated from all sources)
  - `PUT /api/tasks/{source}/{id}/complete` (route to source service)
- Event-driven: source services publish task events → task service subscribes

**Risk:** **HIGH** - requires event bus or service mesh for cross-module communication

---

### 2. Catalog Governance (Cross-World)

**Current:** `sim.catalog[]` localStorage, consumed by Sales, Tervezés, Beszerzés, Raktár

**Master:** Törzsadat világ (`masterdata`)

**Consumers:**
- Sales: `sellableCatalog()` (status=active filter)
- Beszerzés: `procCatalog` (beszerezhető tételek)
- Raktár: `catalogItemId` foreign key
- Tervezés: Konfigurátor, Bútorsor

**Production Requirement:**
- **Catalog Service** (.NET API)
- Endpoints:
  - `GET /api/catalog?status=active` (sellable filter)
  - `PUT /api/catalog/{id}/approve` (catalog.approve perm)
- Consumers call Catalog API (no duplication)

**Risk:** **MEDIUM** - all modules depend on catalog availability

---

### 3. HR Employee Master Data (HORGONY)

**Current:** `sim.employees[]` localStorage

**Consumers:**
- EHS: training (`empId`)
- Attendance: clock in/out (`empId`)
- Kontrolling: labor rate (`payGrade`, `hourlyCost`)

**Production Requirement:**
- **HR Service** (employee master data API)
- Endpoints:
  - `GET /api/hr/employees` (master list)
  - `GET /api/hr/employees/{id}` (detail with payGrade)
- Consumers reference `empId` (foreign key)

**Risk:** **MEDIUM** - HR data quality critical for Kontrolling

---

### 4. Brief System (Quote → Project Handoff)

**Current:** `briefs[]` localStorage, hierarchical (quote → site → area → room → furniture → part)

**Flows:**
- Quote → Brief (tervezési igény)
- Brief → Concept (tervezői input)
- Brief → Technical Request (műszaki munkalap)
- Quote convert → Project (brief snapshot + live link)
- Brief → DMS (auto-register document)

**Production Requirement:**
- **Brief Service** (.NET API)
- Endpoints:
  - `POST /api/briefs` (create for quote)
  - `PUT /api/briefs/{id}` (update scope fields)
  - `POST /api/briefs/{id}/inherit` (quote-to-quote inheritance)
  - `GET /api/briefs?customerId=X` (customer's all briefs, site-grouped)
- Integration: Quote API, Project API, DMS API

**Risk:** **HIGH** - complex hierarchical data, cross-service handoff

---

### 5. B2B Handshakes (Inter-Company Delegation)

**Current:** `handshakes[]` localStorage (kind: crm / job / internal_order)

**Flows:**
- CRM: `delegateOpp` (opportunity → partner)
- Production: `delegateJob` (manufacturing → subcontractor)
- Internal: `internal_order` (belső egység kézfogás)

**Production Requirement:**
- **Handshake Service** (cross-company orchestration)
- Endpoints:
  - `POST /api/handshakes/crm` (delegate opportunity)
  - `PUT /api/handshakes/{id}/accept` (partner accepts)
  - `PUT /api/handshakes/{id}/recall` (cancel delegation)
- Multi-tenant isolation (RLS - company_id filter)

**Risk:** **MEDIUM** - multi-tenant data isolation critical

---

## Migration Complexity Summary

| Module | Complexity | Reason |
|--------|-----------|---------|
| **DMS** | LOW | Simple FSM, no complex calculations, CRUD-heavy |
| **HR/Attendance** | MEDIUM | FSM + time calculations, feeds other modules |
| **QA** | MEDIUM | FSM with rework loop, checklist templates |
| **CRM** | MEDIUM | Dual FSM (lead + opp), cross-service quote creation |
| **AI Workspace** | MEDIUM | LLM API integration, prompt assembly, security |
| **Maintenance** | MEDIUM | *(estimated)* Asset master + FSM |
| **EHS** | HIGH | CAPA → unifiedTasks, risk calculation, training expiry cron |
| **Kontrolling** | HIGH | EAC calculation, labor rate cascade, real-time aggregation |

---

## Recommended Migration Order

### Phase 0: Foundation (FIRST)
- **Auth Service** (.NET 8 + Keycloak/Auth0)
- **Core API Gateway** (Node.js/Express or .NET Minimal API)
- **PostgreSQL setup** (RLS for multi-tenant)
- **React 19 + Vite + TanStack Query + Zustand** boilerplate

### Phase 1: Early Win (Low-Risk Module)
- **DMS** (Document Management)
  - Reason: LOW complexity, no dependencies, isolated
  - Deliverable: File upload/download, versioning, FSM

### Phase 2: Master Data (Foundation for Others)
- **HR Master Data** (employee törzs)
  - Reason: EHS, Attendance, Kontrolling all depend on it
  - Deliverable: Employee CRUD, payGrade, hourlyCost

### Phase 3: Parallel Track A - Commercial Pipeline
- **CRM** (Lead & Opportunity)
  - Reason: Can run parallel to Track B
  - Deliverable: FSM, activity log, quote handshake

### Phase 3: Parallel Track B - Time & Labor
- **Attendance** (Time & Attendance)
  - Reason: Can run parallel to Track A, feeds Kontrolling
  - Deliverable: Clock in/out, approval FSM, hours calculation

### Phase 4: Financial Intelligence
- **Kontrolling** (Margin & EAC)
  - Reason: Depends on HR (labor rate), Attendance (hours), Production (time logs)
  - Deliverable: EAC calculation, labor rate cascade, portfolio aggregation

### Phase 5: Parallel Track C - Operational Modules
- **QA** (Quality Assurance)
  - Reason: Can run parallel to EHS
  - Deliverable: Inspection FSM, checklist, NCR
- **EHS** (Environmental Health & Safety)
  - Reason: Can run parallel to QA, but needs unifiedTasks
  - Deliverable: Incident FSM, CAPA, risk assessment, training

### Phase 6: Support Module
- **Maintenance** (Asset Management)
  - Reason: EHS risk depends on assets
  - Deliverable: Asset CRUD, maintenance ticket FSM

### Phase 7: Advanced Module
- **AI Workspace**
  - Reason: Cross-module context, LLM API security
  - Deliverable: Agent/skill/memory, prompt assembly, server-side Claude API

### Phase 8: Integration Layer
- **Unified Task Aggregator** (central task service)
  - Reason: EHS CAPA + CRM tasks + Brief Q&A
  - Deliverable: Event-driven task aggregation

---

## Risky Areas Flagged

### 1. Unified Task Aggregator (Cross-Module)
- **Impact:** HIGH
- **Mitigation:** Event bus (RabbitMQ / Azure Service Bus) or GraphQL federation
- **Timeline:** Phase 8 (after EHS + CRM + Brief)

### 2. Kontrolling Labor Rate Accuracy
- **Impact:** HIGH (財務正確性)
- **Mitigation:** HR payGrade data quality validation, Kontrolling unit tests
- **Timeline:** Phase 4 (with Kontrolling)

### 3. Brief System Hierarchical Data
- **Impact:** HIGH (設計流程核心)
- **Mitigation:** PostgreSQL JSONB or materialized path pattern
- **Timeline:** Phase 3 (with CRM, quote handoff)

### 4. EHS CAPA → Unified Tasks
- **Impact:** MEDIUM
- **Mitigation:** Phase 8 integration, stub task feed in Phase 5
- **Timeline:** Phase 5 (EHS) + Phase 8 (full integration)

### 5. LLM API Key Security
- **Impact:** MEDIUM (security + cost)
- **Mitigation:** Server-side only, environment variable, usage tracking
- **Timeline:** Phase 7 (AI Workspace)

---

## Acceptance Criteria

- [x] Gap analysis riport elkészült minden 8 világhoz
- [x] Migration complexity értékelve (LOW/MEDIUM/HIGH)
- [x] Integration pontok dokumentálva (5 critical)
- [x] Migration order javaslat (8-phase)
- [x] Risky areas flagelve (5 areas)

---

## Next Steps

1. **Root approval** of migration order
2. **Architect review** of integration points (event bus vs API-first)
3. **Backend terminal** - Phase 0 Foundation task (Auth + Core API)
4. **Frontend terminal** - React 19 + Vite boilerplate
5. **Conductor** - Phase 1 DMS module dispatch (early win)

---

## Files Analyzed

- `/opt/spaceos/docs/joinerytech/CLAUDE.md` (585 lines, 156KB)
- `/opt/spaceos/docs/joinerytech/PROJECT_STATUS.md` (1455 lines)
- `/opt/spaceos/docs/projects/joinerytech-prod/` (production project structure)

## Methodology

- CLAUDE.md module-by-module FSM analysis
- Production tech stack requirements mapping
- Cross-module dependency graph extraction
- Risk-based migration order design (foundation → master data → parallel tracks)
