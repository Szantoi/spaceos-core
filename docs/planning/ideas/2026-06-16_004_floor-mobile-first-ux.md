---
domain: manufacturing
segment: joinery-memory
type: ux_gap
priority: medium
created: 2026-06-16
---

# Manufacturing Floor Mobile-First UX — Responsive Layout + Offline Form Capture

## Mit old meg

Gyártócsarnoknál (Doorstar workshop, CNC operator stations) a felhasználók tipikusan **7–10 inches tablet-ek** vagy **4–5 inches okostelefon**-ok használnak WiFi nélkül. Ma a SpaceOS Portal:
- Responsive `md:hidden` nav (jó), de **oldalak 12-column grid** → mobilon illegible, 320px-en összetolik
- **Nincsenek offline form inputs** (pl. task completion report → sync when online)
- **Nincs barcode/QR scanning** hook → manual data entry tedious
- **Nincs domain-specific mobile widgets** (timer widget process step-hez, numpad layout weight entry-hez)

## Jelenlegi állapot

| Aspect | Status | Gap |
|--------|--------|-----|
| **Responsive Layout** | Partial | `MobileBottomNav` OK, de ProductionPage / InventoryPage 12-col grid → unreadable <576px |
| **Offline Form Capture** | ❌ Missing | No `useOfflineForm` hook; POST requests fail offline (403/5xx) |
| **Barcode/QR Scanning** | ❌ Missing | No integration; manual input only |
| **Mobile Widgets** | ❌ Missing | No timer, numpad, process-step cards optimized for thumb interaction |

## Bekötési lehetőség

### Phase 1 — Layout Responsive (2.5 óra)

**ProductionPage.tsx refactor:**
```
// Before: grid-cols-12 (12 columns, too narrow on mobile)
// After:  grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 
//         (mobile: single column; tablet: 2 cols; desktop: 4 cols)

<ProductionGrid className="grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
  <CuttingPlanCard plan={plan} />
  <ExecutionStatusCard exec={exec} />
</ProductionGrid>
```

**InventoryPage.tsx refactor:**
```
// Single column stock list + search; tablet → 2-col table
<StockListMobile className="space-y-2 sm:grid sm:grid-cols-2 gap-4">
  <StockCard sku={sku} /> // thumb-friendly touch targets (48px min)
</StockListMobile>
```

### Phase 2 — Offline Form Capture (2 óra)

**`useOfflineForm.ts` hook:**
```typescript
const { submitForm, status, error } = useOfflineForm({
  endpoint: '/api/tasks/{id}/complete',
  onSuccess: (data) => refetch(),
  offlineFallback: (data) => store_to_indexeddb('task_completions'),
  syncOnReconnect: true
});

// Usage:
<button onClick={() => submitForm({ taskId, result, timestamp })}>
  {status === 'offline' ? '📦 Save Offline' : 'Submit'}
</button>
```

**Sync mechanism:** on reconnection, `useEffect` batches pending forms → POST with idempotent keys (taskId + timestamp)

### Phase 3 — Barcode/QR Scanning (1.5 óra)

**`useBarcodeScanner.ts` hook (web-based QR reader):**
```typescript
const { openScanner, result, error } = useBarcodeScanner({
  onScan: (barcode) => {
    // Barcode = SKU, TaskId, or CuttingPlanId
    if (barcode.startsWith('SKU-')) {
      navigate(`/stock/${barcode}`);
    } else if (barcode.startsWith('TASK-')) {
      navigate(`/tasks/${barcode}`);
    }
  }
});

<button onClick={openScanner}>📱 Scan QR</button>
```

Use `jsqr` or `html5-qrcode` library (lightweight, no server dependency)

### Phase 4 — Domain-Specific Mobile Widgets (2 óra)

**`TimerWidget.tsx` — process step duration tracking:**
```
<TimerWidget step={step} onComplete={markStepDone} />
// Shows: countdown, pause/resume, big tap targets
```

**`NumpayWidget.tsx` — weight/quantity entry optimized for thumb:**
```
<NumpadWidget label="Weight (kg)" onValue={setWeight} />
// Shows: 0-9 buttons (large), clear, submit
```

## Iparági relevancia

- **Human Factors** — MESA MES best practices: "field-friendly interfaces reduce operator fatigue, data entry errors"
- **Offline Manufacturing** — floor workers can't always reach WiFi hotspot; offline form capture → sync queue
- **Barcode Standard** — SKU/task barcodes on labels is iparági norm; scanning reduces typos
- **Doorstar workshop** — 5+ stations, tablet-based workflow expected after Soft Launch

## Érintett modulok

- **Frontend** (`design-portal`): ProductionPage, InventoryPage, TaskPage refactor + new hooks
- **Infrastructure**: IndexedDB schema for offline forms (new migration: `001_offline_forms.ts`)
- **E2E tests**: offline form submission, barcode scanning, responsive viewport tests

## Blocker

None. Libs available: jsqr (QR), React Query (sync), IndexedDB native API.

## Megkötések

- **QR scanning phase optional** — Phase 1 (layout) + Phase 2 (offline) kötelezők; Phase 3–4 post-Soft-Launch
- **IndexedDB schema**: offline forms tárolt egyszerűen — `{ endpoint, payload, createdAt, synced: false }`
- **Sync retry**: 3× próba, exponential backoff, max 24h storage
