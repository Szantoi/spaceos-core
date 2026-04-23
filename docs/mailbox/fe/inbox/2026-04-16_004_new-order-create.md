---
id: MSG-FE-004
from: root
to: fe
type: task
priority: high
status: READ
ref: FE-PORTAL
created: 2026-04-16
---

# FE-004 — Új rendelés létrehozása (Create Order flow)

## Kontextus

WD: `/opt/spaceos/spaceos-doorstar-portal/`. CLAUDE.md kötelező olvasás.
Pipeline: INBOX READ → CODE → BUILD → TEST → REVIEW → SECURITY → OUTBOX

## Előzmény

FE-003 DONE: Order completion flow él (AddItemForm + kalkuláció + CuttingListPage).
Commit: `de696cc`. Tesztek: 40/40 zöld.

A teljes workflow most: **? → /orders/:id → items → calculate → cutting-list → print**

A hiányzó láncszem: honnan jön az order? Jelenleg nincs "Új rendelés" gomb — FE-004 ezt zárja be.

## BFF API

```typescript
POST   /bff/api/orders    // Új rendelés létrehozása
// Request:
{ reference?: string; notes?: string }
// Response:
{ id: string; reference: string; status: 'Draft'; createdAt: string; items: [] }

GET    /bff/api/orders    // Már implementált (FE-002) — cache invalidate után frissül
```

## Implementálandó

### 1. `CreateOrderModal` komponens

- Modal / slide-over panel (Tailwind `fixed inset-0` overlay)
- Mezők:
  - `reference`: szöveges input (opcionális, max 100 kar) — pl. "Kovács ház 2026"
  - `notes`: textarea (opcionális, max 500 kar)
- `Létrehozás` gomb → `POST /bff/api/orders` → cache invalidate `['orders']` → modal bezárás → navigate `/orders/:newId`
- `Mégse` gomb → modal bezárás, nincs API hívás
- Loading state a submit alatt (gomb disabled + spinner)
- Error state: inline hibaüzenet az API hiba esetén

### 2. `OrdersPage` bővítés (`/orders`)

- `+ Új rendelés` gomb a header-ben (mindig látható, nem státusz-függő)
- Kattintásra `CreateOrderModal` nyílik

### 3. TanStack Query mutation

```typescript
// useCreateOrder hook (src/hooks/useCreateOrder.ts)
const mutation = useMutation({
  mutationFn: (data: CreateOrderInput) => ordersApi.createOrder(data),
  onSuccess: (newOrder) => {
    queryClient.invalidateQueries({ queryKey: ['orders'] });
    navigate(`/orders/${newOrder.id}`);
  },
});
```

### 4. `ordersApi.ts` bővítés

```typescript
export interface CreateOrderInput {
  reference?: string;
  notes?: string;
}

export async function createOrder(data: CreateOrderInput): Promise<Order> {
  const { data: order } = await apiClient.post<Order>('/orders', data);
  return order;
}
```

## Tesztek (kötelező)

- `CreateOrderModal`: render, submit (success path), submit (error path), cancel closes modal, loading state, validation (max length)
- `useCreateOrder` hook: mutation hívás, cache invalidation, navigate after success
- `OrdersPage`: "+ Új rendelés" gomb látható, modal megnyílik kattintásra
- **≥10 új teszt**

## DoD

- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` → mind zöld, ≥10 új teszt (összesen ≥50)
- [ ] `pnpm lint` → 0 hiba
- [ ] `pnpm typecheck` → 0 error
- [ ] `CreateOrderModal` csak bejelentkezett felhasználónak látható (ProtectedRoute-on belül van, ez auto teljesül)
- [ ] Modal bezárul create után, navigate `/orders/:id`-ra
- [ ] Loading state: gomb disabled + vizuális visszajelzés
- [ ] Error state: hibaüzenet inline (ne crash-eljen)
- [ ] git commit + push

## Outbox

DONE: `mailbox/fe/outbox/2026-04-16_004_new-order-create-done.md`

## Skillек & Agentек

- `/senior-frontend` — React modal pattern, controlled form, optimistic UI
- `/javascript-typescript-jest` — modal tesztek, mutation mock, navigate mock
- `/senior-qa` — error state, loading state, network failure edge case
- `/a11y-audit` — modal trap focus, aria-modal, ESC bezárás, backdrop click
- Agent: `expert-react-frontend-engineer` — TanStack Query useMutation pattern, modal UX
- Agent: `se-security-reviewer` — POST body validáció, max length, XSS védelem
- Sub-agenteket nyugodtan indíts párhuzamosan (pl. CreateOrderModal + useCreateOrder hook + tesztek egyszerre)
