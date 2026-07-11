---
id: MSG-EXPLORER-012-DONE
from: explorer
to: root
type: done
priority: high
status: READ
ref: MSG-EXPLORER-012
created: 2026-07-02
---

# JoineryTech UI — Következő Modul Javaslat — DONE

## Elemzett Ötletek (8)

| Ötlet | Komplexitás | Előfeltétel | JoineryTech Relevancia | Prioritás |
|-------|-------------|-------------|------------------------|-----------|
| **KPI Card System** | LOW | Bento Grid (DONE: MSG-FRONTEND-064) | ⭐⭐⭐ FSM status tracking | **P1** |
| **Cost Budget Tracker** | LOW | - | ⭐⭐ Kontrolling EAC widget | **P1** |
| **Kanban Quick Actions** | LOW | - | ⭐⭐⭐ Brief Q&A inline ops | **P2** |
| **Real-time Metrics** | MEDIUM | SSE backend | ⭐ Nice-to-have | P3 |
| **Mermaid Flow Editor** | HIGH | - | ⭐⭐ Epic dependency viz | P3 |
| **Kanban Real-Time** | MEDIUM | SSE backend | ⭐ Nice-to-have | P3 |
| **Mobile Responsive** | MEDIUM | - | ⭐⭐ Műhely tablet | P2 |
| **Dark-First Bento** | MEDIUM | - | ✅ **DONE** (MSG-FRONTEND-064) | - |

---

## TOP 3 Javaslat

### 1. **KPI Card System** — FSM Status Tracking Widget (P1)

**Reasoning:**
- **JoineryTech context:** 8 business modules with FSM-ek (CRM lead/opp, QA inspection, EHS incident, DMS document, Attendance, AI agent)
- **Use case:** Executive dashboard — minden FSM aktuális státusz eloszlása egy pillantással
  - CRM: `uj/kapcsolat/minosites/nurturing/konvertalva` lead distribution
  - QA: `nyitott/folyamatban/megfelelt/selejt` inspection pass rate
  - EHS: `bejelentve/kivizsgalas/intezkedes/lezarva` incident lifecycle
- **Builds on:** Bento Grid layout (MSG-FRONTEND-064 DONE)
- **Implementation:** Reusable `<FSMStatusCard />` component
  - Props: `{world, fsmType, data, statusMeta}`
  - Renders: Status pills, count badges, transition arrows
- **Horizontal value:** Minden FSM-world használhatja (CRM, QA, EHS, DMS, AI, Attendance)
- **Effort:** 2-3 days (sonnet)

**Backend API:**
```
GET /api/crm/leads/status-breakdown
GET /api/qa/inspections/status-breakdown
GET /api/ehs/incidents/status-breakdown
```

### 2. **Cost Budget Tracker Widget** — Kontrolling EAC Monitor (P1)

**Reasoning:**
- **JoineryTech context:** Kontrolling module EAC (Estimate At Completion) calculation
- **Use case:** Executive dashboard — real-time project margin tracking
  - Today's labor cost (Attendance hours × labor rate)
  - EAC margin warning (várható fedezet < 30%)
  - Top 3 slipping projects (cost overrun)
- **Quick win:** Small effort, high business value
- **Data source:** Kontrolling portfolio API (existing calculation engine)
- **Effort:** 1-2 days (haiku)

**Backend API:**
```
GET /api/controlling/portfolio (exists)
GET /api/controlling/eac-alerts
```

### 3. **Kanban Quick Actions** — Brief Q&A Inline Operations (P2)

**Reasoning:**
- **JoineryTech context:** Brief system Q&A workflow (tervezési kérdések)
  - Status: `nyitott → megvalaszolt → lezart`
  - Current UX: Modal-heavy (slow workflow)
- **Use case:** Brief Q&A board — inline status change + quick reply
  - Hover card → [✅ Answer] [🔒 Close] [📝 Edit] buttons
  - Inline textarea for quick answer submission
- **Depends on:** Basic Kanban board (exists)
- **Horizontal value:** Extendable to CRM tasks, EHS CAPA, QA defects
- **Effort:** 2 days (sonnet)

**Backend API:**
```
PUT /api/briefs/{id}/questions/{idx}/status
PUT /api/briefs/{id}/questions/{idx}/answer
```

---

## Dependency Gráf

```
Bento Grid (DONE) ──┬──> KPI Card System (FSM status widgets)
                     │
                     └──> Cost Budget Tracker (Kontrolling EAC)

Brief Q&A Board ──> Kanban Quick Actions (inline ops)

SSE Infrastructure ──> Real-time Metrics (later)

Mobile Responsive ──> Műhely Tablet UI (Phase 2)

Mermaid Flow ──> Epic Dependency Viz (Q3)
```

---

## Következő Modul Egyetlen Javaslat

### **KPI Card System** (FSM Status Widgets)

**Indoklás (egyetlen mondatban):**
A JoineryTech 8 business moduljának FSM-alapú státusz-kezelését horizontálisan támogató, Bento Grid-re építhető, újrafelhasználható KPI komponens-könyvtár, amely az executive dashboard legjobb ROI-jú következő lépése.

**Implementation Plan:**
1. **Phase 1:** Generic `<FSMStatusCard />` component
2. **Phase 2:** CRM lead/opp status cards (proof of concept)
3. **Phase 3:** QA, EHS, DMS, Attendance integration (horizontal expansion)

---

## Blokkolt Feature-ök

- **Real-time Metrics Dashboard** — SSE backend missing (defer to Phase 2)
- **Mermaid Flow Editor** — LARGE effort (opus task, Q3 candidate)
- **Kanban Real-Time Feedback** — SSE infrastructure blocker

---

## Acceptance Criteria

- [x] 8 UI ötlet elemezve
- [x] Dependency-k azonosítva minden ötlethez
- [x] TOP 3 prioritás javaslat (KPI Card, Cost Tracker, Quick Actions)
- [x] Következő modul egyetlen javaslat (KPI Card System)

---

## Files Analyzed

Same 8 planning ideas as MSG-EXPLORER-001, but with **JoineryTech business context** overlay:
- CRM FSM-ek (lead, opportunity)
- Kontrolling EAC (fedezet-számítás)
- Brief Q&A workflow
- 8 business modules FSM status tracking

## Methodology

- JoineryTech CLAUDE.md business module mapping
- FSM lifecycle identification (CRM, QA, EHS, DMS, AI, Attendance)
- Horizontal component reusability analysis
- Quick win vs foundation trade-off (JoineryTech ROI context)
