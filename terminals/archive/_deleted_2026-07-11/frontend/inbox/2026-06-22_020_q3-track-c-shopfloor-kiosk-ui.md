---
id: MSG-FRONTEND-020
from: conductor
to: frontend
type: task
priority: high
status: DONE
model: sonnet
ref: Q3-CUTTING-EXPANSION
created: 2026-06-22
content_hash: 7ab542bc06722ee55ff34abbfa5576bfa6dd4a31d9eca8f53d964dc926a09393
---

# Q3 Track C: ShopFloor Kiosk UI - Operator Interface

## Összefoglaló

Implementáld a **ShopFloor Kiosk** frontend UI-ját: egy dedikált operátori felületet, amely lehetővé teszi PIN-alapú bejelentkezést, a gépvárólistára történő feladat áttekintést, és batch gyártás indítását/befejezését.

## Scope

**App:** `frontend/joinerytech-portal/`
**Route:** `/shopfloor` (dedikált kiosk mód, fullscreen)
**Időkeret:** 2 nap (Track C)
**Prioritás:** HIGH — Doorstar production workflow része

## Implementációs lépések

### 1. ShopFloor Kiosk Page (0.5 nap)

**Új page:** `src/pages/ShopFloorKioskPage.tsx`

```tsx
import { useState } from 'react';
import OperatorLoginScreen from '../components/shopfloor/OperatorLoginScreen';
import MachineQueueScreen from '../components/shopfloor/MachineQueueScreen';
import BatchProductionScreen from '../components/shopfloor/BatchProductionScreen';

export default function ShopFloorKioskPage() {
  const [session, setSession] = useState<OperatorSession | null>(null);
  const [activeBatch, setActiveBatch] = useState<Batch | null>(null);

  if (!session) {
    return <OperatorLoginScreen onLogin={setSession} />;
  }

  if (activeBatch) {
    return (
      <BatchProductionScreen
        batch={activeBatch}
        session={session}
        onComplete={() => setActiveBatch(null)}
        onCancel={() => setActiveBatch(null)}
      />
    );
  }

  return (
    <MachineQueueScreen
      session={session}
      onStartBatch={setActiveBatch}
      onLogout={() => setSession(null)}
    />
  );
}
```

**Routing:** `App.tsx`

```tsx
// Dedicated shopfloor route (outside /w/ world structure)
<Route path="/shopfloor" element={<ShopFloorKioskPage />} />
```

### 2. Operator Login Screen (0.5 nap)

**Component:** `src/components/shopfloor/OperatorLoginScreen.tsx`

