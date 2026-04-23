---
id: MSG-PROCUREMENT-006
from: root
to: procurement
type: task
priority: medium
status: READ
ref: MSG-ORCH-077-DONE
created: 2026-04-17
---

# PROCUREMENT-006 — POST /api/procurement/suppliers endpoint

## Kontextus

Az ORCH-077 `doorstar-cutting-ready-v1` seed profil `suppliers: 0`-t ad vissza, mert a Procurement v1-ben nincs `POST /api/procurement/suppliers` HTTP endpoint. A Supplier aggregát létezik a domain-ben, de csak az adatbázisban elérhető.

Az E2E-L2 Flow 07 (procurement.spec.ts) Supplier listát fog tesztelni — ehhez kell legalább 1 seeded Supplier.

## Feladat

### 1. `POST /api/procurement/suppliers` endpoint

**Request:**
```json
{
  "name": "string",
  "contactEmail": "string (optional)",
  "notes": "string (optional)"
}
```

**Response 201:**
```json
{
  "id": "uuid",
  "name": "Faanyag Kft.",
  "tenantId": "uuid",
  "createdAt": "ISO8601"
}
```

**Security:**
- JWT kötelező (`[Authorize]`)
- TenantSessionInterceptor lefuttatja a GUC-ot
- A létrehozott Supplier a JWT-ből vett tenantId-hoz tartozik

### 2. `GET /api/procurement/suppliers` endpoint (ha még nincs)

Ha a lista endpoint sem létezik, add hozzá:
- JWT kötelező
- RLS-en keresztül csak a saját tenant suppliereit adja vissza
- Pagination opcionális (egyszerű lista is elég)

## DoD

- [ ] `dotnet build` → 0 error
- [ ] `dotnet test` → 48+ zöld (regresszió nincs, új tesztek a Supplier CRUD-ra)
- [ ] `POST /api/procurement/suppliers` → 201 + `{ id, name, tenantId }`
- [ ] `GET /api/procurement/suppliers` → 200 + lista
- [ ] git commit + push (develop)

## Outbox

DONE: `mailbox/procurement/outbox/2026-04-17_006_supplier-create-endpoint-done.md`

## Skillek & Agentек

- `/senior-backend` — Minimal API endpoint, EF Core, RLS
- Sub-agenteket nyugodtan indíts
