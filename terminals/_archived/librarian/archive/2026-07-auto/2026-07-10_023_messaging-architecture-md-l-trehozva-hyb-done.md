---
id: MSG-LIBRARIAN-023
from: librarian
to: root
type: done
priority: medium
status: READ
ref: MSG-LIBRARIAN-028
created: 2026-07-10
content_hash: fee987e9e7fc5b665f733c8767b6350a3c17d161689132525c6867b496e3ddb2
---

# DONE: MESSAGING_ARCHITECTURE.md létrehozva — Hybrid Model dokumentáció (TMB + File-based), MCP tool mapping, transition guidelines, auto-archive policy

**Original Task:** MSG-LIBRARIAN-028

## Details
## Deliverables

### 1. MESSAGING_ARCHITECTURE.md (Új dokumentum)
- **Architecture Overview** — Hybrid model diagram
- **MCP Tools Mapping** — 6 TMB tools vs 5 File-based tools
- **Use Case Decision Tree** — Mikor melyiket használjuk
- **Transition Guidelines** — Új fejlesztés → TMB preferált
- **Auto-Archive Policy** — 7 nap retention, weekly cleanup
- **Database Schema** — TMB SQLite struktúra
- **Monitoring** — Metrics queries mindkét rendszerhez

### 2. Knowledge INDEX.md frissítve
- MESSAGING_ARCHITECTURE.md hozzáadva HOT tier-hez

## Root Decision Implementation
- ✅ Option C (Hybrid) dokumentálva
- ✅ TMB vs File-based use cases tisztázva
- ✅ Nincs kötelező migráció — gradual transition
- ✅ Auto-archive működik (weekly-memory-cleanup.sh)

## Files Changed
- `docs/knowledge/patterns/MESSAGING_ARCHITECTURE.md`
- `docs/knowledge/INDEX.md`

