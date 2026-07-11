---
processed: 2026-07-10
id: MSG-DESIGNER-002
from: conductor
to: designer
type: task
priority: medium
status: READ
model: haiku
ref: MSG-FRONTEND-107
epic_id: EPIC-DOORSTAR-SOFTLAUNCH
estimated_nwt: 60
created: 2026-07-10
content_hash: 8a72d63aa7dc83adef32edafa451e651855273eec5383fbf6e710635f5856f43
---

# Doorstar Mobile Kiosk — UX Accessibility Audit

**Epic:** EPIC-DOORSTAR-SOFTLAUNCH
**Priority:** MEDIUM (parallel track, non-blocking)
**Estimated:** 60 NWT (~2 hours)
**Frontend Ref:** MSG-FRONTEND-107 (DONE 2026-07-10, 15 files)

---

## 🎯 OBJECTIVE

Audit the Doorstar Production UI for **mobile accessibility (WCAG 2.1 AA)**, **touch usability**, and **UX flow consistency** BEFORE integration testing (2026-07-15).

**Parallel Track:** This audit runs parallel to Backend implementation and provides **early feedback** to ensure production-ready UX quality.

---

## 📋 SCOPE

### 1. Touch Target Audit

**WCAG 2.1 AA Requirement:** Minimum 44×44 CSS pixels (ideally 48×48px)

**Components to Audit:**
- [ ] **ProductionJobCard** (`ProductionJobCard.tsx`)
  - Card tap target (entire card clickable?)
  - Circle indicators (decorative or interactive?)
- [ ] **WorkflowStepStepper** (`WorkflowStepStepper.tsx`)
  - "Start" button size
  - "Done" button size
  - Step indicator tap targets (if clickable)
- [ ] **KioskMobileLayout** (`KioskMobileLayout.tsx`)
  - Back button size
  - Header button sizes

**Check CSS Modules:**
```css
/* ProductionJobCard.module.css */
.card {
  min-height: 48px; /* WCAG compliance */
  cursor: pointer;
}

/* WorkflowStepStepper.module.css */
.startButton, .doneButton {
  min-height: 56px; /* Touch-optimized */
  min-width: 120px;
}
```

**Deliverable:**
- List of components with tap target sizes
- Pass/Fail against WCAG 2.1 AA (44×44px minimum)
- Recommended adjustments (if any fail)

---

### 2. Color Contrast Audit

**WCAG 2.1 AA Requirement:** 4.5:1 contrast ratio (normal text), 3:1 (large text, UI components)

**Elements to Audit:**
- [ ] **6 STAGE circles** (grey/yellow/green on card background)
  - Status: Pending (grey), InProgress (yellow), Done (green)
  - Contrast ratio with white/light background
- [ ] **Overdue indicator** (red border on ProductionJobCard)
  - Red border contrast with card background
  - Overdue text (if present) contrast
- [ ] **WorkflowStepStepper** (current step highlighted yellow)
  - Yellow background + black text contrast
  - Step numbers/labels contrast
- [ ] **Button text** ("Start", "Done")
  - Button background + text contrast

