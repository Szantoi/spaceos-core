---
id: MSG-ORCH-070
from: root
to: orchestrator
type: task
priority: high
status: READ
ref: MSG-E2E-031-BLOCKED
created: 2026-04-15
---

# MSG-ORCH-070 — Cutting BFF proxy route-ok

## Kontextus

A Modules.Cutting service-ek VPS-en futnak:
- Inventory → `http://127.0.0.1:5004`
- Cutting → `http://127.0.0.1:5005`
- Procurement → `http://127.0.0.1:5006`

Az E2E terminál csak BFF-en keresztül érheti el őket. Jelenleg nincs `/bff/cutting/*`, `/bff/inventory/*`, `/bff/procurement/*` route.

## Feladat

A meglévő BFF proxy minta alapján (pl. `abstractions.route.ts` → 5003) hozd létre a három új route fájlt.

### 1. `src/routes/inventory.route.ts`

```typescript
// /bff/inventory/* → http://127.0.0.1:5004/api/inventory/*
```

Minta: `abstractions.route.ts` — catch-all proxy, X-SpaceOS-Brand header forwarding, requireAuth middleware.

### 2. `src/routes/cutting.route.ts`

```typescript
// /bff/cutting/* → http://127.0.0.1:5005/api/cutting/*
```

### 3. `src/routes/procurement.route.ts`

```typescript
// /bff/procurement/* → http://127.0.0.1:5006/api/procurement/*
```

### 4. Regisztrálás `src/app.ts`-ben

Ugyanúgy mint az abstractions route — add hozzá mindhárom route-ot.

### 5. Környezeti változók `.env`-be

```
INVENTORY_BASE_URL=http://127.0.0.1:5004
CUTTING_BASE_URL=http://127.0.0.1:5005
PROCUREMENT_BASE_URL=http://127.0.0.1:5006
```

## Build + test gate

```bash
npm run build   → 0 TS error
npm test        → meglévő tesztek mind zöldek (≥207)
```

Új teszteket nem kell írni — az E2E fedezi.

## DoD

- [ ] 3 új route fájl + app.ts regisztráció
- [ ] `npm run build` → 0 error
- [ ] `npm test` → ≥207 pass
- [ ] VPS deploy: `pm2 restart spaceos-orchestrator`
- [ ] Ellenőrzés: `curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:3000/bff/inventory/stock` → 401 (nem 404)
- [ ] Outbox: `MSG-ORCH-070-DONE`
