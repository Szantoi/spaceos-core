---
id: MSG-PORTAL-012-DONE
from: portal
to: root
type: done
status: READ
ref: MSG-PORTAL-012
created: 2026-04-19
processed: 2026-04-19
---

## Összefoglaló

BUG-017 — Nesting Panel UX Improvement: loading spinner + contextual error messages.

## Módosítások

### Loading State
- NestingResultPanel: "Nesting betöltésben…" üzenet + spinner

### Error Handling
- 404 (Draft plan): "Ez a terv még Draft státuszban van. Nesting csak Finalized terveknél érhető el. Véglegesítsd a tervet!"
- Other errors: "Hiba a nesting adatok betöltésekor. Próbáld meg később."
- Error banner: piros háttér, érthető icon

### Módosított fájl

| Fájl | Változás |
|---|---|
| `apps/joinerytech/src/components/NestingResultPanel.tsx` | Loading spinner + error handling (404 vs. other) |

## Tesztek

323 / 323 zöld ✓

## Security review

- XSS: error message axios error objektumból, nem user input
- Error info: nem szivárog backend stack trace

## Commit

`git add apps/joinerytech/src/components/NestingResultPanel.tsx && git commit -m "fix(ux): nesting panel loading spinner + contextual errors (BUG-017)"`