**Tools:**
- Chrome DevTools (Contrast ratio checker)
- WebAIM Contrast Checker (https://webaim.org/resources/contrastchecker/)

**Deliverable:**
- Contrast ratio table (element → ratio → pass/fail)
- Recommended color adjustments (if any fail)

---

### 3. Photo Upload UX Flow

**Component:** WorkflowStepStepper → "Összeszerelés" step

**User Story:**
> "Amikor az összeszerelés step DONE gombra kattintok, fotót kell feltöltenem a műveletről."

**Audit Points:**
- [ ] Is photo upload **required** or **optional**?
  - Frontend code: `MSG-FRONTEND-107` mentions "opcionális fotó upload"
  - Backend spec: `MSG-BACKEND-194` says "Photo upload REQUIRED for Összeszerelés step"
  - **Conflict?** → Clarify and align
- [ ] Photo upload UI location:
  - Before "Done" button? (blocking)
  - After "Done" button click? (modal/inline)
  - Separate "Upload Photo" button? (non-blocking)
- [ ] Photo preview after upload?
  - User sees uploaded photo before confirming "Done"?
  - Can user delete/re-upload?
- [ ] Error handling:
  - What if upload fails? (network error)
  - What if photo too large? (size limit)
  - What if wrong file format? (JPEG/PNG only?)

**Deliverable:**
- UX flow diagram (photo upload step-by-step)
- Recommended improvements (if flow unclear)
- Validation rules documentation (file size, format, required/optional)

---

### 4. Visual Hierarchy & Feedback

**ProductionQueuePage Audit:**
- [ ] **Overdue projects** stand out?
  - Red border visibility (sufficient indicator?)
  - Alternative: Red icon, badge, or text label?
- [ ] **Filter buttons** (Összes/Várakozik/Folyamatban/Kiszállítható)
  - Active filter state clear? (underline, background, border?)
  - Filter label legibility (font size, contrast)
- [ ] **Grid layout** responsiveness
  - Card spacing on mobile (4-column grid → 1-column on mobile?)
  - Scroll performance (large list of 50+ projects)

**WorkflowStepStepper Audit:**
- [ ] **Current step highlighted** (yellow background)
  - Highlight visibility (contrast with grey steps)
  - Step number/label legibility
- [ ] **Progress bar** (X / 6 steps)
  - Bar visibility, size
  - Percentage display (if applicable)
- [ ] **Optimistic UI feedback**
  - Button disabled state after click?
  - Loading spinner during API call?
  - Success/error feedback (toast notification?)

**Deliverable:**
- Visual hierarchy assessment (pass/fail)
- Recommended improvements (icons, labels, feedback)

---

### 5. Mobile Kiosk Mode Testing

**KioskMobileLayout Audit:**
- [ ] **Full-screen support**
  - Does layout adapt to full-screen mode? (iOS Safari, Android Chrome)
  - Back button always accessible? (sticky header)
- [ ] **Landscape orientation**
  - Does UI work in landscape mode? (tablets)
  - Card grid adapts? (2-column layout?)
- [ ] **Touch gestures**
  - Swipe-to-navigate supported? (if applicable)
  - Pull-to-refresh disabled? (kiosk mode shouldn't reload)

**Deliverable:**
- Kiosk mode compatibility report (iOS/Android)
- Landscape orientation screenshots (if applicable)
- Recommended adjustments (if layout breaks)

---

### 6. User Testing Script

**Scenario:** Műhelyvezető (workshop manager) uses mobile kiosk to track production

**Test Flow:**
1. Open `/production/jobs` (ProductionQueuePage)
2. Filter: "Folyamatban" (In Progress)
3. Tap overdue project (red border card)
4. Navigate to detail page (ProductionJobDetailPage)
5. Tap "Start" on "Megmunkálás" step
6. Tap "Done" on "Megmunkálás" step
7. Navigate to "Összeszerelés" step
8. Upload photo
9. Tap "Done"
10. Verify real-time update (SSE event)

**Deliverable:**
**File:** `docs/testing/doorstar-mobile-kiosk-user-test-script.md`

**Content:**
- Step-by-step test script
- Expected outcomes (UI state changes)
- Edge cases (photo upload fail, network error)
- Accessibility checkpoints (screen reader, keyboard navigation)

---

## ✅ ACCEPTANCE CRITERIA

### Touch Targets
- [ ] All interactive elements ≥44×44px (WCAG 2.1 AA)
- [ ] Buttons ≥56×56px (touch-optimized)
- [ ] No tap target failures

### Color Contrast
- [ ] All text ≥4.5:1 contrast ratio
- [ ] UI components ≥3:1 contrast ratio
- [ ] Overdue indicator clearly visible

### Photo Upload UX
- [ ] Upload flow documented (step-by-step)
- [ ] Required/optional clarified (align Frontend + Backend)
- [ ] Error handling defined

### Visual Hierarchy
- [ ] Overdue projects stand out
- [ ] Active filter state clear
- [ ] Current step highlighted (yellow)

### Kiosk Mode
- [ ] Full-screen layout tested
- [ ] Landscape orientation works
- [ ] Touch gestures appropriate

### User Testing Script
- [ ] Test script created
- [ ] Edge cases documented
- [ ] Accessibility checkpoints included

---

## 📁 REFERENCES

| Document | Location |
|----------|----------|
| Frontend UI DONE | MSG-FRONTEND-107 (2026-07-10, 15 files) |
| Frontend Components | `datahaven-web/client/src/components/` |
| Frontend Pages | `datahaven-web/client/src/pages/` |
| CSS Modules | `*.module.css` (same folders) |
| Backend Spec | MSG-BACKEND-194 (photo upload required?) |
| 6 STAGE Workflow | Szabászat → Megmunkálás → Felületkezelés → Összeszerelés → Csomagolás → Kiszállítható |
| WCAG 2.1 AA | https://www.w3.org/WAI/WCAG21/quickref/ |

---

## 🚀 DELIVERABLES

### 1. UX Audit Report
**File:** `/opt/spaceos/terminals/designer/outbox/MSG-DESIGNER-002-DONE.md`

**Content:**
- Touch target audit results (pass/fail table)
- Color contrast audit results (ratio table)
- Photo upload UX flow diagram
- Visual hierarchy assessment
- Kiosk mode compatibility report
- Recommended improvements (if any)

### 2. User Testing Script
**File:** `docs/testing/doorstar-mobile-kiosk-user-test-script.md`

**Content:**
- Step-by-step test flow (10 steps)
- Expected outcomes
- Edge cases (photo upload fail, network error)
- Accessibility checkpoints (screen reader, keyboard)

---

## ⏱️ TIMELINE

**Start:** 2026-07-11 AM (parallel to Backend Day 2)
**ETA:** 2026-07-11 EOD (~2 hours)

**Parallel Coordination:**
- Frontend UI already DONE (MSG-FRONTEND-107)
- Backend implementation in progress (MSG-BACKEND-196, Day 1-4)
- UX audit provides feedback BEFORE integration testing (2026-07-15)

**Non-Blocking:** If audit delayed, integration testing proceeds (low risk)

---

## 🎯 SUCCESS METRICS

- [ ] Audit completed in 60 NWT (~2 hours)
- [ ] WCAG 2.1 AA compliance verified (or issues documented)
- [ ] Photo upload UX flow clarified (required/optional aligned)
- [ ] User testing script created
- [ ] No critical UX blockers found

---

📋 Conductor — MSG-DESIGNER-002 Task Assignment (2026-07-10)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
