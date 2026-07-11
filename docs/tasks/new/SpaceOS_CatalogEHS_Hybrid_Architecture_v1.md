# SpaceOS Catalog + EHS Hybrid Architecture (v1 DRAFT)

**Status:** DRAFT
**Created:** 2026-06-23
**Epic:** CATALOG-EHS-HYBRID
**Timeline:** 2 weeks (Week 1: Foundation + Catalog MVP, Week 2: EHS UI + Polish)

---

## Executive Summary

This architecture implements a **hybrid UI-first + backend-safety strategy** for two high-value features:

1. **Catalog Filter** (frontend-only MVP, Week 1) — immediate business value with fuzzy search, URL state sync, and voice search
2. **EHS Risk Calculator** (backend-first, Week 1-2) — compliance-critical with audit trail, gamification, and PDF export

The architecture follows SpaceOS 5 Golden Rules:
- ✅ **Data → Rules → Geometry** — frontend filters locally cached catalog, backend stores risk calculations
- ✅ **Modular Monolith** — EHS module extends Kernel audit trail patterns
- ✅ **Immutability & Trust** — RiskAssessment is append-only, no UPDATE allowed
- ✅ **Need-to-Know RBAC** — risk assessments scoped to organization/workspace
- ✅ **Walking Skeleton First** — Week 1 delivers working catalog filter + EHS backend

---

## 1. Feature Overview

### Feature A: Catalog Filter System

**User Story:** As a purchasing manager, I want to quickly find products in the catalog using text search, category filters, price range, and stock status, so I can make procurement decisions faster.

**Technical Approach:**
- **Frontend-only** implementation (localStorage + app-store.jsx)
- **No backend API required** for Week 1 MVP
- Optional Week 2 backend tracking for recommendations

**Key Capabilities:**
- Fuzzy text search (typo-tolerant, searches name/SKU/category)
- Multi-select category chips
- Dual-thumb price range slider
- Stock status toggle (all/in-stock/out-of-stock)
- URL state synchronization (shareable filter links)
- Voice search (progressive enhancement, Chrome only)
- Recommendation carousel (localStorage-based browsing history)

---

### Feature B: EHS Risk Assessment Calculator

**User Story:** As a safety officer, I want to calculate risk scores (likelihood × severity) before and after mitigation, track improvement trends, and export audit reports, so I comply with ISO 45001 requirements.

**Technical Approach:**
- **Backend-first** (audit trail + compliance critical)
- **Frontend UI** builds on backend API (Week 2)

**Key Capabilities:**
- 5×5 risk matrix with before/after comparison
- Risk score calculation (1-25 scale)
- Color-coded risk levels (green/lime/yellow/red)
- Achievement system (gamification opt-in)
- Historical trend chart (7/30/90 day views)
- PDF export for audit compliance

---

## 2. Domain Model (Backend)

### 2.1 EHS Module - RiskAssessment Entity

**Namespace:** `SpaceOS.Modules.EHS.Domain.Entities`

