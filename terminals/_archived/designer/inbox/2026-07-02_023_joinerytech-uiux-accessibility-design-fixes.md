---
completed: 2026-07-03
id: MSG-DESIGNER-023
from: conductor
to: designer
type: task
priority: high
status: COMPLETED
model: haiku
ref: MSG-FRONTEND-089-DONE
created: 2026-07-02
content_hash: b81e15b75cfdd8f9ce109f28afe2e59023d6e29fc174f2bb3093c9e40dcd6cc5
---

# JoineryTech UI/UX + Accessibility Design Fixes

## Context

Frontend audit (MSG-FRONTEND-089-DONE) azonosította a **UI/UX és Accessibility** problémákat:

**🟡 UI/UX (P1 - Fontos):**
- Navigációs inkonzisztencia (világváltás szétszórt)
- Dark mode hiányzik
- Desktop layout inkonzisztens

**🟡 Accessibility (P1 - Compliance):**
- Színkontraszt: sky-50/sky-700 = 3.1:1 (WCAG AA fail)
- Billentyűzet kezelés hiányzik
- ARIA hiányok (aria-label, role, live regions)
- Focus trap nincs

**Audit Report:** `/opt/spaceos/docs/joinerytech/AUDIT_UI_PERFORMANCE_A11Y_2026-07-02.md`

---

## Task

Készíts **Design Spec + Component Library** javítási tervet a P1 UI/UX és Accessibility hibákhoz.

### Deliverables

#### 1. UI/UX Design Spec (2 nap)

**1.1 Navigációs Konzisztencia**

Problém: Világváltás szétszórt (Webshop `/shop`, Belső `/`, Settings `/settings`, Kioszk `/kiosk`)

**Javasolt megoldás:**
- Egységes top-level navigation
- Világok = sidebar kategóriák (Sales, Procurement, Manufacturing, stb.)
- Sub-screenek = tab-ok vagy card-ok
- Mobil: Bottom nav 5 world max + "More" overflow

**Deliverable:** Navigation architecture diagram (Figma vagy Markdown)

---

**1.2 Dark Mode Design System**

Jelenleg hiányzik. Tervezd meg a dark mode palettát:

```css
/* Light mode (jelenlegi) */
--bg-primary: #ffffff;
--bg-secondary: #f0f9ff; /* sky-50 */
--text-primary: #1e293b; /* slate-800 */
--text-secondary: #64748b; /* slate-500 */

/* Dark mode (tervezendő) */
--bg-primary: #0f172a; /* slate-900 */
--bg-secondary: #1e293b; /* slate-800 */
--text-primary: #f1f5f9; /* slate-100 */
--text-secondary: #cbd5e1; /* slate-300 */
```

**Deliverable:**
- Dark mode color palette (CSS variables)
- Component példák dark mode-ban (SlideOver, Modal, Card)
- Toggle design (Settings oldalon + navbar)

---

**1.3 Desktop Layout Konzisztencia**

Problém: Mobil jó (bottom nav, card-based), desktop szétszórt

**Javasolt pattern:**
- Sidebar navigation (left, sticky)
- Main content area (center, max-width 1280px)
- Detail pane (right, slide-in for drill-down)

**Deliverable:** Desktop layout wireframe (3 main patterns: List view, Detail view, Dashboard)

---

#### 2. Accessibility Fix Spec (1 nap)

**2.1 Színkontraszt Fix**

Jelenlegi probléma: sky-50 (`#f0f9ff`) / sky-700 (`#0369a1`) = **3.1:1 kontraszt** (WCAG AA fail, kell 4.5:1)

**Javasolt fix:**
- Light mode: sky-50 → white (`#ffffff`) / sky-700 → sky-800 (`#075985`) = **5.2:1** ✅
- Dark mode: slate-900 → slate-800 / slate-100 → white = **12.6:1** ✅

**Deliverable:** Color contrast matrix (all combinations tested against WCAG AA)

---

**2.2 Billentyűzet Navigáció Pattern**

Hiányzik:
- Tab order (focus visible)
- Escape to close (modals, slide-overs)
- Arrow keys (dropdown navigation)
- Enter/Space (button activation)

**Tervezendő komponensek:**
- `<FocusableButton>` — keyboard + click handling
- `<KeyboardNavigableList>` — arrow up/down
- `<ModalWithEscape>` — ESC trap handling

**Deliverable:** Keyboard interaction spec (komponensenként: Button, Modal, Dropdown, Tab, List)

---

**2.3 ARIA Attributes Pattern**

