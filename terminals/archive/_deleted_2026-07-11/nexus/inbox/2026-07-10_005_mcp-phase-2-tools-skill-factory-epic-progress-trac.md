---
id: MSG-NEXUS-005
from: root
to: nexus
type: task
priority: medium
status: PROCESSED
model: sonnet
created: 2026-07-10
content_hash: 9b338551b45d6188ddf5b08d4818bb44315d1d4c0feb7bd65e47b9090bbdaf18
---

# MCP Phase 2 Tools — Skill Factory + Epic Progress Tracker

## Kontextus

Az MCP_TOOLS_CATALOGUE.md Phase 2 Planning szekcióban 5 advanced tool van tervezve. Kezdd el az első kettőt:

## 1. Skill Factory

**Cél:** Automatizált terminál skill generálás.

```typescript
mcp__spaceos-knowledge__create_skill
  terminal: string
  name: string
  trigger_patterns: string[]
  template: string
```

**Use case:** Ha egy terminál ismétlődő workflow-t végez, automatikusan skill-lé alakítható.

**Implementáció:**
- Parse SKILL.md template
- Trigger pattern matching
- Skill registry (`.claude/skills/`)

## 2. Epic Progress Tracker

**Cél:** Real-time epic completion vizualizáció.

```typescript
mcp__spaceos-knowledge__get_epic_progress
  epic_id: string

→ Response:
{
  "epic_id": "EPIC-CUTTING-Q3",
  "progress_percent": 35,
  "tasks_done": 7,
  "tasks_total": 20,
  "blockers": ["MSG-BACKEND-045"],
  "estimated_completion": "2026-07-25"
}
```

**Implementáció:**
- EPICS.yaml parsing
- Task status aggregation
- Burndown estimation

## Érintett Fájlok
- Új: `src/pipeline/skillFactory.ts`
- Új: `src/pipeline/epicProgressTracker.ts`
- `src/mcp.ts` (tool registration)

## Acceptance Criteria
- [ ] Skill Factory: skill generálás működik
- [ ] Epic Progress: % + blockers + estimate
- [ ] MCP tool registration
- [ ] Unit tesztek (>85% coverage)
- [ ] MCP_TOOLS_CATALOGUE.md frissítés

## Acceptance Criteria

- [ ] Skill Factory: skill generálás működik
- [ ] Epic Progress: progress % + blockers + estimate
- [ ] MCP tool registration
- [ ] Unit tesztek (>85% coverage)
- [ ] MCP_TOOLS_CATALOGUE.md frissítés
