---
id: MSG-FE-003
from: root
to: fe
type: task
priority: high
status: READ
ref: FE-PORTAL
created: 2026-04-16
---

# FE-003 — Rendelés kitöltés flow (tételek + kalkuláció + cutting list)

## Kontextus

WD: `/opt/spaceos/spaceos-doorstar-portal/`. CLAUDE.md kötelező olvasás.
Pipeline: INBOX READ → CODE → BUILD → TEST → REVIEW → SECURITY → OUTBOX

## Előzmény

FE-002 DONE: Orders dashboard él (`/orders`, `/orders/:id`), axios client Bearer interceptorral,
TanStack Query cache-szel. Commit: `4bc5984`.

## Feladat

Implementáld a rendelés kitöltési flow-t: ajtótételek hozzáadása → kalkuláció → cutting list megtekintése.

## BFF API

```typescript
POST   /bff/api/orders/:id/items         // Tétel hozzáadása
POST   /bff/api/orders/:id/calculate     // Kalkuláció indítása
GET    /bff/api/orders/:id/cutting-list  // Cutting list lekérése

// POST items request:
{ doorTypeId: string; quantity: number; customOptions?: Record<string, unknown> }

// GET cutting-list response (várható struktúra):
{
  orderId: string;
  items: CuttingListItem[];
  totalSheets: number;
  estimatedWaste: number;
}

// CuttingListItem:
{
  partCode: string;
  description: string;
  widthMm: number;
  heightMm: number;
  quantity: number;
  material: string;
}
```

**DoorType ID-k** (egyelőre hardcode-olva, amíg nincs külön endpoint):
```typescript
export const DOOR_TYPES = [
  { id: 'dt-standard-90', label: 'Standard 90cm' },
  { id: 'dt-standard-100', label: 'Standard 100cm' },
  { id: 'dt-double-180', label: 'Dupla szárny 180cm' },
] as const;
```

## Implementálandó

### 1. `AddItemForm` komponens (az `OrderDetailPage`-be ágyazva)

- Dropdown: DoorType kiválasztás
- Szám input: mennyiség (1–50)
- `+ Tétel hozzáadása` gomb → `POST .../items` → cache invalidate `['orders', id]`
- Csak Draft státuszú rendelésnél látható

### 2. `OrderDetailPage` bővítés

- Tételek listája (doorType, quantity, státusz)
- `Kalkulálás` gomb (Draft státusz, ≥1 tétel esetén aktív) → `POST .../calculate`
- Kalkuláció után státusz frissül (Submitted/InProduction felé mehet)

### 3. `CuttingListPage` (`/orders/:id/cutting-list`)

- Táblázat: partCode, leírás, méret (W×H mm), mennyiség, anyag
- Összesítő: total lapok száma, becsült hulladék %
- `Cache-Control: no-store` — ne cache-elje a böngésző (friss adatok kellenek mindig)
- Vissza gomb → `/orders/:id`
- `Nyomtatás` gomb → `window.print()` (CSS: `@media print` elrejti a navigációt)

### 4. Route hozzáadás

```typescript
/orders/:id/cutting-list  // ProtectedRoute mögött
```

### 5. Navigáció

Az `OrderDetailPage`-en `Cutting lista megtekintése` link/gomb → `/orders/:id/cutting-list`

## Tesztek (kötelező)

- `AddItemForm`: form render, submit, validation (quantity 1-50)
- `CuttingListPage`: táblázat render, loading, empty state, summary row
- `useOrderItems` hook (ha külön hook): API hívás, cache invalidation
- **≥14 új teszt**

## DoD

- [ ] `pnpm build` → 0 error
- [ ] `pnpm test` → mind zöld, ≥14 új teszt
- [ ] `pnpm lint` → 0 hiba
- [ ] `pnpm typecheck` → 0 error
- [ ] `AddItemForm` csak Draft státusznál látható
- [ ] `CuttingListPage` `Cache-Control: no-store` header küld (fetch options)
- [ ] Print CSS: navigáció elrejtve nyomtatáskor
- [ ] git commit + push

## Outbox

DONE: `mailbox/fe/outbox/2026-04-16_003_order-completion-flow-done.md`

## Skillек & Agentек

- `/senior-frontend` — React form pattern, cache invalidation, print CSS
- `/javascript-typescript-jest` — form tesztek, mock API responses
- `/senior-qa` — edge case-ek (üres cutting list, kalkuláció failure)
- `/a11y-audit` — form accessibility (label, aria, keyboard nav)
- Agent: `expert-react-frontend-engineer` — TanStack Query mutation + invalidation pattern
- Agent: `se-security-reviewer` — Cache-Control no-store, token exposure
- Sub-agenteket nyugodtan indíts párhuzamosan (pl. CuttingListPage + AddItemForm + tesztek egyszerre)
