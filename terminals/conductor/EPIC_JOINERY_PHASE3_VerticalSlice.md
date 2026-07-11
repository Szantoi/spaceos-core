# EPIC: JOINERY-PHASE3 — Vertical Slice Extended

> **Konsenzus feldolgozás:** 2026-06-21_consensus.md
> **Státusz:** Planning → Implementation
> **Scope:** Joinery konfigurátor bővítés + Shop Floor SSE + Offline PWA + Mobile UX
> **Timeline:** 8-12 sprint (16-24 hét)

---

## Összefoglaló

A Joinery Phase 1 (E2E Configurator) **COMPLETE** (2026-06-21). A következő phase a **vertical slice bővítés** 4 major feature-rel:

1. **Joinery konfigurátor API bővítés** (2-3 sprint)
2. **Real-time Shop Floor SSE Feed** (1 sprint, párhuzamos)
3. **Offline PWA Enhancement** (2-3 sprint)
4. **Mobile-First Floor UX** (2-3 sprint)

**Üzleti cél:** Doorstar Q3 2026 2. ügyfél fogadása (lapszabász KKV) + műhely élő követés + offline capability

---

## Phase 1: Joinery Konfigurátor API Bővítés (2-3 sprint)

### Backend API Szükségletek

**Meglévő (használható azonnal):**
- ✅ `GET /joinery/api/designs` — termék lista
- ✅ `POST /joinery/api/doors` — create endpoint
- ✅ `GET /joinery/api/doors/{id}/bom` — anyaglista
- ✅ `POST /orchestrator/api/workstations/pair` — QR onboarding alap

**Új endpoint szükséges:**
```csharp
// 1. Joinery konfigurátor
POST /joinery/api/products/configure
  → { productType, dimensions, materials, fittings }
  → { configId, previewUrl, estimatedPrice }

// 2. Munkalap generálás
POST /joinery/api/work-orders
  → { configId, quantity, deliveryDate }
  → { workOrderId, pdfUrl, bomItems[] }
```

**Adatmodell kiegészítés:**
```sql
CREATE TABLE joinery_configurations (
  id UUID PRIMARY KEY,
  product_type VARCHAR(50),
  params JSONB NOT NULL,
  bom_snapshot JSONB,
  created_at TIMESTAMPTZ DEFAULT NOW()
);
```

### Frontend Komponensek

**Új komponensek:**
- `ProductConfiguratorWizard.tsx` — Multi-step: méretek → anyagok → fittingek
- `BOMPreviewCard.tsx` — Anyaglista előnézet árral, PDF export gomb
- `WorkOrderSummary.tsx` — Gyártási lap összefoglaló + download

**State Management:**
- Zustand — lokál UI state (drawer, tabs, filters)
- TanStack Query — szerver state (cache, refetch, optimistic updates)
- localStorage — draft mentés (form adatok túlélése refresh-nél)

**Típusbiztonság:**
- OpenAPI spec (`/swagger`)
- OpenAPI → TypeScript codegen (automatikus típusok)
- Zod schemák (runtime validáció)

---

## Phase 2: Real-time Shop Floor SSE Feed (1 sprint, párhuzamos)

### Backend API

**Új endpoint:**
```csharp
// Gép státusz (mock később OPC-UA)
GET /orchestrator/api/machines/status (SSE)
  → stream { machineId, status, currentJob, eta }
```

**Adatmodell (Event Sourcing):**
```sql
CREATE TABLE machine_status_events (
  id BIGSERIAL PRIMARY KEY,
  machine_id UUID,
  event_type VARCHAR(50),
  payload JSONB,
  timestamp TIMESTAMPTZ DEFAULT NOW()
);
```

### Frontend Komponensek

**Új komponensek:**
- `ShopFloorLiveView.tsx` — TV kijelző mód (SSE-alapú)
- `MachineStatusBadge.tsx` — LED-szerű zöld/sárga/piros státusz
- `useSSEMachineStatus.ts` — Hook újrafelhasználás (meglévő `useSSE`)

**Mock implementáció először:** SSE endpoint mock adattal is impresszív, sales enabler

---

## Phase 3: Offline PWA Enhancement (2-3 sprint)

### Backend API

**Új endpoint:**
```csharp
// Offline audit buffer
POST /kernel/api/audit/buffer
  → { deviceId, events[], clientTimestamp }
  → { acceptedCount, conflictCount }
```

**Adatmodell:**
```sql
CREATE TABLE offline_audit_buffer (
  device_id UUID,
  event_data JSONB,
  client_timestamp TIMESTAMPTZ,
  synced_at TIMESTAMPTZ DEFAULT NOW()
);
```

### Frontend Komponensek & Offline Stratégia

**Stack:**
- Workbox + Dexie.js — Service Worker cache + IndexedDB
- Stale-while-revalidate — mindig gyors, háttérben frissít
- Server timestamp authority — konfliktus automatikus (nincs manual UI)

**Új komponensek:**
- `ServiceWorkerProvider.tsx` — Workbox integráció
- `OfflineIndicator.tsx` — Sárga banner "Nincs internet"
- `PendingSyncBadge.tsx` — "3 művelet vár szinkronra"
- `useSyncQueue.ts` — Feltöltési logika hook

---

## Phase 4: Mobile-First Floor UX (2-3 sprint)

### Frontend Komponensek

**Új komponensek:**
- `MobileNav.tsx` — Bottom navigation (Tailwind breakpoints)
- `TaskCard.tsx` — Swipe gesture support (react-swipeable)
- `QuickActionsFAB.tsx` — Floating action button menü

**Touch-optimalizált design system:**
- Bottom navigation
- Quick actions
- Swipe gestures

---

## Párhuzamosítás (kritikus időnyereség)

```
Sprint 1-3: Joinery Vertical Slice (FE + Backend)
     ║
     ╠═══> Sprint 2-3: Shop Floor SSE (párhuzamos, független)
     ║
Sprint 4-6: Offline PWA (függ Joinery API-tól)
     ║
Sprint 7-9: Mobile UX (függ Offline réteg-től)
```

**Időnyereség:** Shop Floor SSE párhuzamosan fut → 1 sprint megtakarítás

---

## Nyitott kérdések (Conductor döntés)

1. **Backend allokáció:** Joinery konfigurátor endpoint (POST /products/configure) — Backend terminál (Joinery modul szakértelem)?

2. **Offline audit buffer:** Kernel terminál építi be vagy Orchestrator mint gateway kezeli?

3. **Gép státusz mock:** Backend terminál állítja be a mock SSE endpoint-ot vagy külön Infra task?

4. **Mobile testing stratégia:** Backend terminál Playwright mobile viewport tesztekkel indul vagy külön testing track?

5. **Timeline validáció:** 12 sprint = 24 hét = augusztus vége. Doorstar Q3 cél reális? Kell gyorsítás?

6. **Parametrikus szabályok backend:** Joinery modul C# kódbázisában már van szabálymotor? Ha igen, akkor 5 sablon **konfigurációs adat** legyen, nem hardcode logic.

---

## Következő lépés (v2 Pipeline)

Conductor döntés:
- Terminál allokáció (Backend, Frontend, Infra)
- Prioritás sorrend (Phase 1 vs. Phase 2 párhuzamosság)
- Inbox üzenetek kiadása

**Ref:** 2026-06-21_consensus.md (archive)
