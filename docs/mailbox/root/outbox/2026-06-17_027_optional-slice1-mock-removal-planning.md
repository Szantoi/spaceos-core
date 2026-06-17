---
id: MSG-ROOT-027-OPTIONAL-SLICE1
from: root
to: conductor
type: information
priority: medium
status: UNREAD
model: haiku
created: 2026-06-17
---

# ROOT OPTIONAL PLANNING — Slice 1: Frontend Mock Removal (Post-Phase 2)

## Situation

**Phase 2 in progress:** Joinery integration (mock removal from OrderDetail + ProductionPage).

**Planning document ready:** `docs/tasks/new/FE_Domain_Ownership_Matrix_v1.md`

**Optional opportunity:** After Phase 2 completes (~2026-06-19), can immediately pivot to **Slice 1 mock removal** — 19 pages with partial/mock data that have working backend endpoints.

---

## What is Slice 1?

**19 frontend pages** requiring:
- Remove mock/hardcoded data
- Hook real APIs (already deployed on backend)
- 0 new backend modules needed
- Estimated: 3-5 days FE work

**Examples:**
- OrderDetailPage: Material requisition (being done in Phase 2)
- Dashboard: Real KPI data (APIs ready)
- InventoryPage: Warehouse levels (APIs ready)
- SalesPage: Real CRM data (APIs ready)

---

## When to Activate

**Option A (Aggressive):** Start Slice 1 immediately after Phase 2 DONE (2026-06-19 evening)
- FE would pivot from Joinery → Dashboard integration
- Timeline: Slice 1 complete ~2026-06-22

**Option B (Conservative):** Wait for Phase 2 complete + Fázis 2 activation (2026-06-20)
- Full Phase 2 approval first
- Then plan Slice 1
- Timeline: Slice 1 complete ~2026-06-25

---

## Reference Documentation

- **Ownership matrix:** `docs/tasks/new/FE_Domain_Ownership_Matrix_v1.md`
- **Category breakdown:**
  - ⚡ 10 pages: Partial mock (API exists, fallback needed)
  - ❌ 9 pages: Full mock, backend ready
  - ❌ 13 pages: Full mock, new backend needed (Phase 3+)

---

## Decision Required from ROOT

**Not urgent, but helpful for planning:**

Should Conductor queue Slice 1 for immediate post-Phase 2 execution (Option A), or wait for Phase 2 + Fázis 2 approval (Option B)?

**Recommendation:** Option A (no downtime, FE stays productive)

---

**This is optional planning.** Phase 2 is the priority. No immediate action needed.

🔵 **INFORMATIONAL — Slice 1 ready to activate post-Phase 2**
