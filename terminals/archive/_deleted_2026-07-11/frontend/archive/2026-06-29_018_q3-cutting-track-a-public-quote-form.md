---
id: MSG-FRONTEND-018
from: conductor
to: frontend
type: task
priority: high
status: UNREAD
model: sonnet
ref: MSG-CONDUCTOR-007,MSG-BACKEND-030,MSG-BACKEND-031
created: 2026-06-29
---

# Q3 Cutting Expansion — Track A: Public Quote Form UI

## Context

**Q3 Cutting Expansion APPROVED** by Root (MSG-CONDUCTOR-007). This is Track A, Frontend: Customer Self-Service Public Portal.

**Backend ready:** MSG-BACKEND-030 (Quote Request API) + MSG-BACKEND-031 (Email notification)

**Timeline:** Track A complete by 2026-07-07 (Week 1)

## Task: Public Quote Form + Status Tracking

### Feature Overview

**Two main pages:**
1. **Public Quote Form** (`/public/cutting/quote-request`) — submit new quote
2. **Quote Status Tracking** (`/public/cutting/quote/{quoteId}/status`) — track status

Both pages are **unauthenticated**, public-facing, responsive mobile-first UI.

### Page 1: Quote Request Form

**URL:** `/public/cutting/quote-request`

**Layout:**
```
┌─────────────────────────────────────┐
│    Doorstar Cutting Solutions       │
│      Get Your Custom Quote          │  (Hero section)
└─────────────────────────────────────┘

┌─ Quote Request Form ─────────────────┐
│                                       │
│ Contact Information                   │
│  ☐ Full Name *                       │
│  ☐ Email *                           │
│  ☐ Phone                             │
│  ☐ Company Name                      │
│                                       │
│ Material & Specifications             │
│  ☐ Material (dropdown) *             │
│    └─ tölgy, fenyő, mdf, fk           │
│  ☐ Length (mm) *                     │
│  ☐ Width (mm) *                      │
│  ☐ Thickness (mm) *                  │
│  ☐ Quantity *                        │
│                                       │
│ Finishing Options                     │
│  ☐ Edge Type (dropdown)              │
│    └─ ABS, PVC, furnir               │
│  ☐ Surface (dropdown)                │
│    └─ matt, fényes, textúra          │
│                                       │
│ Priority & Notes                      │
│  ☐ Urgency: ○ Standard ○ Express     │
│  ☐ Additional Notes                  │
│  ☐ Attach Files (optional)           │
│                                       │
│              [Submit Quote Request]   │
│                                       │
└───────────────────────────────────────┘
```

**Component Structure:**
```
PublicQuoteLayout
├── PublicQuoteForm
│   ├── ContactSection
│   ├── MaterialSection
│   ├── FinishingSection
│   ├── PrioritySection
│   └── SubmitButton
└── SuccessModal (after submit)
    ├── QuoteId
    ├── TrackingLink
    └── [Go to Status Page]
```

### Form Logic

**Validation:**
- [ ] Name: min 2 chars, max 100
- [ ] Email: valid format + domain check
- [ ] Phone: optional, basic format (if provided)
- [ ] Dimensions: > 0, < 10,000 (prevent typos)
- [ ] Quantity: 1-1000

**Submission Flow:**
1. Client-side validation (React Hook Form + Zod/Yup)
2. Show loading state (spinner)
3. POST to `/api/public/cutting/quote-request`
4. **Success:** Show modal with quoteId + tracking link
   - Auto-redirect to status page after 3 seconds (optional)
   - Or user clicks "Track Status"
5. **Error:** Show error toast (network, validation, 400/500)
   - Retry button available

**Success Response Handler:**
```typescript
interface QuoteRequestResponse {
  quoteId: string;
  status: "received";
  estimatedReplyTime: "2 business days";
  trackingUrl: string;  // e.g., "/public/quote/{quoteId}/status"
}
```

### Page 2: Quote Status Tracking

**URL:** `/public/cutting/quote/{quoteId}/status`

**Layout:**
```
┌─────────────────────────────────────┐
│    Doorstar Cutting Solutions       │
│      Track Your Quote                │
└─────────────────────────────────────┘

Quote ID: ABC-123-XYZ
Submission Date: June 29, 2026

Status Timeline:
  ✓ Quote Received — 2026-06-29 14:30
  → Under Review — Expected by 2026-07-01
  - Quoted
  - Completed

Estimated Completion: 2 business days

[Contact Sales] [Back to Home]
```

**Component Structure:**
```
PublicQuoteStatus
├── QuoteHeader (ID, submission date)
├── StatusTimeline
│   ├── TimelineStep (received)
│   ├── TimelineStep (reviewing)
│   ├── TimelineStep (quoted)
│   └── TimelineStep (completed)
└── ContactSupport (email link)
```

**API Integration:**
```typescript
// GET /api/public/cutting/quote/{quoteId}/status
interface QuoteStatusResponse {
  quoteId: string;
  status: "received" | "reviewing" | "quoted" | "completed" | "rejected";
  submittedAt: string;      // ISO date
  estimatedReplyTime: string;
  estimatedCompletionDate: string;
  currentStep: number;      // 0-3
  timeline: {
    step: string;
    status: "completed" | "current" | "pending";
    date?: string;
  }[];
}
```

