---
id: MSG-FRONTEND-018
from: conductor
to: frontend
type: task
priority: high
status: DONE
model: sonnet
ref: /opt/spaceos/docs/tervezes/SpaceOS_Cutting_Q3_Track_A_Customer_Portal_v1.md
created: 2026-06-23
content_hash: fad981d74aff44a6a31578569abcd0af0302ceb3e4eb707c11959d16dc39ab08
---

# Q3 Track A — Customer Self-Service Portal (Frontend)

**Epic:** CUTTING-Q3-EXPANSION
**Duration:** 2 days
**Priority:** HIGH
**Status:** APPROVED (Root MSG-CONDUCTOR-007)

---

## Executive Summary

Build a public-facing customer portal for lapszabász (panel cutting) customers to submit quote requests and track their orders without authentication.

**Prerequisites:** MSG-BACKEND-030 DONE (TenantResolver + EmailService ready)

**Track A Frontend adds:**
1. Public Quote Request Form (React component)
2. Tracking Page (customer-facing order status)
3. File upload (cutting list, DXF/PDF)
4. Responsive design (mobile-first)

---

## Acceptance Criteria

- [ ] **Public Quote Request Form** (`/quote-request` route)
  - Form fields: name, email, phone, material, dimensions, quantity, notes
  - File upload: cutting list (PDF/DXF), max 10MB
  - Client-side validation (required fields, email format, phone format)
  - Submit → backend API → success message with trackingToken
  - Error handling (network errors, validation errors)
- [ ] **Tracking Page** (`/track/{trackingToken}` route)
  - Fetch quote status from backend
  - Display: status badge (Pending → Approved → Accepted), quote details, price (if approved)
  - Accept button (if approved) → POST to backend
  - Refresh button (manual refresh)
  - Success/error toast notifications
- [ ] **Responsive Design**
  - Mobile-first (320px width minimum)
  - Tailwind CSS components
  - Loading states, error states
- [ ] **Integration Tests**
  - Form submission flow (3 scenarios: success, validation error, network error)
  - Tracking page load (2 scenarios: valid token, invalid token)

---

## Technical Implementation

### 1. Public Quote Request Form

**File:** `src/components/PublicQuoteRequestForm.tsx`

```tsx
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuoteRequest } from '../hooks/useQuoteRequest';

export const PublicQuoteRequestForm = () => {
  const [formData, setFormData] = useState({
    name: '',
    email: '',
    phone: '',
    material: 'MDF',
    panelWidth: '',
    panelHeight: '',
    quantity: '',
    notes: '',
    file: null as File | null
  });

  const { submitQuoteRequest, isLoading, error } = useQuoteRequest();
  const navigate = useNavigate();

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();

    // Client-side validation
    if (!formData.email.includes('@')) {
      alert('Invalid email format');
      return;
    }

    // Submit to backend
    const result = await submitQuoteRequest(formData);

    if (result.success) {
      // Navigate to tracking page
      navigate(`/track/${result.trackingToken}`);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="max-w-2xl mx-auto p-6">
      <h1 className="text-3xl font-bold mb-6">Request a Quote</h1>

      {/* Name */}
      <div className="mb-4">
        <label className="block text-sm font-medium mb-2">Name *</label>
        <input
          type="text"
          required
          value={formData.name}
          onChange={(e) => setFormData({ ...formData, name: e.target.value })}
          className="w-full border rounded px-3 py-2"
        />
      </div>

      {/* Email */}
      <div className="mb-4">
        <label className="block text-sm font-medium mb-2">Email *</label>
        <input
          type="email"
          required
          value={formData.email}
          onChange={(e) => setFormData({ ...formData, email: e.target.value })}
          className="w-full border rounded px-3 py-2"
        />
      </div>

      {/* Phone */}
      <div className="mb-4">
        <label className="block text-sm font-medium mb-2">Phone *</label>
        <input
          type="tel"
          required
          value={formData.phone}
          onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
          className="w-full border rounded px-3 py-2"
          placeholder="+36 20 123 4567"
        />
      </div>

      {/* Material */}
      <div className="mb-4">
        <label className="block text-sm font-medium mb-2">Material *</label>
        <select
          value={formData.material}
          onChange={(e) => setFormData({ ...formData, material: e.target.value })}
          className="w-full border rounded px-3 py-2"
        >
          <option value="MDF">MDF</option>
          <option value="Plywood">Plywood</option>
          <option value="Chipboard">Chipboard</option>
        </select>
      </div>

      {/* Panel Dimensions */}
      <div className="grid grid-cols-2 gap-4 mb-4">
        <div>
          <label className="block text-sm font-medium mb-2">Width (mm) *</label>
          <input
            type="number"
            required
            min="100"
            max="5000"
            value={formData.panelWidth}
            onChange={(e) => setFormData({ ...formData, panelWidth: e.target.value })}
            className="w-full border rounded px-3 py-2"
          />
        </div>
        <div>
          <label className="block text-sm font-medium mb-2">Height (mm) *</label>
          <input
            type="number"
            required
            min="100"
            max="5000"
            value={formData.panelHeight}
            onChange={(e) => setFormData({ ...formData, panelHeight: e.target.value })}
            className="w-full border rounded px-3 py-2"
          />
        </div>
      </div>

      {/* Quantity */}
      <div className="mb-4">
        <label className="block text-sm font-medium mb-2">Quantity *</label>
        <input
          type="number"
          required
          min="1"
          max="1000"
          value={formData.quantity}
          onChange={(e) => setFormData({ ...formData, quantity: e.target.value })}
          className="w-full border rounded px-3 py-2"
        />
      </div>

      {/* File Upload */}
      <div className="mb-4">
        <label className="block text-sm font-medium mb-2">Cutting List (PDF/DXF)</label>
        <input
          type="file"
          accept=".pdf,.dxf"
          onChange={(e) => setFormData({ ...formData, file: e.target.files?.[0] || null })}
          className="w-full border rounded px-3 py-2"
        />
        <p className="text-xs text-gray-500 mt-1">Optional. Max 10MB.</p>
      </div>

      {/* Notes */}
      <div className="mb-4">
        <label className="block text-sm font-medium mb-2">Additional Notes</label>
        <textarea
          value={formData.notes}
          onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
          className="w-full border rounded px-3 py-2"
          rows={4}
        />
      </div>

      {/* Error */}
      {error && (
        <div className="mb-4 p-3 bg-red-100 border border-red-300 rounded text-red-700">
          {error}
        </div>
      )}

      {/* Submit */}
      <button
        type="submit"
        disabled={isLoading}
        className="w-full bg-blue-600 text-white py-3 rounded font-medium hover:bg-blue-700 disabled:opacity-50"
      >
        {isLoading ? 'Submitting...' : 'Submit Quote Request'}
      </button>
    </form>
  );
};
```

