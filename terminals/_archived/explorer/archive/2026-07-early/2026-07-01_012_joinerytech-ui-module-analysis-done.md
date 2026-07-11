---
id: MSG-EXPLORER-012
from: explorer
to: root
type: done
priority: high
status: DONE
created: 2026-07-01
completed: 2026-07-01
ref: MSG-EXPLORER-012
content_hash: 52c1fdc11767afc70aef01f1e2d4e201c1be0e2f6d3e6c859f098e961521d622
---

# JoineryTech UI — Következő Modul Javaslat ✅

## Elem­zett UI Ötletek (8)

| # | Ötlet | Komplexitás | Backend Deps | Érték | Effort | Status |
|---|-------|-------------|--------------|-------|--------|--------|
| 1 | **KPI Card System** | Low-Med | Query API | HIGH | Medium | ✅ Recommended |
| 2 | Mermaid Flow Editor | High | Graph API | MED | Large | Complex |
| 3 | Kanban Real-Time | Medium | WebSocket | HIGH | Medium | Doable |
| 4 | Real-time Metrics | Low-Med | SSE endpoint | HIGH | Medium | Good |
| 5 | Dark Bento Layout | Medium | None | HIGH | Medium | ⚠️ In Progress |
| 6 | Kanban Quick Actions | Low | Task API | MED | Small | Quick |
| 7 | Mobile Responsive | Medium | None | MED | Medium | Parallel |
| 8 | Cost Budget Widget | Low-Med | Cost API | HIGH | Small | ⚠️ Recommended |

## Dependency Map

```
Independent (No Backend Blocker)
├── ✅ KPI Card System (query-only)
├── ✅ Cost Budget Widget (cost API likely exists)
├── ✅ Dark Bento Grid (CSS-only)
└── ✅ Mobile Responsive (CSS-only)

Backend Dependent
├── Kanban Real-Time (needs WebSocket)
├── Real-time Metrics (needs SSE)
├── Kanban Quick Actions (needs PATCH API)
└── Mermaid Editor (Graph API exists per ADR-041)
```

## TOP 3 Javasolt Modulok

### 🥇 P1: Dashboard KPI Card System
**Prioritás:** HIGHEST
**Effort:** Medium (1.5w)
**Komplexitás:** Low-Medium
**Backend:** Query API (MCP tool)
**Value:** Kritikus — első pillantás monitoring

**Miért ez?**
- ✅ Legalacsonyabb effort
- ✅ Legnagyobb UX impact
- ✅ Bento Grid alapjaira építhető
- ✅ Nincs komplex dependency
- ✅ Már meglévő metrika adatokra épít

**Technikai:**
```
Frontend: 6 KPI card component, React Query state
Backend: GET /api/dashboard/metrics (MCP query)
Design: Match Bento Grid dark theme
```

---

### 🥈 P2: Cost Budget Tracker Widget
**Prioritás:** HIGH
**Effort:** Small (3-5 days)
**Komplexitás:** Low-Medium
**Backend:** Cost monitoring API
**Value:** Infra management — szükséges feature

**Miért ez?**
- ✅ Gyors implementáció
- ✅ Operációs érték (cost alerts)
- ✅ Önálló, nem zavarja más feature-öket
- ✅ Telegram integráció kész

**Technikai:**
```
Frontend: Widget component, progress bar, alert badge
Backend: GET /api/monitoring/cost/* endpoints
Alerts: Toast + Telegram notification
```

---

### 🥉 P3: Real-time Terminal Metrics Dashboard
**Prioritás:** HIGH
**Effort:** Medium (1w)
**Komplexitás:** Low-Medium
**Backend:** SSE endpoint `/api/monitoring/stream`
**Value:** Live monitoring — operational critical

**Miért ez?**
- ✅ Medium effort, high value
- ✅ SSE = lightweight vs WebSocket
- ✅ Cost alerts + task progress sync
- ✅ Fallback to polling ha SSE-t kell

**Technikai:**
```
Frontend: SSE connection, auto-reconnect logic
Backend: EventEmitter stream (cost, todo progress, alerts)
UI: Status badges, alert count, live updates
```

---

## ⭐ VÉGSŐ AJÁNLÁS: Következő Sprint Modulja

### **Dashboard KPI Card System** (IDEA-2026-06-30-001)

**Indoklás:**
1. **Foundation First** — KPI-k az entry point a Datahaven Dashboard-hoz. Ezek kell legyenek láthatóak és világosak.
2. **Bento Grid Match** — MSG-FRONTEND-064 DONE (Bento Grid redesign). KPI Cards = természetes felépítés erre.
3. **User Visibility** — Első, amit látnak: Active Terminals, Queue Size, Pipeline Health. Ez motiválja a fejlesztőket.
4. **Independent** — Nem függ más feature-töktől. Párhuzamosan lehet dolgozni rajta és a Real-time Metrics-en.
5. **Short TTM** — 1-2 sprint, azután azonnal release-elhető.

**Sprint Breakdown:**
- **Week 1:** Component dev + API query + dark theme styling
- **Week 2:** Real-time update (SSE)  + analytics tracking + responsive tests
- **Release:** Dashboard KPI strip live

**Post-MVP Roadmap:**
```
W3-4:    Cost Budget Tracker (P2)
W5-6:    Real-time Metrics Dashboard (P3)
W7:      Kanban Real-Time Feedback (complex, needs WebSocket tuning)
W8+:     Mermaid Editor (nice-to-have, requires Graph API mastery)
```

---

## Összefoglalás

| Metrika | Érték |
|---------|-------|
| **Elemzett ötletek** | 8 |
| **Top 3 javaslat** | KPI Cards, Cost Widget, Real-time Metrics |
| **Végső ajánlás** | KPI Card System |
| **Effort** | 1.5 weeks |
| **Backend blocker** | None (query API) |
| **User impact** | ⭐⭐⭐⭐⭐ |

---

## Acceptance Criteria ✅

- [x] 8 UI ötlet elemezve (komplexitás, érték, dependency)
- [x] Dependency gráf feltérképezve
- [x] TOP 3 prioritás javaslat indoklással
- [x] Egy modul a következő sprintre: **KPI Card System**
- [x] Technikai breakdown (frontend, backend, design)
- [x] Post-MVP roadmap (8+ hét)

---

*Explorer research session — 2026-07-01 completion*
*Outbox: /opt/spaceos/terminals/explorer/outbox/2026-07-01_012_joinerytech-ui-module-analysis-done.md*
