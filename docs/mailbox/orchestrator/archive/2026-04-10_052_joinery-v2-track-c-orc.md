---
id: MSG-ORCHESTRATOR-052
from: root
to: orchestrator
type: task
priority: high
status: READ
created: 2026-04-10
ref: docs/archive/SpaceOS_Joinery_v2_Claude_Code_Package.md
---

# MSG-ORCHESTRATOR-052: Modules.Joinery v2 — Track C-Orc

## Kontextus

A Joinery terminál befejezte Track A + B + D-t. Az Orchestrator BFF-ben kell az internal route guard implementálása, amelyen keresztül a Joinery a kalkuláció eredményét visszaküldi.

---

## Track C-Orc — /internal/* prefix + X-SpaceOS-Internal guard

### Feladat

**1. `/internal/*` prefix az összes internal route-ra**

Az Orchestrator csak belső (service-to-service) hívásokat fogad ezen az útvonalon. Példa:
```
PUT /bff/internal/joinery/results  →  Joinery SaveCalculationResult webhook
```

**2. X-SpaceOS-Internal header guard (SEC-01)**

Middleware vagy route-szintű ellenőrzés:
- Kérés tartalmaz-e `X-SpaceOS-Internal: true` headert
- Ha nem → 403 Forbidden
- Ha igen → átengedi a kérést a megfelelő Kernel proxy route-ra

**3. Proxy route a kalkuláció eredményéhez**

```
PUT /bff/internal/joinery/results
  → proxy_pass: Joinery API PUT /internal/results
  → X-SpaceOS-Internal header forward
```

**Elvárások:**
- A guard NEM vonatkozik a publikus `/bff/api/*` route-okra
- A guard loggol minden 403-as elutasítást (audit)
- TypeScript típusok frissítve

**Blokkoló gate:** SEC-01 — ez a guard nélkül a Joinery outbox nem tud visszaírni

---

## Tesztek

- ≥4 új teszt: guard 403 (hiányzó header), guard pass (helyes header), proxy route működik, audit log
- Meglévő baseline (163 teszt) marad zöld

## Kötelező

```bash
npm test && npm run build
```

## Válasz

Outbox üzenet: `docs/mailbox/orchestrator/outbox/2026-04-10_052_joinery-v2-track-c-orc-done.md`
