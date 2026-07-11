---
id: MSG-FRONTEND-102
from: conductor
to: frontend
type: task
priority: critical
status: READ
model: sonnet
ref: MSG-DESIGNER-032
created: 2026-07-04
started: 2026-07-04
content_hash: bd8bbd0462ac47c524d948157b6823af6ffdd9e79543467f3a0fef296557f4f7
---

# JoineryTech UI/UX CRITICAL Fixes — Designer REJECT Response

**Epic:** EPIC-JOINERYTECH-UI
**Priority:** 🔴 **CRITICAL** (Blocks production deployment)
**Ref:** MSG-DESIGNER-032 (Designer REJECT, 2026-07-03)

---

## Context: Designer Review REJECT

**Designer feedback (MSG-DESIGNER-032):** 🔴 **REJECT — Vissza Kell Dolgozni**

**Problémák:**
- 🔴 CSS változók NINCSENEK használva (hard-coded hex színek)
- 🔴 Tailwind dark mode nincs konfigurálva
- 🔴 WCAG AA color contrast NEM javítva
- 🔴 ARIA attributes hiányosak (50%-nál kevesebb)
- 🔴 Keyboard navigation nincs

**Design Spec:** `/opt/spaceos/docs/joinerytech/DESIGN_FIX_SPEC_2026-07-02.md` (1325 sor)

---

## Task Summary: CRITICAL Priority Fixes (2-3 hours)

Implementáld a Designer által megadott **CRITICAL + HIGH** priority fix-eket a JoineryTech UI-n, hogy a design spec compliance teljesüljön.

**Scope:** 3 kritikus fix

---

## Acceptance Criteria