```csharp
/// <summary>
/// Represents a single risk assessment calculation.
/// Immutable after creation - edits create new records.
/// </summary>
public class RiskAssessment
{
    /// <summary>Primary key (GUID for distributed ID generation)</summary>
    public Guid Id { get; private set; }

    /// <summary>Foreign key to parent Assessment (if exists)</summary>
    public int? AssessmentId { get; private set; }

    /// <summary>Organization/tenant scope (for RLS)</summary>
    public int OrganizationId { get; private set; }

    /// <summary>Likelihood rating before mitigation (1-5 scale)</summary>
    [Range(1, 5)]
    public int LikelihoodBefore { get; private set; }

    /// <summary>Severity rating before mitigation (1-5 scale)</summary>
    [Range(1, 5)]
    public int SeverityBefore { get; private set; }

    /// <summary>Likelihood rating after mitigation (1-5 scale, nullable)</summary>
    [Range(1, 5)]
    public int? LikelihoodAfter { get; private set; }

    /// <summary>Severity rating after mitigation (1-5 scale, nullable)</summary>
    [Range(1, 5)]
    public int? SeverityAfter { get; private set; }

    /// <summary>Risk category (machinery, chemical, ergonomic, etc.)</summary>
    [MaxLength(50)]
    public string Category { get; private set; }

    /// <summary>Free-text notes/mitigation description</summary>
    [MaxLength(2000)]
    public string? Notes { get; private set; }

    /// <summary>Immutable audit trail - creation timestamp</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>Immutable audit trail - creator user ID</summary>
    public string CreatedBy { get; private set; }

    /// <summary>Hash of risk data for integrity verification</summary>
    [MaxLength(64)]
    public string DataHash { get; private set; }

    // Calculated properties (not stored in DB)
    public int RiskScoreBefore => LikelihoodBefore * SeverityBefore;
    public int? RiskScoreAfter => LikelihoodAfter.HasValue && SeverityAfter.HasValue
        ? LikelihoodAfter.Value * SeverityAfter.Value
        : null;
    public int? ImprovementScore => RiskScoreAfter.HasValue
        ? RiskScoreBefore - RiskScoreAfter.Value
        : null;

    // Factory method (enforces immutability)
    public static RiskAssessment Create(
        int organizationId,
        int likelihoodBefore,
        int severityBefore,
        string category,
        string createdBy,
        int? likelihoodAfter = null,
        int? severityAfter = null,
        string? notes = null)
    {
        var assessment = new RiskAssessment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            LikelihoodBefore = likelihoodBefore,
            SeverityBefore = severityBefore,
            LikelihoodAfter = likelihoodAfter,
            SeverityAfter = severityAfter,
            Category = category,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };

        assessment.DataHash = assessment.ComputeHash();
        return assessment;
    }

    private string ComputeHash()
    {
        var data = $"{LikelihoodBefore}{SeverityBefore}{LikelihoodAfter}{SeverityAfter}{Category}{OrganizationId}";
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var hash = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash);
    }
}
```

---

### 2.2 Catalog Module - CatalogItem Enhancements (OPTIONAL - Week 2)

**Namespace:** `SpaceOS.Modules.Catalog.Domain.Entities`

```csharp
public partial class CatalogItem
{
    // EXISTING FIELDS (from current implementation)
    // public int Id { get; set; }
    // public string Name { get; set; }
    // public string SKU { get; set; }
    // public string Category { get; set; }
    // public decimal Price { get; set; }
    // public int StockQuantity { get; set; }

    /// <summary>Tracking for recommendation algorithm (Week 2 optional)</summary>
    public int ViewCount { get; private set; }

    /// <summary>Pre-computed search keywords for fuzzy matching (Week 2 optional)</summary>
    public string[]? SearchKeywords { get; private set; }

    /// <summary>Last viewed timestamp (for trending/recommendation)</summary>
    public DateTime? LastViewedAt { get; private set; }

    // Increment view count (for recommendation engine)
    public void IncrementViewCount()
    {
        ViewCount++;
        LastViewedAt = DateTime.UtcNow;
    }
}
```

---

## 3. Database Schema

### 3.1 EHS Risk Assessment Table

**Table:** `ehs.risk_assessments`
**Schema:** `ehs` (new module schema)
**RLS Policy:** `organization_id` scoping

```sql
CREATE TABLE ehs.risk_assessments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    assessment_id INT NULL,  -- FK to future ehs.assessments table
    organization_id INT NOT NULL,  -- FK to kernel.organizations
    likelihood_before INT NOT NULL CHECK (likelihood_before BETWEEN 1 AND 5),
    severity_before INT NOT NULL CHECK (severity_before BETWEEN 1 AND 5),
    likelihood_after INT NULL CHECK (likelihood_after BETWEEN 1 AND 5),
    severity_after INT NULL CHECK (severity_after BETWEEN 1 AND 5),
    category VARCHAR(50) NOT NULL,
    notes TEXT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by VARCHAR(255) NOT NULL,  -- user ID from auth
    data_hash VARCHAR(64) NOT NULL,

    -- Constraints
    CONSTRAINT fk_organization FOREIGN KEY (organization_id)
        REFERENCES kernel.organizations(id) ON DELETE CASCADE,
    CONSTRAINT chk_after_values CHECK (
        (likelihood_after IS NULL AND severity_after IS NULL) OR
        (likelihood_after IS NOT NULL AND severity_after IS NOT NULL)
    )
);

-- Indexes
CREATE INDEX idx_risk_assessments_org ON ehs.risk_assessments(organization_id);
CREATE INDEX idx_risk_assessments_created_at ON ehs.risk_assessments(created_at DESC);
CREATE INDEX idx_risk_assessments_category ON ehs.risk_assessments(category);
CREATE INDEX idx_risk_assessments_assessment_id ON ehs.risk_assessments(assessment_id)
    WHERE assessment_id IS NOT NULL;

-- Row-Level Security (RLS)
ALTER TABLE ehs.risk_assessments ENABLE ROW LEVEL SECURITY;

CREATE POLICY risk_assessments_tenant_isolation ON ehs.risk_assessments
    USING (organization_id = current_setting('app.current_organization_id')::INT);
```

