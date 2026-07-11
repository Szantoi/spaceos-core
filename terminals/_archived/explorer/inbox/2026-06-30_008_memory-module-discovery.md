---
id: MSG-EXPLORER-008
from: root
to: explorer
type: task
priority: high
status: READ
read: 2026-07-01
model: haiku
created: 2026-06-30
content_hash: af81d1e1fb3486585d3a2a0eeca4189845cfc11a1c79eb5782faa0f81927be44
---

# Memória Fájlok Discovery — Modul Mapping

## Cél

Derítsd fel az összes memória és context fájlt a codebase-ben, és készíts egy mapping táblát.

## Feladat

1. **Keress memória fájlokat:**
   ```bash
   find /opt/spaceos -name "MEMORY.md" -o -name "*_CONTEXT.md" -o -name "*.memory.md" 2>/dev/null
   ```

2. **Mappeld modul szintekre:**
   - Melyik fájl melyik modulhoz tartozik (Kernel, Joinery, Cutting, Portal, stb.)
   - Melyik terminálnak releváns

3. **Készíts discovery reportot:**

## Output

`terminals/explorer/outbox/2026-06-30_XXX_memory-discovery-done.md`

## Elvárt formátum

```markdown
# Memory Discovery Report

## Talált fájlok

| Fájl | Modul | Terminál | Méret |
|------|-------|----------|-------|
| docs/memory/conductor.md | Agent Infra | conductor | 4KB |
| docs/knowledge/context/KERNEL_CONTEXT.md | Kernel | backend | 8KB |
| terminals/backend/MEMORY.md | Backend | backend | 2KB |

## Összefoglaló

- X memória fájl talált
- Y modul lefedve
- Ajánlott indexelési prioritás
```

## Constraint

- Csak discovery, nem módosít fájlokat
- 20 perc időkeret
- DONE outbox amikor kész
