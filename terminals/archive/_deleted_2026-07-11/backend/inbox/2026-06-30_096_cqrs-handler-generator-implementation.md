---
id: MSG-BACKEND-096
from: conductor
to: backend
type: task
priority: high
status: READ
model: haiku
ref: MSG-CONDUCTOR-054
created: 2026-06-30
content_hash: 2163d0c499a27a0893e4a3a6bb32418d37a14914de62a335bb943961e4d86772
---

# CQRS Handler Generator Implementáció

## Context

ADR-051 (`/opt/spaceos/docs/architecture/decisions/ADR-051-cqrs-handler-generator.md`) specifikálja a CQRS handler generator toolchain-t.

## Task

Implementáld a handler generátort:

1. **`/opt/spaceos/scripts/codegen/generate-handler.sh`**
   - Template-alapú .NET handler generálás
   - Command → CommandHandler → Test fájlok
   - ADR-051 struktúra szerint

2. **MCP Tool Wrapper**
   - `spaceos-nexus/knowledge-service/src/codegen/codegenEngine.ts`
   - `generate_endpoint` MCP tool integráció

3. **SpaceOS CLI Integráció**
   - `claude code generate:handler` parancs
   - Template selector (Create/Update/Delete/Query)

## Acceptance Criteria

- [ ] `generate-handler.sh` működik standalone
- [ ] MCP tool wrapper elkészült
- [ ] Teszt generálás működik
- [ ] Dokumentáció frissítve (CODE_GENERATOR_CATALOGUE.md)
- [ ] Build sikeres

## Related Files

- `/opt/spaceos/docs/architecture/decisions/ADR-051-cqrs-handler-generator.md`
- `/opt/spaceos/docs/knowledge/patterns/CODE_GENERATOR_CATALOGUE.md`
- `/opt/spaceos/scripts/codegen/` (mappa)

## Notes

Párhuzamosan fut az Architect FSM Subscription design-jával - nincs dependency közöttük.