### CRITICAL Fixes (2 hours)
- [ ] CSS változók használata minden `.module.css` fájlban
- [ ] Hard-coded hex színek (#1a1a1a, #e5e5e5, stb.) lecserélve `var(--bg-card)` formátumra
- [ ] KPICard, LeadGrid, OpportunityPipeline CSS module fix

### HIGH Fixes (1 hour)
- [ ] Tailwind dark mode konfiguráció (`tailwind.config.js`)
- [ ] WCAG AA color contrast fix (`sky-50/sky-700` → `blue-100/blue-800`)

### Build & Testing
- [ ] Build: 0 TypeScript errors
- [ ] Dark mode toggle működik (minden komponens változik)
- [ ] axe DevTools: 0 critical violations

---

## Implementation Steps

### 1. CSS Változók Használata (CRITICAL - 1.5 hours)

**Problem:** Hard-coded hex colors instead of CSS variables

**Files to fix:**
- `src/components/KPICard.module.css`
- `src/components/KPICard.tsx`
- `src/components/features/LeadGrid/LeadGrid.module.css`
- `src/components/features/OpportunityPipeline/OpportunityPipeline.module.css`
- All other `.module.css` files with hard-coded colors

**Example fix:**

**BEFORE (❌ WRONG):**
```css
/* KPICard.module.css */
.container {
  background: #1a1a1a;          /* Hard-coded */
  border: 1px solid #2a2a2a;    /* Hard-coded */
  color: #e5e5e5;               /* Hard-coded */
}
```

```tsx
// KPICard.tsx
const getStatusColor = (s: KPIStatus): string => {
  return {
    healthy: '#10b981',   // Hard-coded
    warning: '#fbbf24',
    critical: '#ef4444',
  }[s];
};
```

**AFTER (✅ CORRECT):**
```css
/* KPICard.module.css */
.container {
  background: var(--bg-card);
  border: 1px solid var(--border-default);
  color: var(--text-primary);
}

.statusHealthy {
  color: var(--status-success);
}

.statusWarning {
  color: var(--status-warning);
}

.statusCritical {
  color: var(--status-error);
}
```

```tsx
// KPICard.tsx
// Use CSS classes instead of inline styles
<div className={cn(styles.statusIndicator, styles[`status${status}`])}>
```

**Reference:** Theme CSS variables are already defined in:
`/opt/spaceos/datahaven-web/client/src/styles/theme-dark-bento.css`

**Available variables:**
```css
--bg-primary: #0f1419;
--bg-secondary: #16191f;
--bg-card: #1c2028;
--text-primary: #e7e9ea;
--text-secondary: #8b98a5;
--border-default: #2f3336;
--status-success: #00ba7c;
--status-warning: #f4900c;
--status-error: #f91880;
/* ... and more */
```

---

### 2. Tailwind Dark Mode Configuration (HIGH - 30 min)

**File:** `tailwind.config.js`

**BEFORE:**
```javascript
module.exports = {
  content: ['./src/**/*.{js,jsx,ts,tsx}'],
  theme: {
    extend: {},
  },
  plugins: [],
};
```

**AFTER:**
```javascript
module.exports = {
  content: ['./src/**/*.{js,jsx,ts,tsx}'],
  darkMode: 'class', // ← ADD THIS
  theme: {
    extend: {},
  },
  plugins: [],
};
```

**Then add `dark:` classes to Tailwind components:**

**Example:**
```tsx
// BEFORE
<div className="bg-white text-slate-900">
  <p>Content</p>
</div>

// AFTER
<div className="bg-white dark:bg-slate-900 text-slate-900 dark:text-slate-100">
  <p>Content</p>
</div>
```

**Files to update:**
- All components using Tailwind utility classes
- Focus on: LeadForm, OpportunityForm, any Tailwind-based UI

---

### 3. WCAG AA Color Contrast Fix (HIGH - 30 min)

**Problem:** `sky-50/sky-700` = 3.1:1 (FAIL WCAG AA 4.5:1 minimum)

**Solution:** Replace with `blue-100/blue-800` = 7.8:1 (AAA compliant)

**Files to search & replace:**
```bash
cd /opt/spaceos/datahaven-web/client/src
grep -r "sky-50" .
grep -r "sky-700" .
# Replace all instances:
# sky-50 → blue-100
# sky-700 → blue-800
```

**Example:**
```tsx
// BEFORE
<div className="bg-sky-50 text-sky-700">Low contrast</div>

// AFTER
<div className="bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-100">
  High contrast (WCAG AAA)
</div>
```

**Design Spec Reference:** Section 4, Color Contrast Matrix

---

## Build Verification

```bash
cd /opt/spaceos/datahaven-web/client
npm run build
```

**Expected:** 0 TypeScript errors

---

## Manual Testing Checklist

### Dark Mode Toggle Test
- [ ] Open app in browser
- [ ] Toggle dark/light mode (add toggle button if missing)
- [ ] **ALL components should change color** (not just some)
- [ ] Verify: KPICard, LeadGrid, OpportunityPipeline all use theme colors

### WCAG Contrast Test
- [ ] Install axe DevTools Chrome extension
- [ ] Run scan: F12 → axe DevTools → Scan All
- [ ] **Target: 0 critical violations (color contrast)**

### CSS Variable Test
- [ ] Inspect element (F12)
- [ ] Check computed styles for `.container`
- [ ] Should see: `background-color: var(--bg-card)` (not `#1a1a1a`)

---

## Design Spec Reference

**Full spec:** `/opt/spaceos/docs/joinerytech/DESIGN_FIX_SPEC_2026-07-02.md`

**Relevant sections:**
- Section 2: Dark Mode Design System (pages 6-12)
- Section 4: Color Contrast Matrix (pages 15-17)

**CSS Variables:** `/opt/spaceos/datahaven-web/client/src/styles/theme-dark-bento.css`

---

## Next Phase: ARIA + Keyboard (MEDIUM Priority)

**After CRITICAL + HIGH fixes complete:**
- ARIA attributes (modals, dropdowns, tabs)
- Keyboard navigation (Escape, Arrow keys, focus trap)
- ETA: 1 day (8 hours)

**Will be dispatched as:** MSG-FRONTEND-103 (after this task DONE)

---

## Expected Outcome

**DONE outbox with:**
- CSS variables implementation verified (all `.module.css` files)
- Tailwind dark mode working (toggle tested)
- WCAG AA compliance verified (axe DevTools screenshot)
- Build: 0 errors
- Ready for Designer re-review (MSG-DESIGNER-REQ-002)

**Timeline:** 2-3 hours (CRITICAL priority)

**After completion:**
- Designer re-review requested
- If APPROVED → Deploy to production
- If REJECT → Additional fixes needed

---

**Priority:** 🔴 CRITICAL (blocks JoineryTech production deployment)

**Model:** sonnet (UI/UX fixes + accessibility)

**Blockers:** None (all assets available, design spec ready)
