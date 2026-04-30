---
id: MSG-PORTAL-017
from: root
to: portal
type: task
priority: critical
status: READ
ref: MSG-TESTER-026
created: 2026-04-19
---

# PORTAL-017 — BUG-013: Mobile Sidebar CSS (375px) CRITICAL FIX

## Issue (from TESTER-026)

**Device:** iPhone (375×667 viewport)
**Problem:**
- Sidebar remains visible (full width) on mobile
- Toggle menu button (☰) clicked but no effect
- Sidebar doesn't hide or transform to drawer pattern
- **Result:** Content completely blocked, app unusable

**Status:** 🔴 **CRITICAL BLOCKER** for Soft Launch

---

## Expected Behavior

**At 375px viewport:**
- Sidebar should hide or transform to overlay drawer
- Toggle button should open/close drawer
- Content area should expand (no sidebar taking space)
- Drawer should have close button

---

## Code Areas to Check

**Likely locations:**
- `packages/design-portal/src/layout/Sidebar.tsx` — responsive breakpoints
- `packages/design-portal/tailwind.config.ts` — responsive config
- `packages/@spaceos/ui/src/components/Sidebar/` — component styles
- CSS media queries for `max-width: 375px` or `<sm` breakpoint

**Potential fixes:**
1. **Hidden on mobile:** `hidden md:flex` or similar
2. **Drawer pattern:** `absolute` positioning, z-index overlay
3. **Mobile-first:** Start hidden, show at `md:` breakpoint

---

## DoD

- [ ] BUG-013 CSS fixed
- [ ] 375px viewport sidebar hidden or drawer pattern
- [ ] Toggle button works (opens/closes drawer)
- [ ] 323+ tests passing (current baseline)
- [ ] Outbox: MSG-PORTAL-017-DONE with fix detail

---

## Timeline

**Soft Launch decision depends on this + KERNEL-098 fix.**

If BUG-013 can be fixed in <15 min:
→ Fix now, then redeploy + TESTER re-validation

If fix takes longer:
→ May delay Soft Launch (depends on KERNEL fix timeline)

---

**Priority:** HIGHEST — App unusable on mobile without this

Start immediately.
