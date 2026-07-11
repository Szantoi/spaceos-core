---
id: MSG-DESIGNER-022
from: root
to: designer
type: task
priority: high
status: READ
injected: 2026-07-03
model: sonnet
epic_id: EPIC-GRAPH-WORKFLOW
checkpoint_id: CP-FLOW-EDITOR
created: 2026-07-01
content_hash: 4717b0e1c46c08ff327d80ba6247ce740f1b20a0c4c0b0a270946b573b755763
---

# Flow Editor UI/UX Design

## Feladat

Tervezd meg a Flow Editor UI/UX-ét az EPIC-GRAPH-WORKFLOW-hoz.

### Kontextus

- **Epic:** EPIC-GRAPH-WORKFLOW
- **Checkpoint:** CP-FLOW-EDITOR

### Követelmények

1. **Node design** — Epic/Task node vizuális megjelenés
   - Státusz színkódolás (done=zöld, active=kék, pending=szürke, blocked=piros)
   - Kompakt és részletes nézet
   - Progress indicator (checkpoints esetén)

2. **Edge design** — Dependency kapcsolatok
   - depends_on: folytonos nyíl
   - parallel_with: szaggatott vonal
   - triggers: villám ikon

3. **Interaction patterns**
   - Drag & drop node mozgatás
   - Zoom & pan
   - Node selection + details panel
   - Context menu (add dependency, change status)

4. **Color palette** — Dark theme kompatibilis
   - Használd a meglévő CSS változókat

### Output

- Wireframe vagy mockup (lehet ASCII/markdown is)
- CSS token javaslatok
- Interaction flow leírás

### Acceptance Criteria

- [ ] Node design specifikáció kész
- [ ] Edge típusok definiálva
- [ ] Interaction patterns dokumentálva

## Acceptance Criteria

- [ ] Node design specifikáció kész
- [ ] Edge típusok definiálva
- [ ] Interaction patterns dokumentálva
