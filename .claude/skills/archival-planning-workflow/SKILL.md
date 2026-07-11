# Archival Planning Workflow

> **Skill:** Strukturált archival strategy a SpaceOS mailbox infrastruktúrához
>
> **Forrás:** MSG-EXPLORER-014 (2026-07-01) — Memory & Task Audit tanulságok
>
> **Owner:** Explorer (kutatás + planning) ↔ Librarian (execution)
>
> **Verzió:** 1.0 (2026-07-01)

---

## CÉLKITŰZÉS

Az archival planning workflow egy **3-phase, risk-gradated megközelítés** a SpaceOS mailbox (inbox/outbox/memory) tisztításához.

**Probléma:** Nagy volumenű (600+) unprocessed vagy orphan üzenet felhalmozódik, infrastruktúra-szintű műveledelmezést okoz.

**Megoldás:** Fázisolt archival stratégia, mely minden fázisban **egyértelműen definiálja:**
- Mi kerülhet archival-ba
- Mi az oka a kockázatnak
- Milyen koordináció szükséges
- Mi a fallback plan

---

## ARCHIVAL PLANNING LÉPÉSEK

### 1. AUDIT ÉS KATEGORIZAÇÃO (Explorer feladata)

**Input:** Mailbox státusza (inbox/outbox/memory fájlok + frontmatter)

**Output:** Strukturált audit report 4 dimenzió szerint:

```yaml
audit:
  scope:
    memory_files: "N total, X days old range"
    task_files: "new/active/archive distribution"
    outbox_messages: "terminal-wise breakdown"
    inbox_items: "status breakdown (UNREAD/READ/INJECTED)"

  findings:
    high_value: "recently updated, actively used"
    medium_value: "5+ nap régi, de még releváns"
    stale: "30+ nap régi, template copies, vagy orphan"
    duplicates: "2+ verzió ugyanarról a topic-ról"
    anomalies: "orphan terminal, untraced origin, status inconsistency"
```

**Döntési kritériumok:**
- **HIGH VALUE**: Utolsó update < 3 nap OR aktív session használja
- **MEDIUM VALUE**: Utolsó update 3-7 nap OR közösen fenntartott doc
- **STALE**: Utolsó update > 7 nap AND <500 bytes (template) AND nincsen aktív referencia
- **DUPLICATES**: 2+ fájl ugyanolyan információ, inkonzisztens update
- **ANOMALY**: Orphan outbox (no sender CLAUDE.md), status mismatch, broken reference

---

### 2. ARCHIVAL PLAN KÉSZÍTÉS (Explorer + Librarian)

**Risk-gradáció alapelve:**

| Phase | Komponens | Risk Level | Koordináció | Időigény |
|-------|-----------|-----------|------------|----------|
| **1** | Stale memory templates + clear duplicates | **MINIMAL** | Librarian alone | 30 min |
| **2** | Inbox READ items + task state audit | **LOW** | Librarian + Backend/Frontend state check | 1-2 hrs |
| **3** | Anomaly investigation + orphan outbox cleanup | **MEDIUM** | Librarian + Root/Conductor consultation | 2-3 hrs |

**Phase 1 — IMMEDIATE EXECUTION**
```yaml
Phase 1:
  criteria:
    - Memory: stale template (260-500 bytes, 2026-06-20 or older)
    - Memory: clear duplicate (X.md + X_v2.md, oldest wins)
    - Decision: no dependencies, no active session reference

  action:
    - Move to archive/ (NOT delete)
    - Update consolidation note (if duplicate consolidation)
    - Log action to audit trail

  fallback:
    - If consolidation is unclear, STOP and escalate to Librarian
    - Never delete last reference to a concept
```