---

### 3.2 Catalog Item Enhancements (OPTIONAL - Week 2)

**Table:** `catalog.items` (existing table)

```sql
-- Add new columns for tracking/recommendation
ALTER TABLE catalog.items
    ADD COLUMN IF NOT EXISTS view_count INT NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS search_keywords TEXT[] NULL,
    ADD COLUMN IF NOT EXISTS last_viewed_at TIMESTAMPTZ NULL;

-- Index for trending/popular items
CREATE INDEX IF NOT EXISTS idx_catalog_items_view_count
    ON catalog.items(view_count DESC);

-- Index for recently viewed
CREATE INDEX IF NOT EXISTS idx_catalog_items_last_viewed
    ON catalog.items(last_viewed_at DESC NULLS LAST);
```

---

## 4. API Surface

### 4.1 EHS Risk Assessment Endpoints

**Base URL:** `/api/ehs`
**Auth:** JWT required, organization scoped
**Module:** `SpaceOS.Modules.EHS`

#### POST /api/ehs/risk-assessments

**Description:** Create a new risk assessment calculation
**Request Body:**

```typescript
interface CreateRiskAssessmentRequest {
  assessmentId?: number;  // optional parent assessment
  likelihoodBefore: number;  // 1-5
  severityBefore: number;    // 1-5
  likelihoodAfter?: number;  // 1-5 (optional)
  severityAfter?: number;    // 1-5 (optional)
  category: string;          // "machinery" | "chemical" | "ergonomic" | ...
  notes?: string;            // max 2000 chars
}
```

**Response:** `201 Created`

```typescript
interface RiskAssessmentResponse {
  id: string;  // UUID
  assessmentId?: number;
  organizationId: number;
  likelihoodBefore: number;
  severityBefore: number;
  likelihoodAfter?: number;
  severityAfter?: number;
  category: string;
  notes?: string;
  createdAt: string;  // ISO8601
  createdBy: string;
  riskScoreBefore: number;  // calculated: likelihood × severity
  riskScoreAfter?: number;  // calculated
  improvementScore?: number;  // calculated
  dataHash: string;
}
```

**Validation (FluentValidation):**

```csharp
public class CreateRiskAssessmentValidator : AbstractValidator<CreateRiskAssessmentRequest>
{
    public CreateRiskAssessmentValidator()
    {
        RuleFor(x => x.LikelihoodBefore).InclusiveBetween(1, 5);
        RuleFor(x => x.SeverityBefore).InclusiveBetween(1, 5);
        RuleFor(x => x.LikelihoodAfter).InclusiveBetween(1, 5).When(x => x.LikelihoodAfter.HasValue);
        RuleFor(x => x.SeverityAfter).InclusiveBetween(1, 5).When(x => x.SeverityAfter.HasValue);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Notes).MaximumLength(2000);

        // Either both after values or neither
        RuleFor(x => x).Must(x =>
            (x.LikelihoodAfter.HasValue && x.SeverityAfter.HasValue) ||
            (!x.LikelihoodAfter.HasValue && !x.SeverityAfter.HasValue)
        ).WithMessage("Both LikelihoodAfter and SeverityAfter must be provided together");
    }
}
```

**Frontend Validation (Zod - synced schema):**

```typescript
import { z } from 'zod';

export const createRiskAssessmentSchema = z.object({
  assessmentId: z.number().int().positive().optional(),
  likelihoodBefore: z.number().int().min(1).max(5),
  severityBefore: z.number().int().min(1).max(5),
  likelihoodAfter: z.number().int().min(1).max(5).optional(),
  severityAfter: z.number().int().min(1).max(5).optional(),
  category: z.string().min(1).max(50),
  notes: z.string().max(2000).optional()
}).refine(
  (data) =>
    (data.likelihoodAfter !== undefined && data.severityAfter !== undefined) ||
    (data.likelihoodAfter === undefined && data.severityAfter === undefined),
  { message: "Both likelihoodAfter and severityAfter must be provided together" }
);
```

