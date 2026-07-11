---
id: MSG-LIBRARIAN-012
from: root
to: librarian
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-30
content_hash: 7026cdf6ee08ffb09411443446c33a045e43f1daed72aa18dbae5dcde561bb5d
---

# ADR-049 Phase 3 Domain Memory Structure + CLAUDE.md Update

## Feladat

Az ADR-049 Phase 3 (Parallel Workers) befejezésével a termináloknak szükségük van domain memory fájlokra és frissített CLAUDE.md dokumentációra.

### 1. Domain Memory Struktúra Létrehozása

Hozd létre a következő domain memory fájlokat minden terminálnak:

```
terminals/<terminal>/knowledge/
  ├── domain.memory.md      ← dinamikus session kontextus
  ├── patterns.memory.md    ← ismétlődő minták, best practices
  └── decisions.memory.md   ← architekturális döntések cache
```

**Tartalom:**
- `domain.memory.md` — Session-specifikus kontextus (hot memory, 48h TTL)
- `patterns.memory.md` — Visszatérő kódminták, workflow-ok (warm memory, 14d TTL)
- `decisions.memory.md` — ADR-ek és döntések összefoglalója (cold memory, 365d TTL)

### 2. CLAUDE.md Frissítés Minden Terminálnál

Add hozzá a Parallel Workers szekciót minden terminál CLAUDE.md-jéhez:

```markdown
## Parallel Workers (ADR-049 Phase 3)

### Mikor használd
- Független feladatok párhuzamos végrehajtása
- Best-of-N prototyping (több megoldás közül a legjobb választása)
- CPU-intenzív feladatok párhuzamosítása

### MCP Tools
- `spawn_parallel_workers` — Több worker indítása dependency kezeléssel
- `spawn_raw_workers` — N-to-1 selection (2-5 worker, automatic best selection)
- `get_worker_status` — Worker status + cost tracking

### Cost Limits
- Soft limit: $3/hour (warning)
- Hard limit: $5/hour (alert)
- Critical: $10/hour (auto-kill)
- Max parallel: 5 worker/terminal

### Használat
\`\`\`
# Parallel tasks with dependencies
mcp spawn_parallel_workers terminal=backend tasks=[
  {id: "api", prompt: "Create API endpoint"},
  {id: "test", prompt: "Write tests", depends_on: ["api"]}
]

# Best-of-N selection
mcp spawn_raw_workers terminal=backend task="Optimize function" count=3 criteria="fastest"
\`\`\`
```

### 3. Knowledge Index Frissítés

Frissítsd a `docs/knowledge/INDEX.md`-t az új domain memory struktúrával.

## Acceptance Criteria

- [ ] terminals/*/knowledge/ mappák létrehozva minden terminálnál
- [ ] domain.memory.md, patterns.memory.md, decisions.memory.md template-ek elkészítve
- [ ] Minden terminál CLAUDE.md-je tartalmazza a Parallel Workers szekciót
- [ ] docs/knowledge/INDEX.md frissítve az új struktúrával

---

## Notes (by root)
*Added: 2026-06-30T10:56:58.277Z*

## Bővített Scope (Root kiegészítés)

A memory struktúra legyen **projekt-specifikus** is:

### 1. Terminál Memory (eredeti)
```
terminals/<terminal>/knowledge/
  ├── domain.memory.md      ← hot (48h)
  ├── patterns.memory.md    ← warm (14d)
  └── decisions.memory.md   ← cold (365d)
```

### 2. Projekt-specifikus Memory (ÚJ)
```
terminals/<terminal>/knowledge/projects/
  ├── cutting.memory.md     ← Cutting modul kontextus
  ├── joinery.memory.md     ← Joinery modul kontextus
  ├── datahaven.memory.md   ← Datahaven infra kontextus
  └── ...
```

### 3. Több TODO.md támogatás (ÚJ)
```
terminals/<terminal>/
  ├── TODO.md               ← Általános TODO
  └── todos/
      ├── cutting.todo.md   ← Cutting projekt TODO
      ├── joinery.todo.md   ← Joinery projekt TODO
      └── ...
```

### CLAUDE.md Frissítés
Add hozzá a multi-memory és multi-TODO kezelést is a terminál CLAUDE.md-khez.

— Sárkány
