# SpaceOS Catalog + EHS Hybrid Architecture (FINAL - READY FOR IMPLEMENTATION)

**Status:** ✅ IMPLEMENTÁCIÓRA KÉSZ
**Created:** 2026-06-23
**Epic:** CATALOG-EHS-HYBRID
**Timeline:** 2 weeks (Week 1: Foundation + Catalog MVP, Week 2: EHS UI + Polish)
**Review Pipeline:** v1 → v2 (DB) → v3 (Security) → v4 (Backend) ✅ COMPLETE

---

## Executive Summary

This architecture implements a **hybrid UI-first + backend-safety strategy** for two high-value features:

1. **Catalog Filter** (frontend-only MVP, Week 1) — immediate business value with fuzzy search, URL state sync, voice search
2. **EHS Risk Calculator** (backend-first, Week 1-2) — compliance-critical with audit trail, gamification, PDF export

**Review Pipeline Results:**
- ✅ v2 DB Review: 5 schema fixes (soft delete, FK constraints, composite indexes, ENUM type)
- ✅ v3 Security Review: 2 CRITICAL + 4 HIGH fixes (RLS bypass, mass assignment, XSS, IDOR, CSRF, rate limiting)
- ✅ v4 Backend Review: 3 HIGH fixes (pagination, validation drift, error handling)

**Critical Fixes Required Before Deployment:**
1. **CRITICAL:** RLS policy bypass prevention (C1 from v3)
2. **CRITICAL:** Mass assignment vulnerability (C2 from v3)
3. **HIGH:** XSS in catalog filter (H1 from v3)
4. **HIGH:** IDOR on assessment endpoints (H2 from v3)
5. **HIGH:** Pagination on history endpoint (H1 from v4)

---

## Task Breakdown (1-2h granularity)

### Week 1 Track A: Catalog Filter (Frontend)

#### FE-CAT-001: App Store Catalog Filter State (1h)
**Assigned to:** Frontend
**Dependencies:** None
**Description:** Extend `app-store.jsx` with catalog filter state management

**Acceptance Criteria:**
- [ ] Add `catalogFilters` state (search, category[], priceRange, stockStatus)
- [ ] Implement `setFilter(key, value)` method
- [ ] Persist filters to localStorage
- [ ] URL sync (push search params to window.history)
- [ ] Implement `resetFilters()` method
- [ ] Write unit test: Filter state updates correctly

**Files to modify:**
- `design-portal/src/store/app-store.jsx`

---

#### FE-CAT-002: SmartSearchBar Component (1.5h)
**Assigned to:** Frontend
**Dependencies:** FE-CAT-001
**Description:** Create search bar with fuzzy search and debouncing

**Acceptance Criteria:**
- [ ] Install `fuzzysort` npm package
- [ ] Create `SmartSearchBar.jsx` component
- [ ] Implement debounced search (300ms delay)
- [ ] Integrate with `catalogFilters.search` state
- [ ] **SECURITY FIX (v3-H1):** Strip HTML tags from input (XSS protection)
- [ ] Write E2E test: XSS payload `<script>alert(1)</script>` is escaped

**Files to create:**
- `design-portal/src/components/catalog/CatalogFilterBar/SmartSearchBar.jsx`

**Code snippet:**
```jsx
const setFilter = (key, value) => {
  if (key === 'search' && typeof value === 'string') {
    value = value.replace(/<[^>]*>/g, '');  // ✅ Strip HTML tags (XSS fix)
  }
  // ... rest of logic
};
```

---

#### FE-CAT-003: Category Chips Component (1h)
**Assigned to:** Frontend
**Dependencies:** FE-CAT-001
**Description:** Create multi-select category filter chips

**Acceptance Criteria:**
- [ ] Create `CategoryChips.jsx` component
- [ ] Load categories from catalog data (distinct values)
- [ ] Multi-select toggle (click to add/remove)
- [ ] Integrate with `catalogFilters.category` state
- [ ] Active state styling (blue background for selected)
- [ ] Write unit test: Category toggle adds/removes from array

**Files to create:**
- `design-portal/src/components/catalog/CatalogFilterBar/CategoryChips.jsx`

---