---

#### GET /api/ehs/risk-assessments/:assessmentId/latest

**Description:** Get the most recent risk assessment for a parent assessment
**Response:** `200 OK` - `RiskAssessmentResponse` (same as POST)

---

#### GET /api/ehs/risk-assessments/:assessmentId/history

**Description:** Get historical risk assessments for trend analysis
**Query Params:**
- `period` (optional): `7d` | `30d` | `90d` | `all` (default: `30d`)

**Response:** `200 OK`

```typescript
interface RiskAssessmentHistoryResponse {
  assessmentId: number;
  period: string;
  items: RiskAssessmentResponse[];
  stats: {
    totalAssessments: number;
    avgRiskScoreBefore: number;
    avgRiskScoreAfter: number;
    avgImprovement: number;
  };
}
```

---

### 4.2 Catalog Endpoints (OPTIONAL - Week 2)

**Base URL:** `/api/catalog`
**Auth:** JWT required

#### GET /api/catalog/categories

**Description:** Get distinct category list for filter chips
**Response:** `200 OK`

```typescript
interface CategoriesResponse {
  categories: string[];
}
```

---

#### POST /api/catalog/:id/track-view

**Description:** Track catalog item view for recommendation algorithm
**Response:** `204 No Content`

---

## 5. Frontend Component Architecture

### 5.1 Catalog Filter System

**Location:** `design-portal/src/components/catalog/`

```
CatalogFilterBar/
├── index.jsx                      ← Main filter bar container
├── SmartSearchBar.jsx             ← Text search with fuzzy + voice
├── CategoryChips.jsx              ← Multi-select category filter
├── PriceRangeSlider.jsx           ← Dual-thumb price slider
├── StockStatusToggle.jsx          ← All/In-stock/Out-of-stock toggle
├── ActiveFilterChips.jsx          ← Show active filters with remove
└── VoiceSearchButton.jsx          ← Progressive enhancement for voice

RecommendationCarousel/
└── index.jsx                      ← Browsing history based recommendations

VirtualizedCatalogGrid/
└── index.jsx                      ← react-window grid for 5000+ items

hooks/
├── useCatalogFilters.js           ← Filter logic + fuzzy search
├── useRecommendations.js          ← localStorage-based algorithm
└── useURLSync.js                  ← URL state persistence
```

**Component Hierarchy:**

```jsx
<CatalogWorldView>
  <CatalogFilterBar>
    <SmartSearchBar
      fuzzySearch={true}
      voiceEnabled={true}
      debounce={300}
      onSearch={handleSearch}
    />
    <CategoryChips
      categories={categories}
      selected={filters.category}
      onToggle={handleCategoryToggle}
      multiSelect={true}
    />
    <PriceRangeSlider
      min={0}
      max={1000000}
      value={filters.priceRange}
      onChange={handlePriceChange}
    />
    <StockStatusToggle
      value={filters.stockStatus}
      onChange={handleStockChange}
    />
    <ActiveFilterChips
      filters={activeFilters}
      onRemove={handleFilterRemove}
    />
  </CatalogFilterBar>

  <RecommendationCarousel
    items={recommendations}
    based="browsing-history"
  />

  <VirtualizedCatalogGrid
    items={filteredCatalog}
    rowHeight={180}
    overscan={5}
  />
</CatalogWorldView>
```

---

### 5.2 EHS Risk Calculator

**Location:** `design-portal/src/components/ehs/`

```
RiskCalculatorWidget/
├── index.jsx                      ← Main widget container
├── BeforeAfterTabs.jsx            ← Tab switcher (before/after)
├── RiskSliders.jsx                ← Likelihood + Severity sliders
├── RiskMatrix5x5.jsx              ← 5×5 risk matrix visualization
├── RiskScoreDisplay.jsx           ← Score + color + action label
├── AchievementBadges.jsx          ← Gamification badges
└── ActionButtons.jsx              ← Save + Export PDF

RiskTrendChart/
└── index.jsx                      ← Historical trend chart (Chart.js)

hooks/
├── useRiskCalculator.js           ← Risk score logic + API calls
├── useAchievements.js             ← Achievement unlock logic
└── useRiskExport.js               ← jsPDF export generator
```

**Component Hierarchy:**