```tsx
import { useState } from 'react';
import { LockClosedIcon } from '@heroicons/react/24/solid';

interface Props {
  onLogin: (session: OperatorSession) => void;
}

export default function OperatorLoginScreen({ onLogin }: Props) {
  const [pin, setPin] = useState('');
  const [workstationId, setWorkstationId] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  const { workstations } = useWorkstations();

  const handleLogin = async () => {
    setLoading(true);
    setError(null);

    try {
      const response = await fetch('/cutting/api/shopfloor/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ operatorPin: pin, workstationId })
      });

      if (!response.ok) {
        throw new Error('Hibás PIN vagy gép kiválasztás');
      }

      const session = await response.json();
      onLogin(session);
    } catch (err: any) {
      setError(err.message);
    } finally {
      setLoading(false);
    }
  };

  const handlePinInput = (digit: string) => {
    if (pin.length < 4) {
      setPin(pin + digit);
    }
  };

  const clearPin = () => setPin('');

  return (
    <div className="min-h-screen bg-stone-900 flex items-center justify-center p-4">
      <div className="max-w-md w-full bg-stone-800 rounded-lg shadow-2xl p-8">
        <div className="text-center mb-8">
          <div className="w-16 h-16 bg-emerald-600 rounded-full flex items-center justify-center mx-auto mb-4">
            <LockClosedIcon className="w-8 h-8 text-white" />
          </div>
          <h1 className="text-2xl font-bold text-white">Operátor bejelentkezés</h1>
        </div>

        {/* Workstation selection */}
        <div className="mb-6">
          <label className="block text-sm font-medium text-gray-300 mb-2">Gép kiválasztása</label>
          <select
            value={workstationId}
            onChange={(e) => setWorkstationId(e.target.value)}
            className="w-full px-4 py-3 bg-stone-700 text-white rounded-md border border-stone-600 focus:border-emerald-500"
          >
            <option value="">Válasszon gépet...</option>
            {workstations.map(ws => (
              <option key={ws.id} value={ws.id}>{ws.name}</option>
            ))}
          </select>
        </div>

        {/* PIN display */}
        <div className="mb-6">
          <label className="block text-sm font-medium text-gray-300 mb-2">PIN kód</label>
          <div className="flex justify-center gap-3 mb-4">
            {[0, 1, 2, 3].map(i => (
              <div
                key={i}
                className="w-12 h-12 bg-stone-700 rounded-md flex items-center justify-center border border-stone-600"
              >
                {pin[i] ? <div className="w-3 h-3 bg-emerald-500 rounded-full" /> : null}
              </div>
            ))}
          </div>

          {/* PIN pad */}
          <div className="grid grid-cols-3 gap-3">
            {[1, 2, 3, 4, 5, 6, 7, 8, 9].map(digit => (
              <button
                key={digit}
                onClick={() => handlePinInput(String(digit))}
                className="h-16 bg-stone-700 hover:bg-stone-600 text-white text-xl font-semibold rounded-md transition-colors"
              >
                {digit}
              </button>
            ))}
            <button onClick={clearPin} className="h-16 bg-red-700 hover:bg-red-600 text-white rounded-md">
              Törlés
            </button>
            <button
              onClick={() => handlePinInput('0')}
              className="h-16 bg-stone-700 hover:bg-stone-600 text-white text-xl font-semibold rounded-md"
            >
              0
            </button>
            <button
              onClick={handleLogin}
              disabled={pin.length !== 4 || !workstationId || loading}
              className="h-16 bg-emerald-600 hover:bg-emerald-500 disabled:bg-stone-600 text-white font-semibold rounded-md transition-colors"
            >
              {loading ? 'Bejelentkezés...' : 'OK'}
            </button>
          </div>
        </div>

        {error && (
          <div className="bg-red-900/50 border border-red-700 rounded-md p-3 text-red-200 text-sm">
            {error}
          </div>
        )}

        <div className="mt-6 text-center">
          <a href="/" className="text-sm text-gray-400 hover:text-gray-300">
            Vissza a portálra
          </a>
        </div>
      </div>
    </div>
  );
}
```

### 3. Machine Queue Screen (0.5 nap)

**Component:** `src/components/shopfloor/MachineQueueScreen.tsx`

```tsx
interface Props {
  session: OperatorSession;
  onStartBatch: (batch: Batch) => void;
  onLogout: () => void;
}

export default function MachineQueueScreen({ session, onStartBatch, onLogout }: Props) {
  const { queue, loading, refresh } = useMachineQueue(session.workstationId);

  useEffect(() => {
    const interval = setInterval(refresh, 10000); // 10s polling
    return () => clearInterval(interval);
  }, []);

  return (
    <div className="min-h-screen bg-stone-900 text-white p-6">
      {/* Header */}
      <div className="flex items-center justify-between mb-8">
        <div>
          <h1 className="text-3xl font-bold">{session.workstationName}</h1>
          <p className="text-gray-400">Operátor: {session.operatorName}</p>
        </div>

        <button
          onClick={onLogout}
          className="px-4 py-2 bg-stone-800 hover:bg-stone-700 rounded-md transition-colors"
        >
          Kijelentkezés
        </button>
      </div>

      {/* Queue status */}
      <div className="mb-6">
        <div className="flex items-center gap-2 mb-2">
          <div className={`w-3 h-3 rounded-full ${queue?.status === 'Active' ? 'bg-green-500' : 'bg-gray-500'}`} />
          <span className="text-sm text-gray-400">
            {queue?.status === 'Active' ? 'Aktív' : 'Tétlen'}
          </span>
        </div>
        <h2 className="text-xl font-semibold">Munkavárólist ({queue?.batches.length || 0} tétel)</h2>
      </div>

      {/* Batches */}
      <div className="space-y-4">
        {queue?.batches.map(batch => (
          <BatchQueueCard
            key={batch.batchId}
            batch={batch}
            canStart={batch.queuePosition === 1 && batch.status === 'Queued'}
            onStart={() => onStartBatch(batch)}
          />
        ))}
      </div>

      {queue?.batches.length === 0 && (
        <div className="text-center py-12 text-gray-500">
          Nincs feladat a várólistában
        </div>
      )}
    </div>
  );
}
```

