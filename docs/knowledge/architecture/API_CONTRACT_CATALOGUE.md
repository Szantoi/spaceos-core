# SpaceOS — API Contract Catalogue

> Összes ismert endpoint. Forrás: Codebase_Status.md + mailbox archive üzenetek.
> **Auth követelmény** (ha nem jelzett): JWT Bearer token kötelező.

---

## Kernel API (port 5000, loopback-only)

### Auth

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| POST | `/api/auth/logout` | Bearer | Refresh token revoke (idempotens) |
| GET | `/.well-known/jwks.json` | Anonymous | ES256 public key (OutputCache 1h) |

### Tenants

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET | `/api/tenants` | Bearer | Tenant lista |
| POST | `/api/tenants` | Bearer | Tenant létrehozás |
| GET | `/api/tenants/:id` | Bearer | Egy tenant |
| PUT | `/api/tenants/:id/brand-skin` | Bearer | Brand skin módosítás |
| GET | `/api/tools/summary` | Bearer | Tenant summary (flowEpicCount, workstationCount) |
| GET | `/api/tools/tenant-id` | Bearer | Kurrent tenant ID a JWT-ből |

### Facilities

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET | `/api/facilities` | Bearer | Lista (RLS szűrt) |
| POST | `/api/facilities` | Bearer | Létrehozás |
| GET | `/api/facilities/:id` | Bearer | Egy facility |
| PUT | `/api/facilities/:id` | Bearer | Módosítás |
| DELETE | `/api/facilities/:id` | Bearer | Törlés |
| GET | `/api/facilities/:id/flow-epics` | Bearer | Facility flow epic-jei |
| GET | `/api/facilities/:id/work-stations` | Bearer | Workstation-ök |
| GET | `/api/facilities/:id/space-layers` | Bearer | Space layer-ek |

### FlowEpics

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET | `/api/flow-epics` | Bearer | Lista |
| POST | `/api/facilities/:id/flow-epics` | Bearer | Létrehozás facility-hez |
| GET | `/api/flow-epics/:id` | Bearer | Egy FlowEpic |
| PUT | `/api/flow-epics/:id/assign-chain` | Bearer | Stage chain hozzárendelés |
| PUT | `/api/flow-epics/:id/advance-stage` | Bearer | Stage előrehaladás |
| PUT | `/api/flow-epics/:id/close` | Bearer | FSM close (→ ClosedDone) |
| DELETE | `/internal/flow-epics/by-tenant` | Internal | Tenant adatok törlése (GDPR) |

### Stage Registry

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET | `/api/stages` | ReadPolicy | Stage definíciók listája |
| POST | `/api/stages` | SystemAdminPolicy | Stage definíció regisztráció |
| GET | `/api/stages/:id` | ReadPolicy | Egy stage |
| PUT | `/api/stages/:id/endpoint` | TenantAdminPolicy | Endpoint frissítés |
| DELETE | `/api/stages/:id` | SystemAdminPolicy | Deaktiválás |
| GET | `/api/stage-chains` | ReadPolicy | Chain template-ek |
| POST | `/api/stage-chains` | TenantAdminPolicy | Chain template létrehozás |
| POST | `/api/stage-chains/:id/steps` | TenantAdminPolicy | Lépés hozzáadás |
| DELETE | `/api/stage-chains/:id/steps/:stepId` | TenantAdminPolicy | Lépés eltávolítás |
| GET | `/api/stage-handoffs/:id` | ReadPolicy | Handoff lekérés |
| POST | `/api/stage-dispatch` | StageOperatorPolicy | Stage dispatch (handoff létrehozás) |

### Workstations & SpaceLayers

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET | `/api/workstations` | Bearer | Lista |
| POST | `/api/workstations` | Bearer | Létrehozás |
| PUT | `/api/workstations/:id/activate` | Bearer | Aktiváció |
| GET | `/api/space-layers` | Bearer | Lista |
| POST | `/api/space-layers` | Bearer | Létrehozás |

