---
id: MSG-FE-042
from: root
to: fe
type: task
priority: high
status: UNREAD
created: 2026-05-29
---

# FE-042 — WorkflowPage NewOrderDrawer POST bekötés

## Háttér

A Joinery `POST /joinery/api/orders` él (5002). A WorkflowPage már tölt FlowEpic kártyákat
API-ból (`GET /api/facilities/{id}/flow-epics`). A `NewOrderDrawer` jelenleg teljesen mock —
nincs `POST` hívás.

## Feladat

### 1. `NewOrderDrawer` — `flowEpicId` prop + POST bekötés

**Fájl:** `src/components/orders/NewOrderDrawer.tsx`

Adj hozzá `flowEpicId?: string` propot. Ha van értéke, a form POST-ol a Joinery API-ra.

**Request body** (`POST /joinery/api/orders`):
```ts
{
  flowEpicId: string        // kötelező — az epic kártyáról jön
  projectId: string         // kötelező — user adja meg (pl. "DOOR-2026-001") VAGY uuid auto-gen
  projectName: string       // kötelező — user adja meg
  clientName?: string       // opcionális
  clientAddress?: string    // opcionális
  clientPhone?: string      // opcionális
  deliveryDate?: string     // opcionális — ISO date (DateOnly)
}
```

**Success flow:** 201 Created → `onSuccess(orderId)` callback + toast.

**Mock fallback:** ha `flowEpicId` nincs megadva (pl. OrdersPage), maradjon a jelenlegi mock viselkedés.

---

### 2. WorkflowPage — "Rendelés indítása" gomb a FlowCard-on / DetailPanel-ban

**Fájl:** `src/pages/WorkflowPage.tsx`

A `FlowCard` komponensbe vagy a `DetailPanel`-ba kerüljön egy **"Rendelés indítása"** gomb/link,
amely megnyitja a `NewOrderDrawer`-t az adott epic ID-vel:

```tsx
<NewOrderDrawer
  open={orderDrawerOpen}
  onClose={() => setOrderDrawerOpen(false)}
  flowEpicId={selectedEpic?.id}
  onSuccess={(orderId) => {
    // toast: "Rendelés létrehozva"
    setOrderDrawerOpen(false)
  }}
/>
```

A meglévő `PrimaryBtn icon="plus"` ("Új feladat") **ne változzon** — az más funkció lesz majd.

---

### 3. Form mezők

| Mező | Típus | Kötelező |
|---|---|---|
| Projektnév | szabad szöveg | igen |
| Projektazonosító | szabad szöveg (pl. "DOOR-2026-001") | igen |
| Ügyfél neve | szabad szöveg | nem |
| Ügyfél cím | szabad szöveg | nem |
| Ügyfél telefon | szabad szöveg | nem |
| Határidő | date picker (min = holnap) | nem |

---

## Mock fallback

Az `OrdersPage`-en lévő `NewOrderDrawer` hívása (`flowEpicId` nélkül) maradjon mock módban —
**ne változzon** az `OrdersPage` viselkedése.

## Build + test gate

- `pnpm build` → 0 TS hiba
- `pnpm test` → minden zöld, min. +8 új teszt (NewOrderDrawer POST + WorkflowPage gomb tesztek)

## DONE kritériumok

- [ ] `NewOrderDrawer` fogad `flowEpicId` propot, POST `/joinery/api/orders`
- [ ] `WorkflowPage` FlowCard / DetailPanel-ban "Rendelés indítása" gomb
- [ ] `OrdersPage` viselkedése változatlan (mock)
- [ ] `pnpm build` → 0 hiba
- [ ] `pnpm test` → minden zöld (+8 legalább)
- [ ] Outbox DONE commit hash-sel
