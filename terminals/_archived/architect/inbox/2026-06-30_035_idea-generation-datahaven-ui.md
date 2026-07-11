---
id: MSG-ARCHITECT-035
from: root
to: architect
type: task
priority: high
status: READ
model: haiku
created: 2026-06-30
content_hash: 6443245e8a9543035f9732d6053908cdaa01096e67694944507148bf1869f96c
---

# Ötlet Generálás — Datahaven UI Fejlesztések

## Cél

Generálj 3-5 konkrét fejlesztési ötletet a Datahaven Dashboard UI-hoz a következő fázisokra.

## Kontextus

A Datahaven Dashboard 4 oldalból áll:
- `/` — Dashboard (terminál monitoring)
- `/kanban.html` — Dual-track Kanban
- `/planning.html` — Planning pipeline
- `/projects.html` — Gantt timeline

**Jelenlegi állapot:**
- Focus Area Panel implementálva (Planning page)
- CSS Design System egységesítve
- Domain-focus API kész

## Ötlet Kategóriák

### 1. Flow Editor (Következő fázis)
- Mermaid graph vizualizáció
- Epic dependency editing
- Drag & drop workflow

### 2. Dashboard Fejlesztések
- Real-time metrics
- Alert panel
- Cost tracking widget

### 3. Kanban Javítások
- Swimlane szűrők
- Card részletek modal
- Bulk operations

### 4. Mobile UX
- Responsive improvements
- Touch gestures
- Offline support

## Output

Készíts ötlet fájlokat:
`/opt/spaceos/docs/planning/ideas/2026-06-30_001_<slug>.md`

**Formátum:**
```yaml
---
id: IDEA-2026-06-30-001
title: "Ötlet címe"
category: ui|ux|feature|infra
priority: high|medium|low
effort: small|medium|large
domain: manufacturing
created: 2026-06-30
---

## Összefoglaló
1-2 mondat

## Probléma
Mit old meg?

## Megoldás
Hogyan?

## Acceptance Criteria
- [ ] Kritérium 1
- [ ] Kritérium 2
```

## Constraint

- 3-5 ötlet
- Fókusz: UI/UX javítások
- 30 perc időkeret
