---
id: MSG-FE-042-DONE
from: fe
to: root
type: done
priority: high
status: READ
date: 2026-05-29
ref: MSG-FE-042
commit: 51f5417
---

# FE-042 DONE — WorkflowPage NewOrderDrawer POST bekötés

## Elvégzett munkák

### Módosított fájlok

| Fájl | Változás |
|---|---|
| `src/components/orders/NewOrderDrawer.tsx` | `flowEpicId?` + `onSuccess?` prop, POST mód mezők, `POST /joinery/api/orders` |
| `src/pages/WorkflowPage.tsx` | `NewOrderDrawer` import + `orderDrawerOpen`/`epicForOrder` state + "Rendelés indítása" gomb `DetailPanel`-ban |
| `src/components/orders/__tests__/NewOrderDrawer.test.tsx` | +5 új teszt (POST mód) |
| `src/pages/__tests__/WorkflowPage.test.tsx` | +4 új teszt (gomb + drawer megnyitás) |

## Funkcionális összefoglaló

### NewOrderDrawer — POST mód

- Ha `flowEpicId` megadva: "Rendelés indítása" cím, projekt mezők jelennek meg
  - Projektnév * (kötelező)
  - Projektazonosító * (kötelező, mono font)
  - Ügyfél neve (opcionális)
  - Ügyfél cím (opcionális)
  - Ügyfél telefon (opcionális)
  - Határidő (date picker, min=holnap)
- Validáció: projektnév + projektazonosító kötelező; hiány esetén "Kötelező mező" felirat
- POST `${API_BASE.joinery}/api/orders` body: `{flowEpicId, projectId, projectName, clientName?, clientAddress?, clientPhone?, deliveryDate?}`
- 201 Created → `onSuccess(orderId)` callback
- Ha `flowEpicId` nincs: eredeti mock mód változatlan (Mentés vázlatként / Mentés és számítás)

### WorkflowPage — Rendelés indítása gomb

- `DetailPanel` actions sorban: `<PrimaryBtn icon="plus" onClick={() => onStartOrder(epic)}>Rendelés indítása</PrimaryBtn>`
- `WorkflowPage` state: `orderDrawerOpen` + `epicForOrder`
- `NewOrderDrawer` mountolva `flowEpicId={epicForOrder?.id}` paraméterrel
- `OrdersPage` viselkedése változatlan (nem módosítva)

## Eredmény

```
pnpm build  → 0 TS hiba, sikeres Vite build
pnpm test   → 370 passed, 0 failed  (+10 új teszt, előtte 360)
```

## DONE kritériumok

- [x] `NewOrderDrawer` fogad `flowEpicId` propot, POST `/joinery/api/orders`
- [x] `WorkflowPage` DetailPanel-ban "Rendelés indítása" gomb
- [x] `OrdersPage` viselkedése változatlan (mock)
- [x] `pnpm build` → 0 hiba
- [x] `pnpm test` → minden zöld (+10 új teszt)
- [x] Outbox DONE commit: `51f5417`
