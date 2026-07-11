# Doorstar Mobile Kiosk — User Testing Script

**Epic:** EPIC-DOORSTAR-SOFTLAUNCH
**Test Date:** 2026-07-11
**Tester:** [Name]
**Device:** [iPhone 12 Pro / Pixel 5 / iPad Air]
**OS Version:** [iOS 15.6 / Android 12]
**Browser:** [Safari / Chrome]

---

## 🎯 Test Scenario

**Role:** Műhelyvezető (Workshop Manager)
**Goal:** Track production job progress on mobile kiosk
**Context:** Production floor, touch-only interaction, no mouse/keyboard

---

## 📋 PRE-TEST SETUP

### 1. Device Configuration
- [ ] Install PWA: Add to Home Screen
- [ ] Enable Full-Screen mode
- [ ] Disable notifications (kiosk mode)
- [ ] Connect to production Wi-Fi
- [ ] Login as test user: `muhely.vezeto@doorstar.hu`

### 2. Test Data Preparation
- [ ] Ensure 3+ production jobs exist
- [ ] At least 1 job is **Overdue** (red border)
- [ ] At least 1 job is **InProgress** (yellow step)
- [ ] Test job ID: `TEST-JOB-001` (for detailed flow)

---

## 🧪 TEST FLOW (10 Steps)

### Step 1: Open Production Queue

**Action:**
- Open `/production/jobs` (ProductionQueuePage)

**Expected Outcome:**
- [ ] Page loads within 2 seconds
- [ ] Kiosk layout: sticky header, "Gyártási sor" title
- [ ] Back button NOT visible (root page)
- [ ] 4 filter buttons visible: Összes / Várakozik / Folyamatban / Kiszállítható
- [ ] Grid shows production job cards

**Accessibility:**
- [ ] Header contrast: title readable (dark text on white)
- [ ] Touch targets: filter buttons ≥48px height

**Edge Cases:**
- [ ] Empty state: "Nincs megjeleníthető projekt" if no jobs
- [ ] Loading state: "Betöltés..." during API call
- [ ] Error state: Red alert if API fails

---

### Step 2: Filter "Folyamatban" (In Progress)

**Action:**
- Tap "Folyamatban" filter button

**Expected Outcome:**
- [ ] Button background changes to **blue** (`#3b82f6`)
- [ ] Button text changes to **white**
- [ ] Grid refreshes to show only InProgress jobs
- [ ] "Összes" button returns to inactive state (white bg, grey border)

**Accessibility:**
- [ ] Active state contrast: white text on blue ≥4.5:1
- [ ] Button tap target: ≥48px height (56px on touch devices)

**Edge Cases:**
- [ ] No InProgress jobs: "Nincs megjeleníthető projekt"
- [ ] Filter toggle: tap "Összes" → all jobs return

**Pass/Fail:** ✅ / ❌
**Notes:** ___________________________

---

### Step 3: Tap Overdue Project (Red Border Card)

**Action:**
- Identify overdue project (red border, "⚠️ Késik" label)
- Tap the card

**Expected Outcome:**
- [ ] Card border: **red** (`#ef4444`), 3px thickness
- [ ] "⚠️ Késik" label visible in footer
- [ ] Tap navigates to `/production/jobs/{jobId}` detail page

**Accessibility:**
- [ ] Red border contrast: ≥3:1 (UI component)
- [ ] "Késik" label contrast: red text on white ≥4.5:1 ⚠️ (borderline 4.0:1)
- [ ] Entire card is tap target (not just label)

**Edge Cases:**
- [ ] No overdue projects: skip this step, test on normal project
- [ ] Multiple overdue: tap any one

**Pass/Fail:** ✅ / ❌
**Notes:** ___________________________

---

### Step 4: Navigate to Detail Page

**Action:**
- (Automatic from Step 3)

**Expected Outcome:**
- [ ] Page loads: `ProductionJobDetailPage`
- [ ] Header: "← Vissza" button visible (sticky)
- [ ] Project name: large, bold text
- [ ] Customer name: smaller, grey text
- [ ] 6 workflow steps visible: Szabászat → Megmunkálás → Felületkezelés → Összeszerelés → Csomagolás → Kiszállítható
- [ ] Current step highlighted: **yellow** background (`#fef3c7`)

**Accessibility:**
- [ ] Back button: ≥48px height
- [ ] Step icons: 48×48px (56×56px on touch)
- [ ] Current step contrast: yellow bg + black text ≥4.5:1

