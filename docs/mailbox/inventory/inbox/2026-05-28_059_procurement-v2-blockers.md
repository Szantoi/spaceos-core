---
id: MSG-INVENTORY-059
from: root
to: inventory
type: task
priority: high
status: READ
ref: PROCUREMENT-V2
created: 2026-05-28
---

# Procurement v2 blokkolók — Inventory oldalon

A Procurement v2 implementáció két Inventory-oldali endpointot igényel.
Ezek **párhuzamosan indíthatók** a Procurement Track A–B mellé.

Spec: `/opt/spaceos/docs/tasks/active/SpaceOS_Modules_Procurement_v2_Architecture_v4.md` — §9 és §5 (ADR-039 függelék)

---

## 1. feladat — `POST /inventory/internal/inbound` receiver (Bearer-auth)

Az endpoint fogadja a Procurement `RecordDelivery` outbox-üzenetét, amikor szállítás érkezik.

**Elvárt viselkedés:**
- Auth: `Authorization: Bearer {SPACEOS_INTERNAL_SECRET}` — constant-time compare, feldolgozás előtt; hiány/rossz → 401
- Header: `X-SpaceOS-TenantId: {guid}` — egyezés a body `TenantId`-val (strict equal), eltérés → 403
- Idempotency: compound key `(TenantId, DeliveryLineId)` — duplikátum → 200 (nem 409, nem 5xx)
- Loopback only: `IPAddress.IsLoopback` assert a middleware-ben
- Egy DB-tx: inbox INSERT + inventory mutáció + audit egy tranzakcióban

**Request body:**
```json
{
  "tenantId": "uuid",
  "deliveryLineId": "uuid",
  "materialCode": "string",
  "quantity": 0.0,
  "unitOfMeasure": "string",
  "supplierId": "uuid",
  "receivedAt": "ISO8601"
}
```

**Válasz:**
- `200 OK` — sikeres feldolgozás (és duplikátum)
- `401` — hiányzó / rossz secret
- `403` — TenantId mismatch
- `422` — orphan materialCode/supplierId (payload validáció)
- `503` — tranziens DB hiba (Procurement worker retry-zik)

**Precedens:** `POST /joinery/internal/orders/from-quote` (Joinery J-003) — Bearer-auth + idempotency compound key + egy-tx + Polly

---

## 2. feladat — `reorder_alert` outbox + worker (ReorderAlert → Procurement irány)

Amikor az Inventory raktárkészlete reorder-szint alá esik, értesíti a Procurement modult.

**Elvárt viselkedés:**
- Trigger: `StockLevel ≤ ReorderPoint` → domain event → outbox INSERT (egy-tx a készlet-mutációval)
- Outbox worker: `POST http://127.0.0.1:5006/procurement/internal/from-reorder-alert`
  - Header: `Authorization: Bearer {SPACEOS_INTERNAL_SECRET}`
  - Header: `X-SpaceOS-TenantId: {tenantId}`
- Polly retry policy: csak tranziens hibákra (5xx / timeout / HttpRequestException); permanens 4xx → azonnal `Failed`
- Circuit-breaker: ha Procurement tartósan hibázik
- Worker: BYPASSRLS connection string, per-message `set_config('app.current_tenant_id', ...)`, tenant DiD assert

**Outbox payload:**
```json
{
  "tenantId": "uuid",
  "materialCode": "string",
  "currentStock": 0.0,
  "reorderPoint": 0.0,
  "suggestedQuantity": 0.0,
  "preferredSupplierId": "uuid | null",
  "unitOfMeasure": "string",
  "alertedAt": "ISO8601"
}
```

**Precedens:** Sales `SalesIntegrationWorker` (outbox + Polly + BYPASSRLS + set_config minta)

---

## DoD

- [ ] `POST /inventory/internal/inbound` — Bearer-auth, idempotency, egy-tx, loopback assert
- [ ] `reorder_alert` outbox tábla + worker (Polly transient-only + circuit-breaker)
- [ ] Minden meglévő teszt zöld (164 Inventory teszt érintetlen)
- [ ] Új tesztek: ≥ 12 db (4 receiver auth/idempotency + 4 outbox/worker + 4 integration)
- [ ] `dotnet build` → 0 error, 0 warning
- [ ] OUTBOX üzenet: `MSG-INVENTORY-059-DONE`

---

## Skill-ek

`/spaceos-terminal` — inbox/outbox protokoll
`/senior-backend` — implementation guidance
`/ef-core` — EF Core patterns

## Fontos

A Procurement terminál Track D (outbox worker) és Track E (receiver) blokkolva van amíg ez a feladat nincs kész. Ha bármi kérdés → BLOCKED outbox üzenet.
