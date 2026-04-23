---
id: MSG-FE-002
from: root
to: fe
type: task
priority: high
status: READ
ref: FE-PORTAL
created: 2026-04-16
---

# FE-002 — Door Order Dashboard (lista + létrehozás)

## Kontextus

Ez a spaceos-fe tmux session. WD: `/opt/spaceos/spaceos-doorstar-portal/`.
Olvasd el a CLAUDE.md-t — az összes constraint ott van.
Pipeline: INBOX READ → CODE → BUILD → TEST → REVIEW → SECURITY → OUTBOX

## Feladat

Implementáld a Doorstar Portal első üzleti funkcióját: az ajtórendelések dashboardját.
A Doorstar gyártó (Doorstar Kft.) itt látja és kezeli az aktív rendeléseket.

## BFF API (elérhető végpontok)

A proxy `/bff/api/*` → Kernel. Minden hívás Bearer tokent igényel (OIDC).

```typescript
// Elérhető endpointok:
GET    /bff/api/orders            // Rendelések listája (paged)
GET    /bff/api/orders/:id        // Egy rendelés részletei
POST   /bff/api/orders            // Új rendelés létrehozása
POST   /bff/api/orders/:id/items  // Tétel hozzáadása
POST   /bff/api/orders/:id/submit // Rendelés beküldése
```

**Request típusok** (a Kernel alapján):
```typescript
// POST /bff/api/orders
{ customerId: string }  // tenantId-ból automatikus

// POST /bff/api/orders/:id/items
{ doorTypeId: string; quantity: number; customOptions?: Record<string, unknown> }

// GET /bff/api/orders válasz:
{ items: DoorOrder[]; totalCount: number; page: number; pageSize: number }

// DoorOrder:
{
  id: string;
  status: 'Draft' | 'Submitted' | 'InProduction' | 'Completed' | 'Cancelled';
  createdAt: string;
  itemCount: number;
  totalItems: number;
}
```

## Implementálandó oldalak

### 1. `OrdersPage` (`/orders`)

- Táblázat: rendelés ID (rövítve), státusz badge, létrehozva dátum, tételek száma
- `+ Új rendelés` gomb (→ modal vagy `/orders/new` route)
- Státusz szerinti szín: Draft=szürke, Submitted=kék, InProduction=sárga, Completed=zöld, Cancelled=piros
- Loading skeleton, empty state ("Még nincs rendelés")
- TanStack Query: `useQuery(['orders'])` — 30s stale time

### 2. `OrderDetailPage` (`/orders/:id`)

- Rendelés státusza, létrehozás dátuma
- Tételek listája (ha van: `GET /bff/api/orders/:id`)
- `Beküldés` gomb (csak Draft státusznál, `POST .../submit`)
- Vissza gomb → `/orders`

### 3. `NewOrderModal` vagy `NewOrderPage`

- Egyszerű form: egyelőre csak a rendelés létrehozása (`POST /bff/api/orders`)
- Siker után → redirect az új rendelés detail oldalára

## Implementációs irányelvek

```typescript
// src/api/ordersApi.ts — BFF hívásfüggvények
// src/hooks/useOrders.ts — TanStack Query hook
// src/hooks/useOrder.ts — egy rendelés lekérdezése
// src/pages/OrdersPage.tsx
// src/pages/OrderDetailPage.tsx
// src/components/OrderStatusBadge.tsx
// src/components/OrdersTable.tsx
```

**Auth:** `useAuth()` hook-ból a token — `Authorization: Bearer <token>` header minden hívásnál.
A `src/api/client.ts` axios instance-t hozz létre interceptorral.

**Tailwind:** Doorstar brand: slate/stone alapszínek, kék akcent.

## Tesztek (kötelező)

Minden új komponenshez/hookhoz teszt:
- `OrdersPage`: rendel lista megjelenik, loading skeleton, empty state
- `OrderStatusBadge`: mind az 5 státuszhoz helyes szín + label
- `useOrders`: API hívás + cache
- `OrderDetailPage`: adatok megjelennek, Beküldés gomb csak Draft-nál látható
- Min. **12 új teszt**

## DoD

- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` → mind zöld, ≥12 új teszt
- [ ] `pnpm lint` → 0 hiba
- [ ] `pnpm typecheck` → 0 error
- [ ] `OrdersPage` route: `/orders` (ProtectedRoute mögött)
- [ ] `OrderDetailPage` route: `/orders/:id`
- [ ] API client: `Authorization: Bearer` header automatikus
- [ ] TanStack Query: `QueryClientProvider` beállítva
- [ ] Loading + error state minden query-nél
- [ ] git commit + push main-ra

## Outbox

DONE: `mailbox/fe/outbox/2026-04-16_002_door-order-dashboard-done.md`
Tartalmazza: test count, komponensek listája, commit hash.

## Skillек & Agentек

- `/senior-frontend` — React 18 pattern, TanStack Query, komponens architektúra
- `/premium-frontend-ui` — Tailwind UI, status badge design
- `/javascript-typescript-jest` — vitest + testing-library tesztek
- `/senior-qa` — test coverage stratégia, edge case-ek
- `/web-design-reviewer` — UI konzisztencia review
- Agent: `expert-react-frontend-engineer` — React hooks, TypeScript types
- Agent: `se-security-reviewer` — auth header kezelés, token exposure ellenőrzés
- Sub-agenteket nyugodtan indíts párhuzamosan — pl. API client + UI komponensek + tesztek egyszerre
