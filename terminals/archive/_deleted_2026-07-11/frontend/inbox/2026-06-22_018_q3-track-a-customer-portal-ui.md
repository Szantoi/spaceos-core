---
id: MSG-FRONTEND-018
from: conductor
to: frontend
type: task
priority: high
status: DONE
model: sonnet
ref: Q3-CUTTING-EXPANSION
created: 2026-06-22
content_hash: 85a8819757c656cea341c33a77f551746af72cca5e9cbb4c89f88e49c465bce0
---

# Q3 Track A: Customer Self-Service Portal - Public Quote Request Form UI

## Összefoglaló

Implementáld a **Customer Self-Service Portal** frontend UI-ját: egy nyilvános (unauthenticated) form-ot, ahol lapszabász KKV ügyfelek online árajánlatkérést nyújthatnak be.

## Scope

**App:** `frontend/joinerytech-portal/`
**Route:** `/quote-request` (public, nincs auth)
**Időkeret:** 3 nap (Track A)
**Prioritás:** HIGH — 2. ügyfél (Lapszabász KKV) onboarding Q3 2026

## Implementációs lépések

### 1. Public Quote Request Form Page (1 nap)

**Új page:** `src/pages/PublicQuoteRequestPage.tsx`

```tsx
import { useState } from 'react';
import { Button, Input, Card, Alert } from '../components/ui';
import { PlusIcon, TrashIcon } from '@heroicons/react/24/outline';

interface CutPieceInput {
  materialCode: string;
  length: number;
  width: number;
  quantity: number;
  edgeBanding: 'None' | 'All' | 'Custom';
}

export default function PublicQuoteRequestPage() {
  const [customerName, setCustomerName] = useState('');
  const [customerEmail, setCustomerEmail] = useState('');
  const [customerPhone, setCustomPhone] = useState('');
  const [pieces, setPieces] = useState<CutPieceInput[]>([
    { materialCode: '', length: 0, width: 0, quantity: 1, edgeBanding: 'None' }
  ]);
  const [submitting, setSubmitting] = useState(false);
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const addPiece = () => {
    if (pieces.length >= 50) {
      setError('Maximum 50 pieces per quote request');
      return;
    }
    setPieces([...pieces, { materialCode: '', length: 0, width: 0, quantity: 1, edgeBanding: 'None' }]);
  };

  const removePiece = (index: number) => {
    setPieces(pieces.filter((_, i) => i !== index));
  };

  const submitQuoteRequest = async () => {
    setSubmitting(true);
    setError(null);

    try {
      const response = await fetch('/cutting/api/public/quote-requests', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          customerName,
          customerEmail,
          customerPhone,
          pieces
        })
      });

      if (!response.ok) {
        const errorData = await response.json();
        throw new Error(errorData.message || 'Failed to submit quote request');
      }

      setSubmitted(true);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setSubmitting(false);
    }
  };

  if (submitted) {
    return (
      <div className="min-h-screen bg-gradient-to-br from-emerald-50 to-teal-100 flex items-center justify-center p-4">
        <Card className="max-w-md w-full p-8 text-center">
          <div className="w-16 h-16 bg-emerald-100 rounded-full flex items-center justify-center mx-auto mb-4">
            <CheckIcon className="w-8 h-8 text-emerald-600" />
          </div>
          <h2 className="text-2xl font-bold text-gray-900 mb-2">Árajánlatkérés elküldve!</h2>
          <p className="text-gray-600 mb-6">
            Köszönjük! Árajánlatkérését megkaptuk. Munkatársunk 24 órán belül felveszi Önnel a kapcsolatot.
          </p>
          <Button onClick={() => window.location.reload()}>Új árajánlatkérés</Button>
        </Card>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gradient-to-br from-emerald-50 to-teal-100 p-4">
      <div className="max-w-4xl mx-auto py-8">
        <Card className="p-8">
          <h1 className="text-3xl font-bold text-gray-900 mb-2">Lapszabászat Árajánlatkérés</h1>
          <p className="text-gray-600 mb-8">Töltse ki az alábbi űrlapot, és munkatársunk 24 órán belül felveszi Önnel a kapcsolatot.</p>

          {error && (
            <Alert variant="error" className="mb-6">
              {error}
            </Alert>
          )}

          {/* Customer Info */}
          <div className="mb-8">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">Kapcsolattartási adatok</h3>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
              <Input
                label="Név"
                value={customerName}
                onChange={(e) => setCustomerName(e.target.value)}
                required
              />
              <Input
                label="Email"
                type="email"
                value={customerEmail}
                onChange={(e) => setCustomerEmail(e.target.value)}
                required
              />
              <Input
                label="Telefon"
                type="tel"
                value={customerPhone}
                onChange={(e) => setCustomerPhone(e.target.value)}
                placeholder="+36301234567"
                required
              />
            </div>
          </div>

          {/* Cut Pieces */}
          <div className="mb-8">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-semibold text-gray-900">Igényelt lapszabászatok</h3>
              <Button variant="secondary" size="sm" onClick={addPiece}>
                <PlusIcon className="w-4 h-4 mr-1" />
                Tétel hozzáadása
              </Button>
            </div>

            <div className="space-y-4">
              {pieces.map((piece, index) => (
                <PieceInputRow
                  key={index}
                  piece={piece}
                  onChange={(updated) => {
                    const newPieces = [...pieces];
                    newPieces[index] = updated;
                    setPieces(newPieces);
                  }}
                  onRemove={() => removePiece(index)}
                  showRemove={pieces.length > 1}
                />
              ))}
            </div>
          </div>

          {/* Submit */}
          <div className="flex justify-end">
            <Button
              onClick={submitQuoteRequest}
              disabled={submitting || !customerName || !customerEmail || pieces.length === 0}
              loading={submitting}
            >
              Árajánlatkérés küldése
            </Button>
          </div>
        </Card>
      </div>
    </div>
  );
}
```

