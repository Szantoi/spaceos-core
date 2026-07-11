---
id: dwi-mcp-integration
type: discovery_work_item
topic: "MCP Integration: Tool Discovery & Protocol Alignment"
status: in_progress
current_phase: 0
next_action: "Explorer: gather current MCP server capabilities and tool registry (must document: all available MCP servers, tool count per server, capability gaps relative to agent needs, protocol version mismatch issues)"
verdict: null
hypothesis_count: 1
validated_count: 0
created: 2026-03-01
updated: 2026-03-03
---

# DWI: MCP Integration — Tool Discovery & Protocol Alignment

## Active Phase

**Phase 0 — Discover** (Explorer phase — gather facts)

Currently in discovery phase mapping existing MCP server landscape and identifying protocol alignment gaps. Need to understand what tools are available and where the integration gaps exist.

## Next Action

> **Explorer: gather current MCP server capabilities and tool registry.** Must document: all MCP servers currently deployed (JoineryTech.McpServer + others?), tool count per server, tool categories, capability gaps relative to agent needs (do we have all tools agents require?), and any protocol version mismatch issues between MCP version and current agents.

## Hypothesis Summary

| ID | Statement (short) | Status | Notes |
|:---|:-----------------|:-------|:------|
| hyp-001 | Current MCP server infrastructure can support all discovery & delivery agent needs without additional tool development | open | Ready for fact gathering |

## Phase Gate History

| Phase | Gate crossed | Date | Notes |
|:------|:------------|:-----|:------|
| 0 — Discover | 🔄 | 2026-03-03 | Explorer gathering MCP capability inventory |
| 1 — Define | ⬜ | — | Blocked on phase 0 completion |
| 2 — Ideate | ⬜ | — | Pending |
| 3 — Prototype | ⬜ | — | Pending |
| 4 — Test & Learn | ⬜ | — | Pending |

## Linked Artifacts

### Observations (Phase 0)
- `00_discovery/obs-001.md` — (in progress) MCP server tool inventory

## Success Criteria for Phase 1 Entry

1. **obs-*.md completed**: Full tool registry with capability matrix (agent role × required tool → availability)
2. **Gap analysis done**: List of 3–5 key capability gaps (if any)
3. **Protocol version audit**: Current MCP version vs. agent compatibility

---

## Phase Notes

### Phase 0 (In Progress — Expected closure 2026-03-10)
Explorer conducting MCP server audit. Initial observation: JoineryTech.McpServer exists but tool inventory needs formal documentation. Secondary MCP servers (if any) need identification.

### Next Step
Once inventory complete, Framer will lock scope for Phase 1: what integration work is needed? New tools? Protocol upgrades? Adapter layer?
