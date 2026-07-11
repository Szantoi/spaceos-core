---
id: MSG-FRONTEND-101
from: conductor
to: frontend
type: task
priority: high
status: READ
model: sonnet
ref: MSG-FRONTEND-100-DONE
created: 2026-07-04
completed: 2026-07-04
content_hash: 4f2cf910b5e8f187a46b779d768aef5ecb4d62deeb97c9e6c4f8e2755d18de0a
---

# CRM Wave 2 Phase 3: Advanced Filters + URL State Sync

**Epic:** EPIC-JT-CRM Frontend Wave 2
**Phase:** 3 of 3 (Phase 1 ✅ Form Validation → Phase 2 ✅ Drag & Drop → **Phase 3 Advanced Filters**)

---

## Task Summary

Implement **advanced filtering** for both LeadGrid and OpportunityPipeline with **URL state synchronization** for shareable filtered views.

Users should be able to filter leads and opportunities by multiple criteria, with filters persisted in URL query parameters for bookmarking and sharing.

---

## Acceptance Criteria

### LeadGrid Filters
- [ ] Date range picker (createdAt from/to)
- [ ] Debounced search input (company, contact name, email)
- [ ] Multi-select status dropdown (allow multiple: New, Contacted, Qualified, etc.)
- [ ] Clear all filters button
- [ ] URL state sync (`?status=New,Contacted&search=acme&from=2026-01-01`)

### OpportunityPipeline Filters
- [ ] AssignedTo dropdown (user selection)
- [ ] ExpectedCloseDate range picker
- [ ] Min/Max value sliders (pipeline value in HUF)
- [ ] Stage visibility toggles (show/hide specific stages)
- [ ] URL state sync (`?assignedTo=user-123&minValue=500000&stage=proposal,negotiation`)

### URL State Management
- [ ] Query params reflect filter state
- [ ] Browser back/forward support (URL changes trigger filter updates)
- [ ] Shareable filtered views (copy URL → share → same view loads)
- [ ] URL params validated on page load (invalid params ignored gracefully)

### Build & Testing
- [ ] Build verification: 0 TypeScript errors
- [ ] Manual testing: filters work in browser
- [ ] URL sync: back/forward navigation works
- [ ] Shareable URLs: paste URL → correct filters applied

---

## Context: Wave 2 Progress

**Already complete (Phase 1-2):**
- ✅ **Phase 1:** Form Validation (Zod + react-hook-form, Hungarian errors, toast notifications)
- ✅ **Phase 2:** Drag & Drop (484 LOC, @dnd-kit, 6 stages, optimistic UI)

**This task (Phase 3):**
- Advanced Filters + URL State Sync

**After completion:**
- ✅ CRM Wave 2: **100% COMPLETE**
- 🚀 Ready for Backend API integration (swap mock → real)

---

## Implementation Steps

### 1. Install Dependencies (if needed)

```bash
cd /opt/spaceos/datahaven-web/client
npm install react-router-dom@6 @tanstack/router-zod-adapter zod
```

**Note:** If already installed, skip this step.

---

### 2. URL State Hook (useUrlFilters)

**Location:** `src/hooks/useUrlFilters.ts` (NEW)

```typescript
import { useSearchParams } from 'react-router-dom';
import { useMemo } from 'react';

export interface LeadFilters {
  search?: string;
  status?: string[]; // Multi-select
  fromDate?: string; // ISO date
  toDate?: string;
}

export interface OpportunityFilters {
  assignedTo?: string;
  minValue?: number;
  maxValue?: number;
  fromCloseDate?: string;
  toCloseDate?: string;
  stages?: string[]; // Multi-select
}

export function useUrlFilters<T>() {
  const [searchParams, setSearchParams] = useSearchParams();

  const filters = useMemo(() => {
    const params: any = {};
    searchParams.forEach((value, key) => {
      // Handle comma-separated multi-select (e.g., status=New,Contacted)
      if (key === 'status' || key === 'stages') {
        params[key] = value.split(',').filter(Boolean);
      }
      // Handle numbers
      else if (key === 'minValue' || key === 'maxValue') {
        const num = parseInt(value, 10);
        if (!isNaN(num)) params[key] = num;
      }
      // Handle strings
      else {
        params[key] = value;
      }
    });
    return params as T;
  }, [searchParams]);

  const setFilters = (newFilters: Partial<T>) => {
    const params = new URLSearchParams();
    Object.entries(newFilters).forEach(([key, value]) => {
      if (value === undefined || value === null || value === '') return;

      // Handle arrays (multi-select)
      if (Array.isArray(value)) {
        if (value.length > 0) params.set(key, value.join(','));
      }
      // Handle numbers
      else if (typeof value === 'number') {
        params.set(key, value.toString());
      }
      // Handle strings
      else {
        params.set(key, value as string);
      }
    });
    setSearchParams(params, { replace: true }); // Don't add to history
  };

  const clearFilters = () => {
    setSearchParams({}, { replace: true });
  };

  return { filters, setFilters, clearFilters };
}
```