**Edge Cases:**
- [ ] 404 error if job not found
- [ ] Loading spinner during API call

**Pass/Fail:** ✅ / ❌
**Notes:** ___________________________

---

### Step 5: Tap "Start" on "Megmunkálás" Step

**Action:**
- Scroll to "Megmunkálás" step (step 2)
- Tap "▶️ Indítás" button

**Expected Outcome:**
- [ ] Button disabled during API call
- [ ] Button text changes: "Indítás..." (loading state)
- [ ] Step status changes: Queued → **InProgress**
- [ ] Step background changes: grey → **yellow** (`#fef3c7`)
- [ ] "✓ Kész" button appears

**Accessibility:**
- [ ] Button: ≥56px height
- [ ] Button text: white on blue ≥4.5:1
- [ ] Disabled state: opacity 0.5, cursor not-allowed

**Edge Cases:**
- [ ] API fail: alert "Hiba történt a lépés indításakor"
- [ ] Step already InProgress: button not visible
- [ ] Network timeout: error after 10 seconds

**Pass/Fail:** ✅ / ❌
**Notes:** ___________________________

---

### Step 6: Tap "Done" on "Megmunkálás" Step

**Action:**
- Wait 5 seconds (simulate work)
- Tap "✓ Kész" button

**Expected Outcome:**
- [ ] Button disabled during API call
- [ ] Button text changes: "Mentés..." (loading state)
- [ ] Step status changes: InProgress → **Done**
- [ ] Step background changes: yellow → **green** (`#d1fae5`)
- [ ] Step icon: checkmark "✓" appears
- [ ] Next step (Felületkezelés) becomes current (yellow)

**Accessibility:**
- [ ] Button: ≥56px height
- [ ] Checkmark icon: white on green ≥4.5:1 ⚠️ (2.3:1 - **FAIL**)

**Edge Cases:**
- [ ] API fail: alert "Hiba történt a lépés befejezésekor"
- [ ] Step already Done: button not visible

**Pass/Fail:** ✅ / ❌
**Notes:** ___________________________

---

### Step 7: Navigate to "Összeszerelés" Step

**Action:**
- Scroll to "Összeszerelés" step (step 4)
- Tap "▶️ Indítás" button

**Expected Outcome:**
- [ ] Step becomes InProgress (yellow background)
- [ ] "✓ Kész" button appears
- [ ] **Photo upload UI appears:**
  - [ ] File input: "Válasszon fotót..."
  - [ ] Upload button: "📷 Fotó feltöltés" (disabled)

**Accessibility:**
- [ ] Photo upload button: ≥56px height
- [ ] File input: visible, accessible

**Edge Cases:**
- [ ] Photo upload UI only visible for "Összeszerelés" step
- [ ] Other steps: no photo upload

**Pass/Fail:** ✅ / ❌
**Notes:** ___________________________

---

### Step 8: Upload Photo

**Action:**
- Tap file input → Camera opens
- Take photo or select from gallery
- Verify photo selected (filename visible)
- Tap "📷 Fotó feltöltés" button

**Expected Outcome:**
- [ ] File input opens camera/gallery
- [ ] Photo selected: filename shown
- [ ] Upload button: **enabled** (purple background)
- [ ] Upload starts: loading state
- [ ] Success: alert "Fotó feltöltve!" ⚠️ (disruptive)
- [ ] Photo file removed from input

**Accessibility:**
- [ ] File input: ≥44px touch target
- [ ] Upload button: ≥56px height

**Edge Cases:**
- [ ] **No photo selected:** Upload button **disabled** ✅
- [ ] **Upload fail:** alert "Hiba történt a fotó feltöltésekor" ❌ (no retry)
- [ ] **Large file (>10MB):** ⚠️ No validation - may fail
- [ ] **Wrong format (PDF):** ⚠️ No validation - may fail

**Critical Issue:**
- [ ] ❌ **"Done" button NOT disabled** if photo not uploaded
- [ ] ❌ **Frontend allows completing step without photo**
- [ ] ❌ **Backend rejects if photo required** (MSG-BACKEND-194 conflict)

**Pass/Fail:** ✅ / ❌
**Notes:** ___________________________

---

### Step 9: Tap "Done" on Összeszerelés

**Action:**
- (After successful photo upload)
- Tap "✓ Kész" button

