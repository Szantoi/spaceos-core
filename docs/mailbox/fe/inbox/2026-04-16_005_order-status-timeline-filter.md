---
id: MSG-FE-005
from: root
to: fe
type: task
priority: high
status: READ
ref: FE-PORTAL
created: 2026-04-16
---

# FE-005 — Rendelés státusz timeline + szűrés

## Kontextus

WD: `/opt/spaceos/spaceos-doorstar-portal/`. CLAUDE.md kötelező olvasás.
Pipeline: INBOX READ → CODE → BUILD → TEST → REVIEW → SECURITY → OUTBOX

## Előzmény

FE-004 DONE: Teljes workflow él — create → items → calculate → cutting-list → print.
Commit: `f19244f`. Tesztek: 55/55 zöld.

A dashboard most listázza a rendeléseket, de nem mutatja a státusz változások történetét, és nincs szűrési lehetőség. Doorstar Soft Launch-hoz ezek szükségesek.

## BFF API

```typescript
GET  /bff/api/orders                          // Már él — teljes lista
GET  /bff/api/orders?status=Draft             // Szűrés státuszra (query param)
GET  /bff/api/orders?status=Submitted         // Status: Draft|Submitted|InProduction|Done|Cancelled
GET  /bff/api/orders/:id/history              // Státusz változások auditlog

// GET /bff/api/orders/:id/history response (várható struktúra):
{
  orderId: string;
  events: OrderHistoryEvent[];
}

// OrderHistoryEvent:
{
  id: string;
  fromStatus: string | null;   // null = initial creation
  toStatus: string;
  occurredAt: string;          // ISO 8601
  triggeredBy: string;         // user display name vagy "system"
  note?: string;
}
```

## Implementálandó

### 1. `StatusFilter` komponens (`OrdersPage`-be ágyazva)

- Tab-szerű szűrő sáv (Tailwind): **Összes | Draft | Submitted | In Production | Done | Cancelled**
- Aktív tab vizuálisan kiemelve
- Kattintásra: URL query param frissítés (`?status=Draft`) — `useSearchParams` (React Router)
- `useOrders` hook `status` paraméterrel hívja a BFF-et: `GET /bff/api/orders?status=...`
- "Összes" esetén nincs query param

### 2. `OrderStatusBadge` komponens

```typescript
// Státusz → szín mapping (Tailwind):
Draft         → gray  (bg-gray-100 text-gray-700)
Submitted     → blue  (bg-blue-100 text-blue-700)
InProduction  → yellow (bg-yellow-100 text-yellow-800)
Done          → green (bg-green-100 text-green-700)
Cancelled     → red   (bg-red-100 text-red-700)
```

- Pill alakú badge (`rounded-full px-2 py-1 text-xs font-medium`)
- Felhasználható az `OrdersPage` listában **és** az `OrderDetailPage` headerben is (cseréld le az esetleges sima szöveges megjelenítést)

### 3. `OrderHistoryPanel` komponens (`OrderDetailPage`-be ágyazva)

- Vertical timeline: minden `OrderHistoryEvent` egy sor
- Sor tartalma: `OrderStatusBadge (toStatus)` + `occurredAt` (lokalizált: `hu-HU` locale, `dateStyle: 'medium', timeStyle: 'short'`) + `triggeredBy`
- Loading skeleton: 3 placeholder sor amíg tölt
- Empty state: "Nincs státusz történet" (ha `events: []`)
- API hívás: `GET /bff/api/orders/:id/history`

### 4. `useOrderHistory` hook (`src/hooks/useOrderHistory.ts`)

```typescript
export function useOrderHistory(orderId: string) {
  return useQuery({
    queryKey: ['order-history', orderId],
    queryFn: () => ordersApi.getOrderHistory(orderId),
    staleTime: 30_000,   // 30s — history ritkán változik
  });
}
```

### 5. `ordersApi.ts` bővítés

```typescript
export interface OrderHistoryEvent {
  id: string;
  fromStatus: string | null;
  toStatus: string;
  occurredAt: string;
  triggeredBy: string;
  note?: string;
}

export interface OrderHistory {
  orderId: string;
  events: OrderHistoryEvent[];
}

export async function getOrderHistory(orderId: string): Promise<OrderHistory> {
  const { data } = await apiClient.get<OrderHistory>(`/orders/${orderId}/history`);
  return data;
}
```

## Tesztek (kötelező)

- `StatusFilter`: render (6 tab), aktív tab stílus, kattintás → query param változik, "Összes" → query param nincs
- `OrderStatusBadge`: minden státusz helyes színnel renderel
- `OrderHistoryPanel`: loading skeleton, empty state, events lista render, dátum formázás
- `useOrderHistory`: API hívás, staleTime, error state
- **≥12 új teszt**

## DoD

- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` → mind zöld, ≥12 új teszt (összesen ≥67)
- [ ] `pnpm lint` → 0 hiba
- [ ] `pnpm typecheck` → 0 error
- [ ] `StatusFilter` URL query param szinkronban van a TanStack Query cache-szel
- [ ] `OrderStatusBadge` mindkét oldalon (list + detail) megjelenik
- [ ] `OrderHistoryPanel` loading / empty / data state mind tesztelt
- [ ] Dátum formázás: `hu-HU` locale
- [ ] git commit + push

## Outbox

DONE: `mailbox/fe/outbox/2026-04-16_005_order-status-timeline-filter-done.md`

## Skillек & Agentек

- `/senior-frontend` — URL query param szinkron, TanStack Query filter, timeline UX
- `/javascript-typescript-jest` — badge tesztek, hook mock, query param szimulálás
- `/senior-qa` — loading skeleton, empty state, lokalizált dátum, edge cases
- `/a11y-audit` — tab navigáció (aria-selected), timeline landmark, badge kontrasztarány
- Agent: `expert-react-frontend-engineer` — useSearchParams + TanStack Query kombinálása, optimistic filter
- Agent: `se-ux-ui-designer` — státusz badge szín kontrasztarány WCAG AA, timeline vizuális hierarchia
- Sub-agenteket nyugodtan indíts párhuzamosan (pl. StatusFilter + OrderHistoryPanel + tesztek egyszerre)