#### FE-CAT-004: Price Range Slider + Stock Status Toggle (1.5h)
**Assigned to:** Frontend
**Dependencies:** FE-CAT-001
**Description:** Create price range slider and stock status toggle

**Acceptance Criteria:**
- [ ] Install `rc-slider` npm package (dual-thumb range slider)
- [ ] Create `PriceRangeSlider.jsx` component
- [ ] Create `StockStatusToggle.jsx` component (3 states: all/in-stock/out-of-stock)
- [ ] Integrate with `catalogFilters.priceRange` and `catalogFilters.stockStatus`
- [ ] Write unit test: Slider updates priceRange state

**Files to create:**
- `design-portal/src/components/catalog/CatalogFilterBar/PriceRangeSlider.jsx`
- `design-portal/src/components/catalog/CatalogFilterBar/StockStatusToggle.jsx`

---

#### FE-CAT-005: Fuzzy Search Hook + Filtering Logic (1.5h)
**Assigned to:** Frontend
**Dependencies:** FE-CAT-001, FE-CAT-002
**Description:** Create `useCatalogFilters` hook with fuzzy search and filter logic

**Acceptance Criteria:**
- [ ] Create `useCatalogFilters.js` hook
- [ ] Implement `applyFuzzySearch(items, searchTerm)` using fuzzysort
- [ ] Apply filters: search + category + price range + stock status
- [ ] Memoize filtered results (useMemo)
- [ ] Limit fuzzy search to 500 results (performance cap)
- [ ] Write unit test: Fuzzy search finds "wood" when searching "wod"

**Files to create:**
- `design-portal/src/hooks/useCatalogFilters.js`

---

#### FE-CAT-006: Virtualized Catalog Grid (1h)
**Assigned to:** Frontend
**Dependencies:** FE-CAT-005
**Description:** Create virtualized grid using `react-window`

**Acceptance Criteria:**
- [ ] Install `react-window` npm package
- [ ] Create `VirtualizedCatalogGrid.jsx` component
- [ ] Use `FixedSizeGrid` for 5000+ items
- [ ] Row height: 180px, overscan: 5 rows
- [ ] Integrate with `filteredCatalog` from `useCatalogFilters`
- [ ] Write E2E test: Grid renders 100 items without lag

**Files to create:**
- `design-portal/src/components/catalog/VirtualizedCatalogGrid/index.jsx`

---

#### FE-CAT-007: Voice Search (Progressive Enhancement) (1h)
**Assigned to:** Frontend
**Dependencies:** FE-CAT-002
**Description:** Add voice search button (Chrome only)

**Acceptance Criteria:**
- [ ] Create `VoiceSearchButton.jsx` component
- [ ] Feature detection: hide button if `webkitSpeechRecognition` not available
- [ ] Implement voice recognition with `hu-HU` language
- [ ] **SECURITY FIX (v3-M4):** Sanitize transcript before setting filter
- [ ] Set `catalogFilters.search` with transcript
- [ ] Write E2E test (manual): Voice input "lap" → filter applies

**Files to create:**
- `design-portal/src/components/catalog/CatalogFilterBar/VoiceSearchButton.jsx`

**Code snippet:**
```jsx
recognition.onresult = (e) => {
  const transcript = e.results[0][0].transcript;
  const sanitized = transcript.replace(/<[^>]*>/g, '');  // ✅ XSS fix
  setFilter('search', sanitized);
};
```

---

### Week 1 Track B: EHS Backend (Backend)

#### BE-EHS-001: Create EHS Module Structure (0.5h)
**Assigned to:** Backend
**Dependencies:** None
**Description:** Set up EHS module folders and namespaces

**Acceptance Criteria:**
- [ ] Create `SpaceOS.Modules.EHS` project structure
- [ ] Folders: Domain/Entities, Application/Commands, Application/Queries, Infrastructure/Persistence
- [ ] Add project reference to main solution
- [ ] Write README.md with module overview

**Files to create:**
- `spaceos-modules-ehs/Domain/Entities/`
- `spaceos-modules-ehs/Application/Commands/`
- `spaceos-modules-ehs/Application/Queries/`
- `spaceos-modules-ehs/Infrastructure/Persistence/`

---