**Component:** `src/components/shopfloor/BatchQueueCard.tsx`

```tsx
interface Props {
  batch: QueuedBatch;
  canStart: boolean;
  onStart: () => void;
}

export function BatchQueueCard({ batch, canStart, onStart }: Props) {
  const statusColor = {
    Queued: 'bg-blue-900 text-blue-200',
    InProgress: 'bg-green-900 text-green-200',
    Completed: 'bg-gray-800 text-gray-400'
  }[batch.status];

  return (
    <div className="bg-stone-800 rounded-lg p-6">
      <div className="flex items-center justify-between mb-4">
        <div className="flex items-center gap-3">
          <div className="text-3xl font-bold text-gray-500">#{batch.queuePosition}</div>
          <div>
            <h3 className="text-lg font-semibold">Batch {batch.batchId.slice(0, 8)}</h3>
            <p className="text-sm text-gray-400">{batch.pieceCount} darab</p>
          </div>
        </div>

        <span className={`px-3 py-1 rounded-full text-xs font-medium ${statusColor}`}>
          {batch.status === 'Queued' ? 'Várakozik' : batch.status === 'InProgress' ? 'Gyártás alatt' : 'Kész'}
        </span>
      </div>

      <div className="flex items-center gap-6 text-sm text-gray-400 mb-4">
        <div className="flex items-center gap-2">
          <ClockIcon className="w-4 h-4" />
          <span>Becsült idő: {batch.estimatedDuration}</span>
        </div>

        {batch.assignedOperator && (
          <div className="flex items-center gap-2">
            <UserIcon className="w-4 h-4" />
            <span>{batch.assignedOperator}</span>
          </div>
        )}
      </div>

      {canStart && (
        <button
          onClick={onStart}
          className="w-full bg-emerald-600 hover:bg-emerald-500 text-white font-semibold py-3 rounded-md transition-colors"
        >
          Gyártás indítása
        </button>
      )}
    </div>
  );
}
```

### 4. Batch Production Screen (0.5 nap)

**Component:** `src/components/shopfloor/BatchProductionScreen.tsx`

```tsx
interface Props {
  batch: Batch;
  session: OperatorSession;
  onComplete: () => void;
  onCancel: () => void;
}

export default function BatchProductionScreen({ batch, session, onComplete, onCancel }: Props) {
  const [producedPieces, setProducedPieces] = useState(batch.pieceCount);
  const [wastePieces, setWastePieces] = useState(0);
  const [startedAt] = useState(new Date());
  const [elapsedTime, setElapsedTime] = useState(0);

  useEffect(() => {
    const interval = setInterval(() => {
      setElapsedTime(Math.floor((Date.now() - startedAt.getTime()) / 1000));
    }, 1000);

    return () => clearInterval(interval);
  }, []);

  const handleComplete = async () => {
    await fetch(`/cutting/api/shopfloor/batches/${batch.batchId}/complete`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        sessionId: session.sessionId,
        producedPieces,
        wastePieces
      })
    });

    onComplete();
  };

  const formatTime = (seconds: number) => {
    const h = Math.floor(seconds / 3600);
    const m = Math.floor((seconds % 3600) / 60);
    const s = seconds % 60;
    return `${h.toString().padStart(2, '0')}:${m.toString().padStart(2, '0')}:${s.toString().padStart(2, '0')}`;
  };

  return (
    <div className="min-h-screen bg-stone-900 text-white p-6">
      <div className="max-w-2xl mx-auto">
        {/* Header */}
        <div className="text-center mb-8">
          <div className="w-20 h-20 bg-green-600 rounded-full flex items-center justify-center mx-auto mb-4 animate-pulse">
            <PlayIcon className="w-10 h-10 text-white" />
          </div>
          <h1 className="text-3xl font-bold mb-2">Gyártás alatt</h1>
          <p className="text-gray-400">Batch {batch.batchId.slice(0, 8)}</p>
        </div>

        {/* Timer */}
        <div className="bg-stone-800 rounded-lg p-8 text-center mb-8">
          <div className="text-6xl font-mono font-bold mb-2">{formatTime(elapsedTime)}</div>
          <div className="text-sm text-gray-400">Eltelt idő</div>
        </div>

        {/* Piece count */}
        <div className="bg-stone-800 rounded-lg p-6 mb-8">
          <div className="flex items-center justify-between mb-4">
            <label className="text-lg font-semibold">Legyártott darabok</label>
            <div className="flex items-center gap-4">
              <button
                onClick={() => setProducedPieces(Math.max(0, producedPieces - 1))}
                className="w-10 h-10 bg-stone-700 hover:bg-stone-600 rounded-md"
              >
                -
              </button>
              <span className="text-3xl font-bold w-16 text-center">{producedPieces}</span>
              <button
                onClick={() => setProducedPieces(producedPieces + 1)}
                className="w-10 h-10 bg-stone-700 hover:bg-stone-600 rounded-md"
              >
                +
              </button>
            </div>
          </div>

          <div className="flex items-center justify-between">
            <label className="text-lg font-semibold">Selejt darabok</label>
            <div className="flex items-center gap-4">
              <button
                onClick={() => setWastePieces(Math.max(0, wastePieces - 1))}
                className="w-10 h-10 bg-stone-700 hover:bg-stone-600 rounded-md"
              >
                -
              </button>
              <span className="text-3xl font-bold w-16 text-center text-red-400">{wastePieces}</span>
              <button
                onClick={() => setWastePieces(wastePieces + 1)}
                className="w-10 h-10 bg-stone-700 hover:bg-stone-600 rounded-md"
              >
                +
              </button>
            </div>
          </div>
        </div>

        {/* Actions */}
        <div className="flex gap-4">
          <button
            onClick={onCancel}
            className="flex-1 bg-stone-700 hover:bg-stone-600 text-white font-semibold py-4 rounded-md transition-colors"
          >
            Mégse
          </button>
          <button
            onClick={handleComplete}
            className="flex-1 bg-emerald-600 hover:bg-emerald-500 text-white font-semibold py-4 rounded-md transition-colors"
          >
            Gyártás befejezése
          </button>
        </div>
      </div>
    </div>
  );
}
```