**Komponens:** `src/components/quote/PieceInputRow.tsx`

```tsx
interface PieceInputRowProps {
  piece: CutPieceInput;
  onChange: (piece: CutPieceInput) => void;
  onRemove: () => void;
  showRemove: boolean;
}

export function PieceInputRow({ piece, onChange, onRemove, showRemove }: PieceInputRowProps) {
  return (
    <div className="flex items-end gap-3 p-4 bg-gray-50 rounded-lg">
      <div className="flex-1">
        <label className="block text-sm font-medium text-gray-700 mb-1">Anyag</label>
        <select
          value={piece.materialCode}
          onChange={(e) => onChange({ ...piece, materialCode: e.target.value })}
          className="w-full px-3 py-2 border border-gray-300 rounded-md"
        >
          <option value="">Válasszon...</option>
          <option value="PAL-18-WHITE">PAL 18mm Fehér</option>
          <option value="PAL-18-OAK">PAL 18mm Tölgy</option>
          <option value="PAL-18-BEECH">PAL 18mm Bükk</option>
          <option value="PAL-25-WHITE">PAL 25mm Fehér</option>
          {/* API-ból töltve később */}
        </select>
      </div>

      <Input
        label="Hossz (mm)"
        type="number"
        value={piece.length || ''}
        onChange={(e) => onChange({ ...piece, length: Number(e.target.value) })}
        className="w-32"
      />

      <Input
        label="Szélesség (mm)"
        type="number"
        value={piece.width || ''}
        onChange={(e) => onChange({ ...piece, width: Number(e.target.value) })}
        className="w-32"
      />

      <Input
        label="Darab"
        type="number"
        value={piece.quantity || ''}
        onChange={(e) => onChange({ ...piece, quantity: Number(e.target.value) })}
        className="w-24"
      />

      <div className="w-40">
        <label className="block text-sm font-medium text-gray-700 mb-1">Éllezés</label>
        <select
          value={piece.edgeBanding}
          onChange={(e) => onChange({ ...piece, edgeBanding: e.target.value as any })}
          className="w-full px-3 py-2 border border-gray-300 rounded-md"
        >
          <option value="None">Nincs</option>
          <option value="All">Mind 4 él</option>
          <option value="Custom">Egyedi</option>
        </select>
      </div>

      {showRemove && (
        <button
          type="button"
          onClick={onRemove}
          className="p-2 text-red-600 hover:bg-red-50 rounded-md"
        >
          <TrashIcon className="w-5 h-5" />
        </button>
      )}
    </div>
  );
}
```

### 2. Routing & Public Access (0.5 nap)

**App.tsx frissítés:**

```tsx
import PublicQuoteRequestPage from './pages/PublicQuoteRequestPage';

// Public routes (no auth)
<Routes>
  <Route path="/" element={<LandingPage />} />
  <Route path="/quote-request" element={<PublicQuoteRequestPage />} />
  <Route path="/login" element={<LoginPage />} />

  {/* Protected routes */}
  <Route path="/w/*" element={<PrivateRoute><WorldRouter /></PrivateRoute>} />
</Routes>
```

### 3. Material Catalog Integration (0.5 nap)

**Abstractions API hook:** `src/hooks/useMaterialCatalog.ts`

```tsx
export function useMaterialCatalog() {
  const [materials, setMaterials] = useState<Material[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const fetchMaterials = async () => {
      setLoading(true);
      try {
        const response = await fetch('/abstractions/api/modules/materials?category=Panel');
        if (response.ok) {
          const data = await response.json();
          setMaterials(data);
        }
      } catch (error) {
        console.error('Failed to fetch materials:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchMaterials();
  }, []);

  return { materials, loading };
}
```

**PieceInputRow frissítés:**

```tsx
const { materials } = useMaterialCatalog();

<select value={piece.materialCode} onChange={...}>
  <option value="">Válasszon anyagot...</option>
  {materials.map(m => (
    <option key={m.code} value={m.code}>
      {m.name} ({m.thickness}mm)
    </option>
  ))}
</select>
```

### 4. Validation & Error Handling (0.5 nap)

**Client-side validáció:**

