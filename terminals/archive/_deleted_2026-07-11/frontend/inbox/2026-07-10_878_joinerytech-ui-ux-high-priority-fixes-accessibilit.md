---
completed: 2026-07-10
id: MSG-FRONTEND-878
from: designer
to: frontend
type: task
priority: high
status: COMPLETED
model: sonnet
created: 2026-07-10
content_hash: 39b2438b8979986c0dbb3cb24cee8986f1661fb0d74ade3173a30245b61b113c
---

# JoineryTech UI/UX — High Priority Fixes (Accessibility + Touch Targets)

# JoineryTech UI/UX Audit — High Priority Fixes

**Context:** Designer audit completed on JoineryTech codebase (CRM, Kontrolling, Navigation)  
**Overall Score:** 7.5/10 — Production-ready with minor fixes needed  
**Total Effort:** ~4 hours  

---

## 🔴 Critical Issues (HIGH Priority)

### 1. Accessibility Compliance (WCAG 2.1 AA)
**Effort:** 2-3 hours  
**Files:**
- `src/components/Layout/JoineryTechHeader.tsx`
- `src/pages/CRMLeadsPage.tsx`

**Required Changes:**

#### a) Navigation ARIA Labels (JoineryTechHeader.tsx:23-92)
```tsx
// BEFORE:
<Link to="/dashboard/crm/leads">CRM</Link>

// AFTER:
<Link 
  to="/dashboard/crm/leads"
  aria-label="Navigate to CRM module"
  aria-current={isActive('/crm') ? 'page' : undefined}
>
  CRM
</Link>
```

Apply to all 7 nav links (CRM, Kontrolling, HR, Maintenance, QA, EHS, DMS).

#### b) Connection Status Indicator (JoineryTechHeader.tsx:96-103)
```tsx
// ADD:
<div
  role="status"
  aria-live="polite"
  aria-label={isConnected ? 'Connected to server' : 'Connection lost'}
  className={`w-2 h-2 rounded-full ${...}`}
/>
```

#### c) Modal Accessibility (CRMLeadsPage.tsx)
```tsx
// ADD to modal wrapper:
<div 
  className={styles.modal}
  role="dialog"
  aria-modal="true"
  aria-labelledby="create-lead-title"
>
  <div className={styles.modalContent}>
    <h2 id="create-lead-title">Create New Lead</h2>
```

**Test:**
- [ ] Screen reader announces nav links correctly
- [ ] Tab navigation works (no focus traps outside modal)
- [ ] Escape key closes modal
- [ ] Connection status announced when changes

---

### 2. Touch Target Validation (iOS Guideline: 44px min)
**Effort:** 1 hour  
**Files:**
- `src/pages/CRMLeadsPage.module.css`
- `src/components/Layout/JoineryTechHeader.tsx`

**Required Changes:**

#### a) CRM Primary Button (CRMLeadsPage.module.css:31-45)
```css
/* BEFORE: */
.primaryBtn {
  padding: 10px 20px;  /* = 40px height */
}

/* AFTER: */
.primaryBtn {
  padding: 12px 20px;  /* = 44px height */
  min-height: var(--touch-target-min, 44px);
}
```

#### b) Navigation Links (JoineryTechHeader.tsx:23-92)
```tsx
// VERIFY all nav links have:
className="px-3 py-2"  // Should be py-3 for 44px
```

**Test on Mobile (Chrome DevTools):**
- [ ] All buttons ≥44px height
- [ ] Nav links ≥44px height
- [ ] Form inputs ≥44px height

---

### 3. Hard-Coded Color Removal
**Effort:** 15 minutes  
**Files:**
- `src/pages/CRMLeadsPage.module.css`
- `src/components/Layout/JoineryTechHeader.tsx`

**Required Changes:**

#### a) CRMLeadsPage.module.css:36
```css
/* BEFORE: */
color: #fff;

/* AFTER: */
color: var(--text-inverse, #fff);
```

#### b) JoineryTechHeader.tsx:27, 37, 47, etc.
```tsx
// BEFORE:
'bg-[var(--accent)] text-white'

// AFTER:
'bg-[var(--accent)] text-[var(--text-inverse)]'
```

**Verify:**
- [ ] No hard-coded hex colors in CSS/TSX (grep for `#fff`, `#000`)
- [ ] Theme toggle works (if implemented)

---

## ✅ Acceptance Criteria

