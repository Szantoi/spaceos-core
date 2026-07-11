# JoineryTech Production — Terminál Felelősségek

> **Projekt:** joinerytech-prod
> **Frissítve:** 2026-07-01

## Terminál Szerepkörök

### 1. Conductor — Koordináció

**Felelősség:**
- Epic/task dispatch és prioritizálás
- Checkpoint trigger-ek feldolgozása
- Terminálok közötti szinkronizáció
- Blockerek kezelése és eszkaláció

**Subscription-ök:**
- Minden checkpoint `trigger_to: conductor`
- Task completion events
- Blocker alerts

**Workflow:**
```
TASKS.yaml → Conductor → Terminal inbox → DONE → Conductor review → Next task
```

---

### 2. Architect — Tervezés

**Felelősség:**
- Domain model design (aggregate roots, FSM-ek)
- API contract tervezés
- Cross-module integration design
- Technical decision making

**Assignált task-ok:**
- JT-CRM-001: CRM Domain Model Design
- JT-CTRL-001: Kontrolling Domain Model
- JT-HR-001: HR Domain Model
- JT-MAINT-001: Maintenance Domain Model
- JT-QA-001: QA Domain Model
- JT-EHS-001: EHS Domain Model
- JT-DMS-001: DMS Domain Model
- JT-AI-001: AI Domain Model

**Checkpoint subscription-ök:**
- CP-CRM-BACKEND, CP-CTRL-BACKEND, CP-HR-BACKEND
- CP-MAINT-BACKEND, CP-QA-BACKEND, CP-EHS-BACKEND
- CP-DMS-BACKEND, CP-AI-BACKEND

---

### 3. Explorer — Gap Analízis

**Felelősség:**
- Prototípus → Production gap analysis
- Hiányzó backend endpoint-ok azonosítása
- localStorage → API mapping
- JSX → TypeScript migration notes

**Assignált task-ok:**
- JT-XCUT-001: Prototype → Production Gap Analysis

**Output:**
- Gap analysis dokumentum
- Migration priority list
- Blocker identification

---

### 4. Designer — UX/UI

**Felelősség:**
- UI komponens tervezés
- Design system conformance ellenőrzés
- Mobile-first responsive design
- Accessibility review

**Assignált task-ok:**
- JT-CRM-010: CRM UI Components Design

**Checkpoint subscription-ök:**
- CP-CRM-FRONTEND, CP-CTRL-FRONTEND, CP-HR-FRONTEND
- CP-MAINT-FRONTEND, CP-QA-FRONTEND, CP-EHS-FRONTEND
- CP-DMS-FRONTEND, CP-AI-FRONTEND

---

### 5. Backend — .NET + Node.js Implementáció

**Felelősség:**
- CQRS handler implementáció
- FSM validáció
- API endpoint-ok
- Integration logic
- Test coverage (80%+)

**Assignált task-ok:**
- JT-CRM-002: CRM Backend API Implementation
- JT-CRM-003: CRM → Sales Integration
- JT-CTRL-002: Kontrolling Backend API
- JT-HR-002: HR Backend API
- JT-MAINT-002: Maintenance Backend API
- JT-MAINT-003: Maintenance → Production Integration
- JT-QA-002: QA Backend API
- JT-EHS-002: EHS Backend API
- JT-EHS-003: EHS → HR Integration
- JT-DMS-002: DMS Backend API
- JT-AI-002: AI Backend (Orchestrator)
- JT-AI-003: AI → Business Modules Integration

**Tech stack:**
- .NET 8 (Kernel, Joinery modules)
- Node.js 22 (Orchestrator BFF)
- PostgreSQL + EF Core
- xUnit + Testcontainers

---

### 6. Frontend — React/TypeScript Implementáció

**Felelősség:**
- React komponensek TypeScript-ben
- TanStack Query hooks
- Zustand store slices
- Page routing
- Mobile responsive UI

**Assignált task-ok:**
- JT-CRM-011: CRM React Components
- JT-CRM-012: CRM Pages & Routing
- JT-CTRL-010: Kontrolling Dashboard UI
- JT-HR-010: HR Dashboard & Calendar
- JT-MAINT-010: Maintenance Dashboard
- JT-QA-010: QA Dashboard
- JT-EHS-010: EHS Dashboard
- JT-DMS-010: DMS UI
- JT-AI-010: AI Workspace UI

