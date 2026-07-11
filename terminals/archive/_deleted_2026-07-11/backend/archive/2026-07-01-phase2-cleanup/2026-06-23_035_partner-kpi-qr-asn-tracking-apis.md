---
id: MSG-BACKEND-035
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: /opt/spaceos/docs/planning/queue/2026-06-22_0037_consensus.md
created: 2026-06-23
content_hash: 43a65fa46c46b438f672a2cfb56d4354198e79f9f50abb691056699176dadb5e
---

# Partner KPI Analytics + QR ASN Tracking API-k

## Összefoglalás

Implementáld a Partner KPI Analytics és QR ASN Tracking backend API-kat a JoineryTech platformra.

**Becslés:** 4 nap (2 nap KPI analytics, 2 nap QR production API-k)
**Blokkolt:** Frontend MSG-022 Week 1-2 (mock) után indítható (Week 3)
**Scope:** Analytics endpoint + QR ASN API-k hash validációval

## Backend API-k

### 1. Partner KPI Analytics (2 nap)

**Endpoint:**
```
GET /api/analytics/partners/:id/kpi?period=30d

Response:
{
  onTimeDelivery: {
    value: 0.87,
    trend: -0.05,
    missing_data_count: 23
  },
  avgLeadTime: {
    days: 12,
    trend: -2
  },
  qualityRate: {
    value: 0.94,
    data_completeness: 0.77
  }
}
```

**Implementáció:**
- Query optimization (PostgreSQL aggregation)
- `missing_data_count` és `data_completeness` számítás
- Cache layer (Redis vagy in-memory, 5 perc TTL)
- Trend calculation (összehasonlítás előző periódussal)

### 2. QR ASN Tracking API-k (2 nap)

**2.1 ASN Generate**
```
POST /api/suppliers/asn/generate

Request:
{
  poId: "PO-12345",
  items: [
    { sku: "DOOR-001", quantity: 20 }
  ],
  expectedDate: "2026-06-25"
}

Response:
{
  asn: "ASN-2026-0622-001",
  qrPayload: "ASN|PO-12345|2026-06-25|sha256_hash",
  printableUrl: "/print/asn/ASN-2026-0622-001"
}
```

**2.2 Receipt Scan**
```
POST /api/inbound/receipt/scan

Request:
{
  qrPayload: "ASN|PO-12345|2026-06-25|sha256_hash",
  scannedBy: "USER-123",
  actualQuantity: 20
}

Response:
{
  valid: true,
  po: { id: "PO-12345", ... },
  hashVerified: true,
  nextAction: "QUANTITY_CONFIRM"
}
```

**Hash validation:**
```javascript
// Server-side
const hash = crypto
  .createHmac('sha256', process.env.ASN_SECRET)
  .update(`${asn}|${poId}|${expectedDate}`)
  .digest('hex');

const qrPayload = `${asn}|${poId}|${expectedDate}|${hash}`;
```

## Adatmodell (PostgreSQL vagy megfelelő DB)

### ASN tábla
```sql
CREATE TABLE asn_shipments (
  id UUID PRIMARY KEY,
  asn_number VARCHAR(50) UNIQUE NOT NULL,
  po_id UUID REFERENCES purchase_orders(id),
  supplier_id UUID REFERENCES suppliers(id),
  expected_date DATE NOT NULL,
  qr_payload TEXT NOT NULL,
  status VARCHAR(20) NOT NULL, -- SHIPPED, RECEIVED, PENDING_SYNC
  offline_scanned_at TIMESTAMP,
  created_at TIMESTAMP DEFAULT NOW(),
  updated_at TIMESTAMP DEFAULT NOW()
);

CREATE INDEX idx_asn_po ON asn_shipments(po_id);
CREATE INDEX idx_asn_status ON asn_shipments(status);
```

### Receipt tábla (offline sync queue)
```sql
CREATE TABLE receipt_queue (
  id UUID PRIMARY KEY,
  asn_id UUID REFERENCES asn_shipments(id),
  scanned_by UUID REFERENCES users(id),
  actual_quantity INT NOT NULL,
  scanned_at TIMESTAMP NOT NULL,
  synced_at TIMESTAMP,
  status VARCHAR(20) DEFAULT 'PENDING', -- PENDING, SYNCED, FAILED
  created_at TIMESTAMP DEFAULT NOW()
);
```

## Nyitott kérdések (válaszolj BLOCKED-ként ha blokkolnak)

**1. Backend kapacitás:** Van-e dedikált backend resource Week 3-ra, vagy Phase 2 csúszik? (Mock addig is működik) — ütemezd be vagy jelezd ha blokkolva vagy

**3. QR SECRET kezelés:** Server-side SECRET rotációs policy szükséges? (Security best practice, de +0.5 nap) — implementálj alapvető SECRET env var-ral, rotációt TODO-ba teheted

**6. Offline sync konfliktus:** Mi történik ha két worker ugyanazt az ASN-t szkenneli offline? (Last-write-wins vs merge strategy) — válassz pragmatikus megoldást vagy kérdezz vissza

## Security követelmények

- ASN_SECRET környezeti változóban (`.env` + production secrets manager)
- Hash validation minden `/receipt/scan` hívásnál
- Rate limiting ASN generálásnál (10 req/min/user)
- Audit log minden ASN generate + receipt scan művelethez

## Performance követelmények

- KPI endpoint max 500ms response time (cache-elt)
- ASN generate max 200ms
- Receipt scan max 300ms (hash validation + DB lookup)

## Definition of Done

- [ ] KPI Analytics endpoint működik, cache réteg kész
- [ ] ASN Generate API elkészült, hash generation logic
- [ ] Receipt Scan API elkészült, hash validation
- [ ] PostgreSQL séma migráció kész (asn_shipments, receipt_queue)
- [ ] Unit tesztek (hash validation, cache logic)
- [ ] Integration tesztek (API endpoints)
- [ ] Security review (SECRET handling, rate limiting)
- [ ] API dokumentáció (Swagger/OpenAPI vagy README)

## Kapcsolódó dokumentumok

- Consensus terv: `/opt/spaceos/docs/planning/queue/2026-06-22_0037_consensus.md`
- Frontend task: `terminals/frontend/inbox/2026-06-23_022_*` (Week 1-2 mock előfutár)

## Blokkoló dependenciák

**Indítható:** Frontend Week 1-2 (mock) után (Week 3)
**Ha hamarabb indítanád:** Koordinálj Frontend-del a mock API contract egyeztetésére
