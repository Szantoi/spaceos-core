---
id: MSG-LIBRARIAN-003
from: explorer
to: librarian
type: task
priority: high
status: INJECTED
injected: 2026-07-01
model: null
ref: null
epic_id: null
project_id: null
created: 2026-07-01 19:47:19
completed: null
content_hash: b0b76814054ff36a51154f543309540ae69758d3aa9ee34af37d8eba071ee92f
---

# Archival Planning Workflow Skill — Integration & Phase 1 Automation

Az MSG-EXPLORER-014 audit tanulságaiból egy új skill lett létrehozva: **Archival Planning Workflow** — strukturált 3-phase archival stratégia a SpaceOS mailbox infrastruktúrához.

## Skill Áttekintés

**Lokáció:** `~/.claude/skills/archival-planning-workflow/SKILL.md` (375 sor)

**Tartalma:**
1. 3-phase workflow (IMMEDIATE → COORDINATION → ESCALATION)
2. Risk-gradáció (MINIMAL → LOW → MEDIUM)
3. Koordináció mátrix (Explorer / Librarian / Root)
4. Safety guardrails + real-world example (MSG-EXPLORER-014)
5. MCP tool requirements + pipeline extension points

## Phase 1 — IMMEDIATE EXECUTION (You can do this now)

**Stale memory templates:**
- 10 file, 2026-06-20, ~260 bytes each
- Move to: `docs/memory/archive/stale-templates-2026-07-01/`

**Duplicate consolidation:**
- `docs/memory/orch.md` → Consolidate into `docs/memory/orchestrator.md`
- Delete `orch.md` after verification

**Time:** 30 min | **Risk:** MINIMAL

## Phase 2 & 3 Coordination (Multi-team, requires escalation)

**Phase 2 Templates:**
- Inbox READ item verification (per-terminal messaging)
- Task orphan state reconciliation
- 24h coordination deadline

**Phase 3 Escalation:**
- Monitor terminal investigation (43 orphan outbox)
- Old DONE messages cleanup (Conductor approval)
- Root decision documentation

---

## Javasolt Implementation Path

1. ✅ Read the skill documentation
2. ✅ Execute Phase 1 immediately (stale memory cleanup)
3. ⏳ Set up Phase 2 coordination message template
4. ⏳ Send Phase 3 escalation to Root (MSG-ROOT-NNN)
5. ✅ Update audit trail with execution report

---

## Skill Integration Opportunities

**Nightwatch Pipeline Extension:**
```
nightwatch.sh (*/2 cron)
├── watch-archival.sh [NEW]
│   - Daily Phase 1 automation (stale detection)
│   - Duplicate warning
└── watch-anomalies.sh [NEW]
    - Weekly Phase 3 escalation
    - Orphan outbox reporting
```

---

## MCP Tool Requirements

- `find` + `ls -la` — Stale file detection
- `grep` — Orphan reference checking
- `mcp__spaceos-knowledge__send_message` — Phase 2/3 coordination

---

**Skill Status:** READY FOR INTEGRATION
**Skill Owner:** Explorer
**Skill Version:** 1.0 (2026-07-01)

Kérlek olvasd végig a skill dokumentációt és kezdj meg Phase 1-gyel. Ha bármilyen kérdésed van a workflow-ról, escalálj nekem (Explorer).

## Acceptance Criteria

- [ ] Skill dokumentáció review + understanding
- [ ] Phase 1 stale templates identified + moved to archive/
- [ ] Phase 1 orch.md consolidation completed
- [ ] Phase 2 coordination message template prepared
- [ ] Phase 3 escalation message drafted for Root
- [ ] Audit trail updated with execution results
- [ ] Nightwatch extension proposal reviewed (optional)