#### BE-EHS-002: Implement RiskAssessment Entity + Factory (1.5h)
**Assigned to:** Backend
**Dependencies:** BE-EHS-001
**Description:** Create `RiskAssessment` entity with factory method and domain validation

**Acceptance Criteria:**
- [ ] Create `RiskAssessment.cs` entity
- [ ] Implement factory method `Create(...)` with domain validation
- [ ] Validate likelihood/severity range (1-5)
- [ ] **DOMAIN VALIDATION (v4-M3):** Enforce "high-risk (score > 15) requires notes"
- [ ] Compute `DataHash` using SHA256
- [ ] Add calculated properties: `RiskScoreBefore`, `RiskScoreAfter`, `ImprovementScore`
- [ ] Write unit test: Factory method throws exception for invalid likelihood
- [ ] Write unit test: High-risk without notes throws `DomainException`

**Files to create:**
- `spaceos-modules-ehs/Domain/Entities/RiskAssessment.cs`
- `spaceos-modules-ehs/Domain/Exceptions/DomainException.cs`

**Code snippet:**
```csharp
public static RiskAssessment Create(...)
{
    ValidateLikelihood(likelihoodBefore);
    ValidateSeverity(severityBefore);

    var riskScore = likelihoodBefore * severityBefore;
    if (riskScore > 15 && string.IsNullOrWhiteSpace(notes))
    {
        throw new DomainException("High-risk assessments (score > 15) require mitigation notes");
    }

    var assessment = new RiskAssessment { ... };
    assessment.DataHash = assessment.ComputeHash();
    return assessment;
}
```

---

#### BE-EHS-003: Create DB Migration (v2 Fixes Applied) (1h)
**Assigned to:** Backend
**Dependencies:** BE-EHS-002
**Description:** Create EF Core migration with v2 DB review fixes

**Acceptance Criteria:**
- [ ] Create `ehs` schema
- [ ] Create `ehs.risk_category` ENUM type (v2-M3 fix)
- [ ] Create `ehs.assessments` table (v2-M1 fix: skeleton table)
- [ ] Create `ehs.risk_assessments` table with:
  - [ ] Soft delete columns: `deleted_at`, `deleted_by`, `version` (v2-H1 fix)
  - [ ] FK constraint to `kernel.organizations` with `ON DELETE RESTRICT` (v2-H2 fix)
  - [ ] FK constraint to `ehs.assessments` (v2-M1 fix)
  - [ ] Composite index on `(organization_id, created_at DESC)` (v2-M4 fix)
- [ ] Enable RLS policy
- [ ] Write migration rollback script

**Files to create:**
- `spaceos-modules-ehs/Infrastructure/Persistence/Migrations/0001_create_ehs_module.sql`

**SQL snippet (v2 fixes):**
```sql
-- ✅ v2-M3 fix: ENUM type
CREATE TYPE ehs.risk_category AS ENUM (...);

-- ✅ v2-H1 fix: soft delete columns
ALTER TABLE ehs.risk_assessments
  ADD COLUMN deleted_at TIMESTAMPTZ NULL,
  ADD COLUMN deleted_by VARCHAR(255) NULL,
  ADD COLUMN version INT NOT NULL DEFAULT 1;

-- ✅ v2-H2 fix: ON DELETE RESTRICT
CONSTRAINT fk_organization FOREIGN KEY (organization_id)
  REFERENCES kernel.organizations(id) ON DELETE RESTRICT;

-- ✅ v2-M4 fix: composite index
CREATE INDEX idx_risk_assessments_org_created
  ON ehs.risk_assessments(organization_id, created_at DESC)
  WHERE deleted_at IS NULL;
```

---

#### BE-EHS-004: Implement ICurrentUserService (v3-C1 Fix) (1h)
**Assigned to:** Backend
**Dependencies:** None
**Description:** Create `ICurrentUserService` to extract organization ID from JWT

**Acceptance Criteria:**
- [ ] Create `ICurrentUserService` interface
- [ ] Implement `CurrentUserService` (read from `HttpContext.User.Claims`)
- [ ] Extract `OrganizationId` from JWT claim `"organization_id"`
- [ ] Extract `UserId` from JWT claim `NameIdentifier`
- [ ] Register in DI container
- [ ] Write unit test: Service extracts org ID from mocked ClaimsPrincipal

