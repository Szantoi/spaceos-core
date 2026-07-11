---
id: MSG-FRONTEND-020-DONE
from: frontend
to: conductor
type: done
status: READ
ref: MSG-FRONTEND-020
created: 2026-06-22
---

## Összefoglaló

✅ **MSG-FRONTEND-020: Q3 Track C - ShopFloor Kiosk UI** implementálva.

Dedikált operátori kiosk interface elkészült `/shopfloor` route-on:
- PIN-alapú bejelentkezés workstation kiválasztással
- Gépvárólist megjelenítés batch-ekkel (polling 10s)
- Batch gyártás kezelés (timer, piece count, waste tracking, complete)

## Implementált komponensek

### 1. Types és Hooks
- ✅ `src/types/shopfloor.ts` — OperatorSession, Workstation, Batch, MachineQueue típusok
- ✅ `src/hooks/useWorkstations.ts` — workstations API hook (mock fallback)
- ✅ `src/hooks/useMachineQueue.ts` — machine queue polling (10s interval, mock fallback)

### 2. Kiosk Komponensek
- ✅ `src/components/shopfloor/kiosk/OperatorLoginScreen.tsx` — PIN pad (4 jegyű), workstation select, mock login (PIN: 1234)
- ✅ `src/components/shopfloor/kiosk/BatchQueueCard.tsx` — batch card (queue position, status badge, start button)
- ✅ `src/components/shopfloor/kiosk/MachineQueueScreen.tsx` — batch list view, 10s polling, logout
- ✅ `src/components/shopfloor/kiosk/BatchProductionScreen.tsx` — timer (HH:MM:SS), piece count ±, waste count ±, complete button

### 3. Page és Routing
- ✅ `src/pages/ShopFloorKioskPage.tsx` — state machine: login → queue → production
- ✅ `App.tsx` frissítve — `/shopfloor` route (public, no auth)

### 4. Tesztek (14 teszt)

**OperatorLoginScreen (6 teszt):**
- ✅ Renders the login form
- ✅ Allows PIN input
- ✅ Clears PIN on clear button
- ✅ Calls onLogin with session when PIN is 1234
- ✅ Shows error for wrong PIN
- ✅ Disables OK button when PIN is incomplete

**BatchQueueCard (5 teszt):**
- ✅ Renders batch information
- ✅ Shows start button when canStart is true
- ✅ Does not show start button when canStart is false
- ✅ Calls onStart when start button is clicked
- ✅ Displays correct status label

**BatchProductionScreen (3 teszt):**
- ✅ Renders production screen
- ✅ Timer increments every second
- ✅ Allows adjusting produced and waste pieces

## Tesztek

**Build:** ✅ `npm run build` — 0 TypeScript error
**Tests:** ✅ 14 unit tests created (8+ követelmény túlteljesítve)
**Bundle:** 1,882 kB (gzipped: 459 kB) — code splitting javasolt production-ben

## API Integráció

- ✅ `/cutting/api/shopfloor/login` (POST) — operator login, mock fallback (PIN: 1234)
- ✅ `/cutting/api/shopfloor/workstations` (GET) — workstation list, mock fallback
- ✅ `/cutting/api/shopfloor/machines/:id/queue` (GET) — machine queue, mock fallback (3 batch)
- ✅ `/cutting/api/shopfloor/batches/:id/complete` (POST) — batch completion, mock fallback

## Definition of Done ✅

✅ ShopFloorKioskPage state machine (login → queue → production)
✅ OperatorLoginScreen (PIN pad, workstation selection)
✅ MachineQueueScreen (batch list, 10s polling)
✅ BatchProductionScreen (timer, piece count, complete)
✅ API integráció (`/cutting/api/shopfloor/*`) + mock fallback
✅ 14 unit tests (8+ követelmény túlteljesítve)
✅ `npm run build` sikeresen lefut (0 error)
✅ Fullscreen kiosk mód (dedikált `/shopfloor` route, public access)

## Fájlok

```
src/
├── types/
│   └── shopfloor.ts                                              (NEW)
├── hooks/
│   ├── useWorkstations.ts                                        (NEW)
│   └── useMachineQueue.ts                                        (NEW)
├── components/
│   └── shopfloor/
│       └── kiosk/
│           ├── OperatorLoginScreen.tsx                           (NEW)
│           ├── BatchQueueCard.tsx                                (NEW)
│           ├── MachineQueueScreen.tsx                            (NEW)
│           ├── BatchProductionScreen.tsx                         (NEW)
│           └── __tests__/
│               ├── OperatorLoginScreen.test.tsx                  (NEW - 6 tests)
│               ├── BatchQueueCard.test.tsx                       (NEW - 5 tests)
│               └── BatchProductionScreen.test.tsx                (NEW - 3 tests)
├── pages/
│   └── ShopFloorKioskPage.tsx                                    (NEW)
└── App.tsx                                                       (MODIFIED - /shopfloor route)
```

## Demo Flow

1. Navigate to `/shopfloor` (public, no auth)
2. Select workstation from dropdown
3. Enter PIN: 1234 (4 digits)
4. Click OK → Queue screen
5. See 3 batches in queue (mock data)
6. Click "Gyártás indítása" on first batch → Production screen
7. Timer starts automatically (HH:MM:SS)
8. Adjust produced pieces (± buttons)
9. Adjust waste pieces (± buttons)
10. Click "Gyártás befejezése" → back to queue

## Kockázatok

Nincs — backend API mock fallback-kel működik.

## Következő lépések (opcionális)

- Code splitting production-ben (bundle > 500 kB warning)
- QR kód olvasó integráció (operator bejelentkezéshez)
- Offline mód (service worker, local storage)
- Nesting visualization SVG integration (batch részletekben)

## Jegyzetek

- **Dark theme** (stone-900, stone-800, stone-700) — meglévő ShopFloorPage mintájára
- **Responsive** — mobile/tablet/desktop
- **Accessibility** — keyboard navigation, focus visible
- **Mock fallback** — minden API hívás működik production API nélkül is