**Auto-refresh:** Poll every 30 seconds (or use WebSocket if available)

### Implementation Checklist

#### Phase 1: Project Structure (30 minutes)

- [ ] Create routes:
  - [ ] `/public/cutting/quote-request` (PublicQuotePage)
  - [ ] `/public/cutting/quote/:quoteId/status` (PublicStatusPage)
- [ ] Create API hooks:
  - [ ] `usePublicQuoteSubmit()` — POST request
  - [ ] `useQuoteStatus()` — GET + polling
- [ ] Global styles for public pages (if needed)

#### Phase 2: Quote Form Component (3 hours)

- [ ] `PublicQuoteForm.tsx` (main form component)
- [ ] Form state management (React Hook Form)
- [ ] Validation schema (Zod or Yup)
- [ ] Material/edge type/surface dropdowns (static data for now)
- [ ] File upload field (optional, might defer if MVP)
- [ ] Loading states + error handling
- [ ] Accessibility (form labels, ARIA, keyboard nav)
- [ ] Mobile responsiveness (Tailwind CSS)
- [ ] Success modal (quoteId + tracking link)

#### Phase 3: Status Page Component (1.5 hours)

- [ ] `PublicStatusPage.tsx`
- [ ] Status timeline visualization
- [ ] Polling logic (30-second refresh)
- [ ] Error state (quote not found, network error)
- [ ] Contact support link (mailto: sales@doorstar.hu)
- [ ] Mobile responsiveness

#### Phase 4: Styling & Polish (1 hour)

- [ ] Tailwind CSS styling (match brand colors)
- [ ] Responsive design (mobile, tablet, desktop)
- [ ] Loading animations
- [ ] Error message styling
- [ ] Success toast styling

#### Phase 5: Testing (2 hours)

- [ ] Component tests:
  - [ ] Form validation (pass/fail scenarios)
  - [ ] Submit button (loading, success, error)
  - [ ] Status page polling
- [ ] Integration test:
  - [ ] Full flow: submit form → success modal → redirect to status
- [ ] E2E test (Playwright):
  - [ ] User flow on desktop + mobile
- [ ] Test coverage target: 80%+

### API Contract Verification

**Backend provides:**
- `POST /api/public/cutting/quote-request` ✅ (MSG-BACKEND-030)
- `GET /api/public/cutting/quote/{quoteId}/status` ✅ (implied in MSG-BACKEND-030)

**Frontend expects:**
```typescript
type CreateQuoteRequest = {
  customerName: string;
  customerEmail: string;
  customerPhone?: string;
  companyName?: string;
  material: string;
  dimensions: { length: number; width: number; thickness: number };
  quantity: number;
  edgeType?: string;
  surface?: string;
  urgency: "standard" | "express";
  notes?: string;
};

type QuoteResponse = {
  quoteId: string;
  status: "received";
  estimatedReplyTime: string;
  trackingUrl: string;
};
```

### Dependencies

- React 18
- React Hook Form (form state)
- Zod or Yup (validation)
- Tailwind CSS (styling)
- axios or fetch (API calls)
- @tanstack/react-query (optional, for polling)

### Definition of Done

✅ Criteria:
1. Quote form page renders (all fields visible)
2. Form validation working (client-side)
3. Submit button calls `/api/public/cutting/quote-request`
4. Success modal shows quoteId + tracking link
5. Error handling (network, 4xx, 5xx) working
6. Status page loads and displays timeline
7. Status page polls every 30 seconds
8. Mobile responsive (tested on Chrome dev tools)
9. No TypeScript errors
10. Tests passing (80%+ coverage)

### Success Indicators

- [ ] `npm run build` PASS (zero TypeScript errors)
- [ ] `npm test` PASS
- [ ] E2E test: full user journey works on desktop + mobile
- [ ] Form submission <2 second response time
- [ ] UI matches Figma design (if available)

### Blocking & Dependencies

⚠️ **Blocks:** MSG-FRONTEND-018 cannot start until MSG-BACKEND-030 is ready

✅ **Can start:** Once Backend MSG-030/031 in progress

---

## Model Hint

**Model: `sonnet`** — React UI implementation

**Effort estimate:** 8 hours (3 form + 1.5 status + 1 styling + 2.5 testing)

**Timeline:** Parallel with Backend MSG-030/031, all Track A done by EOD 2026-06-29 (or next day)

---

**Conductor dispatch:** 2026-06-29 19:35 UTC
**Q3 Target:** Track A complete by 2026-07-07 (Week 1)

### Reference

- Form patterns: `docs/knowledge/patterns/FRONTEND_PATTERNS.md`
- Testing: `docs/knowledge/patterns/TESTING_PATTERNS.md`
- Responsive design: `docs/knowledge/patterns/RESPONSIVE_DESIGN.md`
- Previous quote form work: Check git history for similar components