1. **Accessibility:**
   - [ ] All nav links have `aria-label` and `aria-current`
   - [ ] Connection status has `role="status"` and `aria-live`
   - [ ] Modal has `role="dialog"`, `aria-modal`, `aria-labelledby`
   - [ ] Keyboard navigation tested (Tab, Enter, Escape)

2. **Touch Targets:**
   - [ ] CRM primaryBtn ≥44px height
   - [ ] All nav links ≥44px height
   - [ ] All interactive elements ≥44px

3. **Theme Consistency:**
   - [ ] No hard-coded `#fff` or `#000` in CSS/TSX
   - [ ] All colors use CSS variables
   - [ ] Build passes (0 TypeScript errors)

4. **Testing:**
   - [ ] Chrome DevTools mobile emulation (360px width)
   - [ ] Screen reader test (VoiceOver/NVDA)
   - [ ] Keyboard-only navigation test

---

## 📚 References

- **Design Audit Report:** (Available in chat session with Designer)
- **WCAG 2.1 AA:** https://www.w3.org/WAI/WCAG21/quickref/
- **iOS Touch Guidelines:** 44pt minimum target size
- **SpaceOS Design System:** `/opt/spaceos/terminals/designer/CLAUDE.md`

---

## 🎯 Priority & Timing

**Priority:** HIGH  
**Estimated Effort:** ~4 hours  
**Recommended Sprint:** Next sprint (Week 2)  
**Designer Review:** Required after implementation (2-4h SLA)

---

**Notes:**
- After completion, send DONE outbox → Designer for review
- Designer will verify accessibility + touch targets
- If approved, mark as production-ready

## Acceptance Criteria

- [ ] All nav links have aria-label and aria-current attributes
- [ ] Connection status indicator has role=status and aria-live
- [ ] Modal has proper dialog ARIA attributes
- [ ] CRM primaryBtn is ≥44px height
- [ ] All navigation links are ≥44px height
- [ ] No hard-coded #fff or #000 in CSS/TSX files
- [ ] Build passes with 0 TypeScript errors
- [ ] Keyboard navigation tested (Tab, Enter, Escape)
- [ ] Screen reader announces elements correctly
- [ ] Mobile touch targets verified in Chrome DevTools

---

## Completion Report
*2026-07-10T20:37:55.360Z*

### Summary
Implemented all high-priority UI/UX fixes: Accessibility (ARIA labels, modal attributes), Touch targets (44px min), Hard-coded color removal

### Implementation Details
## Accessibility Fixes\n\n**JoineryTechHeader.tsx:**\n- Added `role=\"navigation\"` and `aria-label=\"JoineryTech modules\"` to nav element\n- All 7 nav links now have:\n  - `aria-label=\"Navigate to [Module] module\"`\n  - `aria-current={isActive(...) ? 'page' : undefined}`\n- Connection status indicator now has:\n  - `role=\"status\"`\n  - `aria-live=\"polite\"`\n  - `aria-label` based on connection state\n\n**CRMLeadsPage.tsx:**\n- Modal wrapper now has:\n  - `role=\"dialog\"`\n  - `aria-modal=\"true\"`\n  - `aria-labelledby=\"create-lead-title\"`\n  - Escape key handler to close modal\n  - Click outside to close\n\n## Touch Target Fixes (44px min)\n\n**JoineryTechHeader.tsx:**\n- Nav links: `py-2` → `py-3` + `min-h-[44px]` + `flex items-center`\n\n**CRMLeadsPage.module.css:**\n- `.primaryBtn`: padding 10px → 12px + `min-height: 44px`\n- `.submitBtn`: padding 10px → 12px + `min-height: 44px`\n- `.cancelBtn`: padding 10px → 12px + `min-height: 44px`\n\n## Hard-Coded Color Removal\n\n- `text-white` → `text-[var(--text-inverse)]` in header\n- `color: #fff` → `color: var(--text-inverse, #fff)` in CSS\n- All status badge colors now use CSS variables with fallbacks\n\n## Build Status\n✅ 0 TypeScript errors\n✅ 2376 modules transformed\n✅ Built in 18.30s

### Files Changed
- `datahaven-web/client/src/pages/CRMLeadsPage.tsx`
- `datahaven-web/client/src/pages/CRMLeadsPage.module.css`
- `datahaven-web/client/src/components/Layout/JoineryTechHeader.tsx`

