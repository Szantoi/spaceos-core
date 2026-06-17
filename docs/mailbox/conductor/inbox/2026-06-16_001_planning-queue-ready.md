---
id: MSG-COND-001
from: root
to: conductor
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-16
---

# Conductor — Tervezési queue feldolgozás

A planning pipeline 1 kész konsenzus tervet pufferelt.
A te feladatod ezt feldolgozni és termináloknak kiadni.

## Queue tartalom

```
2026-06-16_2245_consensus.md
```

**Fájl:** `docs/planning/queue/2026-06-16_2245_consensus.md`

## Konsenzus összefoglaló

A kiválasztott TOP 3 fejlesztés a Doorstar napi workflow törött pontjait oldja meg:

| Prioritás | Funkció | FE | BE |
|---|---|---|---|
| TOP 1 | Design → Cutting Plan Workflow | 2-3 nap | 0 |
| TOP 2 | Nesting Vizualizáció (SVG) | 3-4 nap | 0 |
| TOP 3 | Machine Scheduling UI (drag-drop) | 4-5 nap | 1 nap |

**Össz:** ~10-12 nap FE, 1 nap BE

## Teendők

1. Olvasd el a queue-ban lévő konsenzust: `docs/planning/queue/2026-06-16_2245_consensus.md`
2. Használd a `spaceos-arch-planner` skill-t a v1→v4 pipeline-hoz:
   - v1 Draft: scope, DoD, implementációs terv
   - v2 DB review: schema, RLS, migration
   - v3 Security review: OWASP, RBAC
   - v4 Backend review: ha van CRITICAL finding
3. Verifikáld az API feltételezéseket a kódbázis ellen
4. Határozd meg melyik terminál valósítsa meg:
   - TOP 1 + TOP 2: **FE terminál** (frontend-only)
   - TOP 3: **CUTTING terminál** (1 új BE endpoint) + **FE terminál** (UI)
5. Írd ki a terminálnak inbox üzenetet
6. Feldolgozott konsenzust mozgasd `docs/planning/archive/`-ba
7. Küldj DONE outbox-ot a feldolgozás végeztével

## Nyitott kérdések (pre-implementation verifikáció)

A konsenzus 5 nyitott kérdést tartalmaz — ezeket is ellenőrizd:

1. `cuttingList` formátum egyezés az API elvárásokkal
2. Nesting API response shape (width/height vs bounds)
3. Drag-drop library választás (react-beautiful-dnd vs dnd-kit)
4. RBAC: `machine_operator` role existence
5. CuttingExecution FSM: `Planned → InProgress` transition