**Files to create:**
- `spaceos-kernel/Application/Common/Interfaces/ICurrentUserService.cs`
- `spaceos-kernel/Infrastructure/Identity/CurrentUserService.cs`

---

#### BE-EHS-005: Implement TenantIsolationInterceptor (v3-C1 Fix) (1h)
**Assigned to:** Backend
**Dependencies:** BE-EHS-004
**Description:** Create `DbConnectionInterceptor` to set GUC parameter for RLS

**Acceptance Criteria:**
- [ ] Create `TenantIsolationInterceptor` class
- [ ] Override `ReaderExecutingAsync` method
- [ ] Set `app.current_organization_id` GUC parameter before every query
- [ ] Get org ID from `ICurrentUserService`
- [ ] Register interceptor in `DbContext` options
- [ ] Write integration test: RLS policy filters org B's data when org A queries

**Files to create:**
- `spaceos-kernel/Infrastructure/Persistence/Interceptors/TenantIsolationInterceptor.cs`

**Code snippet:**
```csharp
public override async ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(...)
{
    var orgId = _currentUser.OrganizationId;
    command.CommandText = $"SET LOCAL app.current_organization_id = {orgId}; {command.CommandText}";
    return await base.ReaderExecutingAsync(...);
}
```

---

#### BE-EHS-006: Create POST /risk-assessments Endpoint (v3-C1+C2 Fixes) (2h)
**Assigned to:** Backend
**Dependencies:** BE-EHS-002, BE-EHS-004, BE-EHS-005
**Description:** Implement Create Risk Assessment endpoint with security fixes

**Acceptance Criteria:**
- [ ] Create `CreateRiskAssessmentRequest` DTO (NO audit fields - v3-C2 fix)
- [ ] Create `CreateRiskAssessmentCommand` + Handler
- [ ] Get `organizationId` from `ICurrentUserService` (v3-C1 fix)
- [ ] Get `createdBy` from `ICurrentUserService` (v3-C2 fix)
- [ ] Call `RiskAssessment.Create(...)` factory method
- [ ] Save to database via `DbContext`
- [ ] Return `201 Created` with `RiskAssessmentResponse`
- [ ] Write integration test: Org A cannot create assessment for Org B
- [ ] Write integration test: Client cannot override `created_at` or `data_hash`

**Files to create:**
- `spaceos-modules-ehs/Contracts/Requests/CreateRiskAssessmentRequest.cs`
- `spaceos-modules-ehs/Application/Commands/CreateRiskAssessmentCommand.cs`
- `spaceos-modules-ehs/Application/Commands/CreateRiskAssessmentHandler.cs`

**Code snippet (v3-C1+C2 fixes):**
```csharp
public async Task<RiskAssessmentResponse> Handle(...)
{
    var organizationId = _currentUser.OrganizationId;  // ✅ v3-C1 fix
    var createdBy = _currentUser.UserId;               // ✅ v3-C2 fix

    var assessment = RiskAssessment.Create(
        organizationId: organizationId,  // ✅ Server-side only
        likelihoodBefore: request.LikelihoodBefore,
        // ... other fields from request
        createdBy: createdBy  // ✅ Server-side only
    );

    await _dbContext.AddAsync(assessment);
    await _dbContext.SaveChangesAsync();

    return mapper.Map(assessment);
}
```

---

#### BE-EHS-007: FluentValidation for CreateRiskAssessment (1h)
**Assigned to:** Backend
**Dependencies:** BE-EHS-006
**Description:** Create FluentValidation rules for request DTO

**Acceptance Criteria:**
- [ ] Install `FluentValidation.AspNetCore` package
- [ ] Create `CreateRiskAssessmentValidator`
- [ ] Validate likelihood/severity range (1-5)
- [ ] Validate category (not empty, max 50 chars)
- [ ] Validate notes (max 2000 chars)
- [ ] Validate `likelihoodAfter`/`severityAfter` together (both or neither)
- [ ] Write unit test: Validator rejects likelihood = 6
- [ ] Write integration test: Invalid request returns 400 with error details

**Files to create:**
- `spaceos-modules-ehs/Application/Validators/CreateRiskAssessmentValidator.cs`

---