```jsx
<PageEHSDashboard>
  <RiskCalculatorWidget assessmentId={id}>
    <BeforeAfterTabs activeTab={tab} onChange={setTab}>
      <RiskSliders
        type="before"
        likelihood={likelihoodBefore}
        severity={severityBefore}
        onChange={handleBeforeChange}
      />
      <RiskSliders
        type="after"
        likelihood={likelihoodAfter}
        severity={severityAfter}
        onChange={handleAfterChange}
        disabled={!hasMitigation}
      />
    </BeforeAfterTabs>

    <RiskMatrix5x5
      highlight={[likelihood, severity]}
      responsive={true}
    />

    <RiskScoreDisplay
      score={riskScore}
      level={riskLevel}
      improvement={improvementScore}
    />

    <AchievementBadges
      unlocked={achievements}
      optOut={settings.quietMode}
    />

    <ActionButtons>
      <SaveButton onClick={saveToBackend} loading={saving} />
      <ExportPDFButton onClick={exportPDF} />
    </ActionButtons>
  </RiskCalculatorWidget>

  <RiskTrendChart
    data={assessmentHistory}
    period="30days"
  />
</PageEHSDashboard>
```

---

## 6. State Management (app-store.jsx extensions)

### 6.1 Catalog Filter State

```javascript
// design-portal/src/store/app-store.jsx

// Add to Zustand store
export const useAppStore = create((set, get) => ({

  // CATALOG FILTERS
  catalogFilters: {
    search: '',
    category: [],
    priceRange: [0, 1000000],
    stockStatus: 'all'  // 'all' | 'in-stock' | 'out-of-stock'
  },

  setFilter: (key, value) => {
    set(state => {
      const newFilters = { ...state.catalogFilters, [key]: value };

      // Persist to localStorage
      localStorage.setItem('catalog_filters', JSON.stringify(newFilters));

      // Sync to URL
      const params = new URLSearchParams();
      if (newFilters.search) params.set('search', newFilters.search);
      if (newFilters.category.length) params.set('cat', newFilters.category.join(','));
      if (newFilters.stockStatus !== 'all') params.set('stock', newFilters.stockStatus);
      if (newFilters.priceRange[0] > 0 || newFilters.priceRange[1] < 1000000) {
        params.set('price', `${newFilters.priceRange[0]}-${newFilters.priceRange[1]}`);
      }

      window.history.pushState({}, '', `?${params.toString()}`);

      return { catalogFilters: newFilters };
    });
  },

  resetFilters: () => {
    set({
      catalogFilters: {
        search: '',
        category: [],
        priceRange: [0, 1000000],
        stockStatus: 'all'
      }
    });
    localStorage.removeItem('catalog_filters');
    window.history.pushState({}, '', window.location.pathname);
  },

  loadFiltersFromURL: () => {
    const params = new URLSearchParams(window.location.search);
    const filters = {
      search: params.get('search') || '',
      category: params.get('cat')?.split(',') || [],
      stockStatus: params.get('stock') || 'all',
      priceRange: params.get('price')?.split('-').map(Number) || [0, 1000000]
    };
    set({ catalogFilters: filters });
  },

  // EHS RISK CALCULATOR
  currentRiskAssessment: null,
  riskAssessmentHistory: [],

  setCurrentRiskAssessment: (assessment) => {
    set({ currentRiskAssessment: assessment });
  },

  addRiskAssessment: async (data) => {
    const response = await fetch('/api/ehs/risk-assessments', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });
    const assessment = await response.json();

    set(state => ({
      currentRiskAssessment: assessment,
      riskAssessmentHistory: [assessment, ...state.riskAssessmentHistory]
    }));

    return assessment;
  },

  loadRiskAssessmentHistory: async (assessmentId, period = '30d') => {
    const response = await fetch(`/api/ehs/risk-assessments/${assessmentId}/history?period=${period}`);
    const data = await response.json();

    set({ riskAssessmentHistory: data.items });
  }
}));
```

---

## 7. Dependencies

### 7.1 NPM Packages (Frontend)

```json
{
  "dependencies": {
    "fuzzysort": "^2.0.4",           // Fuzzy search for catalog
    "react-window": "^1.8.10",       // Virtualized catalog grid
    "jspdf": "^2.5.1",               // PDF export for EHS reports
    "chart.js": "^4.4.0",            // Risk trend charts
    "react-chartjs-2": "^5.2.0",     // React wrapper for Chart.js
    "zod": "^3.22.4"                 // Validation schema (synced with backend)
  }
}
```

---