**Expected Outcome:**
- [ ] Button disabled during API call
- [ ] Step status: InProgress → **Done**
- [ ] Step background: yellow → **green**
- [ ] Step icon: checkmark "✓"
- [ ] Next step (Csomagolás) becomes current

**Accessibility:**
- [ ] Button: ≥56px height

**Edge Cases:**
- [ ] **Photo NOT uploaded:** ⚠️ Frontend allows, Backend may reject
  - [ ] **Expected:** alert "Hiba történt" (generic)
  - [ ] **Better:** alert "⚠️ Fotó feltöltés kötelező!" (specific)

**Pass/Fail:** ✅ / ❌
**Notes:** ___________________________

---

### Step 10: Verify Real-Time Update (SSE)

**Action:**
- Open second device/browser tab
- Navigate to same project detail page
- Complete step on first device
- Observe second device

**Expected Outcome:**
- [ ] **First device:** Step marked Done ✅
- [ ] **Second device:** Real-time update via SSE
- [ ] **Second device:** Step automatically changes to Done (no refresh needed)
- [ ] **Update latency:** <2 seconds

**Accessibility:**
- N/A (backend feature)

**Edge Cases:**
- [ ] SSE connection lost: page does NOT update (stale data)
- [ ] Network offline: no real-time update

**Pass/Fail:** ✅ / ❌
**Notes:** ___________________________

---

## ✅ ACCEPTANCE CRITERIA

### Touch Targets
- [ ] All buttons ≥48px (56px on touch devices)
- [ ] Step icons ≥48px
- [ ] Checkbox ⚠️ **FAIL** (20px - needs fix to 44px)

### Color Contrast
- [ ] Text contrast ≥4.5:1 ✅ (most elements)
- [ ] Overdue label ⚠️ BORDERLINE (4.0:1 - recommend `#dc2626`)
- [ ] Step icon (InProgress/Done) ❌ **FAIL** (1.8:1 / 2.3:1)

### Photo Upload
- [ ] Upload UI visible for Összeszerelés ✅
- [ ] Upload button disabled if no file ✅
- [ ] Success/error feedback ⚠️ (alert - recommend toast)
- [ ] ❌ **CRITICAL:** Frontend allows Done without photo (conflicts with Backend spec)

### Visual Hierarchy
- [ ] Overdue projects stand out (3px red border) ✅
- [ ] Active filter clear (blue bg, white text) ✅
- [ ] Current step highlighted (yellow bg) ✅

### Kiosk Mode
- [ ] Full-screen mode works ✅
- [ ] Sticky header accessible ✅
- [ ] Landscape orientation ⚠️ (works but not optimized)
- [ ] ❌ **CRITICAL:** Pull-to-refresh NOT disabled (kiosk risk)

---

## 🐛 ISSUES FOUND

### Critical
1. **Photo upload conflict:** Frontend allows Done without photo, Backend requires photo (MSG-BACKEND-194)
2. **Pull-to-refresh enabled:** Kiosk mode must disable to prevent accidental reloads

### High
3. **Step icon contrast fail:** White text on yellow/green bg (1.8:1 / 2.3:1 - needs 4.5:1)
4. **Alert() for feedback:** Disruptive, blocks UI (replace with toast)

### Medium
5. **Checkbox size:** 20×20px (needs 44×44px for WCAG AA)
6. **Step circles size:** 32×32px desktop (needs 44×44px)

### Low
7. **Overdue label contrast:** 4.0:1 (borderline, recommend `#dc2626` for 4.5:1+)
8. **Landscape orientation:** Works but not optimized for phone landscape

---

## 📸 SCREENSHOTS

- [ ] ProductionQueuePage (filter active)
- [ ] Overdue project (red border)
- [ ] ProductionJobDetailPage (6 steps)
- [ ] InProgress step (yellow highlight)
- [ ] Photo upload UI (Összeszerelés)
- [ ] Done step (green checkmark)

---

## 🎯 SUCCESS METRICS

- [ ] Test completed in ≤30 minutes
- [ ] All 10 steps executed successfully
- [ ] Critical issues documented
- [ ] Accessibility checkpoints verified
- [ ] Edge cases tested

---

**Tester Signature:** ___________________________
**Date:** ___________________________
**Overall Pass/Fail:** ✅ / ❌

---

📋 Generated for MSG-DESIGNER-002 — Doorstar Mobile Kiosk UX Audit (2026-07-11)

Co-Authored-By: Claude Sonnet 4.5 <noreply@anthropic.com>
