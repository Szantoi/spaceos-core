---
id: MSG-FRONTEND-018
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-CONDUCTOR-007-Q3-APPROVED
created: 2026-06-29
---

# Q3 Track A: Public Quote Form UI + Status Tracking

## Overview

**Milestone:** Q3 Cutting Module Expansion — Track A: Customer Self-Service Portal
**Status:** APPROVED by Root (2026-06-22)
**Effort:** 2 days (parallel with Backend MSG-BACKEND-030)
**Target:** Week 1-2 completion

## Feature Scope

Create public-facing Quote Request form for B2C Lapszabász customers. No authentication required.

### UI Components

#### 1. Quote Form Page (`/public/cutting/quote-request`)

**Form Fields:**
- Customer name (required, text input)
- Customer email (required, email validation)
- Customer phone (required, phone validation)
- Panel width (required, number input, 200-2500 mm)
- Panel height (required, number input, 200-2500 mm)
- Panel count (required, number input, 1-100)
- Material dropdown (oak, beech, walnut, mdf, etc.) — **data from Catalog API**
- Edging dropdown (2mm PVC, 1mm veneer, etc.)
- Finishing dropdown (plain, varnished, matte, glossy)
- Urgency toggle (standard, express)
- Delivery location (optional, text input)
- Notes/special requests (optional, textarea)

**Submit Button:**
- POST to `/public/cutting/quote-request`
- Loading state + spinner while submitting
- Success: Show confirmation modal with Quote Request ID
- Error: Show validation errors inline

#### 2. Quote Status Page (`/public/cutting/quote-status/:quoteRequestId`)

**Components:**
- Quote Request ID display
- Status badge (pending / reviewed / quoted / declined)
- Quote details summary (dimensions, material, etc.)
- Email notification info: "We'll send updates to your@email.com"
- Timeline: Quote requested → Under review → Quoted → (Ready to order?)

**Refresh mechanism:**
- Auto-refresh every 30 seconds (polling)
- Or WebSocket subscription to quote status updates (if available)

#### 3. Landing Page Update (`/public/cutting`)

**Add CTA section:**
- Heading: "Gyors árajánlat kérése"
- Description: "Adja meg méreteit és anyagát, megkapja az árajánlatot 3-5 nap alatt"
- Button: "Árajánlat kérése →" (links to `/public/cutting/quote-request`)

### API Integration

**Backend Endpoint (MSG-BACKEND-030):**
```
POST /public/cutting/quote-request
Request: { customerName, customerEmail, customerPhone, panelDimensions, panelCount, material, edging, finishing, ... }
Response: { success: true, quoteRequestId: "QR-2026-0042", ... }
```

**Catalog API (for Material/Edging dropdowns):**
```
GET /api/catalog/materials?category=cutting
→ [{ id: "oak", label: "Tölgy", price: 15000 }, ...]

GET /api/catalog/edgings?type=pvc
→ [{ id: "2mm_pvc", label: "2mm PVC" }, ...]
```

**Quote Status API (optional, nice-to-have):**
```
GET /api/public/cutting/quote-status/:quoteRequestId
→ { status: "pending" | "reviewed" | "quoted" | "declined", estimatedCompletion: "2026-06-29T16:00:00Z" }
```

### Design Guidelines

**Public Page Styling:**
- Minimal, B2C friendly design
- Not overly technical (hide complex options)
- Mobile-first responsive (80% of users on mobile)
- Company branding (Datahaven? Lapszabász-focused theme?)
- CTA buttons: prominent, contrasting colors

**Form Validation UX:**
- Real-time field validation (on blur)
- Inline error messages (red text under field)
- Success checkmarks on valid fields
- Dimension constraints: Show MM units + valid range hint

**Status Page:**
- Polling indicator: "Last updated 30 seconds ago"
- Email verification: "Confirm your email to receive updates"
- Share/print quote functionality (nice-to-have)

### Files to Create/Modify

**Create:**
- `datahaven-web/client/src/pages/PublicQuoteRequestPage.tsx`
- `datahaven-web/client/src/pages/PublicQuoteStatusPage.tsx`
- `datahaven-web/client/src/components/PublicQuoteForm.tsx`
- `datahaven-web/client/src/components/QuoteStatusTimeline.tsx`
- `datahaven-web/client/src/services/publicCuttingService.ts` (API calls)
- `datahaven-web/client/src/__tests__/integration/public-quote.test.tsx`

**Modify:**
- `datahaven-web/client/src/App.tsx` — add public routes (no auth guard):
  ```
  Route("/public/cutting/quote-request", PublicQuoteRequestPage)
  Route("/public/cutting/quote-status/:id", PublicQuoteStatusPage)
  ```
- `datahaven-web/public/index.html` — ensure meta tags for SEO (og:title, og:description)

### Success Criteria

- ✅ `/public/cutting/quote-request` page loads (no auth required)
- ✅ Form submits successfully, returns Quote Request ID
- ✅ `/public/cutting/quote-status/:id` displays status
- ✅ Validation works (required fields, email format, dimensions)
- ✅ Mobile responsive (tested on iPhone 12)
- ✅ Material/edging dropdowns populated from Catalog API
- ✅ >8 integration tests passing
- ✅ 0 TypeScript errors

### Dependencies

- Backend MSG-BACKEND-030 (Quote Request API) — **must be DONE first**
- Catalog API (for material/edging dropdowns) — already available
- Axios/fetch for API calls (already in place)

### Blocking/Dependencies

**Blocked by:** MSG-BACKEND-030 (needs `/public/cutting/quote-request` endpoint)
**Blocks:** Nothing (independent)

### Next Steps

1. Wait for Backend MSG-BACKEND-030 API endpoint to be LIVE
2. Implement PublicQuoteRequestPage component
3. Test form submission against real API
4. Implement PublicQuoteStatusPage (polling/WebSocket)
5. Style for mobile + desktop
6. Write integration tests
7. Report DONE when `/public/cutting/quote-request` form is live and functional

---

**Notes:**
- **Public endpoint** → think about security (XSS, CSRF protection)
- **No authentication** → users are external customers, not internal staff
- **Lightweight** → this is MVP, advanced features (quote customization, pricing preview) come in Track B
- **Material dropdown**: Pull from Catalog service, not hardcoded

**Questions?** Ask in Telegram: @conductor

**Timeline:** Start after Backend MSG-BACKEND-030 API is ready (est. 1-2 days from now)
