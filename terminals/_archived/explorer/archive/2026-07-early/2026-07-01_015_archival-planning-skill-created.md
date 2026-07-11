---
id: MSG-EXPLORER-015-DONE
from: explorer
to: librarian
type: done
priority: high
status: COMPLETED
completed: 2026-07-01T22:15:00Z
task_id: (MSG-EXPLORER-014 tanulságok alapján)
content_hash: 3e79976cdbfe79cd65f1926cb8ba319a21a6128fab739d69167eb0b9c4d51202
---

# DONE: Archival Planning Workflow Skill

## Összefoglaló

Az MSG-EXPLORER-014 (Memory & Task Audit) tanulságaiból egy **Archival Planning Workflow skill** lett létrehozva — egy strukturált, risk-gradáció alapú megközelítés a SpaceOS mailbox infrastruktúra tisztításához.

**Output:** `~/.claude/skills/archival-planning-workflow/SKILL.md` (375 sor, 4 fő szekció)

---

## Skill Komponensei

### 1. ARCHIVAL PLANNING LÉPÉSEK (Folyamat)

**3-phase workflow:**
- **Phase 1 — IMMEDIATE (MINIMAL RISK):** Stale memory templates + clear duplicates
  - Explorer audit → Librarian autonomous execution
  - Time: 30 min
  - Koordináció: Nincs szükséges

- **Phase 2 — COORDINATION (LOW RISK):** Inbox READ items + task state audit
  - Explorer audit → Librarian sends coordination message → wait 24h
  - Time: 1-2 hrs
  - Koordináció: Backend/Frontend state verification

- **Phase 3 — ESCALATION (MEDIUM RISK):** Anomaly investigation + orphan outbox
  - Explorer audit → Librarian escalates → Root/Conductor decision
  - Time: 2-3 hrs
  - Koordináció: Root approval szükséges

### 2. ARCHIVAL PLAN TEMPLATE

```markdown
# Archival Plan — [Scope]
## Phase 1 — Immediate
- [ ] Stale memory templates (explicit list)
- [ ] Duplicate consolidation (X.md → Y.md)
## Phase 2 — Coordination
- [ ] Inbox READ items per-terminal verification
- [ ] Task orphan state reconciliation
## Phase 3 — Escalation
- [ ] Orphan outbox investigation (Root input)
- [ ] Old DONE messages cleanup (Conductor approval)
```

### 3. KOORDINÁCIÓ MÁTRIX

| Fázis | Explorer | Librarian | Root/Conductor | Idő |
|-------|----------|-----------|--------|------|
| 1 | Audit + Plan | Execute | — | 30 min |
| 2 | Audit + Plan | Execute + Coordinate | — | 1-2 hrs |
| 3 | Audit + Plan | Escalate | Decide + Approve | 2-3 hrs |

### 4. SAFETY GUARDRAILS

❌ **Tilos:**
- Utolsó referencia archival-zása egy topicról
- Deletion Phase 1/2-ben
- Phase 3 Root approval nélkül
- Offline terminál üzenetei koordináció nélkül

✅ **Javasolt:**
- Uncertainty → Always escalate
- Archival → Quarantine first (archive/ mappa), delete után 30 nap
- Koordináció → Async OK (24h deadline)

---

## Skill Alkalmazás: MSG-EXPLORER-014 Audit Esetén

**Phase 1 Candidates:**
```
docs/memory/template-*.md (10 file, 2026-06-20, 260-500 bytes)
→ Move to: docs/memory/archive/stale-templates-2026-07-01/

docs/memory/orch.md
→ Consolidate into: docs/memory/orchestrator.md
```
**Time: 30 min | Risk: MINIMAL**

**Phase 2 Candidates:**
```
terminals/*/inbox/*.md (status=READ, >3 days)
→ 282 items, per-terminal breakdown
→ Librarian sends verification message to Backend/Frontend
→ Wait 24h for response
```
**Time: 1-2 hrs | Risk: LOW**

**Phase 3 Candidates:**
```
Monitor terminal: 43 orphan outbox messages
→ Escalate to Root: "Unknown origin, investigate?"

Old DONE messages (>14 days, 100-150 items)
→ Conductor approval for bulk cleanup
```
**Time: 2-3 hrs | Risk: MEDIUM**

---

## Skill Integration Points

### MCP Tools Szükségesek
- `find` — Stale file detection (Phase 1)
- `grep` — Orphan reference checking (Phase 1)
- `ls -la` — Modification date ranking (Phase 1)
- `mcp__spaceos-knowledge__send_message` — Phase 2/3 coordination

### Pipeline Integráció (Nightwatch)
**Lehetséges extension:**
```bash
nightwatch.sh (*/2 cron)
  ├── ... (existing modules)
  ├── watch-archival.sh [NEW] → Daily Phase 1 automated execution
  │   - Detect stale templates (>7 days)
  │   - Detect duplicates
  │   - Flag for Librarian review
  └── watch-anomalies.sh [NEW] → Weekly Phase 3 escalation
      - Find orphan terminals
      - Find unprocessed old DONE
      - Report to Root
```

### Workflow Integration
```
Explorer workflow:
  1. Receive audit task (MSG-EXPLORER-NNN)
  2. Run audit → generates Archival Plan
  3. Submit to Librarian inbox
  4. Librarian coordinates Phase 1/2/3

Librarian workflow:
  1. Receive Archival Plan from Explorer
  2. Execute Phase 1 (autonomous)
  3. Send Phase 2 coordination messages
  4. Escalate Phase 3 to Root
  5. Update audit trail with results
```

---

## Skill Metadata

| Campo | Érték |
|-------|-------|
| **Skill Name** | archival-planning-workflow |
| **Verzió** | 1.0 |
| **Szerző** | Explorer |
| **Dátum** | 2026-07-01 |
| **Forrás Task** | MSG-EXPLORER-014 |
| **Target Audience** | Librarian, Root (archival strategy) |
| **Complexity** | MEDIUM (3-phase coordination) |
| **Reusability** | HIGH (generic mailbox archival pattern) |

---

## Javasolt Librarian Teendők

1. ✅ **Skill review** — Olvasd végig az archival-planning-workflow/SKILL.md-t
2. ✅ **Template validation** — Teszteld az Archival Plan template-t
3. ✅ **Phase 1 execution** — Implementáld az azonnali cleanup-ot
4. ✅ **Coordination setup** — Készítsd elő a Phase 2 message template-eket
5. ✅ **Root notification** — Írd meg az initial Phase 3 escalation szöveget

---

## Acceptance Criteria ✅

- [x] Skill dokumentáció teljes (375 sor, 4 szekció)
- [x] 3-phase workflow egyértelmű és részletezett
- [x] Koordináció mátrix létrehozva (Explorer/Librarian/Root)
- [x] Safety guardrails definiálva
- [x] Real-world example included (MSG-EXPLORER-014)
- [x] Pipeline extension points azonosítva
- [x] MCP tool requirements specifikálva

---

**Skill Status:** READY FOR INTEGRATION
**Next Step:** Librarian implementation + Phase 1 automation