---

### 3. LeadGrid Filters Component

**Location:** `src/components/features/LeadGrid/LeadFilters.tsx` (NEW)

```tsx
import { useState, useEffect } from 'react';
import { useUrlFilters, LeadFilters as Filters } from '@/hooks/useUrlFilters';
import { useDebouncedValue } from '@/hooks/useDebouncedValue'; // Implement if missing

export function LeadFilters() {
  const { filters, setFilters, clearFilters } = useUrlFilters<Filters>();
  const [search, setSearch] = useState(filters.search || '');
  const debouncedSearch = useDebouncedValue(search, 500); // 500ms debounce

  // Update URL when debounced search changes
  useEffect(() => {
    if (debouncedSearch !== filters.search) {
      setFilters({ ...filters, search: debouncedSearch });
    }
  }, [debouncedSearch]);

  return (
    <div className="lead-filters">
      {/* Search Input */}
      <input
        type="text"
        placeholder="Search company, contact, email..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        className="search-input"
      />

      {/* Multi-Select Status */}
      <select
        multiple
        value={filters.status || []}
        onChange={(e) => {
          const selected = Array.from(e.target.selectedOptions, opt => opt.value);
          setFilters({ ...filters, status: selected });
        }}
        className="status-select"
      >
        <option value="New">New</option>
        <option value="Contacted">Contacted</option>
        <option value="Qualified">Qualified</option>
        <option value="Converted">Converted</option>
        <option value="Lost">Lost</option>
      </select>

      {/* Date Range */}
      <input
        type="date"
        placeholder="From date"
        value={filters.fromDate || ''}
        onChange={(e) => setFilters({ ...filters, fromDate: e.target.value })}
      />
      <input
        type="date"
        placeholder="To date"
        value={filters.toDate || ''}
        onChange={(e) => setFilters({ ...filters, toDate: e.target.value })}
      />

      {/* Clear All */}
      <button onClick={clearFilters} className="clear-filters-btn">
        Clear All
      </button>
    </div>
  );
}
```

---

### 4. OpportunityPipeline Filters Component

**Location:** `src/components/features/OpportunityPipeline/OpportunityFilters.tsx` (NEW)

```tsx
import { useUrlFilters, OpportunityFilters as Filters } from '@/hooks/useUrlFilters';

export function OpportunityFilters() {
  const { filters, setFilters, clearFilters } = useUrlFilters<Filters>();

  return (
    <div className="opportunity-filters">
      {/* Assigned To Dropdown */}
      <select
        value={filters.assignedTo || ''}
        onChange={(e) => setFilters({ ...filters, assignedTo: e.target.value })}
        className="assigned-to-select"
      >
        <option value="">All Users</option>
        <option value="user-1">John Doe</option>
        <option value="user-2">Jane Smith</option>
      </select>

      {/* Value Range Sliders */}
      <div className="value-range">
        <label>Min Value: {filters.minValue || 0} HUF</label>
        <input
          type="range"
          min="0"
          max="10000000"
          step="100000"
          value={filters.minValue || 0}
          onChange={(e) => setFilters({ ...filters, minValue: parseInt(e.target.value) })}
        />
      </div>

      <div className="value-range">
        <label>Max Value: {filters.maxValue || 10000000} HUF</label>
        <input
          type="range"
          min="0"
          max="10000000"
          step="100000"
          value={filters.maxValue || 10000000}
          onChange={(e) => setFilters({ ...filters, maxValue: parseInt(e.target.value) })}
        />
      </div>

      {/* Close Date Range */}
      <input
        type="date"
        placeholder="Close from"
        value={filters.fromCloseDate || ''}
        onChange={(e) => setFilters({ ...filters, fromCloseDate: e.target.value })}
      />
      <input
        type="date"
        placeholder="Close to"
        value={filters.toCloseDate || ''}
        onChange={(e) => setFilters({ ...filters, toCloseDate: e.target.value })}
      />

      {/* Stage Visibility Toggles */}
      <div className="stage-toggles">
        {['draft', 'proposal', 'negotiation', 'won', 'lost', 'abandoned'].map(stage => (
          <label key={stage}>
            <input
              type="checkbox"
              checked={!filters.stages || filters.stages.includes(stage)}
              onChange={(e) => {
                const current = filters.stages || [];
                const updated = e.target.checked
                  ? [...current, stage]
                  : current.filter(s => s !== stage);
                setFilters({ ...filters, stages: updated.length > 0 ? updated : undefined });
              }}
            />
            {stage.charAt(0).toUpperCase() + stage.slice(1)}
          </label>
        ))}
      </div>

      {/* Clear All */}
      <button onClick={clearFilters} className="clear-filters-btn">
        Clear All
      </button>
    </div>
  );
}
```

