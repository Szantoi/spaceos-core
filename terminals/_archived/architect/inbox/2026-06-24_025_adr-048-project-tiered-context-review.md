---
id: MSG-ARCHITECT-025
from: root
to: architect
type: task
priority: high
status: READ
model: opus
ref: ADR-048
created: 2026-06-24
processed: 2026-06-24
completed: 2026-06-24
content_hash: 7480886fa9dfcfb519cc607d34633244fdeec8772618a4d5e6c46c94ca7ac55f
---

# ADR-048 Review: Project-Level Tiered Context System

## Kontextus

A `get_project_context` MCP tool jelenleg ~10.8k tokent ad vissza, ami pazarló. Készítettem egy ADR-t a tiered memory rendszer projekt-szintű kiterjesztéséről.

## Feladat

Kérlek review-zd az **ADR-048**-at:

**Fájl:** `docs/architecture/decisions/ADR-048-project-tiered-context.md`

## Review szempontok

1. **Architektúra illeszkedés**
   - Illeszkedik-e a meglévő tiered memory rendszerhez (ADR-046)?
   - Konzisztens-e a memoryStore.ts implementációval?

2. **Tier policy**
   - Jók-e a TTL értékek (hot: 48h, warm: 14d, cold: 365d)?
   - A decay rate-ek megfelelőek?

3. **Tömörítési stratégia**
   - Haiku-alapú summarization megfelelő?
   - Cache invalidation logika helyes?

4. **MCP Tool design**
   - `get_project_summary` interfész jó?
   - Kell-e más paraméter?

5. **Pipeline integráció**
   - DONE → hot tier frissítés logikus?
   - ADR merge → cold tier helyes?

## Elvárt output

DONE outbox a következő struktúrában:

```markdown
## ADR-048 Review

### Verdict: APPROVE / APPROVE_WITH_CHANGES / REJECT

### Erősségek
- ...

### Változtatási javaslatok
- ...

### Kérdések/tisztázandók
- ...
```

## Határidő

Ma (2026-06-24) — a Librarian feladattal párhuzamosan fut.