#### BE-EHS-008: Create GET /risk-assessments/:id/latest Endpoint (0.5h)
**Assigned to:** Backend
**Dependencies:** BE-EHS-006
**Description:** Get most recent risk assessment for an assessment

**Acceptance Criteria:**
- [ ] Create `GetLatestRiskAssessmentQuery` + Handler
- [ ] **SECURITY FIX (v3-H2):** Validate `assessmentId` belongs to current org (IDOR fix)
- [ ] Return 404 if not found (same error for non-existent and unauthorized)
- [ ] Write integration test: Org A cannot access Org B's assessment

**Files to create:**
- `spaceos-modules-ehs/Application/Queries/GetLatestRiskAssessmentQuery.cs`
- `spaceos-modules-ehs/Application/Queries/GetLatestRiskAssessmentHandler.cs`

**Code snippet (v3-H2 fix):**
```csharp
// ✅ IDOR fix: verify ownership
var assessment = await _dbContext.Assessments
    .Where(a => a.Id == query.AssessmentId)
    .Where(a => a.OrganizationId == _currentUser.OrganizationId)  // ✅ Authorization
    .FirstOrDefaultAsync();

if (assessment == null)
{
    throw new NotFoundException($"Assessment {query.AssessmentId} not found");
}
```

---

#### BE-EHS-009: Create GET /risk-assessments/:id/history Endpoint (v4-H1 Fix) (2h)
**Assigned to:** Backend
**Dependencies:** BE-EHS-006, BE-EHS-008
**Description:** Get paginated risk assessment history

**Acceptance Criteria:**
- [ ] Create `GetRiskAssessmentHistoryQuery` with pagination params (page, pageSize)
- [ ] **PAGINATION FIX (v4-H1):** Implement pagination (default 50, max 100)
- [ ] **SECURITY FIX (v3-H2):** Validate assessmentId ownership
- [ ] Return `PagedRiskAssessmentHistoryResponse` (items, page, pageSize, totalCount, totalPages)
- [ ] Filter by period (7d, 30d, 90d, all)
- [ ] Write integration test: Page 1 returns 50 items, page 2 returns next 50
- [ ] Write E2E test: 150 assessments → 3 pages

**Files to create:**
- `spaceos-modules-ehs/Application/Queries/GetRiskAssessmentHistoryQuery.cs`
- `spaceos-modules-ehs/Application/Queries/GetRiskAssessmentHistoryHandler.cs`
- `spaceos-modules-ehs/Contracts/Responses/PagedRiskAssessmentHistoryResponse.cs`

**Code snippet (v4-H1 fix):**
```csharp
// ✅ Pagination fix
var skip = (query.Page - 1) * query.PageSize;
var take = Math.Min(query.PageSize, 100);  // ✅ Cap at 100

var totalCount = await historyQuery.CountAsync();
var items = await historyQuery
    .Skip(skip)
    .Take(take)
    .ToListAsync();

return new PagedRiskAssessmentHistoryResponse
{
    Items = mapper.Map(items),
    Page = query.Page,
    PageSize = take,
    TotalCount = totalCount,
    TotalPages = (int)Math.Ceiling(totalCount / (double)take)
};
```

---

#### BE-EHS-010: Add Rate Limiting (v3-H4 Fix) (1h)
**Assigned to:** Backend
**Dependencies:** BE-EHS-006
**Description:** Implement rate limiting on EHS API endpoints

**Acceptance Criteria:**
- [ ] Install `AspNetCoreRateLimit` package
- [ ] Configure IP rate limiting in `appsettings.json`
- [ ] POST endpoints: 10 requests/minute per IP
- [ ] GET endpoints: 100 requests/minute per IP
- [ ] Return `429 Too Many Requests` with `Retry-After` header
- [ ] Write E2E test: 11th POST request in 1 minute returns 429

**Files to modify:**
- `appsettings.json`
- `Program.cs` or `Startup.cs`

**Code snippet (v3-H4 fix):**
```json
{
  "IpRateLimiting": {
    "GeneralRules": [
      { "Endpoint": "POST:/api/ehs/*", "Period": "1m", "Limit": 10 },
      { "Endpoint": "GET:/api/ehs/*", "Period": "1m", "Limit": 100 }
    ]
  }
}
```

---

