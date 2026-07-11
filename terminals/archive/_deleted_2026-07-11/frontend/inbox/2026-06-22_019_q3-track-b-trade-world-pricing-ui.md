---
id: MSG-FRONTEND-019
from: conductor
to: frontend
type: task
priority: high
status: DONE
model: sonnet
ref: Q3-CUTTING-EXPANSION
created: 2026-06-22
content_hash: 538e11f258535ace0dc9b25a74fd6670c441379ecb1a4caa34687dce85a1c634
---

# Q3 Track B: Trade World - Price List Management UI

## Összefoglaló

Implementáld a **Trade World** (Kereskedelem világ) frontend UI-ját, amely lehetővé teszi árajánlati listák kezelését, anyagárak szerkesztését, és árazási szabályok definiálását.

## Scope

**App:** `frontend/joinerytech-portal/`
**World:** Trade World (új világ) — `/w/trade/:screen`
**Időkeret:** 2.5 nap (Track B)
**Prioritás:** HIGH — Quote-to-Cash pipeline része

## Implementációs lépések

### 1. Trade World Routing (0.5 nap)

**Új world page:** `src/pages/TradeWorldPage.tsx`

```tsx
import { useNavigate, useParams } from 'react-router-dom';
import WorldLayout from '../components/layout/WorldLayout';
import PriceListsPage from './trade/PriceListsPage';
import QuoteRequestsPage from './trade/QuoteRequestsPage';
import TradeDashboardPage from './trade/TradeDashboardPage';

export default function TradeWorldPage() {
  const { screen } = useParams<{ screen: string }>();
  const navigate = useNavigate();

  const screens = [
    { id: 'dash', name: 'Áttekintés', icon: ChartBarIcon },
    { id: 'price-lists', name: 'Árlisták', icon: CurrencyDollarIcon },
    { id: 'quote-requests', name: 'Árajánlatkérések', icon: DocumentTextIcon },
  ];

  const renderScreen = () => {
    switch (screen) {
      case 'dash': return <TradeDashboardPage />;
      case 'price-lists': return <PriceListsPage />;
      case 'quote-requests': return <QuoteRequestsPage />;
      default: return <TradeDashboardPage />;
    }
  };

  return (
    <WorldLayout
      worldName="Kereskedelem"
      screens={screens}
      currentScreen={screen || 'dash'}
      onScreenChange={(id) => navigate(`/w/trade/${id}`)}
    >
      {renderScreen()}
    </WorldLayout>
  );
}
```

**App.tsx frissítés:**

```tsx
import TradeWorldPage from './pages/TradeWorldPage';

// Inside WorldRouter
<Route path="trade/:screen?" element={<TradeWorldPage />} />
```

### 2. Price Lists Page (1 nap)

**Page:** `src/pages/trade/PriceListsPage.tsx`

```tsx
import { useState } from 'react';
import { usePriceLists } from '../../hooks/usePriceLists';
import PriceListCard from '../../components/trade/PriceListCard';
import CreatePriceListDrawer from '../../components/trade/CreatePriceListDrawer';
import PriceListDetailSlideOver from '../../components/trade/PriceListDetailSlideOver';

export default function PriceListsPage() {
  const { priceLists, loading } = usePriceLists();
  const [createOpen, setCreateOpen] = useState(false);
  const [selectedPriceListId, setSelectedPriceListId] = useState<string | null>(null);

  return (
    <div className="p-6">
      <div className="flex items-center justify-between mb-6">
        <h1 className="text-2xl font-bold text-gray-900">Árlisták</h1>
        <Button onClick={() => setCreateOpen(true)}>
          <PlusIcon className="w-5 h-5 mr-2" />
          Új árlista
        </Button>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        {priceLists.map(priceList => (
          <PriceListCard
            key={priceList.id}
            priceList={priceList}
            onClick={() => setSelectedPriceListId(priceList.id)}
          />
        ))}
      </div>

      <CreatePriceListDrawer
        isOpen={createOpen}
        onClose={() => setCreateOpen(false)}
        onSuccess={(id) => {
          setCreateOpen(false);
          setSelectedPriceListId(id);
        }}
      />

      {selectedPriceListId && (
        <PriceListDetailSlideOver
          priceListId={selectedPriceListId}
          onClose={() => setSelectedPriceListId(null)}
        />
      )}
    </div>
  );
}
```

