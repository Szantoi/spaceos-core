# SpaceOS Cutting Q3 Track A — Customer Self-Service Portal

**Version:** v1
**Created:** 2026-06-23
**Status:** Approved (Root MSG-CONDUCTOR-007)
**Epic:** CUTTING-Q3-EXPANSION
**Duration:** 4 days (Week 1-2)
**Priority:** HIGH

---

## Executive Summary

Extend the existing B2B Quote Request API (Phase 1) with a public-facing B2C customer portal that enables lapszabász (panel cutting) customers to submit quote requests via a web form without authentication.

**Phase 1 baseline:** Backend Quote Request API endpoints exist (6 endpoints) with tracking token flow.

**Q3 Track A adds:**
1. Public Quote Request Form (React component)
2. Subdomain-based tenant resolution
3. Email notifications (Brevo SMTP)
4. Tracking page (customer-facing)

---

## Business Context

**Target customer:** 2. ügyfél (Lapszabász KKV) — panel cutting service provider
**Use case:** End customers (carpenter, furniture maker) submit cutting job requests via web form
**Current gap:** API exists but no public UI, tenant resolution is TODO, no email notifications

---

## Acceptance Criteria

**Backend (2 days):**
- [ ] Subdomain-based tenant resolution implemented (`tenant1.joinerytech.hu` → TenantId)
- [ ] Email notification on quote request creation (customer + admin)
- [ ] Email notification on quote approval/rejection
- [ ] Brevo SMTP integration (already configured, see Codebase_Status.md)
- [ ] Unit tests: TenantResolver (95%), EmailService (90%)

**Frontend (2 days):**
- [ ] Public Quote Request Form (`/quote-request` route, React)
  - Fields: name, email, phone, material, dimensions, quantity, notes
  - File upload (cutting list, DXF/PDF)
  - reCAPTCHA v3 (optional, defer if not ready)
- [ ] Tracking Page (`/track/{trackingToken}` route)
  - Status display: Pending → Approved → Accepted
  - Quote price display
  - Accept button
- [ ] Responsive design (mobile-first)
- [ ] Integration tests: Form submission flow (3 scenarios)

**Integration:**
- [ ] E2E test: Public form → Backend → Email → Tracking page → Accept → Order created
- [ ] Tenant resolution tested with 2 subdomains

**Documentation:**
- [ ] README: How to set up subdomain locally (hosts file)
- [ ] API docs: Updated with tenant resolution flow

---

## Technical Design

### Architecture

```
Public User
  ↓
Public Form (React, /quote-request)
  ↓ POST /public/cutting/quote-request
  ↓ (X-Tenant-Id header or subdomain)
Backend API (QuoteRequestEndpoints.cs)
  ↓ TenantResolver.GetTenantIdFromSubdomain()
  ↓ CreateQuoteRequestCommand
  ↓ EmailService.SendQuoteRequestNotification()
Database (spaceos_cutting.QuoteRequests)
```

### Subdomain Tenant Resolution

**Strategy:** Extract tenant from subdomain (`tenant1.joinerytech.hu`)

**Implementation:**
```csharp
public class TenantResolver : ITenantResolver
{
    public async Task<Guid> GetTenantIdFromSubdomain(string hostname)
    {
        // Extract subdomain from hostname
        // Query Tenants table by subdomain mapping
        // Return TenantId or throw TenantNotFoundException
    }
}
```

**Database schema:**
```sql
ALTER TABLE "Tenants" ADD COLUMN "Subdomain" text UNIQUE;
CREATE INDEX idx_tenants_subdomain ON "Tenants"("Subdomain");
```

**Nginx config:**
```nginx
server {
    server_name *.joinerytech.hu;
    location /api/ {
        proxy_pass http://localhost:5005;
        proxy_set_header X-Original-Host $host;
    }
}
```

### Email Notifications