### 7.2 NuGet Packages (Backend)

```xml
<PackageReference Include="FluentValidation.AspNetCore" Version="11.3.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
```

---

## 8. Implementation Timeline

### Week 1: Foundation + Catalog MVP

**Days 1-3 (Frontend Track):**
- [ ] CatalogFilterBar components (search, category, price, stock)
- [ ] Fuzzy search integration (fuzzysort)
- [ ] URL state sync (useURLSync hook)
- [ ] Virtualized grid (react-window)
- [ ] Voice search (progressive enhancement)
- [ ] app-store.jsx catalog filter state

**Days 2-4 (Backend Track - parallel):**
- [ ] EHS module setup (`ehs` schema)
- [ ] RiskAssessment entity + factory method
- [ ] DB migration for `ehs.risk_assessments` table
- [ ] POST /api/ehs/risk-assessments endpoint
- [ ] GET /api/ehs/risk-assessments/:id/latest endpoint
- [ ] GET /api/ehs/risk-assessments/:id/history endpoint
- [ ] FluentValidation rules
- [ ] Unit tests (RiskAssessment factory, hash computation)
- [ ] Integration tests (API endpoints)

---

### Week 2: EHS UI + Polish

**Days 5-8:**
- [ ] RiskCalculatorWidget components (sliders, matrix, score display)
- [ ] BeforeAfterTabs UI
- [ ] RiskMatrix5x5 visualization
- [ ] Achievement system (useAchievements hook)
- [ ] RiskTrendChart (Chart.js integration)
- [ ] PDF export (jsPDF)
- [ ] app-store.jsx EHS state management
- [ ] Zod validation schema (synced with backend)

**Days 7-10 (optional enhancements - parallel):**
- [ ] RecommendationCarousel (localStorage-based)
- [ ] Catalog ViewCount tracking (backend)
- [ ] GET /api/catalog/categories endpoint
- [ ] POST /api/catalog/:id/track-view endpoint

---

## 9. Open Questions & Risks

### 9.1 Open Questions

**Q1: Catalog backend integration timing**
Should we implement catalog tracking (ViewCount, SearchKeywords) in Week 1 or Week 2?

**Recommendation:** Week 2 (optional). Catalog filter MVP doesn't need backend for core value.

---

**Q2: Achievement opt-out mechanism**
How should users disable gamification if they prefer "quiet mode"?

**Recommendation:** User settings toggle in profile (`settings.quietMode`). Persist to backend user preferences table.

---

**Q3: Voice search browser support**
Voice search only works in Chrome (WebKit Speech API). How to handle Firefox/Safari users?

**Recommendation:** Progressive enhancement - button hidden on unsupported browsers. No fallback needed.

---

**Q4: Risk assessment parent linkage**
The `assessment_id` FK references a future `ehs.assessments` table. Should we create skeleton table now?

**Recommendation:** No. Allow `assessment_id` to be nullable. Create parent table when full assessment module is scoped.

---

### 9.2 Risks

| Risk | Impact | Mitigation |
|---|---|---|
| **localStorage size limit** (5-10MB) for catalog filters + recommendations | 🟡 MEDIUM | Monitor usage. Implement LRU cache for browsing history (max 100 items). Clear old data on overflow. |
| **Fuzzy search performance** on 5000+ catalog items | 🟡 MEDIUM | Use `fuzzysort` with `threshold: -10000` to limit results. Virtualize grid to render only visible rows. |
| **Validation schema drift** (Zod vs FluentValidation) | 🟡 MEDIUM | Manual sync for v1. Week 2: explore schema code generation from OpenAPI or shared JSON schema. |
| **Mobile UX** for 5×5 risk matrix | 🟡 MEDIUM | Responsive design: show score card on mobile, full matrix in modal on tap. Test on 375px viewport. |
| **PDF export file size** for audit reports | 🟢 LOW | jsPDF generates lightweight PDFs (~50-100KB). No images, only text + vector shapes. |
| **RLS policy performance** on large `risk_assessments` table | 🟢 LOW | Index on `organization_id` + `created_at`. Partition table by year if >1M records. |

---

## 10. Next Steps (v2 Review)

- [ ] **DB Review (v2):** Validate `ehs.risk_assessments` schema, indexes, RLS policy
- [ ] **Security Review (v3):** OWASP Top 10 check (injection, XSS, RBAC, audit trail)
- [ ] **Backend Review (v4):** API design, validation, error handling, performance