#### BE-EHS-011: Implement RFC 7807 Error Responses (v4-H3 Fix) (1.5h)
**Assigned to:** Backend
**Dependencies:** BE-EHS-006
**Description:** Standardize error responses using Problem Details

**Acceptance Criteria:**
- [ ] Create `ProblemDetailsResponse` DTO
- [ ] Implement `GlobalExceptionHandler` middleware
- [ ] Map exceptions to Problem Details:
  - [ ] `ValidationException` → 400 with field errors
  - [ ] `NotFoundException` → 404
  - [ ] `ForbiddenAccessException` → 403
  - [ ] Unhandled exceptions → 500 (generic message in Production)
- [ ] Include `type`, `title`, `status`, `detail`, `instance` fields
- [ ] Write integration test: Validation error returns RFC 7807 format

**Files to create:**
- `spaceos-kernel/Contracts/Responses/ProblemDetailsResponse.cs`
- `spaceos-kernel/Infrastructure/Middleware/GlobalExceptionHandler.cs`

---

### Week 2 Track A: EHS Frontend (Frontend)

#### FE-EHS-001: App Store EHS Risk State (1h)
**Assigned to:** Frontend
**Dependencies:** BE-EHS-006
**Description:** Extend `app-store.jsx` with EHS risk assessment state

**Acceptance Criteria:**
- [ ] Add `currentRiskAssessment` state
- [ ] Add `riskAssessmentHistory` state
- [ ] Implement `addRiskAssessment(data)` method (calls POST endpoint)
- [ ] Implement `loadRiskAssessmentHistory(assessmentId, page)` method
- [ ] Write unit test: `addRiskAssessment` updates state

**Files to modify:**
- `design-portal/src/store/app-store.jsx`

---

#### FE-EHS-002: Zod Validation Schema (Synced with Backend) (1h)
**Assigned to:** Frontend
**Dependencies:** BE-EHS-007
**Description:** Create Zod schema matching FluentValidation rules

**Acceptance Criteria:**
- [ ] Install `zod` npm package
- [ ] Create `createRiskAssessmentSchema` in `schemas/ehs.ts`
- [ ] Validate likelihood/severity (1-5 range)
- [ ] Validate category (not empty, max 50 chars)
- [ ] Validate notes (max 2000 chars)
- [ ] Validate `likelihoodAfter`/`severityAfter` together (refine method)
- [ ] **v4-H2 FIX:** Write E2E test: Backend 400 error → Zod also rejects

**Files to create:**
- `design-portal/src/schemas/ehs.ts`

**Code snippet (v4-H2 fix - sync test):**
```typescript
test('Zod schema matches backend validation', async () => {
  const payload = {
    likelihoodBefore: 3,
    severityBefore: 4,
    category: 'machinery',
    notes: 'x'.repeat(2001)  // Over limit
  };

  // ✅ Backend rejects
  const response = await fetch('/api/ehs/risk-assessments', {
    method: 'POST',
    body: JSON.stringify(payload)
  });
  expect(response.status).toBe(400);

  // ✅ Zod also rejects
  const zodResult = createRiskAssessmentSchema.safeParse(payload);
  expect(zodResult.success).toBe(false);
});
```

---

#### FE-EHS-003: RiskSliders Component (Before/After) (1.5h)
**Assigned to:** Frontend
**Dependencies:** FE-EHS-001
**Description:** Create likelihood + severity sliders

**Acceptance Criteria:**
- [ ] Create `RiskSliders.jsx` component
- [ ] Two sliders: Likelihood (1-5) + Severity (1-5)
- [ ] Real-time risk score display (likelihood × severity)
- [ ] Color-coded risk level (green/lime/yellow/red)
- [ ] Before/After mode (disabled after sliders if no mitigation)
- [ ] Write unit test: Risk score updates when slider changes

**Files to create:**
- `design-portal/src/components/ehs/RiskCalculatorWidget/RiskSliders.jsx`

---

#### FE-EHS-004: RiskMatrix5x5 Component (1.5h)
**Assigned to:** Frontend
**Dependencies:** FE-EHS-003
**Description:** Create 5×5 risk matrix visualization

