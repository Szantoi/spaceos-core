---
id: MSG-FE-055
from: root
to: fe
type: task
priority: high
status: READ
ref: —
created: 2026-06-16
---

# FE API Integration Audit

## Cél

A 27 élő világ jelenleg részben vagy teljesen `window.sim` / mock adattal fut.  
Meg kell határoznunk pontosan melyik page-ek vannak már valódi backend API-hoz kötve és melyik nem.

## Feladat

Végezd el az audit-ot a `frontend/joinerytech-portal/src/pages/` mappában.

**Minden page-hez határozd meg:**

| Page | Valódi API hívás van? | Endpoint(ek) | Mock/sim függőség | Megjegyzés |
|---|---|---|---|---|

Keresési szempontok:
- `fetch(` / `axios` / `useQuery` / `useMutation` hívások → valódi API
- `window.sim` / `mockData` / `import ... from '../mocks/` → mock
- `import.meta.env.VITE_API_URL` használat → valódi API jelöl
- Hook-ok: `useSales*`, `useInventory*`, `useProcurement*` stb. → nézzük meg mi van mögötte

## Output

Hozz létre: `frontend/joinerytech-portal/API_INTEGRATION_STATUS.md`

```markdown
# API Integration Status — 2026-06-16

| Page | Status | Real Endpoints | Mock Dependencies |
|---|---|---|---|
| SalesPage | ✅ REAL | GET /sales/api/customers, ... | none |
| CrmPage | ❌ MOCK | — | window.sim.leads |
...
```

## DoD

- `API_INTEGRATION_STATUS.md` létrejött a portal gyökérben
- Minden page státusza meghatározva (REAL / PARTIAL / MOCK)
- Outbox üzenet: `MSG-FE-055-DONE`

## Skill

Használd a `/spaceos-terminal` skillt. Sub-agent engedélyezett.