**Brevo SMTP:** Already configured (see Codebase_Status.md line 369)
**Templates:**
- `quote-request-customer.html` — confirmation email to customer
- `quote-request-admin.html` — notification to tenant admin
- `quote-approved.html` — quote approved, price included
- `quote-rejected.html` — quote rejected, reason included

**Email Service:**
```csharp
public class EmailService : IEmailService
{
    public async Task SendQuoteRequestNotification(
        string customerEmail,
        string adminEmail,
        QuoteRequestDto quote);
}
```

### Frontend Components

**1. PublicQuoteRequestForm.tsx** (new)
```tsx
- Input fields: name, email, phone, material, dimensions, quantity
- File upload: <input type="file" accept=".pdf,.dxf" />
- Submit button → POST /public/cutting/quote-request
- Success: Show trackingToken, email confirmation
```

**2. TrackingPage.tsx** (new)
```tsx
- Route: /track/:trackingToken
- Fetch: GET /public/cutting/quotes/track/{trackingToken}
- Display: Status badge, quote details, price (if approved)
- Accept button → POST /public/cutting/quotes/track/{trackingToken}/accept
```

**Routing:**
```tsx
<Route path="/quote-request" element={<PublicQuoteRequestForm />} />
<Route path="/track/:trackingToken" element={<TrackingPage />} />
```

---

## Implementation Tasks

### Backend (MSG-030)

**Sub-tasks:**
1. `TenantResolver` service + `Tenants.Subdomain` migration
2. `EmailService` + Brevo SMTP templates (4 templates)
3. Update `QuoteRequestEndpoints.GetTenantIdFromContext()` to use TenantResolver
4. Add email notification hooks to CreateQuoteRequest, ApproveQuote, RejectQuote commands
5. Unit tests: TenantResolver (10 tests), EmailService (8 tests)

**Files to modify:**
- `Tenants` migration (add Subdomain column)
- `QuoteRequestEndpoints.cs` (TenantResolver integration)
- `IEmailService.cs` + `EmailService.cs` (new)
- `ITenantResolver.cs` + `TenantResolver.cs` (new)
- `CreateQuoteRequestHandler.cs` (add email notification)
- `ApproveQuoteHandler.cs` (add email notification)
- `RejectQuoteHandler.cs` (add email notification)

**Estimate:** 2 days (16 hours)

---

### Frontend (MSG-018)

**Sub-tasks:**
1. Create `PublicQuoteRequestForm.tsx` component
2. Create `TrackingPage.tsx` component
3. Add routes to `App.tsx`
4. Create `useQuoteRequest` custom hook (API calls)
5. Integration tests: Form submission flow (3 scenarios)

**Files to create:**
- `src/components/PublicQuoteRequestForm.tsx`
- `src/pages/TrackingPage.tsx`
- `src/hooks/useQuoteRequest.ts`
- `src/components/PublicQuoteRequestForm.test.tsx`
- `src/pages/TrackingPage.test.tsx`

**Files to modify:**
- `src/App.tsx` (add routes)

**Estimate:** 2 days (16 hours)

---

## Dependencies

**Blocks:**
- Track B (Pricing Integration) — wait for Track A email flow

**Blocked by:**
- None (Phase 1 Quote Request API is done)

---

## Success Metrics

- Public form submissions: 0 → 10+ test submissions
- Email delivery rate: 100% (Brevo dashboard)
- Tracking page loads: 100% success rate
- E2E test pass rate: 100%

---

## Risks & Mitigation

| Risk | Mitigation |
|---|---|
| Subdomain DNS not configured | Use X-Tenant-Id header fallback for testing |
| Email delivery fails | Brevo SMTP already configured, test thoroughly |
| File upload size limits | Set nginx client_max_body_size 10M |
| Public form spam | Defer reCAPTCHA to Track D if time constrained |

---

## References

- **Phase 1 Baseline:** `QuoteRequestEndpoints.cs` (lines 1-229)
- **Codebase_Status.md:** Brevo SMTP config (line 369)
- **Doorstar onboarding:** `SpaceOS_Doorstar_Onboarding_v4.md`
