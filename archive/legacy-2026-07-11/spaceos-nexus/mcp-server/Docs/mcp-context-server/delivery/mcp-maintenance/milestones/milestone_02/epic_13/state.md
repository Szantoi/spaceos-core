---
id: epic-mcp-maintenance-13
title: "Epic 13: Discovery Track Tools (DWI Support)"
type: epic
milestone: M02
project: mcp-maintenance
status: "COMPLETE — T13-01..07 ALL DONE"
fsm_state: "DONE"
fsm_retry_count: 0
updated: 2026-03-12
---

# EPIC-13 State Summary

## Hogyan lesz megoldva
- Discovery role definiciok es DWI workflow/template keszlet SQLite-ba seedelve.
- Discovery-specifikus toolok: `reference_prior_discovery()` es `submit_discovery_outcome()`.
- A request context routing discovery fazisokra lesz kotve (ideation/validation/iteration/handoff).
- Two-track RBAC enforcement blokkolja a cross-track es phase-tiltott hivasokat.
- E2E validacio lefedi a teljes discovery flow-t es a jogosultsagi tiltasi eseteket.

## Levont tapasztalatok (tervezesi)
- A konkret, merheto AC-k gyorsitjak a fejlesztoi kivitelezest es a QA atadast.
- A role-phase matrix explicit kezelese megeloz tobb rejtett authorization hibat.
- A discovery memory visszacsatolas csak stabil EPIC-12 alappal lesz megbizhato.

## Celallapot
- AC target: 32 ✅
- Teszt target: 19 ✅ (unit tesztek; integráció EPIC-11 E2E-ben)
- Kimenet: Discovery-track ready MCP capability stack ✅

## Megvalósított fájlok
- `src/mcp/tools/discovery.ts` — DiscoveryPlugin: request_context, reference_prior_discovery, submit_discovery_outcome, check_constraints, get_phase_guidance, track_blocker, query_blockers
- `src/tests/unit/DiscoveryTools.test.ts` — 19 unit teszt

## Teszt lefedettség
| Describe block | Task | Tesztek |
|:---------------|:-----|:--------|
| request_context tool | T13-02 | 4 ✅ |
| reference_prior_discovery tool | T13-03 | 3 ✅ |
| submit_discovery_outcome tool | T13-04 | 4 ✅ |
| phase-specific tools | T13-05 | 4 ✅ |
| blocker tracking tools | T13-06 | 2 ✅ |
| RBAC two-track enforcement | T13-07 | 2 ✅ |
| **Összesen** | | **19 ✅** |

## Levont tapasztalatok
- A two-track RBAC (`track !== 'discovery'` check) a kontextusból olvasható.
- A phase order enforcement (iteration → validation előtt nem kezdhető el) FSM state-re épül.
- A reference_prior_discovery jelenleg EpisodeManager.searchKeyword proxy-val működik.
