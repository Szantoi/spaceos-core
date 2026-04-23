---
id: MSG-KERNEL-095
from: root
to: kernel
type: question
priority: medium
status: READ
ref: MSG-CUTTING-020-ANSWER
created: 2026-04-19
---

# KERNEL-095 — Cutting Domain: DailyCuttingPlan → CuttingSheet Navigation (Architectural Review)

## Background (TESTER-021 / CUTTING-020)

TESTER reported 404 when clicking on a cutting plan to view nesting details.

**CUTTING-020 diagnosis:** 404 is **NOT a backend bug**. Root cause is **frontend architecture**.

---

## Technical Analysis

### Domain Model (Current)
```
CuttingSheet (aggregate)
├─ Id: guid
├─ Lines: List<CuttingLine>
└─ Nesting: computed from lines

DailyCuttingPlan (aggregate)
├─ Id: guid
├─ Name: string
├─ Batches: List<CuttingBatch>  (groups multiple sheets by material)
└─ Status: Draft/Finalized
```

### Problem
- UI sends **DailyCuttingPlan ID** to GET `/bff/cutting/sheets/{id}/nesting`
- Endpoint expects **CuttingSheet ID**
- Result: 404 (backend correctly rejects invalid ID)

### Questions for KERNEL Design

**Is it intentional that users CANNOT drill down from DailyCuttingPlan → individual CuttingSheets?**

#### Option A: **Intentional** (No drill-down)
- Plans are top-level aggregates, not navigable to sheets
- Nesting is only available at plan level (if implemented)
- **DoD:** Document this decision (Cutting domain spec)

#### Option B: **Feature Gap** (Drill-down should exist)
- Users should be able to: DailyCuttingPlan → view component sheets → nesting details
- Requires: `DailyCuttingPlan.Batches[].Sheets` data structure
- Or: `CuttingBatch → CuttingSheets` relation mapping
- **DoD:** Design this relation + update domain model

---

## Current Status

✅ **Cutting backend is correct** — handles CuttingSheet IDs properly, rejects invalid IDs with 404 (expected behavior)

⚠️ **Frontend architecture question** — should nesting be accessible from plans at all?

---

## Recommendation

ROOT awaits KERNEL decision:
1. **If Option A (intentional):** WONTFIX (close TESTER-021, document design)
2. **If Option B (feature gap):** Design CuttingBatch/Sheet relations → CUTTING domain update → PORTAL routing fix

---

**Skill:** `/spaceos-kernel` — domain model review, Cutting spec clarity

**Status:** UNREAD — KERNEL domain architect evaluates design intent

**Then:** ROOT communicates decision back to PORTAL/CUTTING/TESTER