**Phase 2 — COORDINATION REQUIRED**
```yaml
Phase 2:
  criteria:
    - Inbox: status=READ AND last_update > 3 days ago AND no active reference
    - Task: status=archive already, but still in active/ (orphan state)
    - Decision: requires state verification from contributing terminals

  action:
    - Query Backend/Frontend: "Is this task state still active?" (async)
    - Wait for confirmation (2-4 hrs)
    - If no reply after 24h, assume safe to archive
    - Archive to terminals/<terminal>/archive/

  fallback:
    - If terminal is unreachable, escalate to Conductor
    - If reference count ambiguous, re-check with grep across docs/
```

**Phase 3 — ESCALATION + ROOT CONSULTATION**
```yaml
Phase 3:
  criteria:
    - Outbox: orphan (no sender CLAUDE.md exists, origin unknown)
    - Outbox: DONE/BLOCKED > 14 days old AND not processed
    - Memory: uncertain canonical version (too many variants)

  action:
    - Librarian submits findings to Root/Conductor: "Monitor investigation needed"
    - Root decides: "Keep as historical artifact" OR "Safe to remove"
    - Decision logged with reasoning
    - Archival executed per Root decision

  fallback:
    - If ambiguity remains, move to "uncertain/" quarantine
    - Re-review in 7 days
    - Never force-delete without Root approval
```

---

## ARCHIVAL PLAN TEMPLATE

```markdown
# Archival Plan — [Scope Name]

## Executive Summary
- Total items analyzed: X
- Archival candidates: Y
- Estimated cleanup cost: Z hours
- Risk level: MINIMAL/LOW/MEDIUM/HIGH

## Phase 1 — Immediate (MINIMAL RISK)
### Memory Templates (Stale Candidates)
- [ ] `docs/memory/template-1.md` (2026-06-20, 260 bytes, no active ref)
- [ ] `docs/memory/template-2.md` (2026-06-20, 280 bytes, duplicate of X)
- [ ] ...
**Action:** Move to `archive/memory-templates-batch-2026-07-01/`

### Duplicate Consolidation
- [ ] `docs/memory/orch.md` → DELETE (consolidated into `orchestrator.md`)
- [ ] ...
**Action:** Single canonical document retained, others archived

## Phase 2 — Coordination (LOW RISK)
### Inbox READ Items (>3 days, status=READ)
- Backend terminal: 47 items
- Frontend terminal: 23 items
- ...
**Coordination:** Check with Backend/Frontend "Still relevant?" within 24h

### Task Orphan States
- [ ] Task X in active/ but marked archive elsewhere
- [ ] ...
**Action:** Reconcile with task owner

## Phase 3 — Escalation (MEDIUM RISK)
### Orphan Outbox (Unknown Origin)
- [ ] Monitor terminal: 43 messages, no CLAUDE.md
**Decision Required:** Root/Conductor investigation → "Archive" or "Keep as artifact"

### Old DONE Messages (>14 days)
- [ ] X DONE items from Y terminals
**Coordinator Action:** Bulk review + approval before archival

## Fallbacks & Contingencies
- Phase 1 unclear: escalate to Librarian (decision point)
- Phase 2 no reply after 24h: assume safe
- Phase 3 ambiguous: quarantine to "uncertain/", re-review in 7 days
- Emergency: Always retain 1 canonical reference per topic

## Audit Trail
- Executed by: [Librarian]
- Approved by: [Conductor/Root if needed]
- Date: YYYY-MM-DD
- Total items archived: N
- Total items deleted: M (only if explicit Root approval)
- Recovery checkpoint: [S3/backup location if disaster recovery needed]
```

---

## KOORDINÁCIÓ: EXPLORER ↔ LIBRARIAN ↔ ROOT

### Explorer Workflow
1. **Audit** — Mailbox analyze, kategorize (HIGH/MEDIUM/STALE/ANOMALY)
2. **Plan** — 3-phase strategy write, risk assessment
3. **Document** — Report to Librarian inbox (task format)

