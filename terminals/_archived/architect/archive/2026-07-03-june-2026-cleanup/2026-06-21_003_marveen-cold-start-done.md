---
id: MSG-ARCHITECT-003-DONE
from: architect
to: root
type: done
priority: high
status: READ
ref: MSG-ARCHITECT-003
created: 2026-06-21
content_hash: ee63eb87e2b3e9ee5dab4452327559c0f87a363c4b78b4c86051bbfac2a4e7a0
---

# DONE: Marveen Hideg Indítási Stratégia — ADR-046

## Summary

A Marveen cold start stratégia architekturális specifikációja elkészült. Az ADR-046 dokumentum tartalmazza:

1. **Architekturális döntések** — cold start vs traditional, tier semantics, decay policies
2. **Implementációs terv 4 track-ra bontva** — A (Memory), B (Session Hooks), C (Retrospective/Handoff), D (Dashboard)
3. **API spec TypeScript interface-ekkel** — TieredMemory, SessionLifecycle, Retrospective, Handoff, DailyDigest
4. **Trade-off elemzés** — token cost, latency, storage, risk

## Artifacts Created

| File | Description |
|---|---|
| `docs/adr/ADR-046_marveen-cold-start-strategy.md` | Teljes ADR dokumentum |

## Key Decisions

| Decision | Rationale |
|---|---|
| **Cold start model** | Cleaner context, crash-resilient, 50-80% token savings |
| **4-tier memory** (hot/warm/cold/shared) | Proven Marveen pattern, automatic decay |
| **5K token context budget** | Balance: quality vs cost |
| **MCP tool integration** | Consistent interface, audit trail |

## Implementation Tracks

| Track | Scope | Est. Days |
|---|---|---|
| **A: Memory Tier** | Extend memoryStore.ts with tier semantics | 1.5 |
| **B: Session Hooks** | Pre-start context injection, post-end save | 1.5 |
| **C: Retrospective/Handoff** | MCP tools for skills integration | 1.0 |
| **D: Dashboard** | Datahaven integration, daily digest | 1.0 |

## Migration Strategy

- **Phase 1** (Week 1): Parallel operation, collect metrics
- **Phase 2** (Week 2): Opt-in cold start for low-risk terminals
- **Phase 3** (Week 3+): Default cold start for non-priority terminals

## Definition of Done Checklist

- [x] ADR dokumentum elkészült (`docs/adr/ADR-046_marveen-cold-start-strategy.md`)
- [x] Implementációs terv track-okra bontva (A/B/C/D)
- [x] API spec TypeScript interface-ekkel (8 interface-csoport)
- [x] Trade-off elemzés (token cost, latency, storage)
- [x] DONE outbox üzenet elküldve

## Next Steps

1. Root jóváhagyja az ADR-t
2. Backend terminálnak inbox üzenet Track A-val (Memory Tier implementáció)
3. Párhuzamosan: Track D (Dashboard) előkészítés

---

## MCP Visszajelzés

### Használt eszközök

- Glob, Read — kódbázis feltérképezés
- Bash — fájlstruktúra ellenőrzés

### Javasolt új eszközök

- **`get_adr_list`** — ADR katalógus lekérdezése
- **`validate_typescript_interface`** — interface syntax ellenőrzés
- **`estimate_token_count`** — szöveg token költség becslés
