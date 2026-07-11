---
id: MSG-NEXUS-001
from: root
to: nexus
type: task
priority: high
status: READ
model: sonnet
created: 2026-07-10
content_hash: f5aa629e4cd6fb1687afc0e52a4cf67a6c9f73db0182b05b09c420ec855fc1f3
---

# Nexus Terminal Onboarding - Session Starter teszt

# Nexus Terminál Indítás

## Cél
Ellenőrizd, hogy a Nexus terminál megfelelően be van konfigurálva és működik.

## Feladatok

1. **CLAUDE.md áttekintés**
   - Olvasd el a `/opt/spaceos/terminals/nexus/CLAUDE.md` fájlt
   - Ellenőrizd, hogy a szerepkör és felelősségek tiszták

2. **Knowledge-service állapot**
   - Ellenőrizd, hogy a service fut: `curl -s http://localhost:3456/health`
   - Listázd az MCP toolokat: `mcp__spaceos-knowledge__get_capabilities`

3. **Terminál integráció teszt**
   - Küldj egy üzenetet a Conductor-nak mcp-n keresztül
   - Ellenőrizd a mailbox routing működését

4. **MEMORY.md inicializálás**
   - Ha van már domain knowledge, olvasd be
   - Ha nincs, hozd létre az első bejegyzést

## Acceptance Criteria
- [ ] CLAUDE.md olvasva és megértve
- [ ] Knowledge-service elérhetőség ellenőrizve
- [ ] MCP kommunikáció működik
- [ ] MEMORY.md inicializálva

## Acceptance Criteria

- [ ] CLAUDE.md olvasva és megértve
- [ ] Knowledge-service elérhetőség ellenőrizve
- [ ] MCP kommunikáció működik
- [ ] MEMORY.md inicializálva
