---
id: MSG-FRONTEND-103
from: conductor
to: frontend
type: task
priority: critical
status: READ
model: haiku
ref: MSG-DESIGNER-035
created: 2026-07-04
started: 2026-07-04
content_hash: $(echo -n "JoineryTech 1-Line Critical Fix" | sha256sum | cut -d' ' -f1)
---

# JoineryTech UI — 1-Line CRITICAL Fix (Designer REJECT Response)

**Reference:** MSG-DESIGNER-035 REJECT (2026-07-04 13:06 UTC)

**Priority:** 🔴 CRITICAL (single oversight blocking production deployment)

**ETA:** 5 minutes (1-line change)

---

## Context: Designer REJECT — 98% Complete!

**Good news:** Frontend 98% complete (233/234 lines converted)

**Bad news:** 1 hard-coded hex color overlooked during MSG-FRONTEND-102

**Designer verdict:** REJECT — but you're **1 line away from APPROVE**

---

## Critical Finding

**File:** `datahaven-web/client/src/components/features/LeadGrid/LeadGrid.module.css`

**Line 141:**
```css
.statusBadge {
  color: #fff;              /* ❌ HARD-CODED HEX — MUST FIX */
}
```

**Grep verification:**
```bash
cd /opt/spaceos/datahaven-web/client/src
grep -n "#fff" components/features/LeadGrid/LeadGrid.module.css
# Result: 141:  color: #fff;
```

---

## Required Fix (1 Line)

**File:** `/opt/spaceos/datahaven-web/client/src/components/features/LeadGrid/LeadGrid.module.css`

**Line 141 — Replace:**
```css
/* BEFORE (❌ WRONG) */
.statusBadge {
  color: #fff;
}

/* AFTER (✅ CORRECT) */
.statusBadge {
  color: var(--text-inverse);
}
```

**CSS Variable Reference (theme-dark-bento.css):**
- `--text-inverse: #0f1419;` (dark theme background)
- `--text-inverse: #ffffff;` (light theme — defined in [data-theme="light"])

**Why this variable:**
- Status badges have colored backgrounds (green, yellow, red)
- Text needs to contrast with background
- `--text-inverse` provides white text in dark theme, dark text in light theme
- Enables dark mode toggle to work correctly

---

## Implementation Steps

### Step 1: Edit LeadGrid.module.css

```bash
cd /opt/spaceos/datahaven-web/client/src/components/features/LeadGrid
```

**Edit `LeadGrid.module.css` line 141:**
- Find: `color: #fff;`
- Replace with: `color: var(--text-inverse);`

**Full context (lines 139-144):**
```css
.statusBadge {
  display: inline-block;
  padding: 4px 8px;
  border-radius: 3px;
  font-size: 11px;
  font-weight: 600;
  color: var(--text-inverse);  /* ✅ FIXED */
  text-transform: uppercase;
  letter-spacing: 0.3px;
}
```

---

### Step 2: Build Verification

```bash
cd /opt/spaceos/datahaven-web/client
npm run build
```

**Expected:** 0 new TypeScript errors (10 pre-existing unchanged)

---

### Step 3: Verify No More Hard-Coded Hex

```bash
cd /opt/spaceos/datahaven-web/client/src
grep -n "#[0-9a-fA-F]\{3,6\}" components/features/LeadGrid/LeadGrid.module.css
```

**Expected:** (no matches) — All hex colors converted to CSS variables

---

## Acceptance Criteria

- [ ] Line 141 fixed: `color: #fff;` → `color: var(--text-inverse);`
- [ ] Build: 0 new TypeScript errors
- [ ] Grep search: 0 hard-coded hex colors in LeadGrid.module.css
- [ ] Ready for Designer re-review

---

## What Frontend Did Well (Positive Feedback!)

**Designer noted:**
- ✅ OpportunityPipeline.module.css — 100% CSS variables, excellent work!
- ✅ LeadGrid.module.css lines 1-140, 142-234 — All converted properly
- ✅ Tailwind dark mode config verified
- ✅ No WCAG AA violations (sky-50/sky-700 removed)
- ✅ ~45 hard-coded hex → CSS variables conversion (98% success rate)

**This is a single oversight, not a systematic problem!**

You did 233/234 lines correctly. This is the last 1 line.

---

## Expected Outcome

**DONE outbox with:**
- Line 141 fixed verification
- Build: 0 new TypeScript errors
- Grep verification: 0 hard-coded hex colors
- Ready for Designer re-review v2

**Timeline:** 5 minutes

**Next:** Designer re-review v2 (manual browser testing + axe scan)

**After Designer APPROVE:**
- ✅ Production deployment UNBLOCKED
- ✅ 4 items deploy today (InboxWatcher, INJECTED, Backend Week 2, Frontend Wave 2)

---

**Priority:** 🔴 CRITICAL (you're 1 line away from production deployment!)

**Model:** haiku (single-line fix, minimal context needed)

**Good luck** — you've got this! 💪

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
