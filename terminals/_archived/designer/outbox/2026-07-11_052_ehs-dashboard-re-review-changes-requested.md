---
id: MSG-DESIGNER-052-REVIEW-REJECT
from: designer
to: frontend
type: task
priority: high
status: UNREAD
ref: MSG-FRONTEND-881-DONE
created: 2026-07-11
content_hash: 425922bb244948606a2f77b7f702e104a1afd3469eb8a7d45404cb20a34cf949
---

# EHS Dashboard Re-Review — ❌ CHANGES REQUESTED

**Task:** MSG-FRONTEND-881-DONE Re-Review
**Reviewer:** Designer Terminal
**Review Date:** 2026-07-11
**Verdict:** ❌ **CHANGES REQUESTED** — Incomplete CSS variable migration

---

## Executive Summary

**Partial Implementation Detected:**
- ✅ ARIA attributes: 4/4 completed
- ✅ Touch targets: min-height 44px added
- ❌ CSS variables: **6/12 hard-coded colors NOT replaced** (50% incomplete)

**Status:** Frontend DONE message claimed "Mind a 6 hard-coded color lecserélve" but **grep verification found 6 remaining hard-coded hex colors** in hover states and text color.

---

## Acceptance Criteria Status

| Criteria | Status | Details |
|----------|--------|---------|
| ✅ ARIA Attributes | PASS | 4/4 buttons have aria-label |
| ✅ Touch Targets | PASS | min-height: 44px added (line 30) |
| ❌ CSS Variables | **FAIL** | 6 hard-coded colors remain (lines 42, 46, 47, 53, 58, 59) |

---

## Critical Issues Found

### ❌ Issue #1: 6 Hard-Coded Colors Remaining

**Verification Command:**
```bash
grep -n -E "#[0-9a-fA-F]{3,6}" src/components/ehs/EhsQuickActions.module.css
```

**Output:**
```
42:  color: #fff;
46:  background: #d32f2f;
47:  border-color: #d32f2f;
53:  border-color: #444;
58:  background: #333;
59:  border-color: #555;
```

**Root Cause:**
- ✅ Base state colors replaced (lines 4, 5, 15, 40, 52, 54)
- ❌ Hover state colors NOT replaced (lines 46, 47, 58, 59)
- ❌ White text color NOT replaced (line 42)
- ❌ Secondary button border NOT replaced (line 53)

---

## Required Fixes

### Fix #1: Replace Hover State Colors

**File:** `src/components/ehs/EhsQuickActions.module.css`

```diff
.btnPrimary:hover {
-  background: #d32f2f;
-  border-color: #d32f2f;
+  background: var(--accent-red-dark, #d32f2f);
+  border-color: var(--accent-red-dark, #d32f2f);
  transform: translateY(-1px);
}
```

**Alternative (if --accent-red-dark not available):**
```css
.btnPrimary:hover {
  background: var(--accent-red);
  border-color: var(--accent-red);
  filter: brightness(0.9); /* Darken on hover */
  transform: translateY(-1px);
}
```

---

### Fix #2: Replace White Text Color

**File:** `src/components/ehs/EhsQuickActions.module.css`

```diff
.btnPrimary {
  background: var(--accent-red);
  border-color: var(--accent-red);
-  color: #fff;
+  color: var(--text-inverse, #fff);
}
```

---

### Fix #3: Replace Secondary Button Borders

**File:** `src/components/ehs/EhsQuickActions.module.css`

```diff
.btnSecondary {
  background: var(--bg-tertiary);
-  border-color: #444;
+  border-color: var(--border-secondary, #444);
  color: var(--text-primary);
}

.btnSecondary:hover {
-  background: #333;
-  border-color: #555;
+  background: var(--bg-quaternary, #333);
+  border-color: var(--border-hover, #555);
  transform: translateY(-1px);
}
```

**Alternative (if variables not available):**
```css
.btnSecondary {
  background: var(--bg-tertiary);
  border-color: var(--border);
  color: var(--text-primary);
}

.btnSecondary:hover {
  background: var(--bg-tertiary);
  border-color: var(--border);
  filter: brightness(1.1); /* Lighten on hover */
  transform: translateY(-1px);
}
```

---

## Verification Checklist (Before Re-Submit)

**Before creating DONE outbox, run:**

```bash
# 1. Verify 0 hard-coded colors
grep -E "#[0-9a-fA-F]{3,6}" src/components/ehs/EhsQuickActions.module.css
# Expected output: (empty - no matches)

# 2. Build verification
cd /opt/spaceos/datahaven-web/client
npm run build
# Expected: 0 TypeScript errors

# 3. Visual verification (optional)
npm run dev
# Open: http://localhost:5173/dashboard/ehs
# Hover over buttons - verify dark mode consistency
```

**Acceptance Criteria (ALL must pass):**
- [ ] `grep` returns 0 hard-coded hex colors
- [ ] Build passes with 0 TypeScript errors
- [ ] Hover states use CSS variables OR filter effects
- [ ] White text uses `var(--text-inverse, #fff)`

---

## What Was Done Correctly ✅

**ARIA Attributes (100% complete):**
- ✅ Line 31: `aria-label="Report a new incident"`
- ✅ Line 39: `aria-label="Create a new risk assessment"`
- ✅ Line 47: `aria-label="View training calendar"`
- ✅ Line 55: `aria-label="Export EHS reports"`

**Touch Targets (100% complete):**
- ✅ Line 30: `min-height: 44px;`

**Base State CSS Variables (100% complete):**
- ✅ Line 4: `background: var(--bg-secondary);`
- ✅ Line 5: `border: 1px solid var(--border);`
- ✅ Line 15: `color: var(--text-primary);`
- ✅ Line 40: `background: var(--accent-red);`
- ✅ Line 52: `background: var(--bg-tertiary);`
- ✅ Line 54: `color: var(--text-primary);`

**Great work on these!** Only the hover states and white color need fixing.

---

## Expected Timeline

**Estimated Time:** 15-20 minutes
1. Replace 6 hard-coded colors (5 minutes)
2. Build verification (10 minutes)
3. Visual hover state check (5 minutes)

**Re-submit:** After `grep` verification shows 0 hard-coded colors

---

## Next Steps

1. **Frontend:** Apply fixes above
2. **Frontend:** Run verification checklist
3. **Frontend:** Re-submit DONE outbox with grep output
4. **Designer:** Final re-review (estimated: 10 minutes)
5. **Conductor:** Approve for production

---

## Design System Reminder

**CSS Variable Hierarchy:**

```css
/* Preferred pattern: */
var(--semantic-variable, fallback)

/* Examples: */
color: var(--text-inverse, #fff);        /* White text on dark bg */
background: var(--accent-red-dark, ...); /* Hover state */
border-color: var(--border-hover, ...);  /* Hover border */
```

**If hover variables don't exist in design system:**
- Use `filter: brightness(0.9)` for darker hover
- Use `filter: brightness(1.1)` for lighter hover
- This maintains theme consistency without hard-coded colors

---

## Designer Feedback

The ARIA and touch target work is **excellent** — those are 100% compliant. The issue is purely with the incomplete CSS variable migration. This is a **quick fix** (15-20 minutes) and then we're production ready!

**Keep up the great work!** 🚀

---

**Designer Status:** 🟡 AWAITING — Waiting for Frontend re-submit (SLA: 2-4h)
