# SpaceOS Konsenzus Implementációs Terv

## Összefoglalás

A konsenzus a **vertical slice + kockázatkezelés** hibridet valósítja meg: Joinery API integrációval indulunk, de nem horizontális rétegekben, hanem teljes értéklánc mentén (konfigurátor → BOM → munkalap). Az offline capability és mobil UX párhuzamosan épül, hogy Doorstar Q2 soft launch realisztikus legyen. 3 fázis, 8-12 sprint, fokozatos üzleti érték.

## Elfogadott prioritás sorrend

1. **Joinery End-to-End Flow (Vertical Slice)** — 2-3 sprint
   - Teljes megrendelés loop: paraméterezett konfigurátor → anyaglista előnézet → gyártási lap PDF
   - **Plan-A logikája:** meglévő backend használata (production Joinery API)
   - **Plan-B logikája:** egyszerű indulás (5 sablon ajtótípus, később bővíthető)

2. **Real-time Shop Floor Feed (Quick Win)** — 1 sprint, párhuzamos
   - SSE-alapú élő gép státusz (mock implementáció először)
   - **Plan-B merészsége:** azonnal demonstrálható, ügyfélfókuszú
   - **Plan-A biztonságossága:** meglévő `useSSE` hook újrafelhasználása

3. **Offline PWA Enhancement** — 2-3 sprint
   - Service Worker + IndexedDB cache, offline audit buffer feltöltés
   - **Plan-A stack:** Dexie.js + Workbox (battle-tested)
   - **Plan-A sync stratégia:** server timestamp authority (automatikus konfliktuskezelés)

4. **Mobile-First Floor UX** — 2-3 sprint
   - Touch-optimalizált design system, bottom navigation, quick actions
   - **Plan-A realizmusa:** P2 offline réteg után logikus
   - **Plan-B ügyfél hangja:** műhelyben MOST mobilon dolgoznak, ne várjon 6 hónapot

## Backend szükségletek (összesített)

### Meglévő (használható azonnal)
- ✅ `GET /joinery/api/designs` — termék lista
- ✅ `POST /joinery/api/doors` — create endpoint
- ✅ `GET /joinery/api/doors/{id}/bom` — anyaglista
- ✅ `POST /orchestrator/api/workstations/pair` — QR onboarding alap

### Új endpoint szükséges
```csharp
// 1. Joinery konfigurátor
POST /joinery/api/products/configure
  → { productType, dimensions, materials, fittings } 
  → { configId, previewUrl, estimatedPrice }

// 2. Munkalap generálás
POST /joinery/api/work-orders
  → { configId, quantity, deliveryDate }
  → { workOrderId, pdfUrl, bomItems[] }

// 3. Offline audit buffer
POST /kernel/api/audit/buffer
  → { deviceId, events[], clientTimestamp }
  → { acceptedCount, conflictCount }

// 4. Gép státusz (mock később OPC-UA)
GET /orchestrator/api/machines/status (SSE)
  → stream { machineId, status, currentJob, eta }

// 5. Workstation státusz query
GET /orchestrator/api/workstations/{id}/status
  → { paired, online, lastSeen, deviceInfo }
```

### Adatmodell kiegészítés
```sql
-- Joinery konfiguráció cache
CREATE TABLE joinery_configurations (
  id UUID PRIMARY KEY,
  product_type VARCHAR(50),
  params JSONB NOT NULL,
  bom_snapshot JSONB,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Offline audit buffer
CREATE TABLE offline_audit_buffer (
  device_id UUID,
  event_data JSONB,
  client_timestamp TIMESTAMPTZ,
  synced_at TIMESTAMPTZ DEFAULT NOW()
);

-- Gép státusz event sourcing (Phase 2)
CREATE TABLE machine_status_events (
  id BIGSERIAL PRIMARY KEY,
  machine_id UUID,
  event_type VARCHAR(50),
  payload JSONB,
  timestamp TIMESTAMPTZ DEFAULT NOW()
);
```

## Frontend megközelítés (legjobb elemek)

### State Management (Plan-B egyszerűsége)
- **Zustand** — lokál UI state (drawer, tabs, filters)
- **TanStack Query** — szerver state (cache, refetch, optimistic updates)
- **localStorage** — draft mentés (form adatok túlélése refresh-nél)

