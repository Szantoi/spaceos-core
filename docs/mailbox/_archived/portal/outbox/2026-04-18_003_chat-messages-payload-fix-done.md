---
id: MSG-PORTAL-003-DONE
from: portal
to: root
type: done
status: READ
ref: MSG-PORTAL-003
created: 2026-04-18
---

## Összefoglaló

**BUG-005c — Chat messages payload üres tömb:** javítva.

**Gyökérok:** `useCallback([])` stale closure — a `messages` state mindig `[]` a closure-ban. A korábbi megközelítés `setMessages` functional updater-ben rögzítette `requestMessages`-t, de az updater **render után fut**, a `JSON.stringify` (fetch body) viszont **előtte** számítódik ki → üres tömb kerül a POST body-ba.

**Fix:** `messagesRef` (useRef) tükrözi a messages state-et `useEffect`-en keresztül. A `sendMessage` a ref-ből olvas (`messagesRef.current`), nem a stale closure-ból. `clearMessages` is nullázza a ref-et.

## Módosított fájlok

| Fájl | Változás |
|---|---|
| `src/hooks/useStreamingChat.ts` | `messagesRef` + `useEffect` szinkronizáció; `sendMessage` ref-et használ; `clearMessages` ref-et nulláz |
| `src/hooks/useStreamingChat.test.ts` | `mockFetch` spy-t ad vissza; +2 teszt: fetch body assertion + multi-turn history |

## Tesztek

311 / 311 zöld (+2 új teszt).
- `fetch body contains user message (not empty array)` — direkt assertion a POST body-ra
- `second sendMessage includes prior assistant reply in body` — multi-turn history ellenőrzés

## Security review

- XSS: változatlan — SSE feldolgozás, React auto-escape
- Auth: változatlan — Bearer token a fetch headerben
- Token handling: változatlan — csak memóriában
- Input sanitization: változatlan — zod 500 char max

## Commit

`94a79e6` — fix(chat): messages payload empty array — stale closure race condition (BUG-005c)

## Megjegyzés

INFRA deploy szükséges a fixhez (DoD szerint). Portal oldalon kész.