---

### 5. Debounced Value Hook (if missing)

**Location:** `src/hooks/useDebouncedValue.ts` (NEW if missing)

```typescript
import { useEffect, useState } from 'react';

export function useDebouncedValue<T>(value: T, delay: number): T {
  const [debouncedValue, setDebouncedValue] = useState(value);

  useEffect(() => {
    const handler = setTimeout(() => {
      setDebouncedValue(value);
    }, delay);

    return () => {
      clearTimeout(handler);
    };
  }, [value, delay]);

  return debouncedValue;
}
```

---

### 6. Apply Filters to Data (Mock API or TanStack Query)

**Option A: TanStack Query with filters**

```typescript
// In useLeads hook
export function useLeads() {
  const { filters } = useUrlFilters<LeadFilters>();

  return useQuery({
    queryKey: ['leads', filters], // Filters in query key → refetch on change
    queryFn: () => crmApi.getLeads(filters),
  });
}
```

**Option B: Client-side filtering**

```typescript
// In LeadGrid component
const { filters } = useUrlFilters<LeadFilters>();
const { data: leads } = useLeads();

const filteredLeads = useMemo(() => {
  if (!leads) return [];

  let result = leads;

  // Search filter
  if (filters.search) {
    const search = filters.search.toLowerCase();
    result = result.filter(lead =>
      lead.company.toLowerCase().includes(search) ||
      lead.contactName.toLowerCase().includes(search) ||
      lead.email.toLowerCase().includes(search)
    );
  }

  // Status filter
  if (filters.status && filters.status.length > 0) {
    result = result.filter(lead => filters.status.includes(lead.status));
  }

  // Date range filter
  if (filters.fromDate) {
    result = result.filter(lead => new Date(lead.createdAt) >= new Date(filters.fromDate!));
  }
  if (filters.toDate) {
    result = result.filter(lead => new Date(lead.createdAt) <= new Date(filters.toDate!));
  }

  return result;
}, [leads, filters]);
```

---

## Build Verification

```bash
cd /opt/spaceos/datahaven-web/client
npm run build
```

**Expected:** 0 TypeScript errors

---

## Manual Testing Checklist

**LeadGrid:**
- [ ] Search "acme" → URL updates to `?search=acme`
- [ ] Select status "New,Contacted" → URL updates
- [ ] Set date range → URL updates
- [ ] Clear all → URL clears, all filters reset
- [ ] Copy URL → paste in new tab → filters preserved

**OpportunityPipeline:**
- [ ] Set assignedTo → URL updates
- [ ] Adjust min/max value sliders → URL updates
- [ ] Hide "Lost" stage → URL updates to `?stages=draft,proposal,negotiation,won,abandoned`
- [ ] Browser back button → filters revert
- [ ] Browser forward button → filters re-apply

---

## Expected Outcome

**DONE outbox with:**
- Filters working for both LeadGrid and OpportunityPipeline
- URL state sync verified (back/forward navigation)
- Shareable URLs tested (copy → paste → filters load)
- Build: 0 TypeScript errors
- Code quality summary
- Ready for Backend API integration

**Timeline:** 3-4 hours

---

**Priority:** HIGH (CRM Wave 2 final phase)

**Model:** sonnet (React + URL state management)
