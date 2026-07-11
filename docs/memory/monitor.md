# Monitor Terminal Memory — Updated 2026-07-08

## PRIMARY MISSION

**CRITICAL ROLE:** Conductor Coach + JoineryTech Development Enabler

### Role Definition
- **Previous:** Passive system health observer
- **NEW:** Active Conductor coaching + JoineryTech quality focus
- **Duration:** Until JoineryTech Portal v1 OPERATIONAL + Doorstar ready

### Quality Standards
- UI Design (Figma/UX) + Backend API = 1:1 correspondence
- No orphan components, no mock data
- Forms → real endpoints, Dashboards → real data
- RBAC/Security operational

### Delivery Criteria
- Design + Code integrated (not separate)
- API contracts defined (Orchestrator BFF)
- Frontend-Backend tested end-to-end
- Ready for Doorstar beta (first customer)

---

## CONFIGURATION

**Config File:** `/opt/spaceos/terminals/monitor/MONITOR-CONFIG.yaml`

**Parameters (v1.1):**
```yaml
mode: agent-optimized
health_check.interval_minutes: 30
phase_transition.progress_threshold_percent: 90%
phase_transition.conductor_idle_timeout_minutes: 120
escalation.phase_gap_threshold_minutes: 360
encouragement.auto_send_on_phase_complete: true
```

---

## COACH WORKFLOW

### Health Check Sequence
1. Check BLOCKED Count (target: <5)
2. Check Epic Progress (JT-CRM, JT-CTRL, JT-EHS)
3. Check Conductor Activity (working or idle?)
4. Assess Quality (UI-Backend sync? Real data?)

### Trigger Actions
- **Progress Stalled (>20 min):** Identify blocker → Suggest workaround → Inbox nudge
- **Quality Risk:** Flag in coaching message → Suggest integration
- **Conductor Responsive:** Confirm priorities → Celebrate milestones

---

## ÖSZTÖNZÉS PROTOCOL

**Trigger Conditions:**
- Conductor idle >120 minutes
- Phase progress >90%

**Auto-Send:** Phase progress report + Phase 3 tasks + Timeline coordination

---

## MODE #4 METRICS

| Metric | Value |
|--------|-------|
| Cost | ~$0.50-1/hour (70% savings) |
| Cycle time | 1 completion every 8 min |
| Quality | All reviews passed |
| Workflow | Active/idle orchestration |

---

## CURRENT STATUS (2026-07-08)

| Metric | Value |
|--------|-------|
| Infrastructure | OPERATIONAL |
| MCP Auth | FIXED (2026-07-07) |
| Mode | Hot — Conductor Coaching |
| Active Epics | JT-EHS, Doorstar M1 |

---

## RECENT MILESTONES

| Date | Event |
|------|-------|
| 2026-07-08 | Memory cleanup (Librarian) |
| 2026-07-07 | MCP Auth fixed |
| 2026-07-06 | PRIMARY MISSION activated |
| 2026-07-04 | Encouragement Protocol APPROVED |

---

## ANTI-PATTERNS (AVOID!)

**DO NOT add to this memory:**
- Individual cycle logs (use outbox instead)
- Repetitive status updates (keep only current state)
- Detailed escalation history (archive to outbox)

**Memory should stay <20KB** — only mission-critical context.

---

_Last Updated: 2026-07-08_
_Compressed by Librarian: Removed ~7000 lines of repetitive cycle logs_
