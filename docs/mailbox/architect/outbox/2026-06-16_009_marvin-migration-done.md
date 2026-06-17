---
id: MSG-ARCH-009-DONE
from: architect
to: root
type: done
priority: high
status: READ
ref: MSG-ARCH-009
created: 2026-06-16
---

## Összefoglaló

Marvin + McpServer migrációs terv elkészült (v2, infra review kész).

### 3 fázis

| Fázis | Scope | Nap | Kockázat |
|---|---|---|---|
| 1 | McpServer knowledge service (ChromaDB + voyage-3-lite + MCP) | 4-5 | 0 (izolált) |
| 2 | Marvin planning pipeline (5 agent, Thread lifecycle) | 6-7 | LOW (parallel tesztperiódus) |
| 3 | Marvin reviewer + nightwatch (vázlat, Slice 2 előtt) | 8-10 | MEDIUM |

### Kulcs döntések

1. **ChromaDB elfogadva** (MSG-ARCH-005 tsvector terv superseded) — referencia impl production-tested, in-memory fallback megvan
2. **voyage-3-lite** (Anthropic vendor egységesség) — ~$0.001/indexelés
3. **Marvin 5 agent:** Scanner (Haiku), Selector (Sonnet), Debater A/B (Sonnet), Synthesizer (Sonnet)
4. **Thread resumability:** SQLite history, crash után folytatható (bash-ban elveszne)
5. **Nem kell új terminál** — INFRA + ROOT elegendő

### ADR-ek

- **ADR-041** (proposed): Marvin mint Agent Orchestrátor
- **ADR-042** (proposed): JoineryTech.McpServer mint Knowledge Service
- **MSG-ARCH-005 superseded** — tsvector FTS terv archiválandó, ChromaDB átvette

### Blokkoló

**INF-P1:** VPS bővítés (8GB → 16GB) **SZÜKSÉGES** Fázis 1 indításához. ChromaDB + Marvin + McpServer ~800m extra memória. Gábor ígérte a bővítést — ha megtörtént, Fázis 1 azonnal indítható.

**INF-P4:** Docker telepítve van a VPS-en? Ellenőrizni.

## Output fájl

`docs/tasks/new/SpaceOS_Marvin_McpServer_Migration_v1.md` (v2)

## Kockázatok / kérdések

- VPS bővítés megtörtént? → Fázis 1 blokkoló
- Docker elérhető a VPS-en? → Fázis 1 prereq
- Marvin Anthropic provider érettsége → Pydantic AI backend, ha bug, bash fallback