Hiányos elemek:
- `aria-label` (ikonok, gombok szöveg nélkül)
- `role` (custom UI komponensek)
- `aria-live` (toast notification, status updates)
- `aria-expanded` (dropdown state)

**Javasolt pattern:**
```jsx
<button
  aria-label="Close modal"
  aria-expanded={isOpen}
  onClick={handleClose}
>
  <CloseIcon />
</button>

<div role="alert" aria-live="polite">
  Order created successfully
</div>
```

**Deliverable:** ARIA attribute checklist (komponensenként: Modal, Dropdown, Button, Toast, Tab)

---

## Timeline

| Task | Duration | Deliverable |
|------|----------|-------------|
| **1. Navigation arch** | 4 óra | Navigation diagram (Figma/MD) |
| **2. Dark mode palette** | 6 óra | Color system + component examples |
| **3. Desktop layout** | 4 óra | 3 wireframes (List/Detail/Dashboard) |
| **4. Color contrast fix** | 2 óra | Contrast matrix (WCAG AA tested) |
| **5. Keyboard pattern** | 4 óra | Keyboard interaction spec |
| **6. ARIA spec** | 4 óra | ARIA attribute checklist |

**Total:** 24 óra (~3 nap)

---

## Output Format

**Document:** `/opt/spaceos/docs/joinerytech/DESIGN_FIX_SPEC_2026-07-02.md`

Tartalma:
1. UI/UX Design Fixes (Navigation + Dark mode + Desktop layout)
2. Accessibility Fixes (Color contrast + Keyboard + ARIA)
3. Component Library Update (dark mode variants, keyboard-friendly components)
4. Implementation Priority (P0 color contrast → P1 dark mode → P2 keyboard nav)

---

## Success Criteria

- ✅ Navigation architecture designed (1 consistent pattern)
- ✅ Dark mode color palette defined (CSS variables)
- ✅ Desktop layout wireframes created (3 patterns)
- ✅ Color contrast fixed (all WCAG AA compliant)
- ✅ Keyboard interaction spec complete (5+ components)
- ✅ ARIA attribute patterns documented (5+ components)

---

## References

- **Frontend Audit:** `/opt/spaceos/docs/joinerytech/AUDIT_UI_PERFORMANCE_A11Y_2026-07-02.md`
- **WCAG 2.1 Guidelines:** https://www.w3.org/WAI/WCAG21/quickref/
- **Tailwind Dark Mode:** https://tailwindcss.com/docs/dark-mode

---

**Generated:** 2026-07-02T18:05 UTC
**Terminal:** conductor
**Model:** Haiku (design spec + review task)
**Priority:** HIGH (P1 fixes coordination)

---

## Completion Report
*2026-07-03T09:42:30.256Z*

### Summary
Complete design specification for JoineryTech P1 UI/UX and Accessibility fixes - 6 deliverables: navigation architecture, dark mode system, desktop layouts, WCAG AA color matrix, keyboard interaction patterns, ARIA checklists. Design spec document ready for Frontend implementation (~24h effort).

### Implementation Details
Comprehensive design specification created with 1325 lines covering:
- Navigation: Unified 3-level hierarchy (Worlds → Tabs → Detail panels) with sidebar + bottom nav
- Dark Mode: Complete CSS variable system, Tailwind config, 10+ component examples
- Desktop Layouts: 3 wireframe patterns (List, Detail, Dashboard) with ASCII diagrams
- Color Contrast: WCAG AA compliant matrix, fixed sky-50/sky-700 (3.1:1 ❌) to blue-100/blue-800 (7.8:1 ✅)
- Keyboard: 5+ component patterns with TypeScript implementations (focus trap, arrow navigation, shortcuts)
- ARIA: 6 component checklists with correct/incorrect examples

Document location: /opt/spaceos/docs/joinerytech/DESIGN_FIX_SPEC_2026-07-02.md
Implementation phases: P0 (2h critical fixes), P1 (2d UI/UX), P2 (1d A11y compliance)

### Files Changed
- `docs/joinerytech/DESIGN_FIX_SPEC_2026-07-02.md`

### Next Steps
Frontend terminal can begin implementation:
1. Review design spec document
2. Start with Phase 0 critical fixes (2h): color contrast, focus indicators, modal keyboard trap
3. Implement Phase 1 UI/UX (2d): navigation, dark mode, layouts
4. Complete Phase 2 A11y (1d): ARIA attributes, keyboard nav
5. Test with axe DevTools + manual keyboard + screen reader validation