**Component:** `src/components/trade/PriceListCard.tsx`

```tsx
interface PriceListCardProps {
  priceList: PriceList;
  onClick: () => void;
}

export function PriceListCard({ priceList, onClick }: PriceListCardProps) {
  const statusColor = {
    Draft: 'bg-gray-100 text-gray-800',
    Active: 'bg-green-100 text-green-800',
    Archived: 'bg-slate-100 text-slate-600'
  }[priceList.status];

  return (
    <div
      onClick={onClick}
      className="bg-white rounded-lg shadow p-6 cursor-pointer hover:shadow-lg transition-shadow"
    >
      <div className="flex items-start justify-between mb-4">
        <h3 className="text-lg font-semibold text-gray-900">{priceList.name}</h3>
        <span className={`px-2 py-1 rounded-full text-xs font-medium ${statusColor}`}>
          {priceList.status === 'Active' ? 'Aktív' : priceList.status}
        </span>
      </div>

      <div className="space-y-2 text-sm">
        <div className="flex items-center text-gray-600">
          <CalendarIcon className="w-4 h-4 mr-2" />
          Érvényes: {formatDate(priceList.validFrom)} - {priceList.validUntil ? formatDate(priceList.validUntil) : 'Folyamatos'}
        </div>

        <div className="flex items-center text-gray-600">
          <CubeIcon className="w-4 h-4 mr-2" />
          {priceList.materialCount} anyag
        </div>

        <div className="flex items-center text-gray-600">
          <TagIcon className="w-4 h-4 mr-2" />
          {priceList.ruleCount} árazási szabály
        </div>
      </div>
    </div>
  );
}
```

**Component:** `src/components/trade/PriceListDetailSlideOver.tsx` (640px)

```tsx
export function PriceListDetailSlideOver({ priceListId, onClose }: Props) {
  const { priceList, loading } = usePriceList(priceListId);
  const [editingMaterialId, setEditingMaterialId] = useState<string | null>(null);

  const handleActivate = async () => {
    await fetch(`/pricing/api/price-lists/${priceListId}/activate`, { method: 'PATCH' });
    // Refresh...
  };

  return (
    <SlideOver isOpen onClose={onClose} title={priceList?.name || 'Árlista'} width="640px">
      {/* FSM Actions */}
      <div className="flex gap-2 mb-6">
        {priceList?.status === 'Draft' && (
          <Button onClick={handleActivate}>Aktiválás</Button>
        )}
        {priceList?.status === 'Active' && (
          <Button variant="secondary" onClick={handleArchive}>Archiválás</Button>
        )}
      </div>

      {/* Material Prices Table */}
      <div className="mb-8">
        <div className="flex items-center justify-between mb-4">
          <h3 className="text-lg font-semibold">Anyagárak</h3>
          <Button variant="secondary" size="sm" onClick={() => setAddMaterialOpen(true)}>
            <PlusIcon className="w-4 h-4 mr-1" />
            Anyag hozzáadása
          </Button>
        </div>

        <table className="min-w-full divide-y divide-gray-200">
          <thead>
            <tr>
              <th className="px-3 py-2 text-left text-xs font-medium text-gray-500">Anyag</th>
              <th className="px-3 py-2 text-right text-xs font-medium text-gray-500">Ár (Ft/m²)</th>
              <th className="px-3 py-2 text-right text-xs font-medium text-gray-500">Min. díj</th>
              <th className="px-3 py-2"></th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {priceList?.materialPrices.map(mp => (
              <tr key={mp.id}>
                <td className="px-3 py-3 text-sm">{mp.materialName}</td>
                <td className="px-3 py-3 text-sm text-right font-mono">
                  {editingMaterialId === mp.id ? (
                    <Input
                      type="number"
                      value={mp.pricePerSqm}
                      onChange={(e) => updateMaterialPrice(mp.id, Number(e.target.value))}
                      onBlur={() => setEditingMaterialId(null)}
                      className="w-24 text-right"
                    />
                  ) : (
                    <span onClick={() => setEditingMaterialId(mp.id)} className="cursor-pointer">
                      {formatNumber(mp.pricePerSqm)} Ft
                    </span>
                  )}
                </td>
                <td className="px-3 py-3 text-sm text-right font-mono">{formatNumber(mp.minimumCharge)} Ft</td>
                <td className="px-3 py-3 text-right">
                  <button onClick={() => deleteMaterialPrice(mp.id)}>
                    <TrashIcon className="w-4 h-4 text-red-600" />
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pricing Rules */}
      <div>
        <h3 className="text-lg font-semibold mb-4">Árazási szabályok</h3>
        <div className="space-y-2">
          {priceList?.rules.map(rule => (
            <PriceRuleCard key={rule.id} rule={rule} />
          ))}
        </div>
      </div>
    </SlideOver>
  );
}
```

