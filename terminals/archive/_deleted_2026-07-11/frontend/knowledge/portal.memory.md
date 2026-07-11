# Portal Domain Memory

> Automatikusan betöltődik ha a feladat Customer Portal-hoz kapcsolódik.

## Domain Scope

- **App:** `datahaven-web/client`
- **Felelősség:** Customer-facing UI, Quote forms, Order tracking
- **Tech stack:** React 18, TypeScript, Vite, TailwindCSS

## Aktív Patterns

### 1. Public Quote Form (Q3 Track A)
```typescript
// Anonymous route - no auth required
<Route path="/public/cutting/quote-request" element={<PublicQuoteRequestPage />} />
<Route path="/public/cutting/quote-status/:id" element={<PublicQuoteStatusPage />} />
<Route path="/track/:trackingToken" element={<TrackingPage />} />
```

### 2. Form Validation Pattern
```typescript
const [errors, setErrors] = useState<Record<string, string>>({});

const validateForm = (): boolean => {
  const newErrors: Record<string, string> = {};

  if (!formData.customerEmail) {
    newErrors.customerEmail = 'Email kötelező';
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.customerEmail)) {
    newErrors.customerEmail = 'Érvénytelen email formátum';
  }

  if (formData.dimensions.width < 1 || formData.dimensions.width > 10000) {
    newErrors.width = 'Szélesség 1-10000 mm között';
  }

  setErrors(newErrors);
  return Object.keys(newErrors).length === 0;
};
```

### 3. Mock/Real API Toggle
```typescript
// Service with environment-based toggle
const USE_MOCK = import.meta.env.VITE_USE_MOCK_API === 'true';

export async function submitQuoteRequest(data: QuoteRequestDto): Promise<QuoteResponse> {
  if (USE_MOCK) {
    return mockSubmitQuoteRequest(data);
  }
  return realSubmitQuoteRequest(data);
}
```

### 4. Status Timeline Component
```typescript
interface TimelineStep {
  status: 'completed' | 'current' | 'pending';
  label: string;
  timestamp?: string;
}

const QuoteStatusTimeline: React.FC<{ steps: TimelineStep[] }> = ({ steps }) => (
  <div className="flex flex-col space-y-4">
    {steps.map((step, i) => (
      <div key={i} className={`flex items-center ${step.status === 'current' ? 'text-blue-600' : ''}`}>
        <StatusIcon status={step.status} />
        <span>{step.label}</span>
        {step.timestamp && <span className="text-gray-500 ml-2">{step.timestamp}</span>}
      </div>
    ))}
  </div>
);
```

## Routes

| Route | Component | Auth |
|-------|-----------|------|
| `/public/cutting/quote-request` | PublicQuoteRequestPage | No |
| `/public/cutting/quote-status/:id` | PublicQuoteStatusPage | No |
| `/track/:trackingToken` | TrackingPage | No |
| `/dashboard` | DashboardPage | JWT |
| `/orders` | OrdersPage | JWT |

## Komponens Struktúra

```
src/
├── components/
│   ├── PublicQuoteForm.tsx      ← Quote request form
│   ├── QuoteStatusTimeline.tsx  ← Status visualization
│   └── Auth/AuthOverlay.tsx     ← Login modal
├── pages/
│   ├── PublicQuoteRequestPage.tsx
│   ├── PublicQuoteStatusPage.tsx
│   └── TrackingPage.tsx
├── hooks/
│   ├── useQuoteRequest.ts       ← Form state + submission
│   └── useAuth.ts               ← JWT auth
└── services/
    └── publicCuttingService.ts  ← API calls
```

## Legutóbbi Tanulságok

- **Mobile-first** design TailwindCSS-sel
- **hu-HU locale** dátumokhoz (`toLocaleDateString('hu-HU')`)
- **Loading states** minden async műveletnél
- **Error boundaries** kritikus komponenseknél
