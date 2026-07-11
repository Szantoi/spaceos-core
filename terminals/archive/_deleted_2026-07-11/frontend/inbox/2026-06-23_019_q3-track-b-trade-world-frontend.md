---
id: MSG-FRONTEND-019
from: conductor
to: frontend
type: task
priority: high
status: DONE
model: sonnet
ref: /opt/spaceos/docs/tervezes/SpaceOS_Cutting_Q3_Track_B_Pricing_Integration_v1.md
created: 2026-06-23
content_hash: eefb2b5b4eab178fac20e034a5300317b1508e3fa3b5b0b7468a963c10d9fcd9
---

# Q3 Track B — Trade World (Frontend)

**Epic:** CUTTING-Q3-EXPANSION
**Duration:** 1 day
**Priority:** HIGH
**Status:** APPROVED (Root MSG-CONDUCTOR-007)

---

## Executive Summary

Build a new "Trade" world in the frontend portal that exposes pricing configuration UI for admins to manage material prices, complexity modifiers, and view revenue analytics.

**Prerequisites:** MSG-BACKEND-031 DONE (Pricing Engine API ready)

**Track B Frontend adds:**
1. Trade World (`/w/trade` route)
2. TradeDashboard (revenue KPI-k, quote conversion rate)
3. PricingRulesPanel (material árlisták, modifiers)
4. EditPricingRuleSlideOver (update material price)

---

## Acceptance Criteria

- [ ] **Trade World** (`/w/trade` route)
  - Accessible only to authenticated users (admin role)
  - Layout: Sidebar navigation + main content area
  - 2 tabs: "Dashboard", "Pricing Rules"
- [ ] **TradeDashboard**
  - Revenue metrics: Total revenue, avg quote price, quote count
  - Quote conversion rate chart (Pending → Approved → Accepted)
  - Top materials by revenue
  - Mock data (real analytics deferred to Q4)
- [ ] **PricingRulesPanel**
  - Material pricing table (type, price/m², currency, edit button)
  - Complexity modifiers table (type, multiplier, edit button)
  - Fetch pricing rules from backend API
- [ ] **EditPricingRuleSlideOver**
  - Edit material price
  - Save → PUT /api/cutting/pricing/rules/{id}
  - Success toast notification
- [ ] **Integration Tests**
  - Trade World navigation (2 tabs)
  - Fetch pricing rules (mock API)
  - Edit material price (mock API)
  - Error handling (network error, validation error)

---

## Technical Implementation

### 1. Trade World Layout

**File:** `src/pages/TradeWorld.tsx`

