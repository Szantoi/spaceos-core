---
id: MSG-FE2-005
from: root
to: fe2
type: task
priority: medium
status: READ
model: sonnet
created: 2026-06-16
---

# FE2 — Új skill + DESIGN_MEMORY elvek

## Változások a fejlesztési környezetben

A VPS-en elérhetővé vált a `/spaceos-frontend-arch-planner` skill.
Ezt használd minden UX tervdokumentum vagy komponens architektúra döntésnél.

## DESIGN_MEMORY — amit kötelező követni

```
docs/knowledge/architecture/DESIGN_MEMORY.md
```

Az alábbi elvek **zároltak** — a frontendet ezekre kell tervezni:

### Data → Rules → Geometry (5 Golden Rule #1)
- A frontend **csak renderel** — nem számol, nem validál üzleti logikát
- Minden adat a backendről jön (API), a UI csak megjelenít
- Geometriai számítás (pl. nesting, vágási terv) a C# Driver végzi

### IParametricProduct interfész (ADR-024)
- A Kernel nem tudja mi egy „ajtó" vagy „szekrény" — csak `IParametricProduct`-ot lát
- A frontend ne vegyen fel Joinery-specifikus logikát a közös komponensekbe

### Need-to-Know RBAC (5 Golden Rule #4)
- Megrendelő ne lássa a gyártó belső anyaglistáját
- Minden view-t a bejelentkezett felhasználó role-ja határoz meg

## Ezt NEM kell megismételni

Ez tájékoztató üzenet — nem kell DONE outbox, nem kell implementálni semmit.
A következő fejlesztési feladatoknál tartsd szem előtt a fenti elveket.