```tsx
const validateQuoteRequest = (): string | null => {
  if (!customerName.trim()) return 'Név megadása kötelező';
  if (!customerEmail.includes('@')) return 'Érvényes email cím szükséges';
  if (!customerPhone.match(/^\+36[0-9]{9}$/)) return 'Magyar telefonszám formátum: +36301234567';

  if (pieces.length === 0) return 'Legalább 1 tétel megadása szükséges';

  for (const piece of pieces) {
    if (!piece.materialCode) return 'Minden tételnél válasszon anyagot';
    if (piece.length < 10 || piece.length > 3000) return 'Hossz: 10-3000mm';
    if (piece.width < 10 || piece.width > 3000) return 'Szélesség: 10-3000mm';
    if (piece.quantity < 1) return 'Minimum 1 darab';
  }

  return null;
};

const submitQuoteRequest = async () => {
  const validationError = validateQuoteRequest();
  if (validationError) {
    setError(validationError);
    return;
  }

  // ... (submit logic)
};
```

### 5. Landing Page CTA (0.5 nap)

**LandingPage.tsx frissítés:**

```tsx
export default function LandingPage() {
  return (
    <div className="min-h-screen bg-white">
      {/* Hero section */}
      <div className="bg-gradient-to-br from-emerald-600 to-teal-700 text-white py-20">
        <div className="max-w-7xl mx-auto px-4">
          <h1 className="text-5xl font-bold mb-6">SpaceOS — Magyar faipar digitális gerince</h1>
          <p className="text-xl mb-8">Lapszabászat, ajtógyártás, ERP — egy platformon.</p>

          <div className="flex gap-4">
            <Button variant="white" size="lg" onClick={() => window.location.href = '/quote-request'}>
              Ingyenes árajánlat kérése
            </Button>
            <Button variant="outline-white" size="lg" onClick={() => window.location.href = '/login'}>
              Bejelentkezés
            </Button>
          </div>
        </div>
      </div>

      {/* Features... */}
    </div>
  );
}
```

### 6. Tesztek (0.5 nap)

**Test file:** `src/pages/PublicQuoteRequestPage.test.tsx`

```tsx
describe('PublicQuoteRequestPage', () => {
  it('renders the form', () => {
    render(<PublicQuoteRequestPage />);
    expect(screen.getByText('Lapszabászat Árajánlatkérés')).toBeInTheDocument();
  });

  it('adds a new piece input row', () => {
    render(<PublicQuoteRequestPage />);
    fireEvent.click(screen.getByText('Tétel hozzáadása'));
    expect(screen.getAllByLabelText('Anyag')).toHaveLength(2);
  });

  it('validates required fields', async () => {
    render(<PublicQuoteRequestPage />);
    fireEvent.click(screen.getByText('Árajánlatkérés küldése'));
    await waitFor(() => {
      expect(screen.getByText(/Név megadása kötelező/)).toBeInTheDocument();
    });
  });

  it('submits quote request successfully', async () => {
    global.fetch = jest.fn(() =>
      Promise.resolve({
        ok: true,
        json: () => Promise.resolve({ quoteRequestId: 'test-id' })
      })
    ) as jest.Mock;

    render(<PublicQuoteRequestPage />);

    fireEvent.change(screen.getByLabelText('Név'), { target: { value: 'Test Customer' } });
    fireEvent.change(screen.getByLabelText('Email'), { target: { value: 'test@example.com' } });
    fireEvent.change(screen.getByLabelText('Telefon'), { target: { value: '+36301234567' } });

    fireEvent.click(screen.getByText('Árajánlatkérés küldése'));

    await waitFor(() => {
      expect(screen.getByText('Árajánlatkérés elküldve!')).toBeInTheDocument();
    });
  });
});
```

**Minimum 8 teszt:**
- Form rendering
- Add/remove piece row
- Client-side validation (name, email, phone, pieces)
- Material catalog API integration
- Successful submission
- Error handling (API failure)

## Definition of Done

✅ `PublicQuoteRequestPage.tsx` komponens kész
✅ `PieceInputRow` komponens kész (anyag, méret, darab, éllezés)
✅ `/quote-request` route (public, no auth)
✅ Material catalog API integráció (`/abstractions/api/modules/materials`)
✅ Client-side validáció (név, email, telefon, méretek)
✅ Success state (submission confirmation)
✅ Landing page CTA ("Ingyenes árajánlat kérése" gomb)
✅ 8+ frontend teszt pass
✅ `pnpm build` sikeresen lefut (0 error)

## Blokkolók

**Backend API (MSG-BACKEND-030)** — párhuzamosan futhat, mock fallback használható.

## Kapcsolódó feladatok

- **Backend:** MSG-BACKEND-030 (Customer Portal API)
- **Track B:** MSG-FRONTEND-019 (Trade World UI)

## Referenciák

- Landing page design: `frontend/joinerytech-portal/src/pages/LandingPage.tsx`
- Form components: `frontend/joinerytech-portal/src/components/ui/`

---

**Határidő:** 2026-06-25 (Track A, 3 nap)
**Assigned to:** Frontend terminal
**Model:** sonnet
