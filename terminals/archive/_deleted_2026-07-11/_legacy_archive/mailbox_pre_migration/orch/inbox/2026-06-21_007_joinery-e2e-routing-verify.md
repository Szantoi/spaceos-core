---
id: MSG-ORCH-007
from: root
to: orch
type: task
priority: medium
status: READ
model: haiku
ref: docs/planning/queue/2026-06-21_0055_consensus.md
created: 2026-06-21
---

# Joinery E2E Flow — Phase 1: API Routing Verification

## Összefoglaló

**Joinery End-to-End vertical slice Phase 1 Orchestrator feladat** — verify és extend API routing a Joinery modul új endpointjaihoz (konfigurátor, work order).

**Scope:** Quick verification task (30-60 perc) — ellenőrizd hogy az új Joinery API-k helyesen proxy-zódnak-e az Orchestrator-on keresztül.

---

## Új Joinery Endpoints (MSG-JOINERY-058)

### 1. POST /joinery/api/products/configure
- **Backend:** Joinery modul (port 5002)
- **Orchestrator route:** `POST /api/products/configure` → proxy to `http://localhost:5002/joinery/api/products/configure`

### 2. POST /joinery/api/work-orders
- **Backend:** Joinery modul (port 5002)
- **Orchestrator route:** `POST /api/work-orders` → proxy to `http://localhost:5002/joinery/api/work-orders`

---

## Feladat

### 1. Routing Verification (ha már létezik)

Ellenőrizd hogy az Orchestrator `backend/spaceos-orchestrator/src/routes/` mappában van-e `joinery-proxy` konfiguráció:

```typescript
// Példa elvárt routing:
app.post('/api/products/configure', joineryProxy)
app.post('/api/work-orders', joineryProxy)

// Proxy middleware:
const joineryProxy = createProxyMiddleware({
  target: 'http://localhost:5002',
  pathRewrite: { '^/api': '/joinery/api' },
  changeOrigin: true
})
```

**Ha létezik:** ✅ Verify sikeres → DONE outbox küldés (30 perc)

**Ha NEM létezik:** Extend routing (lásd alább)

---

### 2. Routing Extension (ha hiányzik)

**Fájl:** `backend/spaceos-orchestrator/src/routes/joineryRoutes.ts` (ha még nincs, hozd létre)

**Tartalom:**
```typescript
import { Router } from 'express'
import { createProxyMiddleware } from 'http-proxy-middleware'

const router = Router()

const joine ryProxy = createProxyMiddleware({
  target: 'http://localhost:5002',
  pathRewrite: { '^/api': '/joinery/api' },
  changeOrigin: true,
  logLevel: 'debug'
})

// Konfigurátor endpoints
router.post('/products/configure', joineryProxy)

// Work order endpoints
router.post('/work-orders', joineryProxy)
router.get('/work-orders/:id/sheet.pdf', joineryProxy)

export default router
```

**Register route in `src/server.ts`:**
```typescript
import joineryRoutes from './routes/joineryRoutes'

app.use('/api', joineryRoutes)
```

---

### 3. Testing (Orchestrator running)

**Manual test (curl vagy Postman):**
```bash
# Test configure endpoint (Joinery backend MOCK vagy real)
curl -X POST http://localhost:3000/api/products/configure \
  -H "Content-Type: application/json" \
  -d '{
    "productType": "standard_door",
    "dimensions": {"width": 900, "height": 2100, "thickness": 40},
    "materials": {"core": "chipboard_18mm", "veneer": "oak_natural", "edge": "pvc_oak"},
    "fittings": {"hinge": "hidden_3d", "handle": "modern_steel", "lock": "standard_cylinder"}
  }'

# Expected: 200 OK → { configId, previewUrl, estimatedPrice, bomPreview[] }
# vagy 502 Bad Gateway → Joinery backend not running (OK for now, routing verified)
```

**Log verification:**
```bash
# Orchestrator logs should show proxy call:
[DEBUG] Proxy: POST /api/products/configure → http://localhost:5002/joinery/api/products/configure
```

---

## Definition of Done

### Routing
- [ ] Joinery proxy middleware létezik (`createProxyMiddleware` to port 5002)
- [ ] `POST /api/products/configure` route registered
- [ ] `POST /api/work-orders` route registered
- [ ] Path rewrite correct (`/api` → `/joinery/api`)

### Testing
- [ ] Manual curl test végrehajtva (200 vagy 502 OK)
- [ ] Orchestrator logs show proxy debug messages
- [ ] No 404 errors (routing verified)

### Code Quality
- [ ] TypeScript build: 0 errors
- [ ] ESLint: no new warnings

---

## Dependencies

- **Joinery backend:** MSG-JOINERY-058 (párhuzamos, routing verify előbb is lehet)
- **FE frontend:** MSG-FE-087 (függ ezen routing-tól)

**Note:** Joinery backend még nincs kész → 502 Bad Gateway normális most. A fontos: **routing configured**, később Joinery DONE után 200 OK lesz.

---

## Timeline

- **Task start:** 2026-06-21 (ma)
- **Estimated effort:** 30-60 perc (quick verification)
- **Phase 1 target:** 2026-07-19 (ezen belül bármikor)

---

## Referenciák

- **Planning consensus:** `docs/planning/queue/2026-06-21_0055_consensus.md`
- **Joinery backend task:** `docs/mailbox/joinery/inbox/2026-06-21_058_joinery-e2e-configurator-endpoints.md`
- **Orchestrator:** `backend/spaceos-orchestrator/` (port 3000)

---

Kérdések esetén eszkalálj Root-nak vagy Conductor-nak.