---

## Appendix: Code Snippets

### A1. Fuzzy Search Implementation

```javascript
// hooks/useCatalogFilters.js
import Fuzzysort from 'fuzzysort';

export const useCatalogFilters = () => {
  const { catalogFilters, setFilter } = useAppStore();
  const catalog = useCatalog();  // fetch from API or localStorage

  const applyFuzzySearch = (items, searchTerm) => {
    if (!searchTerm) return items;

    const results = Fuzzysort.go(searchTerm, items, {
      keys: ['name', 'sku', 'category'],
      threshold: -10000,  // allow typos
      limit: 500  // performance cap
    });

    return results.map(r => r.obj);
  };

  const filteredCatalog = useMemo(() => {
    let items = catalog;

    // Text search (fuzzy)
    items = applyFuzzySearch(items, catalogFilters.search);

    // Category filter
    if (catalogFilters.category.length > 0) {
      items = items.filter(item =>
        catalogFilters.category.includes(item.category)
      );
    }

    // Price range filter
    const [minPrice, maxPrice] = catalogFilters.priceRange;
    items = items.filter(item =>
      item.price >= minPrice && item.price <= maxPrice
    );

    // Stock status filter
    if (catalogFilters.stockStatus === 'in-stock') {
      items = items.filter(item => item.stockQuantity > 0);
    } else if (catalogFilters.stockStatus === 'out-of-stock') {
      items = items.filter(item => item.stockQuantity === 0);
    }

    return items;
  }, [catalog, catalogFilters]);

  return { filteredCatalog, setFilter };
};
```

---

### A2. Risk Level Calculation Logic

```javascript
// utils/riskCalculator.js

export const getRiskLevel = (score) => {
  if (score <= 4) {
    return {
      color: '#10b981',  // green
      label: 'Elhanyagolható',
      action: 'Nincs intézkedés szükséges',
      priority: 1
    };
  }

  if (score <= 9) {
    return {
      color: '#84cc16',  // lime
      label: 'Alacsony',
      action: 'Figyelem javasolt',
      priority: 2
    };
  }

  if (score <= 16) {
    return {
      color: '#eab308',  // yellow
      label: 'Közepes',
      action: 'Cselekvési terv készítése',
      priority: 3
    };
  }

  return {
    color: '#ef4444',  // red
    label: 'Magas',
    action: 'Azonnali intézkedés szükséges!',
    priority: 4,
    alert: true  // trigger browser notification
  };
};

export const calculateImprovement = (before, after) => {
  if (!after) return null;
  const improvement = before - after;
  const percentage = ((improvement / before) * 100).toFixed(1);
  return { score: improvement, percentage };
};
```

---

### A3. Achievement System Logic

```javascript
// hooks/useAchievements.js

const ACHIEVEMENTS = {
  safetyChampion: {
    id: 'safety-champion',
    title: 'Safety Champion',
    description: 'Created 10 low-risk assessments (score ≤ 6)',
    badge: '🛡️',
    condition: (history) =>
      history.filter(r => r.riskScoreBefore <= 6).length >= 10
  },

  riskMitigator: {
    id: 'risk-mitigator',
    title: 'Risk Mitigator',
    description: 'Reduced risk by 10+ points in 3 assessments',
    badge: '🎯',
    condition: (history) => {
      const improvements = history.filter(r =>
        r.improvementScore && r.improvementScore >= 10
      );
      return improvements.length >= 3;
    }
  },

  consistencyKing: {
    id: 'consistency-king',
    title: 'Consistency King',
    description: 'Completed 7 consecutive days of assessments',
    badge: '👑',
    condition: (history) => {
      const last7Days = history.filter(r => {
        const daysDiff = (Date.now() - new Date(r.createdAt)) / (1000 * 60 * 60 * 24);
        return daysDiff <= 7;
      });

      const uniqueDays = new Set(
        last7Days.map(r => new Date(r.createdAt).toDateString())
      );

      return uniqueDays.size >= 7;
    }
  }
};

export const useAchievements = (history) => {
  const { settings } = useAppStore();

  if (settings.quietMode) return { unlocked: [], progress: {} };

  const unlocked = Object.values(ACHIEVEMENTS)
    .filter(achievement => achievement.condition(history));

  return { unlocked, allAchievements: ACHIEVEMENTS };
};
```

---

**END OF v1 DRAFT**

Next: v2 DB Review
