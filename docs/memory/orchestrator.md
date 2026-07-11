# Orchestrator Terminal Memory — Updated 2026-06-21

## RECENT WORK: MSG-ORCH-007 Joinery E2E Routing ✅ DONE

**Result:** 3 new proxy routes, 0 build errors, 121/121 tests passing
**Port:** 3000 (PM2 managed)

---

## ROUTING PATTERN

**Client → Orchestrator → Backend Proxy:**
```
POST /api/work-orders → axios.post("http://localhost:5002/joinery/api/work-orders")
```

**Implementation:**
- `src/routes/proxy.route.ts` — All backend proxy routes
- `app.use('/api', proxyRouter)` — Mount point (index.ts:60)

**Backend Services:**
- Joinery: `localhost:5002`
- Identity: `localhost:5003`
- Cutting: `localhost:5004`

---

## KEY PATTERNS

### Error Handling
- 502 Bad Gateway on backend unavailable (no info leak)
- Timeouts: POST 10s, GET 5s (configurator + PDF latency)

### Auth
- Authorization header passthrough to backend services

### Active Routes
- `/api/products/configure`, `/api/work-orders`, `/api/work-orders/:id/sheet.pdf` → Joinery
- `/api/orders/:id/material-req`, `/api/orders/:id/hardware-list` → Joinery
- `/api/cutting/plans` (GET/POST) → Cutting

---

## PM2 MANAGEMENT

```bash
sudo -u root -i pm2 restart orchestrator
```
**Always restart after build** to pick up new routes.

---

**Last Updated:** 2026-06-21
**Status:** 🟢 OPERATIONAL
**Focus:** BFF (Backend-for-Frontend) proxy + LLM Tool Calling
**Memory Tier:** Warm (14-day, stable routing patterns)

---

_This memory is compressed from 2.4KB to ~1.3KB by removing verbose deployment notes. Preserved: routing pattern, backend service ports, error handling, and PM2 management._
