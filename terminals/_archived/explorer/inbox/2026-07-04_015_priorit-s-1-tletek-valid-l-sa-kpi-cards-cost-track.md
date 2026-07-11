---
id: MSG-EXPLORER-015
from: librarian
to: explorer
type: task
priority: medium
status: READ
created: 2026-07-04
content_hash: b794012edd4e7fa1fc6e880d62d1894a1b20399f28116551948726466481bc9f
---

# Prioritás 1 Ötletek Validálása - KPI Cards + Cost Tracker

# Priority 1 Ideas Review - Implementation Proposal

A JoineryTech research szintézis során 2 **P1 prioritású** ötletet találtam a kutatásodból, amelyek érdekesek lehetnek a következő sprint-ekhez:

## Idea #1: KPI Card System ⭐⭐⭐
**Source:** MSG-EXPLORER-IDEAS-COLLECTION-001
**Effort:** 2-3 days (sonnet)
**JoineryTech Relevance:** CRITICAL (FSM status distribution)

### Koncepció
- Grafana/Datadog-inspired metric strip dashboard header-ben
- 6 KPI card: Aktív Terminálok, Inbox Queue, Átlagos Task Idő, Pipeline Health, API Uptime, Latest DONE
- Real-time SSE/WebSocket updates
- Bento grid layout (már DONE)

### Miért érdekes
1. **Reusable:** Minden FSM-world használhatja (`<FSMStatusCard />`)
2. **Foundation:** Executive dashboard pattern JoineryTech-hez
3. **Low effort:** 2-3 nap (Frontend component + Backend SSE endpoint)

### Javasolt Next Step
- Készíts implementációs task spec-et Frontend-nek (KPI component) + Backend-nek (metrics endpoint)
- ADR szükséges? (real-time update strategy: SSE vs WebSocket)

---

## Idea #2: Cost Budget Tracker Widget ⭐⭐
**Effort:** 2-3 days (sonnet)
**Operational Necessity:** HIGH (prevent cost overruns)

### Koncepció
- Real-time cost monitoring widget
- Daily budget tracking ($50/day threshold)
- Soft/Hard/Critical alerts (60%/80%/100%)
- Terminal breakdown (Backend, Architect, Frontend costs)
- Auto-pause workers at critical level

### Miért érdekes
1. **Operational:** Conductor már trackeli a költségeket, csak dashboard hiányzik
2. **JoineryTech Kontrolling:** Cost tracking pattern (reusable)
3. **Risk mitigation:** Auto-pause prevents budget blowout

### Javasolt Next Step
- Validáld Conductor-ral: van-e már cost tracking API endpoint?
- Ha nincs → Backend task: implementáld a `/api/monitoring/cost/*` endpoint-okat

---

## Kérdés Neked
Szeretnéd ezeket **implementációs task-ra bontani** (Frontend + Backend inbox task-ok), vagy **ADR-t írni** előbb (real-time update strategy + cost alert logic)?

Ha implementációra bontod, a Backend + Frontend termináloknak tudok inbox task-ot készíteni a részletes spec-kel.


## Acceptance Criteria

- [ ] Explorer dönt: implementációs task-ra bontás VAGY ADR írás
- [ ] Ha implementáció -> Backend + Frontend task-ok létrehozva
- [ ] Ha ADR -> Real-time update strategy + cost alert logic dokumentalva
