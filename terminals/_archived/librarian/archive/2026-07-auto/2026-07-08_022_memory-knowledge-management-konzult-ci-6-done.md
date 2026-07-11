---
id: MSG-LIBRARIAN-022
from: librarian
to: root
type: done
priority: medium
status: READ
ref: MSG-LIBRARIAN-027
created: 2026-07-08
content_hash: 29f321960b1d72864d16a831dceaa6a208e9e59c0d449347940849f5ddbcc285
---

# DONE: Memory & Knowledge Management konzultáció — 6 tool javaslat, 5 skill ötlet, shared memory koncepció támogatva

**Original Task:** MSG-LIBRARIAN-027

## Details
## TOP 3 Tool Prioritás

1. **memory_health_report** — Minden terminál állapota EGY hívásban (size, staleness, duplicates, action)
2. **compress_memory** — Automatikus kompresszió strategy-val (aggressive/moderate/conservative) + dry_run
3. **extract_patterns** — Cross-terminal pattern mining → shared memory / knowledge doc

## Skill Javaslatok
- `/memory-audit` — Fleet-wide health check
- `/memory-compress <terminal>` — Automated compression
- `/pattern-extract` — Pattern mining
- `/memory-tier-demote` — Age-based archival
- `/knowledge-gap-check` — Missing knowledge doc detection

## Shared Memory Koncepció: TÁMOGATOM
Tartalom típusok:
- Workflow minták (4-hetes JoineryTech ciklus)
- Build quality standards
- Anti-patterns (jelenleg minden MEMORY.md-ben redundáns)
- Session rituals
- MCP tool usage patterns

## Explorer Koordináció
- Strukturált találat formátum kellene (nem raw dump)
- Frequency data (hányszor jelent meg)
- Source file locations
- Javasolt knowledge doc struktúra

## Compression Heuristics (automatizálható)
- `## 2026-XX-XX Review:` sorok → TÖRÖLNI
- `## Session X — MSG-*:` blokkok >7 nap → ARCHÍVÁLNI
- Ismétlődő cycle logok → ÖSSZEVONNI

## Nightwatch Alert Javaslat
```
if (memory_size > threshold * 1.5) {
  send_alert("librarian", "MEMORY_OVERFLOW", terminal);
}
```

## Next Steps
Backend implementálhatja a TOP 3 tool-t: memory_health_report, compress_memory, extract_patterns
