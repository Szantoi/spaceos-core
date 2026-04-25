---
id: MSG-FE-016
from: root
to: fe
type: task
priority: high
status: READ
ref: MSG-TESTER-044
created: 2026-04-25
---

# FE-016 — Portal bugfixes (BUG-PORTAL-003 + BUG-ORCH-001)

> TESTER 1/8 PASS — login OK, de 2 bug blokkolja a többit.

---

## BUG-PORTAL-003: userStore → sessionStorage

**Tünet:** F5 / page reload → /login redirect (token elvész)
**Root cause:** `userStore: InMemoryWebStorage` — page reload törli

**Fix:** `src/auth/keycloak.config.ts`

```typescript
// VOLT:
userStore: new WebStorageStateStore({ store: inMemoryStorage }),

// KELL:
userStore: new WebStorageStateStore({ store: sessionStorage }),
```

**SEC-UI-02 update:** Az eredeti szándék az volt hogy token ne maradjon a storage-ban tab bezárás után. A `sessionStorage` ezt teljesíti — tab bezáráskor törlődik, de F5 reload-nál megmarad. Ez a helyes kompromisszum.

---

## BUG-ORCH-001: GET /bff/api/orders → 404

**Tünet:** Rendelés lista üres, API 404
**Root cause:** A portal `/bff/api/orders`-t hív, de az Orchestrator BFF-ben valószínűleg más a route.

**Ellenőrizd:**
1. Mi az orders API URL a portal kódban? (`src/api/` vagy hooks)
2. Mi az Orchestrator BFF-ben az orders route? (Kernel proxy: `/bff/api/*` → Kernel `/api/*`)
3. A Kernel `/api/orders` létezik? (Joinery endpoint: `/api/orders` a Joinery porton, nem a Kernelen)

**Lehetséges fix:** A portal a Joinery orders-t hívja `/bff/joinery/orders`-en keresztül, nem `/bff/api/orders`-en. Ellenőrizd és javítsd az API client URL-t.

---

## Definition of Done

- [ ] F5 reload → token megmarad (sessionStorage)
- [ ] Orders lista betölt (helyes API URL)
- [ ] `pnpm build` 0 error
- [ ] `pnpm test` ≥ 99 pass
- [ ] Outbox DONE
