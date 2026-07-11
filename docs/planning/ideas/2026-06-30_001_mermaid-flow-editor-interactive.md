---
id: IDEA-2026-06-30-001
title: "Interactive Mermaid Flow Editor for Epic Dependencies"
category: feature
priority: high
effort: large
domain: manufacturing
created: 2026-06-30
---

## Összefoglaló

Interactive workflow editor az EPICS.yaml vizualizációjához és szerkesztéséhez, Mermaid alapú dependency gráffal.

## Probléma

Jelenleg az epic dependency-k csak YAML fájlban szerkeszthetők, nincs vizuális feedback a dependency láncokról, parallel track-ekről és critical path-ról.

## Megoldás

**Komponensek:**
- Mermaid.js graph rendering (Planning page Workflow tab)
- Interactive node clicking → Epic details modal
- Drag-and-drop dependency editing
- Status color coding (done=zöld, active=sárga, pending=szürke, blocked=piros)
- Critical path highlighting (ADR-041 graph API alapján)

**API integráció:**
```
GET /api/graph/mermaid/epic/EPICS
PUT /api/graph/epic/{id}/status
POST /api/graph/epic/{id}/dependency
DELETE /api/graph/epic/{id}/dependency/{targetId}
```

**UX Flow:**
1. User opens Planning page → Workflow tab
2. Mermaid graph loads from API
3. Click epic node → Details modal (milestones, tasks, status)
4. Drag edge to create dependency
5. Auto-save + graph re-render

## Acceptance Criteria

- [ ] Mermaid graph renderelése a Workflow tab-ban
- [ ] Epic node click → Details modal megjelenik
- [ ] Status színkódok működnek (4 állapot)
- [ ] Critical path pirossal highlighted
- [ ] Dependency drag-drop működik (basic)
- [ ] Auto-save + re-render cycle < 500ms
- [ ] Mobile responsive (touch support basic)
