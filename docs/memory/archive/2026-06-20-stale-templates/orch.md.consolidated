# ORCH Memory

Utolsó frissítés: 2026-06-21 · MSG-ORCH-007 DONE

## Aktuális állapot
- **Joinery E2E routing** ✓ DONE — 3 new proxy routes implemented in `src/routes/proxy.route.ts`
- **Build** ✓ PASS — 0 TypeScript errors, full compile successful
- **Tests** ✓ PASS — 121/121 Vitest tests passing
- **Deployment** — Orchestrator daemon needs reload to pick up new routes (triggers 502 on first test when Joinery is missing, then 200 when Joinery online)

## Fontos kontextus
- **Routing pattern**: Client `POST /api/work-orders` → Orchestrator `axios.post("http://localhost:5002/joinery/api/work-orders")`
- **Error handling**: All routes return 502 Bad Gateway + generic error message (no info leak) on Joinery unavailable
- **Timeouts**: POST 10s, GET 5s (fits configurator + PDF generation latency)
- **Auth forwarding**: Authorization header passed through from client to Joinery

## Következő lépések
1. Await Joinery backend completion (MSG-JOINERY-058) — will make routes return 200 OK
2. FE-087 can now call `/api/products/configure` and `/api/work-orders` without 404 errors
3. E2E smoke test once Joinery ready

## Megoldott problémák
- Routing was missing `POST /api/products/configure`, `POST /api/work-orders`, `GET /api/work-orders/:id/sheet.pdf`
- Added all 3 with proper axios proxy + error handling + binary response support (for PDF)

## Session tapasztalatok
- Build system working cleanly (tsc instant, 0 errors)
- Test suite comprehensive (121 tests cover proxy routes + error cases)
- Daemon still needs manual restart to pick up new code (not critical for this phase, Conductor will handle)