```tsx
import { useState } from 'react';
import { TradeDashboard } from '../components/TradeDashboard';
import { PricingRulesPanel } from '../components/PricingRulesPanel';

export const TradeWorld = () => {
  const [activeTab, setActiveTab] = useState<'dashboard' | 'pricing'>('dashboard');

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white border-b px-6 py-4">
        <h1 className="text-2xl font-bold">Trade World</h1>
        <p className="text-gray-600">Manage pricing, quotes, and revenue</p>
      </header>

      {/* Tabs */}
      <div className="border-b bg-white px-6">
        <nav className="flex space-x-8">
          <button
            onClick={() => setActiveTab('dashboard')}
            className={`py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'dashboard'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            Dashboard
          </button>
          <button
            onClick={() => setActiveTab('pricing')}
            className={`py-4 px-1 border-b-2 font-medium text-sm ${
              activeTab === 'pricing'
                ? 'border-blue-500 text-blue-600'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            Pricing Rules
          </button>
        </nav>
      </div>

      {/* Content */}
      <div className="p-6">
        {activeTab === 'dashboard' && <TradeDashboard />}
        {activeTab === 'pricing' && <PricingRulesPanel />}
      </div>
    </div>
  );
};
```

### 2. Trade Dashboard

**File:** `src/components/TradeDashboard.tsx`

```tsx
export const TradeDashboard = () => {
  // Mock data (real analytics deferred to Q4)
  const metrics = {
    totalRevenue: 1250000,
    avgQuotePrice: 25000,
    quoteCount: 50,
    conversionRate: 0.68 // 68% approval rate
  };

  const topMaterials = [
    { type: 'MDF', revenue: 680000, percentage: 54 },
    { type: 'Plywood', revenue: 420000, percentage: 34 },
    { type: 'Chipboard', revenue: 150000, percentage: 12 }
  ];

  return (
    <div className="space-y-6">
      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        <div className="bg-white p-6 rounded-lg shadow">
          <p className="text-sm text-gray-600">Total Revenue</p>
          <p className="text-3xl font-bold mt-2">
            {metrics.totalRevenue.toLocaleString()} HUF
          </p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow">
          <p className="text-sm text-gray-600">Avg Quote Price</p>
          <p className="text-3xl font-bold mt-2">
            {metrics.avgQuotePrice.toLocaleString()} HUF
          </p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow">
          <p className="text-sm text-gray-600">Total Quotes</p>
          <p className="text-3xl font-bold mt-2">{metrics.quoteCount}</p>
        </div>
        <div className="bg-white p-6 rounded-lg shadow">
          <p className="text-sm text-gray-600">Conversion Rate</p>
          <p className="text-3xl font-bold mt-2">
            {(metrics.conversionRate * 100).toFixed(0)}%
          </p>
        </div>
      </div>

      {/* Top Materials */}
      <div className="bg-white p-6 rounded-lg shadow">
        <h2 className="text-lg font-medium mb-4">Top Materials by Revenue</h2>
        <div className="space-y-4">
          {topMaterials.map((material) => (
            <div key={material.type}>
              <div className="flex justify-between mb-1">
                <span className="text-sm font-medium">{material.type}</span>
                <span className="text-sm text-gray-600">
                  {material.revenue.toLocaleString()} HUF ({material.percentage}%)
                </span>
              </div>
              <div className="w-full bg-gray-200 rounded-full h-2">
                <div
                  className="bg-blue-600 h-2 rounded-full"
                  style={{ width: `${material.percentage}%` }}
                />
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
```

### 3. Pricing Rules Panel

**File:** `src/components/PricingRulesPanel.tsx`

```tsx
import { useState, useEffect } from 'react';
import { usePricingRules } from '../hooks/usePricingRules';
import { EditPricingRuleSlideOver } from './EditPricingRuleSlideOver';

export const PricingRulesPanel = () => {
  const { pricingRules, fetchPricingRules, updatePricingRule, isLoading, error } = usePricingRules();
  const [editingMaterial, setEditingMaterial] = useState<any>(null);

  useEffect(() => {
    fetchPricingRules();
  }, []);

  if (isLoading) {
    return <div className="text-center py-20">Loading pricing rules...</div>;
  }

  if (error) {
    return (
      <div className="p-4 bg-red-100 border border-red-300 rounded text-red-700">
        {error}
      </div>
    );
  }

  const priceList = pricingRules?.priceLists?.[0]; // Active price list

  return (
    <div className="space-y-6">
      {/* Material Pricing Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="px-6 py-4 border-b">
          <h2 className="text-lg font-medium">Material Pricing</h2>
        </div>
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Material Type
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Price / m²
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Currency
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {priceList?.materials?.map((material: any) => (
              <tr key={material.type}>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                  {material.type}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm">
                  {material.pricePerM2.toLocaleString()}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  {material.currency}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-right text-sm">
                  <button
                    onClick={() => setEditingMaterial(material)}
                    className="text-blue-600 hover:text-blue-900"
                  >
                    Edit
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Complexity Modifiers Table */}
      <div className="bg-white rounded-lg shadow">
        <div className="px-6 py-4 border-b">
          <h2 className="text-lg font-medium">Complexity Modifiers</h2>
        </div>
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Modifier Type
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                Multiplier
              </th>
              <th className="px-6 py-3 text-right text-xs font-medium text-gray-500 uppercase">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {priceList?.modifiers?.map((modifier: any) => (
              <tr key={modifier.type}>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium">
                  {modifier.type}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm">
                  {modifier.multiplier.toFixed(2)}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-right text-sm">
                  <button className="text-blue-600 hover:text-blue-900">
                    Edit
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Edit SlideOver */}
      {editingMaterial && (
        <EditPricingRuleSlideOver
          material={editingMaterial}
          priceListId={priceList?.id}
          onClose={() => setEditingMaterial(null)}
          onSave={(updatedMaterial) => {
            updatePricingRule(priceList?.id, updatedMaterial);
            setEditingMaterial(null);
          }}
        />
      )}
    </div>
  );
};
```

### 4. Edit Pricing Rule SlideOver

**File:** `src/components/EditPricingRuleSlideOver.tsx`

```tsx
import { useState } from 'react';

interface Props {
  material: any;
  priceListId: string;
  onClose: () => void;
  onSave: (updatedMaterial: any) => void;
}

export const EditPricingRuleSlideOver = ({ material, priceListId, onClose, onSave }: Props) => {
  const [pricePerM2, setPricePerM2] = useState(material.pricePerM2);

  const handleSave = () => {
    onSave({ ...material, pricePerM2 });
  };

  return (
    <div className="fixed inset-0 overflow-hidden z-50">
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black bg-opacity-50" onClick={onClose} />

      {/* SlideOver */}
      <div className="absolute inset-y-0 right-0 max-w-md w-full bg-white shadow-xl">
        <div className="p-6">
          <h2 className="text-lg font-medium mb-4">Edit Material Price</h2>

          <div className="mb-4">
            <label className="block text-sm font-medium mb-2">Material Type</label>
            <input
              type="text"
              value={material.type}
              disabled
              className="w-full border rounded px-3 py-2 bg-gray-100"
            />
          </div>

          <div className="mb-4">
            <label className="block text-sm font-medium mb-2">Price per m²</label>
            <input
              type="number"
              value={pricePerM2}
              onChange={(e) => setPricePerM2(Number(e.target.value))}
              className="w-full border rounded px-3 py-2"
              min="0"
              step="100"
            />
          </div>

          <div className="mb-4">
            <label className="block text-sm font-medium mb-2">Currency</label>
            <input
              type="text"
              value={material.currency}
              disabled
              className="w-full border rounded px-3 py-2 bg-gray-100"
            />
          </div>

          <div className="flex space-x-3">
            <button
              onClick={handleSave}
              className="flex-1 bg-blue-600 text-white py-2 rounded hover:bg-blue-700"
            >
              Save
            </button>
            <button
              onClick={onClose}
              className="flex-1 border border-gray-300 py-2 rounded hover:bg-gray-50"
            >
              Cancel
            </button>
          </div>
        </div>
      </div>
    </div>
  );
};
```

### 5. Custom Hook (API Integration)

**File:** `src/hooks/usePricingRules.ts`

```ts
import { useState } from 'react';

export const usePricingRules = () => {
  const [pricingRules, setPricingRules] = useState<any>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchPricingRules = async () => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await fetch('/api/cutting/pricing/rules');

      if (!response.ok) {
        throw new Error('Failed to fetch pricing rules');
      }

      const data = await response.json();
      setPricingRules(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setIsLoading(false);
    }
  };

  const updatePricingRule = async (priceListId: string, updatedMaterial: any) => {
    try {
      const response = await fetch(`/api/cutting/pricing/rules/${priceListId}`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ materials: [updatedMaterial] })
      });

      if (!response.ok) {
        throw new Error('Failed to update pricing rule');
      }

      // Refresh pricing rules
      await fetchPricingRules();
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    }
  };

  return { pricingRules, fetchPricingRules, updatePricingRule, isLoading, error };
};
```

### 6. Routing

**File:** `src/App.tsx`

```tsx
// Add route
<Route path="/w/trade" element={<TradeWorld />} />
```

---

## Files to Create

1. `src/pages/TradeWorld.tsx`
2. `src/components/TradeDashboard.tsx`
3. `src/components/PricingRulesPanel.tsx`
4. `src/components/EditPricingRuleSlideOver.tsx`
5. `src/hooks/usePricingRules.ts`
6. `src/pages/TradeWorld.test.tsx`

---

## Files to Modify

1. `src/App.tsx` (add `/w/trade` route)

---

## Testing Requirements

### Integration Tests

**File:** `src/pages/TradeWorld.test.tsx`

```tsx
describe('TradeWorld', () => {
  it('renders dashboard tab by default', () => {
    // Render TradeWorld
    // Assert dashboard visible
  });

  it('switches to pricing rules tab', () => {
    // Render TradeWorld
    // Click "Pricing Rules" tab
    // Assert pricing rules panel visible
  });

  it('fetches and displays pricing rules', async () => {
    // Mock API
    // Render PricingRulesPanel
    // Assert material pricing table displayed
  });

  it('edits material price', async () => {
    // Mock API
    // Click edit button
    // Update price
    // Click save
    // Assert API called with updated price
  });
});
```

---

## Build & Test Gate

```bash
cd /opt/spaceos/frontend/joinerytech-portal

# Build
npm run build

# Run tests
npm test -- --coverage --watchAll=false

# Type check
npm run type-check
```

**Expected:** All tests pass, TypeScript 0 errors.

---

**Estimated effort:** 1 day (8 hours)
**Model:** sonnet
**Priority:** HIGH
