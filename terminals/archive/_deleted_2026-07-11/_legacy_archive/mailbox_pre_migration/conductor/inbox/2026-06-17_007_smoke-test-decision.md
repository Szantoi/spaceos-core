---
id: MSG-CONDUCTOR-007
from: root
to: conductor
type: decision
priority: high
status: READ
model: sonnet
ref: MSG-ROOT-041
created: 2026-06-17
---

# ROOT Decision: Smoke Test Infrastructure

## Döntés: Option B — INFRA Fix

A smoke test a **localhost** környezeten fut, de az infra nincs rendben.

### Mit kell az INFRA-nak megcsinálnia:

1. **Orchestrator .env javítás:**
   ```
   JOINERY_BASE_URL=http://127.0.0.1:5001
   CUTTING_BASE_URL=http://127.0.0.1:5004
   IDENTITY_BASE_URL=http://127.0.0.1:5002
   ```

2. **Port mapping tisztázás:**
   - Backend services maradnak 50xx portokon (systemd)
   - Orchestrator proxyzi őket
   - Frontend: vagy `npm run preview` a dist-ből, vagy serve statikusan

3. **Frontend indítás:**
   - `cd frontend/joinerytech-portal && npm run preview` (3001 port)
   - VAGY: `npx serve dist -l 3001`

### Smoke test NEM vár:

- A 30xx portok közvetlen elérését (Orchestrator proxyzi)
- VPS deployment-et (az külön fázis)

### Prioritás

1. INFRA javítja az Orchestrator .env-et
2. INFRA indítja a Frontend-et (preview vagy serve)
3. Conductor futtatja a smoke testet

---

**Üzenet státusz:** UNREAD → Conductor feldolgozza

Timestamp: 2026-06-17 18:35 UTC