### Librarian Workflow
1. **Review** — Read audit, validate risk categorization
2. **Execute Phase 1** — Immediate stale cleanup (no coordination)
3. **Coordinate Phase 2** — Contact Backend/Frontend, wait 24h
4. **Escalate Phase 3** — Send to Root (MSG-ROOT-NNN)

### Root Workflow (Phase 3 Only)
1. **Review** — Understand anomaly & historical context
2. **Decide** — "Archive as artifact" or "Delete after Y-day retention"
3. **Approve** — Sign off with written reasoning
4. **Document** — Add to audit trail

---

## IMPLEMENTATION CHECKLIST

- [ ] Audit report created (Explorer)
- [ ] Risk categorization validated (Librarian)
- [ ] Phase 1 candidates listed (explicit file paths)
- [ ] Phase 1 executed (archival, no deletion without approval)
- [ ] Phase 2 coordination sent to terminals (24h deadline)
- [ ] Phase 2 results reviewed (responses captured)
- [ ] Phase 3 anomaly documented & escalated (Root message)
- [ ] Audit trail updated (who, when, why, recovery point)
- [ ] Stakeholder notification (Conductor/terminals informed)

---

## SAFETY GUARDRAILS

❌ **NIKÁLOS LÉPÉSEK:**
- Nem szabad utolsó referenciát egy topicról archival-ni
- Nem szabad deletion Phase 1/2-ben (csak archival)
- Nem szabad Phase 3 Root approval nélkül dönteni
- Nem szabad inaktív terminál (offline >7 nap) üzenetét kraft nélkül archival-ni

✅ **JAVASLAT:**
- Phase 1 → Librarian autonomy
- Phase 2 → Librarian + team coordination (async OK)
- Phase 3 → Root decision (always)
- Uncertainty → Always escalate (don't guess)

---

## REAL-WORLD EXAMPLE: 2026-07-01 Audit

**Context:** SpaceOS memory/task audit, 21 memory files + 583 outbox messages

**Phase 1 Execution:**
```
Stale templates (10 files, 2026-06-20, ~260 bytes each)
→ Archive to: docs/memory/archive/stale-templates-2026-07-01/
→ Duplicate consolidation: orch.md → orchestrator.md
→ Time: 30 min
→ Risk: MINIMAL (no active references, template-only)
```

**Phase 2 Coordination:**
```
Inbox READ items (282 total, >3 days old)
→ Per-terminal verification (Backend: 81, Frontend: 93, etc.)
→ Librarian sends: "Verify if still relevant?" message
→ Deadline: 24h
→ Expected time: 1-2 hrs actual work + waiting
```

**Phase 3 Escalation:**
```
Monitor terminal (43 orphan outbox, no CLAUDE.md)
→ Escalate to Root: "Unknown origin, investigate?"
→ Root decision: "Keep as historical artifact" or "Archive"
→ Expected time: 2-3 hrs (depends on investigation depth)
```

---

## KITERJESZTÉSI LEHETŐSÉGEK

1. **Automatikus Phase 1** — nightwatch.sh integráció
   - STALE memory templates automatikusan archival-ni >30 nap után
   - DUPLICATE detection + warning message (nem automatikus delete)

2. **Memory Tiering** — HOT/WARM/COLD retention
   - HOT (48h): frequently accessed, no archival
   - WARM (14d): medium relevance, archival after 2 weeks
   - COLD (365d): historical, archival after 1 year
   - SHARED: global reference, nem archival-ni

3. **Orphan Detection** — Automated finding
   - Grep orphan outbox (no sender CLAUDE.md)
   - Flag broken task references (dead links)
   - Report to Conductor weekly

---

## VERZIÓHISTÓRIA

| Verzió | Dátum | Frissítés |
|--------|-------|----------|
| 1.0 | 2026-07-01 | Initial release based on MSG-EXPLORER-014 audit |

---

**Skill Maintainer:** Explorer
**Last Updated:** 2026-07-01
