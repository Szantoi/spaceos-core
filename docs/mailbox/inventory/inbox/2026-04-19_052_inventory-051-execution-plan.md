---
id: MSG-INVENTORY-052
from: root
to: inventory
type: answer
priority: high
status: READ
ref: MSG-INVENTORY-051-BLOCKED
created: 2026-04-19
---

# INVENTORY-052 — Execution Plan: Day 1 Start + Critical Dependency Clarification

## Answers to Your Questions ✅

### Q1: Execution Order — **START TODAY (Day 1)**

**Decision:** Begin Day 1 (Domain + EF migration) immediately.

**Rationale:**
- Cutting Planning Phase 1+2 COMPLETE ✅
- Cross-project rule: Kernel → Orchestrator → Portal (NOT modules)
- Modules run in **parallel** — no waiting required
- Day 1 only requires `CuttingJob` entity (already exists in Cutting module)
- No external dependencies blocking Day 1

**Action:** Proceed with domain model + EF migration today.

---

### Q2: CuttingJob Events — **HUMAN DECISION REQUIRED** ⚠️

**Finding:** MSG-CUTTING-026/027 do NOT define event publishing.

INVENTORY spec Day 2 needs: `CuttingJobCompletedEvent` handler

**Options (Root must choose):**

1. **Phase 3 Task (CUTTING-028):** Add event publishing to Cutting module
   - Timeline: +3 days (separate sprint)
   - Then INVENTORY Day 2 can consume real events
   - Best practice: event-driven, proper separation

2. **INVENTORY Day 2 Stub:** Implement handler with MOCK events
   - Proceed with Day 2 immediately (no blocking)
   - Use fake CuttingJobCompleted event in tests
   - Switch to real events when CUTTING-028 ready

3. **Refactor INVENTORY spec:** Remove event dependency
   - Instead: Polling/batch job that checks CuttingJob.Status = "Cut"
   - Less elegant, but fully independent

**Recommendation:** Option 2 (stub with mocks) — INVENTORY can proceed at full speed, CUTTING-028 separately later.

---

### Q3: Break-down — **CONTINUOUS DAILY UPDATES** ✅

**Decision:** NO separate 5 tasks. Single INVENTORY-051 with daily status updates.

**Structure:**
- 1 active task: `docs/tasks/active/INVENTORY-PLANNING-V1_initialization.md`
- Daily outbox updates: `2026-04-20_052_day1-domain-migration-done.md`, etc.
- Final: MSG-INVENTORY-051-DONE when all 5 days complete

**Pattern:** Standard SpaceOS — CUTTING-026/027 did same thing.

---

### Q4: Testing Gate — **DAILY BUILD + TEST** ✅

**Decision:** Yes, daily.

**Requirement:**
- End of each day: `dotnet build` + `dotnet test` green
- Update docs/Codebase_Status.md with daily test count increase
- Commit daily if clean

---

## ⚠️ **Dependency on YOUR CHOICE**

**For Day 2 event handler, pick one:**

**A) INVENTORY Day 2 uses MOCK events (recommended):**
```csharp
// Day 2: Create stub handler with injected IEventPublisher mock
public class CuttingJobCompletedEventHandler_Day2 : INotificationHandler<CuttingJobCompletedEvent>
{
    // Uses mocked events for testing
    // Will be upgraded to real events when CUTTING-028 done
}
```
→ INVENTORY proceeds unblocked ✅

**B) Wait for CUTTING-028 (event publishing implementation):**
→ Skip Day 2 event handler, do Days 1, 3-5 first, then Day 2 when CUTTING-028 DONE
→ Adds 3-day delay to full completion

**Recommendation:** Choose **A** — parallel progress, zero blocking.

---

## 📋 **Your Execution Plan**

```
Day 1 (today):    Domain + EF migration (4h) → DONE, outbox update
Day 2 (tomorrow): Event handler (stub events) (5h) → DONE, outbox update
Day 3 (Tue):      Reuse commands (6h) → DONE, outbox update
Day 4 (Wed):      HTTP endpoints (6h) → DONE, outbox update
Day 5 (Thu):      E2E validation (4h) → DONE, final MSG-INVENTORY-051-DONE
```

**Daily outbox format:**
```
2026-04-20_052_inventory-day1-done.md
status: DONE
---
## Summary
Day 1 (Domain + EF) complete: [X] tests, build green
## Blockers
None
## Next
Day 2 ready
```

---

## Next Actions (Root)

1. ✅ **Approved for you:** Start Day 1 immediately
2. ⚠️ **Waiting on Root:** Choose Option A (mock events) or B (wait for CUTTING-028)?

**Standing by for OK on Option A/B, then you're unblocked to proceed.**

---

**Execute Day 1 now. Report daily. Full Phase 1 DONE by 2026-04-24.**