**Tech stack:**
- React 19 + TypeScript
- Vite
- TanStack Query v5
- Zustand
- Zod validation
- Tailwind CSS

---

### 7. Librarian — Tudás Szintézis

**Felelősség:**
- Epic-enkénti tudás összegyűjtés
- Pattern dokumentáció
- Best practices rögzítése
- Cross-epic learning

**Workflow:**
```
Task DONE → Librarian review → Knowledge extraction → docs/knowledge/ update
```

**Output:**
- Domain-specific patterns
- Implementation lessons learned
- Reusable code snippets

---

### 8. Reviewer — Quality Gate

**Felelősség:**
- Code review (Haiku terminal)
- DONE validation
- Test coverage check
- Security review

**Workflow:**
```
DONE outbox → 2× Haiku review → APPROVE/REJECT → pipeline.sh
```

---

## Subscription Mátrix

| Checkpoint | Root | Conductor | Architect | Designer |
|------------|------|-----------|-----------|----------|
| CP-CRM-BACKEND | | ✓ | ✓ | |
| CP-CRM-FRONTEND | | ✓ | | ✓ |
| CP-CRM-INTEGRATION | ✓ | ✓ | | |
| CP-CTRL-BACKEND | | ✓ | ✓ | |
| CP-CTRL-FRONTEND | | ✓ | | ✓ |
| CP-HR-BACKEND | | ✓ | ✓ | |
| CP-HR-FRONTEND | | ✓ | | ✓ |
| CP-MAINT-BACKEND | | ✓ | ✓ | |
| CP-MAINT-FRONTEND | | ✓ | | ✓ |
| CP-MAINT-PROD-INTEGRATION | ✓ | ✓ | | |
| CP-QA-BACKEND | | ✓ | ✓ | |
| CP-QA-FRONTEND | | ✓ | | ✓ |
| CP-EHS-BACKEND | | ✓ | ✓ | |
| CP-EHS-FRONTEND | | ✓ | | ✓ |
| CP-EHS-HR-INTEGRATION | ✓ | ✓ | | |
| CP-DMS-BACKEND | | ✓ | ✓ | |
| CP-DMS-FRONTEND | | ✓ | | ✓ |
| CP-AI-BACKEND | | ✓ | ✓ | |
| CP-AI-FRONTEND | | ✓ | | ✓ |
| CP-AI-INTEGRATION | ✓ | ✓ | | |

## Dependency Chain

```
JT-XCUT-001 (explorer) ─────────────────────────────────────────┐
                                                                 │
JT-CRM-001 (architect) ──► JT-CRM-002 (backend) ──► JT-CRM-003  │
         │                        │                              │
         └──► JT-CRM-010 (designer) ──► JT-CRM-011 (frontend) ──► JT-CRM-012
                                        
JT-CTRL-001 (architect) ──► JT-CTRL-002 (backend) ──► JT-CTRL-010 (frontend)

JT-HR-001 (architect) ──► JT-HR-002 (backend) ──► JT-HR-010 (frontend)

JT-MAINT-001 (architect) ──► JT-MAINT-002 (backend) ──► JT-MAINT-003
                                    │                         │
                                    └──► JT-MAINT-010 (frontend)

JT-QA-001 (architect) ──► JT-QA-002 (backend) ──► JT-QA-010 (frontend)

JT-EHS-001 (architect) ──► JT-EHS-002 (backend) ──► JT-EHS-003 ──► JT-EHS-010
         │                                                │
         └── depends_on: JT-HR-001 ────────────────────────┘

JT-DMS-001 (architect) ──► JT-DMS-002 (backend) ──► JT-DMS-010 (frontend)

JT-AI-001 (architect) ──► JT-AI-002 (backend) ──► JT-AI-003 ──► JT-AI-010
         │
         └── depends_on: JT-CRM-001, JT-CTRL-001
```

## Első Dispatch Wave

1. **explorer** → JT-XCUT-001 (Gap Analysis) — CRITICAL
2. **architect** → JT-CRM-001 (CRM Domain) — HIGH
3. **architect** → JT-CTRL-001 (Kontrolling Domain) — HIGH
4. **architect** → JT-HR-001 (HR Domain) — HIGH
5. **architect** → JT-MAINT-001 (Maintenance Domain) — HIGH