### 5. API Hooks (0.5 nap)

**Hook:** `src/hooks/useMachineQueue.ts`

```tsx
export function useMachineQueue(workstationId: string) {
  const [queue, setQueue] = useState<MachineQueue | null>(null);
  const [loading, setLoading] = useState(false);

  const refresh = async () => {
    setLoading(true);
    try {
      const response = await fetch(`/cutting/api/shopfloor/machines/${workstationId}/queue`);
      if (response.ok) {
        const data = await response.json();
        setQueue(data);
      }
    } catch (error) {
      console.error('Failed to fetch machine queue:', error);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    refresh();
  }, [workstationId]);

  return { queue, loading, refresh };
}
```

### 6. Tesztek (0.5 nap)

**Test coverage:**
- OperatorLoginScreen (PIN input, workstation selection, login success/failure)
- MachineQueueScreen rendering (batches, status)
- BatchQueueCard (start button visibility)
- BatchProductionScreen (piece count adjustment, timer, complete)
- useMachineQueue hook

**Minimum 8 teszt.**

## Definition of Done

✅ ShopFloorKioskPage state machine (login → queue → production)
✅ OperatorLoginScreen (PIN pad, workstation selection)
✅ MachineQueueScreen (batch list, polling)
✅ BatchProductionScreen (timer, piece count, complete)
✅ API integráció (`/cutting/api/shopfloor/*`)
✅ 8+ frontend teszt pass
✅ `pnpm build` sikeresen lefut (0 error)
✅ Fullscreen kiosk mód (dedikált `/shopfloor` route)

## Blokkolók

**Backend API (MSG-BACKEND-032)** — párhuzamosan futhat, mock fallback használható.

## Kapcsolódó feladatok

- **Backend:** MSG-BACKEND-032 (ShopFloor Integration)
- **Track A:** MSG-FRONTEND-018 (Customer Portal UI)
- **Track B:** MSG-FRONTEND-019 (Trade World UI)

## Referenciák

- Existing ShopFloor page: `frontend/joinerytech-portal/src/pages/ShopFloorPage.tsx` (referencia design)
- Dark theme (stone-900, stone-800, stone-700)

---

**Határidő:** 2026-06-27 (Track C, 2 nap)
**Assigned to:** Frontend terminal
**Model:** sonnet
