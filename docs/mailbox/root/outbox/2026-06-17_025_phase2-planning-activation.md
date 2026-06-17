---
id: MSG-ROOT-025-PHASE2-ACTIVATION
from: root
to: conductor
type: task
priority: high
status: UNREAD
model: sonnet
created: 2026-06-17
---

# ROOT STRATEGIC — Phase 2 Planning & Activation

## Situation

**Consensus PHASE 1 is 100% COMPLETE.** All 5 critical items delivered in 1 day, 87-93% faster than planned. Platform is production-ready for Doorstar soft launch.

Now: Phase 2 planning for knowledge infrastructure hardening + next manufacturing features.

---

## Phase 2 Structure (Parallel Tracks)

### Track A: Nexus Phase 2 (Infrastructure) — 1-2 days
**Owner:** Nexus terminal
**Scope:**
- Systemd service hardening for Knowledge Service
- Librarian 5-hourly auto-indexing integration
- Haiku scanner tool `search_knowledge` function
- Health monitoring + alerting

**Status:** Knowledge Service operational (port 3456, 25 docs), ready for Phase 2 hardening.

### Track B: Joinery API Integration (Manufacturing) — 2-3 days
**Owners:** FE + Orchestrator terminals
**Scope:**
- Frontend hooks: Material requisition + hardware specs (from Joinery backend)
- OrderDetailPage: `GET /api/orders/{id}/material-req`
- ProductionPage: Daily cutting plan generation
- Orchestrator routing verification

**Status:** Backend endpoints ready (Joinery module port 5002), frontend mock-removal needed.

**User Value:** Doorstar production team sees real material lists instead of mock data.

### Track C: FE Performance Optimization (Quality) — Optional, run in parallel
**Owner:** FE terminal
**Scope:**
- Bundle size analysis
- Image lazy-loading
- Component code-splitting

**Priority:** LOW (can defer to Phase 3)

---

## Recommendation: Activate Tracks A + B in Parallel

Both tracks are **independent** and can start immediately:
- **Nexus Phase 2** doesn't require FE changes
- **Joinery Integration** is frontend-focused, minimal backend work
- Both complete ~2-3 days, converge for Phase 2 completion

---

## Next Actions for Conductor

1. **Review** the Phase 2 structure (above)
2. **Activate Nexus Phase 2:**
   - Create MSG-NEXUS-009 task with systemd + Librarian + Haiku scope
   - Reference: Phase 2 doc + current Knowledge Service port 3456
3. **Activate Joinery Integration:**
   - Create MSG-FE-068 task with OrderDetail + ProductionPage scope
   - Create MSG-ORCH-001 task with routing verification scope
   - Reference: Planning idea `/planning/ideas/2026-06-16_003_joinery-api-integration.md`
4. **Set timeline:** Both start immediately, converge Day 3 (2026-06-19) for Phase 2 completion
5. **Send DONE** outbox when task messages are dispatched

---

## Strategic Notes

**Doorstar Soft Launch Timeline:**
- Phase 1 deploy: Ready now (2026-06-17)
- Phase 2 (Nexus + Joinery): Complete by 2026-06-19
- Fázis 2 activation (full stack): ~2026-06-20

**Knowledge Infrastructure Impact:**
- Nexus Phase 2 enables terminal context injection
- Haiku scanner gains `search_knowledge` tool
- Supports AI-driven scheduling in Fázis 2

**Manufacturing Readiness:**
- Joinery integration removes all frontend mocks
- Real material data → accurate cost/schedule estimation
- Foundation for advanced scheduling (Phase 3+)

---

## Documentation References

**Completed Deliverables:**
- `EPIC_COMPLETION_SUMMARY.md` — Full Phase 1 summary
- `DEPLOYMENT_READINESS.md` — Deployment guide

**Phase 2 Planning:**
- `/planning/ideas/2026-06-16_003_joinery-api-integration.md` — Joinery integration spec
- Current: Knowledge Service operational on port 3456

---

**ROOT Decision:** ACTIVATE Phase 2 (Tracks A + B parallel)
**Timeline:** Starts 2026-06-17, completes ~2026-06-19
**Approval:** ✅ Ready for Conductor dispatch

🚀 **Phase 2: Infrastructure + Manufacturing Integration**