**Acceptance Criteria:**
- [ ] Create `RiskMatrix5x5.jsx` component
- [ ] 5×5 grid (likelihood × severity)
- [ ] Highlight current cell (likelihood, severity)
- [ ] Color-coded cells (green/lime/yellow/red based on score)
- [ ] Responsive: mobile shows score card, desktop shows full matrix
- [ ] Write E2E test: Matrix highlights correct cell for (3, 4)

**Files to create:**
- `design-portal/src/components/ehs/RiskCalculatorWidget/RiskMatrix5x5.jsx`

---

#### FE-EHS-005: RiskCalculatorWidget + Save to Backend (2h)
**Assigned to:** Frontend
**Dependencies:** FE-EHS-001, FE-EHS-002, FE-EHS-003, FE-EHS-004
**Description:** Assemble calculator widget and wire up save functionality

**Acceptance Criteria:**
- [ ] Create `RiskCalculatorWidget` container component
- [ ] Wire up before/after sliders
- [ ] Display RiskMatrix5x5
- [ ] Display risk score + color + action label
- [ ] Save button calls `addRiskAssessment` (POST endpoint)
- [ ] Show success toast on save
- [ ] Handle validation errors (Zod + backend errors)
- [ ] Write E2E test: Create assessment → saved to backend

**Files to create:**
- `design-portal/src/components/ehs/RiskCalculatorWidget/index.jsx`

---

#### FE-EHS-006: RiskTrendChart Component (1.5h)
**Assigned to:** Frontend
**Dependencies:** BE-EHS-009, FE-EHS-001
**Description:** Create historical trend chart using Chart.js

**Acceptance Criteria:**
- [ ] Install `chart.js` and `react-chartjs-2` packages
- [ ] Create `RiskTrendChart.jsx` component
- [ ] Load data from GET /history endpoint (paginated)
- [ ] Line chart: X-axis = createdAt, Y-axis = riskScoreBefore/After
- [ ] Period selector: 7d / 30d / 90d buttons
- [ ] Write E2E test: Chart displays 10 data points

**Files to create:**
- `design-portal/src/components/ehs/RiskTrendChart/index.jsx`

---

#### FE-EHS-007: Achievement System (Gamification) (2h)
**Assigned to:** Frontend
**Dependencies:** BE-EHS-009, FE-EHS-001
**Description:** Implement achievement unlock logic and badges

**Acceptance Criteria:**
- [ ] Create `useAchievements.js` hook
- [ ] Define achievements: Safety Champion, Risk Mitigator, Consistency King
- [ ] Check unlock conditions based on history data
- [ ] Create `AchievementBadges.jsx` component
- [ ] Opt-out mode (check `settings.quietMode`)
- [ ] Write unit test: Safety Champion unlocks after 10 low-risk assessments

**Files to create:**
- `design-portal/src/hooks/useAchievements.js`
- `design-portal/src/components/ehs/RiskCalculatorWidget/AchievementBadges.jsx`

---

#### FE-EHS-008: PDF Export Functionality (1.5h)
**Assigned to:** Frontend
**Dependencies:** FE-EHS-005
**Description:** Export risk assessment to PDF using jsPDF

**Acceptance Criteria:**
- [ ] Install `jspdf` package
- [ ] Create `useRiskExport.js` hook
- [ ] Generate PDF with:
  - [ ] Assessment title, date, created by
  - [ ] Risk score before/after
  - [ ] Risk matrix visualization (text-based)
  - [ ] Mitigation notes
- [ ] Export PDF button in `RiskCalculatorWidget`
- [ ] Write E2E test: PDF download triggers

**Files to create:**
- `design-portal/src/hooks/useRiskExport.js`

---

### Week 2 Track B: Optional Enhancements

#### FE-CAT-008: Recommendation Carousel (Optional) (1.5h)
**Assigned to:** Frontend
**Dependencies:** FE-CAT-006
**Description:** Add browsing history-based recommendations

**Acceptance Criteria:**
- [ ] Create `useRecommendations.js` hook
- [ ] Track viewed items in localStorage (max 100, LRU eviction)
- [ ] Recommend based on: recently viewed, same category, price range
- [ ] Create `RecommendationCarousel.jsx` component
- [ ] Write unit test: Recommendation algorithm returns relevant items

