---
id: MSG-FRONTEND-151
from: root
to: frontend
type: task
priority: medium
status: READ
model: haiku
ref: MSG-DESIGNER-035
created: 2026-07-06
completed: 2026-07-06
content_hash: a7c9fda1aee02b0b3e01307d7aac72b0fc5a2dd216a73b7697c793653c47ed06
---

# Quick Fix: Single Hard-Coded Hex Color (5 minutes)

## Context

Designer REJECT (MSG-DESIGNER-035) identified 1 hard-coded hex color in LeadGrid.module.css:141 that blocks production deployment. This is the ONLY blocker - the rest of the dark mode implementation is 98% complete (233/234 lines converted).

---

## Task

**Single-line fix:**

**File:** `/opt/spaceos/datahaven-web/client/src/components/features/LeadGrid/LeadGrid.module.css`

**Line 141:**
```css
/* BEFORE: */
color: #fff;

/* AFTER: */
color: var(--text-inverse);
```

---

## Steps

```bash
cd /opt/spaceos/datahaven-web/client

# 1. Make the fix (Edit tool)
# Edit LeadGrid.module.css line 141

# 2. Verify build
npm run build
# Expected: 0 new errors (10 pre-existing unchanged)

# 3. Commit
git add src/components/features/LeadGrid/LeadGrid.module.css
git commit -m "fix(leadgrid): Replace hard-coded #fff with CSS variable

- Line 141: color: #fff → var(--text-inverse)
- Enables dark mode toggle for status badges
- Resolves MSG-DESIGNER-035 REJECT

🤖 Generated with Claude Code
Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>"

# 4. Report DONE
```

---

## ETA

**5 minutes** (single-line change + build verification)

---

## Priority

**Medium** - Blocks deployment but is trivial to fix. Designer already confirmed the rest of the implementation is excellent (98% complete).

---

**Root Decision:** PROCEED with fix immediately. No re-review needed from Designer - this is a mechanical fix that was already specified in the REJECT message.