### Spatial BIM

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| POST | `/api/spaces` | Bearer | Physical Space regisztrálás |
| GET | `/api/spaces/:id` | Bearer | Space lekérés |
| POST | `/api/spaces/:id/elements` | Bearer | Spatial element hozzáadás |
| POST | `/api/elements/:id/links` | Bearer | Element link |
| GET | `/api/spaces/:id/timeline` | Bearer | 4D snapshot |
| GET | `/api/spaces/:id/timeline/events` | Bearer | Timeline események |

### Nodes & Sync

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| POST | `/api/nodes/register` | Bearer | Node regisztráció |
| GET | `/api/nodes` | Bearer | Node lista |
| POST | `/api/sync/signal` | Bearer | Sync signal (SIP header required) |
| GET | `/api/sync/status` | Bearer | Sync státusz |

### Audit

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET | `/api/audit-events` | Bearer | Audit log lista |
| POST | `/api/audit-events/verify-chain` | Bearer | Hash chain validálás |
| POST | `/api/audit-events/rehash` | Bearer | Rehash (admin) |

### Health

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET | `/healthz` | Anonymous | `{"status":"healthy","db":"connected"}` |
| GET | `/health/ready` | Anonymous | JwksHealthCheck + DB |

---

## Orchestrator BFF API (port 3000)

### Auth

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET | `/bff/auth/me` | Bearer | `{tenantId, tenants[], activeTenantId, roles, brandSkin}` |

### Kernel proxy `/bff/api/*` → `127.0.0.1:5000/api/*`

Minden Kernel `/api/*` endpoint elérhető `/bff/api/*` prefix-szel.
`X-SpaceOS-Brand` header automatikusan forwarded a domain alapján.

**Fontos BFF-specifikus route-ok:**

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET/POST/... | `/bff/api/*` | Bearer | Kernel proxy (általános) |
| GET/POST/... | `/bff/nodes/*` | Bearer | Kernel `/api/nodes/*` (SIP header) |
| GET/POST/... | `/bff/sync/*` | Bearer | Kernel `/api/sync/*` |
| GET/POST/... | `/bff/layers/*` | Bearer | Kernel `/api/layers/*` |
| GET/POST/... | `/bff/audit-events/*` | Bearer | Kernel `/api/audit-events/*` |

### Spatial

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET/POST/... | `/bff/api/spaces/*` | Bearer | Kernel Spatial BIM proxy |

### Chat

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| POST | `/bff/chat` | Bearer + chatLimiter | LLM chat (Gemini 2.0 Flash) |
| GET | `/bff/chat/stream` | Bearer | SSE chat stream |

### Stage dispatch

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| POST | `/bff/stages/dispatch` | Bearer | Stage dispatch proxy |

### B2B Handshakes

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET/POST/PUT | `/bff/handshakes/*` | Bearer | B2B handshake lifecycle |

### Internal (service-to-service)

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| PUT | `/bff/internal/joinery/results` | X-SpaceOS-Internal | Kalkuláció eredmény callback |

### Joinery

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET/POST/... | `/bff/joinery/*` | Bearer | Joinery module proxy → 127.0.0.1:5002 |
| GET/POST/... | `/bff/joinery/orders/*` | Bearer | Door orders |
| GET/POST/... | `/bff/joinery/door-orders/*` | Bearer | Door order lifecycle |

