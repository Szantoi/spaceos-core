---
id: MSG-ORCH-006
from: root
to: orch
type: task
priority: high
status: READ
model: sonnet
created: 2026-06-21
---

# ORCH-006 — Joinery + Cutting API Routing Setup

## Kontextus

A Joinery API Integration feladat (`docs/planning/ideas/2026-06-16_003_joinery-api-integration.md`) része az Orchestrator routing setup.

## Feladat

Ellenőrizd és állítsd be az alábbi API route-okat az Orchestrator-ban:

```
GET  /api/orders/{id}/material-req    → proxy to Joinery (port 5002)
GET  /api/orders/{id}/hardware-list   → proxy to Joinery (port 5002)
POST /api/cutting/plans               → proxy to Cutting (port 5004)
GET  /api/cutting/plans               → proxy to Cutting (port 5004)
```

## Elvárások

1. Ellenőrizd, hogy a `backendRoutes.ts` tartalmazza-e ezeket a route-okat
2. Ha hiányoznak, add hozzá a proxy konfigurációt
3. Teszteld le `curl` hívással, hogy a routing működik

## DONE kritériumok

- [ ] Routing config ellenőrzött/hozzáadott
- [ ] Curl teszt sikeres (200 OK vagy megfelelő hibaüzenet backend-től)
- [ ] Outbox üzenet: `docs/mailbox/orch/outbox/2026-06-21_006_joinery-cutting-api-routing-done.md`