### 2. Tracking Page

**File:** `src/pages/TrackingPage.tsx`

```tsx
import { useEffect } from 'react';
import { useParams } from 'react-router-dom';
import { useQuoteTracking } from '../hooks/useQuoteRequest';

export const TrackingPage = () => {
  const { trackingToken } = useParams<{ trackingToken: string }>();
  const { quote, fetchQuote, acceptQuote, isLoading, error } = useQuoteTracking();

  useEffect(() => {
    if (trackingToken) {
      fetchQuote(trackingToken);
    }
  }, [trackingToken]);

  if (isLoading) {
    return <div className="text-center py-20">Loading...</div>;
  }

  if (error) {
    return (
      <div className="max-w-2xl mx-auto p-6">
        <div className="p-4 bg-red-100 border border-red-300 rounded text-red-700">
          {error}
        </div>
      </div>
    );
  }

  if (!quote) {
    return null;
  }

  return (
    <div className="max-w-2xl mx-auto p-6">
      <h1 className="text-3xl font-bold mb-6">Track Your Quote</h1>

      {/* Status Badge */}
      <div className="mb-6">
        <span className={`px-3 py-1 rounded text-sm font-medium ${
          quote.status === 'Pending' ? 'bg-yellow-100 text-yellow-800' :
          quote.status === 'Approved' ? 'bg-green-100 text-green-800' :
          quote.status === 'Accepted' ? 'bg-blue-100 text-blue-800' :
          'bg-gray-100 text-gray-800'
        }`}>
          {quote.status}
        </span>
      </div>

      {/* Quote Details */}
      <div className="bg-gray-50 p-4 rounded mb-6">
        <h2 className="font-medium mb-3">Quote Details</h2>
        <dl className="space-y-2 text-sm">
          <div className="flex justify-between">
            <dt className="text-gray-600">Material:</dt>
            <dd className="font-medium">{quote.material}</dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-gray-600">Dimensions:</dt>
            <dd className="font-medium">{quote.panelWidth} × {quote.panelHeight} mm</dd>
          </div>
          <div className="flex justify-between">
            <dt className="text-gray-600">Quantity:</dt>
            <dd className="font-medium">{quote.quantity} pieces</dd>
          </div>
          {quote.price && (
            <div className="flex justify-between">
              <dt className="text-gray-600">Price:</dt>
              <dd className="font-medium text-lg">{quote.price.toLocaleString()} HUF</dd>
            </div>
          )}
        </dl>
      </div>

      {/* Accept Button */}
      {quote.status === 'Approved' && (
        <button
          onClick={() => acceptQuote(trackingToken!)}
          className="w-full bg-green-600 text-white py-3 rounded font-medium hover:bg-green-700"
        >
          Accept Quote & Place Order
        </button>
      )}

      {/* Refresh Button */}
      <button
        onClick={() => fetchQuote(trackingToken!)}
        className="w-full mt-3 border border-gray-300 py-2 rounded hover:bg-gray-50"
      >
        Refresh Status
      </button>
    </div>
  );
};
```