### 3. Quote Requests Page (0.5 nap)

**Page:** `src/pages/trade/QuoteRequestsPage.tsx`

```tsx
export default function QuoteRequestsPage() {
  const { quoteRequests, loading } = useQuoteRequests();
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const statusFilter = ['Submitted', 'Processing', 'Quoted', 'Rejected'];

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Árajánlatkérések</h1>

      {/* Status filters */}
      <div className="flex gap-2 mb-6">
        {statusFilter.map(status => (
          <button
            key={status}
            className={`px-4 py-2 rounded-md ${...}`}
          >
            {translateStatus(status)}
          </button>
        ))}
      </div>

      {/* Table */}
      <table className="min-w-full divide-y divide-gray-200">
        <thead>
          <tr>
            <th>Ügyfél</th>
            <th>Email</th>
            <th>Tételek</th>
            <th>Státusz</th>
            <th>Beérkezett</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          {quoteRequests.map(qr => (
            <tr key={qr.id} onClick={() => setSelectedId(qr.id)}>
              <td>{qr.customerName}</td>
              <td>{qr.customerEmail}</td>
              <td>{qr.pieceCount} db</td>
              <td><StatusBadge status={qr.status} /></td>
              <td>{formatDate(qr.submittedAt)}</td>
              <td><ChevronRightIcon className="w-5 h-5" /></td>
            </tr>
          ))}
        </tbody>
      </table>

      {selectedId && (
        <QuoteRequestDetailSlideOver
          quoteRequestId={selectedId}
          onClose={() => setSelectedId(null)}
        />
      )}
    </div>
  );
}
```

**Component:** `src/components/trade/QuoteRequestDetailSlideOver.tsx`

```tsx
export function QuoteRequestDetailSlideOver({ quoteRequestId, onClose }: Props) {
  const { quoteRequest } = useQuoteRequest(quoteRequestId);

  const handleGenerateQuote = async () => {
    // Calculate price
    const priceResult = await fetch('/pricing/api/calculate', {
      method: 'POST',
      body: JSON.stringify({
        priceListId: activePriceListId,
        pieces: quoteRequest.pieces
      })
    }).then(r => r.json());

    // Generate quote
    await fetch(`/cutting/api/quote-requests/${quoteRequestId}/generate-quote`, {
      method: 'POST',
      body: JSON.stringify({
        totalPrice: priceResult.totalPriceGross,
        quoteDocumentUrl: '...'
      })
    });

    // Refresh...
  };

  return (
    <SlideOver isOpen onClose={onClose} title="Árajánlatkérés" width="560px">
      {/* Customer info */}
      <div className="mb-6">
        <h3 className="text-sm font-medium text-gray-500 mb-2">Ügyfél</h3>
        <p className="text-lg font-semibold">{quoteRequest.customerName}</p>
        <p className="text-sm text-gray-600">{quoteRequest.customerEmail}</p>
        <p className="text-sm text-gray-600">{quoteRequest.customerPhone}</p>
      </div>

      {/* Pieces table */}
      <div className="mb-6">
        <h3 className="text-sm font-medium text-gray-500 mb-2">Tételek</h3>
        <table className="min-w-full">
          <thead>
            <tr>
              <th>Anyag</th>
              <th>Méret</th>
              <th>Darab</th>
              <th>Éllezés</th>
            </tr>
          </thead>
          <tbody>
            {quoteRequest.pieces.map((p, i) => (
              <tr key={i}>
                <td>{p.materialCode}</td>
                <td>{p.length} × {p.width} mm</td>
                <td>{p.quantity}</td>
                <td>{p.edgeBanding}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Actions */}
      {quoteRequest.status === 'Submitted' && (
        <div className="flex gap-2">
          <Button onClick={handleGenerateQuote}>Árajánlat készítése</Button>
          <Button variant="danger" onClick={handleReject}>Elutasítás</Button>
        </div>
      )}
    </SlideOver>
  );
}
```

