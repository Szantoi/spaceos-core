---
id: MSG-P021-DONE
from: portal
to: architect
type: done
date: 2026-04-08
re: MSG-P021
---

# MSG-P021 — Day 11: CabinetOrdersPage live B2BHandshake hookup

**Date:** 2026-04-08
**Status:** DONE

---

## Implementált fájlok

### `src/api/handshakes.service.ts` (új)
- `listHandshakes()` → GET /bff/handshakes
- `createHandshake()` → POST /bff/handshakes
- `acceptHandshake(id)` → PUT /bff/handshakes/:id/accept
- `rejectHandshake(id)` → PUT /bff/handshakes/:id/reject
- `HandshakeDto`, `CreateHandshakeRequest` típusok

### `src/hooks/useHandshakes.ts` (új)
- `useHandshakes()` — TanStack Query, queryKey: ['handshakes']
- `useCreateHandshake()` — invalidates on success
- `useAcceptHandshake()` — invalidates on success
- `useRejectHandshake()` — invalidates on success

### `src/modules/cabinet/CabinetOrdersPage.tsx` (frissítve)
- `allowedHosts` kizárólag `useAuthStore(s => s.allowedHosts)` — nem URL/localStorage
- `HandshakeOrderPanel` with `activeTrade="cabinet"` (BE-P3CP-07 filter)
- `HandshakeHostPanel` incoming handshakes listája
- Error state és loading skeleton

### `packages/@spaceos/joinery-ui/src/base/HandshakeOrderPanel/index.tsx` (frissítve)
- `onCreateRequest?(hostTenantId, tradeType)` prop hozzáadva (app layer kezeli az API hívást)
- `isCreating` prop loading state-hez
- "Rendelés" gomb minden host sorban

## Architektúra compliance
- CLAUDE.md Rule 2: hook fetch, service API call, component render ✅
- CLAUDE.md Rule 3: TanStack Query minden API híváshoz ✅
- SEC-P3CP-05: `allowedHosts` partialize-ból kizárva ✅
- BE-P3CP-07: `activeTrade="cabinet"` filter ✅

## DoD
- `tsc --noEmit` → 0 error ✅
- auth.store.test.ts → 13/13 ✅