### Abstractions

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET/POST/... | `/bff/abstractions/*` | Bearer | Abstractions module pass-through proxy → 127.0.0.1:5003/api/* |

### Cutting & Inventory

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET/POST/... | `/bff/cutting/*` | Bearer | Cutting module proxy → 127.0.0.1:5005 |
| GET/POST/... | `/bff/inventory/*` | Bearer | Inventory module proxy → 127.0.0.1:5004 |

### Health

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET | `/bff/health` | Anonymous | BFF health |

---

## Joinery Module API (port 5002)

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET | `/health` | Anonymous | `{"status":"healthy","service":"spaceos-joinery"}` |
| GET | `/api/orders` | Bearer | Door order lista |
| POST | `/api/orders` | Bearer | Door order létrehozás |
| GET | `/api/orders/:id` | Bearer | Egy order |
| POST | `/api/orders/:id/submit` | Bearer | Order véglegesítés |
| GET | `/api/orders/:id/sheet` | Bearer | Gyártási lap PDF (SEC-05: nosniff, no-store) |
| GET | `/api/orders/:id/snapshots` | Bearer | Cutting list snapshot-ok |
| PUT | `/api/orders/:id/revert` | Bearer | Revert to draft |
| PUT | `/internal/results` | X-SpaceOS-Internal | Kalkuláció eredmény callback |

---

## Abstractions Module API (port 5003)

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET | `/health` | Anonymous | `{"status":"healthy","service":"spaceos-abstractions"}` |
| POST | `/api/modules/templates` | ManufacturerOnly | ProductTemplate létrehozás |
| GET | `/api/modules/templates` | ManufacturerOnly | Template lista |
| GET | `/api/modules/templates/:name` | ManufacturerOnly | Egy template |
| POST | `/api/modules/templates/:id/slots` | ManufacturerOnly | Slot hozzáadás |
| POST | `/api/modules/templates/:id/connections` | ManufacturerOnly | Connection hozzáadás |
| POST | `/api/modules/templates/:name/calculate` | ManufacturerOnly | Kalkuláció futtatás |
| GET | `/api/modules/templates/:name/cutting-list` | ManufacturerOnly | Vágólista |
| GET | `/api/modules/templates/:name/cnc-plan` | ManufacturerOnly | CNC terv |
| GET | `/api/modules/templates/:name/process-plan` | ManufacturerOnly | Gyártási folyamat |

---

## Inventory Module API (port 5004)

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET | `/health` | Anonymous | Health |
| GET | `/api/inventory/stock` | Bearer | Készlet lekérés (?materialType=) |
| GET | `/api/inventory/offcuts` | Bearer | Hulladék lista |
| POST | `/api/inventory/movements/consumption` | Bearer | Felhasználás rögzítés |
| POST | `/api/inventory/movements/inbound` | Bearer | Beérkező áru |
| POST | `/api/inventory/movements/offcut` | Bearer | Hulladék rögzítés |
| GET | `/api/inventory/trend` | Bearer | Felhasználási trend (?from=&to=) |

---

## Cutting Module API (port 5005)

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET | `/health` | Anonymous | Health |
| POST | `/api/cutting/sheets` | Bearer | Cutting sheet létrehozás |
| GET | `/api/cutting/sheets` | Bearer | Sheet lista |
| GET | `/api/cutting/sheets/:id` | Bearer | Egy sheet |
| POST | `/api/cutting/sheets/:id/submit` | Bearer | Sheet submit → nesting |
| GET | `/api/cutting/sheets/:id/nesting` | Bearer | Nesting eredmény |
| DELETE | `/internal/cutting/by-tenant` | Internal | Tenant adatok törlése |

---

## Procurement Module API (port 5006)

| Method | Path | Auth | Leírás |
|--------|------|------|--------|
| GET | `/health` | Anonymous | Health |
| GET | `/api/procurement/items` | Bearer | Procurement item lista |
| POST | `/api/procurement/items` | Bearer | Item létrehozás |
| PUT | `/api/procurement/items/:id/order` | Bearer | Megrendelés |

---

## Közös auth konvenciók

- **Bearer token:** `Authorization: Bearer <JWT>` header kötelező
- **X-SpaceOS-Brand:** Nginx-ből forwarded, domain alapján (`joinerytech` / `asztalostech`)
- **X-SpaceOS-Active-Tenant:** Opcionális tenant override (GUID) — validálva a Kernel által
- **X-SpaceOS-Internal:** Service-to-service jelölő (`true`) — belső endpointokhoz
- **SIP header:** Node sync endpointokhoz szükséges (séma verzió)