### 4. Trade Dashboard (0.5 nap)

**Page:** `src/pages/trade/TradeDashboardPage.tsx`

```tsx
export default function TradeDashboardPage() {
  const { stats } = useTradeDashboard();

  return (
    <div className="p-6">
      <h1 className="text-2xl font-bold text-gray-900 mb-6">Kereskedelem áttekintés</h1>

      {/* KPI Cards */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4 mb-8">
        <StatCard
          label="Aktív árlista"
          value={stats.activePriceListName}
          icon={CurrencyDollarIcon}
        />
        <StatCard
          label="Új kérések (7 nap)"
          value={stats.newQuoteRequests}
          icon={DocumentTextIcon}
        />
        <StatCard
          label="Kiadott ajánlatok"
          value={stats.quotesGenerated}
          icon={CheckCircleIcon}
        />
        <StatCard
          label="Átl. válaszidő"
          value={`${stats.avgResponseTime}h`}
          icon={ClockIcon}
        />
      </div>

      {/* Recent quote requests */}
      <div>
        <h2 className="text-lg font-semibold mb-4">Legutóbbi árajánlatkérések</h2>
        {/* Table or list... */}
      </div>
    </div>
  );
}
```

### 5. API Hooks (0.5 nap)

**Hooks:** `src/hooks/usePriceLists.ts`, `useQuoteRequests.ts`

```tsx
export function usePriceLists() {
  const [priceLists, setPriceLists] = useState<PriceList[]>([]);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    const fetchPriceLists = async () => {
      setLoading(true);
      try {
        const response = await fetch('/pricing/api/price-lists');
        if (response.ok) {
          const data = await response.json();
          setPriceLists(data);
        }
      } catch (error) {
        console.error('Failed to fetch price lists:', error);
      } finally {
        setLoading(false);
      }
    };

    fetchPriceLists();
  }, []);

  return { priceLists, loading };
}
```

### 6. Tesztek (0.5 nap)

**Test coverage:**
- TradeWorldPage routing (3 screens)
- PriceListCard rendering
- PriceListDetailSlideOver (activate, add material, edit price)
- QuoteRequestsPage filtering
- QuoteRequestDetailSlideOver (generate quote, reject)
- API hooks (usePriceLists, useQuoteRequests)

**Minimum 10 teszt.**

## Definition of Done

✅ TradeWorldPage + 3 screens (dash, price-lists, quote-requests)
✅ PriceListsPage + PriceListCard + PriceListDetailSlideOver
✅ QuoteRequestsPage + QuoteRequestDetailSlideOver
✅ TradeDashboardPage (KPI cards)
✅ API integráció (`/pricing/api/*`, `/cutting/api/quote-requests`)
✅ FSM actions (activate, archive, generate quote, reject)
✅ 10+ frontend teszt pass
✅ `pnpm build` sikeresen lefut (0 error)

## Blokkolók

**Backend API (MSG-BACKEND-031)** — párhuzamosan futhat, mock fallback használható.

## Kapcsolódó feladatok

- **Backend:** MSG-BACKEND-031 (Pricing Integration)
- **Track A:** MSG-FRONTEND-018 (Customer Portal UI)
- **Track C:** MSG-FRONTEND-020 (ShopFloor UI)

## Referenciák

- Sales world: `frontend/joinerytech-portal/src/pages/SalesWorldPage.tsx`
- WorldLayout: `frontend/joinerytech-portal/src/components/layout/WorldLayout.tsx`

---

**Határidő:** 2026-06-26 (Track B, 2.5 nap)
**Assigned to:** Frontend terminal
**Model:** sonnet
