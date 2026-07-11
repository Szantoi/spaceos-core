---
id: MSG-FRONTEND-087
from: root
to: frontend
type: task
priority: high
status: READ
injected: 2026-07-01
model: sonnet
epic_id: EPIC-GRAPH-WORKFLOW
checkpoint_id: CP-MERMAID-RENDER
created: 2026-07-01
content_hash: 68af1bd22cf8e2caf5b60c47da396857114aec397626ed0b16b7d7f53e4012b6
---

# Mermaid Epic Diagram Rendering

## Feladat

Implementálj Mermaid diagram renderelést az epic dependency gráfhoz a Projects oldalon.

### Kontextus

- **Epic:** EPIC-GRAPH-WORKFLOW
- **Checkpoint:** CP-MERMAID-RENDER

### Követelmények

1. Használd a `/api/graph/mermaid/epic/EPICS` endpoint-ot
2. Rendereld a Mermaid diagramot a Projects oldalon
3. A diagram mutassa az epic dependency-ket (depends_on, parallel_with)
4. Színkódolás: done=zöld, active=kék, pending=szürke

### Fájlok

- `datahaven-web/public/projects.html` - diagram container
- `datahaven-web/public/js/projects.js` - API hívás + render

### Acceptance Criteria

- [ ] Mermaid diagram renderel a Projects oldalon
- [ ] Epic státuszok színkódolva
- [ ] Dependency nyilak láthatók

## Acceptance Criteria

- [ ] Mermaid diagram renderel a Projects oldalon
- [ ] Epic státuszok színkódolva (done=zöld, active=kék, pending=szürke)
- [ ] Dependency nyilak láthatók