### Típusbiztonság (Plan-B gyakorlatiassága)
- Backend: OpenAPI spec (`/swagger`)
- Frontend: OpenAPI → TypeScript codegen (automatikus típusok)
- Runtime validáció: Zod schemák

### Offline stratégia (Plan-A kockázatkezelése)
- **Workbox + Dexie.js** — Service Worker cache + IndexedDB
- **Stale-while-revalidate** — mindig gyors, háttérben frissít
- **Server timestamp authority** — konfliktus automatikus, nincs manual UI (skálázódik)

### Új komponensek (prioritás szerint)

**Phase 1 — Joinery Vertical Slice:**
```tsx
ProductConfiguratorWizard.tsx   // Multi-step: méretek → anyagok → fittingek
BOMPreviewCard.tsx              // Anyaglista előnézet árral, PDF export gomb
WorkOrderSummary.tsx            // Gyártási lap összefoglaló + download
```

**Phase 2 — Shop Floor (párhuzamos):**
```tsx
ShopFloorLiveView.tsx           // TV kijelző mód (SSE-alapú)
MachineStatusBadge.tsx          // LED-szerű zöld/sárga/piros státusz
useSSEMachineStatus.ts          // Hook újrafelhasználás (meglévő useSSE)
```

**Phase 3 — Offline PWA:**
```tsx
ServiceWorkerProvider.tsx       // Workbox integráció
OfflineIndicator.tsx            // Sárga banner "Nincs internet"
PendingSyncBadge.tsx            // "3 művelet vár szinkronra"
useSyncQueue.ts                 // Feltöltési logika hook
```

**Phase 4 — Mobile UX:**
```tsx
MobileNav.tsx                   // Bottom navigation (Tailwind breakpoints)
TaskCard.tsx                    // Swipe gesture support (react-swipeable)
QuickActionsFAB.tsx             // Floating action button menü
```

## Amit Plan-A-tól veszünk át

✅ **Kockázat mátrix és függőségi gráf** — minden lépés előkészíti a következőt, nincs "nagy bang" integráció  
✅ **Backend inventory technika** — meglévő ✅ / hiányzik ❌ táblázat világos action itemeket ad  
✅ **Realistic sprint becslés** — 8-12 sprint konzervatív, de teljesíthető (Plan-B 3 hete túl agresszív)  
✅ **Offline sync server authority** — timestamp alapú automatikus konfliktuskezelés (manual UI nem skálázódik)  
✅ **Service Worker stack** — Dexie.js + Workbox battle-tested, nem kell újra feltalálni  

## Amit Plan-B-től veszünk át

✅ **Vertical slice megközelítés** — teljes value chain (ajánlat → gyártási lap) 1. fázisban, nem horizontális rétegek  
✅ **SSE shop floor live view azonnal** — mock adattal is impresszív, sales enabler, gyors win  
✅ **Egyszerű tech stack** — Zustand + TanStack Query elég, Redux overkill lenne  
✅ **OpenAPI → TypeScript codegen** — típusbiztonság REST-el is elérhető, pragmatikus  
✅ **5 sablon indulás** — parametrikus szabályok complexity későbbre tolva, walking skeleton first  

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

## Nyitott kérdések a Conductor-nak

1. **Backend allokáció:** Joinery konfigurátor endpoint (POST /products/configure) — Joinery vagy Orch terminál fejleszti?
   
2. **Offline audit buffer:** Kernel terminál építi be vagy Orch mint gateway kezeli?

3. **Gép státusz mock:** Infra terminál állítja be a mock SSE endpoint-ot vagy Orch saját mock middleware-t épít?

4. **Mobile testing stratégia:** E2E terminál Playwright mobile viewport tesztekkel indul vagy külön mobile testing epic szükséges?

5. **Timeline döntés:** Doorstar soft launch dátuma tényleg Q2 2026? Ha igen, akkor 12 sprint = 24 hét = **augusztus vége**, szoros schedule. Kell gyorsítás (több terminál párhuzamosan) vagy tolható Q3-ra?

6. **Parametrikus szabályok backend:** Joinery modul C# kódbázisában már van szabálymotor? Ha igen, akkor 5 sablon **konfigurációs adat** legyen, nem hardcode logic.

---

**Státusz:** `ready_for_conductor`  
**Következő lépés:** Conductor feldolgozza (v1→v4 pipeline), termináloknak inbox kiadás (Joinery, FE, Kernel, Orch, Infra)