**Files to create:**
- `design-portal/src/hooks/useRecommendations.js`
- `design-portal/src/components/catalog/RecommendationCarousel/index.jsx`

---

#### BE-CAT-001: Catalog Tracking Endpoints (Optional) (1h)
**Assigned to:** Backend
**Dependencies:** None
**Description:** Add view count tracking for recommendations

**Acceptance Criteria:**
- [ ] Add `view_count`, `search_keywords`, `last_viewed_at` columns to `catalog.items`
- [ ] Create `POST /api/catalog/:id/track-view` endpoint (increment view count)
- [ ] Create `GET /api/catalog/categories` endpoint (distinct categories)
- [ ] Write integration test: Track view increments counter

**Files to modify:**
- `spaceos-modules-catalog/Infrastructure/Persistence/Migrations/...sql`

---

## Implementation Roadmap

### Week 1: Days 1-3 (Parallel Tracks)

**Track A (Frontend):**
- Day 1: FE-CAT-001, FE-CAT-002, FE-CAT-003
- Day 2: FE-CAT-004, FE-CAT-005
- Day 3: FE-CAT-006, FE-CAT-007

**Track B (Backend):**
- Day 2: BE-EHS-001, BE-EHS-002, BE-EHS-003, BE-EHS-004, BE-EHS-005
- Day 3: BE-EHS-006, BE-EHS-007
- Day 4: BE-EHS-008, BE-EHS-009, BE-EHS-010, BE-EHS-011

---

### Week 2: Days 5-10

**Frontend:**
- Day 5-6: FE-EHS-001, FE-EHS-002, FE-EHS-003, FE-EHS-004
- Day 7-8: FE-EHS-005, FE-EHS-006, FE-EHS-007
- Day 9-10: FE-EHS-008, FE-CAT-008 (optional)

**Backend (optional):**
- Day 9-10: BE-CAT-001 (catalog tracking)

---

## Testing Checklist

### Unit Tests
- [ ] RiskAssessment factory method validates domain rules
- [ ] Achievement unlock logic (Safety Champion, Risk Mitigator)
- [ ] Fuzzy search algorithm (typo tolerance)
- [ ] Zod validation schema (matches backend rules)

### Integration Tests
- [ ] POST /risk-assessments → RLS policy enforced (Org A ≠ Org B)
- [ ] GET /risk-assessments/:id/history → pagination works (150 items → 3 pages)
- [ ] IDOR attack blocked (Org A cannot access Org B's assessment)
- [ ] Rate limiting (11th POST returns 429)
- [ ] RFC 7807 error format (validation error returns Problem Details)

### E2E Tests
- [ ] XSS payload in catalog search → escaped (no alert)
- [ ] Create risk assessment → saved to backend → appears in history
- [ ] Risk matrix highlights correct cell for (likelihood=3, severity=4)
- [ ] PDF export downloads file

---

## Deployment Checklist

### Week 1 Deployment (Catalog MVP + EHS Backend)
- [ ] Run database migration (EHS schema + tables)
- [ ] Deploy backend (EHS module + security fixes)
- [ ] Deploy frontend (catalog filter)
- [ ] Run smoke tests (catalog search, EHS POST endpoint)
- [ ] Monitor error logs (check for RLS violations)

### Week 2 Deployment (EHS UI + Polish)
- [ ] Deploy frontend (EHS UI components)
- [ ] Run E2E tests (risk calculator, trend chart, PDF export)
- [ ] Load test (1000 concurrent users on catalog filter)
- [ ] Performance audit (Lighthouse score >90)

---

## Next Steps

1. **Conductor** dispatches tasks to Backend and Frontend terminals
2. **Backend terminal** implements BE-EHS-001 through BE-EHS-011 (Week 1)
3. **Frontend terminal** implements FE-CAT-001 through FE-CAT-007 (Week 1)
4. **Week 1 checkpoint:** Catalog MVP + EHS backend deployed
5. **Week 2:** Frontend terminal implements FE-EHS-001 through FE-EHS-008
6. **Final review:** E2E tests, security audit, performance benchmarks

---

**STATUS:** ✅ READY FOR IMPLEMENTATION
**ASSIGN TO:** Conductor → dispatch tasks to Backend & Frontend terminals

---

**END OF FINAL ARCHITECTURE**
