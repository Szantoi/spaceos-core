---
id: MSG-BACKEND-034
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: /opt/spaceos/docs/planning/queue/2026-06-22_2214_consensus.md
created: 2026-06-23
content_hash: 970ef38497728cdf0670287ead2ba3a404f54dd3b84bcbc5ee0073be9d0e2ce7
---

# Assembly Planning + Catalog Version Management — Backend API-k

**Konsenzus:** Hibrid megközelítés REST + cache-elt backend (Phase 1), WebSocket-ready architektúra (Phase 2 upgrade path).

## Prioritás sorrend

1. **Assembly Planning — Unified Timeline** (5-6 nap, TOP 3)
2. **Catalog Version Time-Travel** (4-5 nap, TOP 2)

---

## 1. Assembly Planning — Backend API

### Endpoint

```
GET /api/assembly/:id/timeline?merged=true
```

**Response:**
```json
{
  "events": [
    {
      "timestamp": "2026-06-22T10:00:00Z",
      "type": "plan|actual",
      "step": "cutting",
      "material": "Oak 18mm",
      "status": "ok|delay|ahead",
      "deltaMinutes": -15,
      "plannedQty": 20,
      "actualQty": 18
    }
  ],
  "summary": {
    "totalSteps": 12,
    "onTime": 8,
    "delayed": 3,
    "ahead": 1,
    "criticalPath": ["cutting", "assembly", "finishing"]
  }
}
```

### Implementáció

**Materialized view:**
```sql
CREATE MATERIALIZED VIEW assembly_timeline_cache AS
SELECT
  a.id,
  a.assembly_id,
  bom.material,
  pl.timestamp,
  pl.step,
  pl.planned_qty,
  pl.actual_qty,
  -- Delta calculations
FROM assemblies a
JOIN bom_items bom ON bom.assembly_id = a.id
JOIN production_logs pl ON pl.assembly_id = a.id;

-- Refresh every 5 minutes
CREATE UNIQUE INDEX ON assembly_timeline_cache (id);
REFRESH MATERIALIZED VIEW CONCURRENTLY assembly_timeline_cache;
```

**Cache invalidation trigger:**
```sql
CREATE TRIGGER production_log_invalidate
AFTER INSERT ON production_logs
FOR EACH ROW
EXECUTE FUNCTION refresh_assembly_timeline_cache();
```

**TTL:** 5 perc

### Phase 2 Upgrade Path (később)

```
WebSocket: /ws/assembly/:id/live-stream
SignalR/Socket.IO, auth middleware, reconnect logic
```

---

## 2. Catalog Version Management — Backend API

### Endpoint-ok

```
GET /api/catalog/:id/timeline
```

**Response:**
```json
{
  "snapshots": [
    {
      "version": "1.2.3",
      "timestamp": "2026-06-20T14:30:00Z",
      "authorId": "user-123",
      "changeCount": 5,
      "summary": "Price update, material spec change"
    }
  ],
  "keyChanges": [
    {
      "timestamp": "2026-06-20T14:30:00Z",
      "field": "price",
      "impact": "price|material|dimensions"
    }
  ]
}
```

```
GET /api/catalog/:id/version/:versionId
```

**Response:**
```json
{
  "snapshot": {
    "id": "prod-123",
    "version": "1.2.3",
    "data": { /* product state */ }
  },
  "diff": {
    "changed": ["price", "material"],
    "added": [],
    "removed": ["obsoleteField"]
  }
}
```

### Adatmodell

**Tábla: `product_versions`**

```sql
CREATE TABLE product_versions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  product_id UUID NOT NULL REFERENCES products(id),
  version VARCHAR(20) NOT NULL,
  snapshot_json JSONB NOT NULL,
  created_at TIMESTAMP DEFAULT NOW(),
  user_id UUID REFERENCES users(id),
  change_summary TEXT
);

CREATE INDEX idx_product_versions_product ON product_versions(product_id);
CREATE INDEX idx_product_versions_created ON product_versions(created_at);
```

**Migration script:**
- Retrospektív snapshot generálás meglévő product-okból
- Ha nincs `product_versions` tábla, generálj initial snapshot-ot minden product-hoz

### Library

**Backend:** `jsondiffpatch` (npm)
```bash
npm install jsondiffpatch
```

**Használat:**
```javascript
const jsondiffpatch = require('jsondiffpatch');
const delta = jsondiffpatch.diff(oldSnapshot, newSnapshot);
```

---

## Nyitott kérdések

1. **Verziókezelés meglévő state:** Van-e `product_versions` tábla? Ha nincs, mennyi retrospektív snapshot generálás engedhető meg?

2. **WebSocket infrastruktúra prioritás:** Phase 2 mikor induljon? (Javasolt: Phase 1 után 2 hetes user feedback)

3. **Production logs adatmodell:** Van-e `production_logs.assembly_id` foreign key? (Migration szükséges?)

4. **Virtual scrolling threshold:** 500+ event reális production-ben? (Ha nem, elhagyható a komplexitás)

---

## DoD

- [ ] Assembly Planning API endpoint működik
- [ ] Materialized view 5 perc TTL-lel frissül
- [ ] Cache invalidation trigger működik
- [ ] Catalog Version API endpoint-ok működnek
- [ ] `product_versions` tábla létrehozva
- [ ] `jsondiffpatch` integráció működik
- [ ] Migration script: retrospektív snapshot generálás
- [ ] API dokumentáció frissítve
- [ ] Unit teszt: cache invalidation
- [ ] Unit teszt: jsondiffpatch diff generation
- [ ] Integration teszt: E2E timeline fetch
- [ ] Integration teszt: E2E version timeline + diff

---

**Referencia:** `/opt/spaceos/docs/planning/queue/2026-06-22_2214_consensus.md`