### 3. Custom Hook (API Integration)

**File:** `src/hooks/useQuoteRequest.ts`

```ts
import { useState } from 'react';

interface QuoteRequestData {
  name: string;
  email: string;
  phone: string;
  material: string;
  panelWidth: string;
  panelHeight: string;
  quantity: string;
  notes: string;
  file: File | null;
}

export const useQuoteRequest = () => {
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const submitQuoteRequest = async (data: QuoteRequestData) => {
    setIsLoading(true);
    setError(null);

    try {
      // Build form data (multipart for file upload)
      const formData = new FormData();
      formData.append('name', data.name);
      formData.append('email', data.email);
      formData.append('phone', data.phone);
      formData.append('material', data.material);
      formData.append('panelWidth', data.panelWidth);
      formData.append('panelHeight', data.panelHeight);
      formData.append('quantity', data.quantity);
      formData.append('notes', data.notes);
      if (data.file) {
        formData.append('cuttingList', data.file);
      }

      const response = await fetch('/public/cutting/quote-request', {
        method: 'POST',
        body: formData
      });

      if (!response.ok) {
        throw new Error('Failed to submit quote request');
      }

      const result = await response.json();
      return { success: true, trackingToken: result.trackingToken };
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
      return { success: false };
    } finally {
      setIsLoading(false);
    }
  };

  return { submitQuoteRequest, isLoading, error };
};

export const useQuoteTracking = () => {
  const [quote, setQuote] = useState<any>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchQuote = async (trackingToken: string) => {
    setIsLoading(true);
    setError(null);

    try {
      const response = await fetch(`/public/cutting/quotes/track/${trackingToken}`);

      if (!response.ok) {
        throw new Error('Quote not found');
      }

      const data = await response.json();
      setQuote(data);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    } finally {
      setIsLoading(false);
    }
  };

  const acceptQuote = async (trackingToken: string) => {
    try {
      const response = await fetch(`/public/cutting/quotes/track/${trackingToken}/accept`, {
        method: 'POST'
      });

      if (!response.ok) {
        throw new Error('Failed to accept quote');
      }

      // Refresh quote status
      await fetchQuote(trackingToken);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Unknown error');
    }
  };

  return { quote, fetchQuote, acceptQuote, isLoading, error };
};
```

### 4. Routing

**File:** `src/App.tsx`

```tsx
// Add routes
<Route path="/quote-request" element={<PublicQuoteRequestForm />} />
<Route path="/track/:trackingToken" element={<TrackingPage />} />
```

---

## Files to Create

1. `src/components/PublicQuoteRequestForm.tsx`
2. `src/pages/TrackingPage.tsx`
3. `src/hooks/useQuoteRequest.ts`
4. `src/components/PublicQuoteRequestForm.test.tsx`
5. `src/pages/TrackingPage.test.tsx`

---

## Files to Modify

1. `src/App.tsx` (add routes)

---

## Testing Requirements

### Integration Tests

**File:** `src/components/PublicQuoteRequestForm.test.tsx`

```tsx
describe('PublicQuoteRequestForm', () => {
  it('submits form successfully', async () => {
    // Mock API
    // Fill form
    // Submit
    // Assert redirect to tracking page
  });

  it('shows validation error for invalid email', async () => {
    // Fill form with invalid email
    // Submit
    // Assert error message displayed
  });

  it('handles network error gracefully', async () => {
    // Mock API failure
    // Submit
    // Assert error message displayed
  });
});
```

**File:** `src/pages/TrackingPage.test.tsx`

```tsx
describe('TrackingPage', () => {
  it('fetches and displays quote status', async () => {
    // Mock API
    // Render with trackingToken
    // Assert quote details displayed
  });

  it('shows error for invalid tracking token', async () => {
    // Mock API 404
    // Render with invalid token
    // Assert error message displayed
  });
});
```

---

## Build & Test Gate

```bash
cd /opt/spaceos/frontend/joinerytech-portal

# Install dependencies (if needed)
npm install

# Build
npm run build

# Run tests
npm test -- --coverage --watchAll=false

# Type check
npm run type-check
```

**Expected:** All tests pass, TypeScript 0 errors, build succeeds.

---

## Dependencies

**Blocked by:**
- MSG-BACKEND-030 (Backend API ready)

**Blocks:**
- None (standalone frontend feature)

**References:**
- Track A spec: `/opt/spaceos/docs/tervezes/SpaceOS_Cutting_Q3_Track_A_Customer_Portal_v1.md`

---

**Estimated effort:** 2 days (16 hours)
**Model:** sonnet
**Priority:** HIGH
