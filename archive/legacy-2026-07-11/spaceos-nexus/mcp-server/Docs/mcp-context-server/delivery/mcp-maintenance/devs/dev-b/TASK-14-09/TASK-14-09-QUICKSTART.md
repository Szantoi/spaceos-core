---
id: DEV-B-TASK-14-09-QUICKSTART
title: "TASK-14-09 — Sampling & Argument Completion (Overview & Pre-Req)"
developer: Dev B
date: 2026-03-12
status: "✅ COMPLETE"
---

# TASK-14-09 Quickstart (Preview)

## 🎯 What This Task Does

Enables tools to ask LLMs for clarification when arguments are ambiguous — **tool delegation pattern**.

### Real-World Example

```
User: "Get me some context"
Agent calls: request_context({ filters: "ambiguous" })
Tool can't proceed → Asks LLM: "Which filters? [by-role, by-phase, by-status]"
LLM responds: "by-role"
Tool continues with clarified arguments
```

**Why?** Complex tools shouldn't fail on ambiguous input; instead, they ask back.

---

## 📋 Timeline

| Date | Phase | Hours | Status |
|------|-------|-------|--------|
| 2026-03-12/13 | TASK-14-08 (Days 1-2) | 6h | 🟢 DO NOW |
| 2026-03-14 | TASK-14-08 Complete | 2h | 🟢 DO NOW |
| **2026-03-14+** | **TASK-14-09 Starts** | **10h** | 🟡 **STARTS DAY 4** |
| 2026-03-15/16 | TASK-14-09 (Days 1-2) | 6h | 🟡 WEEKS OUT |
| 2026-03-17 | TASK-14-09 Complete | 4h | 🟡 WEEKS OUT |

**You're here now:** Finish TASK-14-08 first, then pivot to this on Friday.

---

## 📚 High-Level Architecture (Preview)

### Message Flow

```
1. Tool executing
2. Detects ambiguous argument
3. Calls context.requestSampling({ prompt, options })
4. Server queues request + waits (5s timeout)
5. LLM agent receives sampling request
6. Agent responds with selected option
7. Tool gets response + continues
```

### Protocol (Sketch)

```typescript
// Tool side: request clarification
const result = await context.requestSampling({
  prompt: "Which filters?",
  options: [
    { label: "by-role", value: "role" },
    { label: "by-phase", value: "phase" }
  ],
  timeout: 5000
});

if (result.error) {
  return { status: "error", needsClarification: true };
}

// Tool continues with clarified args
args.filters = result.selected;
```

---

## 🔧 What You'll Build (Days 4-6)

### Day 4 (2026-03-14, 3 hours)

- Design: Sampling protocol + state machine
- Sketch: RequestContext extension
- Sketch: Tool integration example (request_context)

### Days 5-6 (2026-03-15/16, 5 hours)

- Implement: RequestContext.requestSampling()
- Implement: Sampling state tracking in session
- Enhance: request_context tool with ambiguity detection
- Tests: Full coverage

### Day 7 (2026-03-17, 2 hours)

- Integration: Both transports (stdio + HTTP)
- E2E: Sampling flow end-to-end
- Sign-off: Implementation brief

---

## 📝 Acceptance Criteria (5 AC)

See `DEV-B-PHASE-2-TASK-ASSIGNMENT.md` for full AC details.

Quick summary:

- AC-1: Tool can request sampling
- AC-2: Response includes clarified arguments
- AC-3: Error handling ("needs_clarification" flag)
- AC-4: 5s timeout enforced
- AC-5: Example: request_context with ambiguous filters

---

## 🔗 When to Start Reading TASK-14-09 Docs

**Don't read the full spec yet** (it will confuse you while doing 14-08).

**When to read:**

- After TASK-14-08 design review (Wed 03-13 EOD)
- Before TASK-14-09 design phase (Thu 03-14 PM)

**Files to read then:**

- `EPIC-14-TASK-MATRIX.md` lines 370-410 (full AC spec)
- `PLUGIN-SYSTEM-API-REFERENCE.md` — RequestContext interface
- Your own implementation brief from TASK-14-02 code

---

## 📞 Questions for Tech Lead (Ask on Wed 03-13)

Before pivoting to TASK-14-09, clarify:

1. **Sampling timeout:** Should it be configurable per-tool or fixed 5s?
2. **Error recovery:** If sampling fails, should tool retry or fail immediately?
3. **LLM integration:** Who implements the "agent receives sampling request" part? (Is it in 14-11 E2E tests?)
4. **State persistence:** Does sampling state persist across transport disconnects?

---

## ✅ Pre-Work Checklist (For Later)

When you start TASK-14-09 (after 14-08), you'll need:

- [ ] Read `EPIC-14-TASK-MATRIX.md` (full TASK-14-09 spec)
- [ ] Review RequestContext interface in PLUGIN-SYSTEM-API-REFERENCE.md
- [ ] Review your request_context tool implementation
- [ ] Sketch sampling protocol state machine
- [ ] Design RequestContext.requestSampling() signature

---

**For now:** Focus on TASK-14-08 (Resource Templates).

When TASK-14-08 is done (Thursday EOD), this quickstart becomes your Day 1 guide.

See you Friday! 🚀
