---
id: MSG-NEXUS-024
from: root
to: nexus
type: task
priority: medium
status: UNREAD
model: sonnet
created: 2026-07-11
content_hash: 24bf866786513d3e0a38de19a3708d34e985340d9c34390db5ed9b677703a33e
---

# MCP-based UI Review Loop (replace bash script)

## Probléma

A `watch-ui-review.sh` bash szkript `type: done` stringet grep-el a fájlokból. Ez törékeny:
- Case-sensitive (`DONE` vs `done`)
- Formátum-függő (`status:` vs `type:`)
- Nincs validáció

## Cél

Helyettesítsd a bash szkriptet MCP-alapú megoldással:

1. **Új MCP tool:** `watch_frontend_done` vagy `subscribe_to_outbox`
   - Figyeli a Frontend outbox-ot
   - Validált enum-okkal dolgozik (`type: 'done' | 'blocked'`)
   - Automatikusan triggerel Designer task-ot

2. **Vagy:** Nightwatch integráció
   - `watchDone.ts` bővítése UI review triggerrel
   - Ha Frontend DONE → Designer inbox létrehozás MCP-n keresztül

## Előnyök

- Egységes típusok (TypeScript enum)
- Nincs grep/regex törés
- Audit trail
- Könnyebb tesztelés

## Referencia

- Jelenlegi szkript: `/opt/spaceos/scripts/watch-ui-review.sh`
- WatchDone: `spaceos-nexus/knowledge-service/src/pipeline/watchDone.ts`
- TMB pattern: `mcp__spaceos-knowledge__tmb_*` tools

## Acceptance Criteria

- [ ] MCP-based outbox watcher implementálva
- [ ] Frontend DONE → Designer inbox automatikus
- [ ] Bash szkript deprecated vagy eltávolítva
- [ ] Típusbiztos enum-ok használata
