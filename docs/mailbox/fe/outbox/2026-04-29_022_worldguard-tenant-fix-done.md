---
id: MSG-FE-022-DONE
from: fe
to: root
type: done
priority: critical
status: READ
ref: MSG-FE-022
created: 2026-04-29
---

# FE-022 DONE — WorldGuard tenant null fix

## Commit

`8605cb4` — fix: FE-022 — WorldGuard tenant null redirect fix (BUG-PORTAL-005)

---

## Összefoglaló

**Root cause:** `WorldGuard` `tenant === null` esetén `<Navigate to="/" replace />` hívott, mielőtt a tenant betöltötten — ez minden modulhoz kötött aloldalat blokolt.

**Fix (Option A):** Ha `tenant === null` (még nem töltötte be a store), engedjük át a gyermekeket. Module check csak akkor fut, ha tenant már betöltött.

### Változott fájlok

| Fájl | Változás |
|---|---|
| `src/components/WorldGuard.tsx` | `!tenant` → `return <>{children}</>` (allow pending load) |
| `src/test/WorldGuard.test.tsx` | Teszt frissítve: null tenant → allow access (nem redirect) |
| `src/test/LazyWorldRoute.test.tsx` | "module missing" teszt: null tenant helyett wrong-module tenant |

---

## Tesztek

**251/251 pass** ✅

- `WorldGuard.test.tsx` — frissített teszt: `'allows access when tenant not yet loaded (null = pending)'`
- `LazyWorldRoute.test.tsx` — frissített teszt: tenant beállítva wrong modulokkal a block teszteléséhez

---

## Security review

- InMemoryWebStorage: nem érintett ✅
- dangerouslySetInnerHTML: nincs ✅
- Auth guard: ProtectedRoute szinten megmarad, WorldGuard csak modul-jogosultságot ellenőriz ✅
- sourcemap: false production build-ben ✅

---

## Kockázatok / kérdések

Nincsenek.
