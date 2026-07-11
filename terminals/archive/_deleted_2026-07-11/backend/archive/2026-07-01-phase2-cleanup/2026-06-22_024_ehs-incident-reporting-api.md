---
id: MSG-BACKEND-024
from: conductor
to: backend
type: task
priority: high
status: READ
model: sonnet
ref: /opt/spaceos/docs/planning/queue/2026-06-22_1620_consensus.md
created: 2026-06-22
content_hash: c528f6cdba8db61a6b2b876c35dc00d43cf2acf590c2e6682b935acdea268d17
---

# EHS Incident Reporting API — Sprint 1 Backend

## Context

**Planning Consensus:** 2026-06-22_1620 (EHS Baleset-Bejelentés prioritás #1)

SpaceOS első compliance-kritikus feature: munkahelyi balesetek/near-miss incidensek jelentése mobil eszközökről. Event sourcing pattern, offline-first support, append-only audit trail.

**Üzleti érték:**
- Compliance (OSHA/EU direktívák)
- Audit trail teljes története
- Offline működés (gyárudvar nincs WiFi)

## API Endpoint Spec

### POST /api/ehs/events

**Request:**
```json
{
  "type": "INCIDENT_REPORTED",
  "payload": {
    "reporterId": "uuid",
    "incidentType": "near-miss|injury|property",
    "locationId": "workshop-zone-id",
    "timestamp": "ISO8601",
    "photoS3Key": "string?",
    "description": "string"
  },
  "meta": {
    "deviceId": "string",
    "clientTimestamp": "ISO8601"
  }
}
```

**Response (201 Created):**
```json
{
  "eventId": "uuid",
  "sequence": 42,
  "status": "accepted",
  "serverTimestamp": "ISO8601"
}
```

**Idempotency:** Client-side UUID v4 `eventId` → duplicate POST = 200 OK (nem 201)

## Data Model

### ehs_events (immutable, append-only)
```sql
CREATE TABLE ehs_events (
  event_id UUID PRIMARY KEY,
  sequence BIGSERIAL,
  type VARCHAR(50) NOT NULL,
  payload JSONB NOT NULL,
  meta JSONB,
  created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  tenant_id UUID NOT NULL
);
CREATE INDEX idx_ehs_events_sequence ON ehs_events(sequence);
CREATE INDEX idx_ehs_events_created_at ON ehs_events(created_at DESC);
```

### ehs_incident_snapshots (materialized view)
```sql
CREATE MATERIALIZED VIEW ehs_incident_snapshots AS
SELECT
  (payload->>'reporterId')::UUID as reporter_id,
  payload->>'incidentType' as incident_type,
  payload->>'locationId' as location_id,
  (payload->>'timestamp')::TIMESTAMPTZ as incident_timestamp,
  payload->>'photoS3Key' as photo_s3_key,
  payload->>'description' as description,
  created_at,
  tenant_id
FROM ehs_events
WHERE type = 'INCIDENT_REPORTED';
```

**Refresh trigger:** After each INSERT (vagy 5 perc cron ha performance issue)

## Validation Rules

| Rule | Validation | HTTP Status |
|------|-----------|-------------|
| Reporter exists | `SELECT 1 FROM users WHERE id = reporterId` | 400 "Reporter not found" |
| Timestamp drift | `abs(clientTimestamp - serverTime) < 2h` | 400 "Clock skew too large" |
| Photo size | `photoS3Key` file < 5MB (S3 metadata check) | 413 "File too large" |
| Idempotency | `eventId` already exists → return existing event | 200 OK |
| Incident type | ENUM validation | 400 "Invalid incident type" |

## Photo Upload Flow

**IMPORTANT:** Ne base64-ben küldje a frontend!

1. Frontend → `POST /api/ehs/photos/presigned-url` (photo metadata: filename, size, mime)
2. Backend → validate (5MB max, mime `image/jpeg|png`) → generate S3 presigned URL (15 min TTL)
3. Frontend → Direct upload to S3 via presigned URL (client-side compression 800px max width)
4. Frontend → `POST /api/ehs/events` (photoS3Key referencia)
5. Backend → **EXIF strip on S3 trigger** (Lambda vagy background worker) → replace original

**EXIF Strip kötelező:** Privacy (GPS location, device metadata eltávolítása)

### Backend endpoint: POST /api/ehs/photos/presigned-url

**Request:**
```json
{
  "filename": "incident_20260622.jpg",
  "size": 2048576,
  "mime": "image/jpeg"
}
```

**Response:**
```json
{
  "uploadUrl": "https://s3.amazonaws.com/...",
  "s3Key": "ehs/photos/uuid-timestamp.jpg",
  "expiresAt": "ISO8601"
}
```

## Testing Requirements

### Unit Tests
- ✅ Event insertion (valid payload)
- ✅ Idempotency (duplicate eventId)
- ✅ Validation failures (invalid reporterId, timestamp drift, photo size)
- ✅ EXIF strip (verify metadata removed after S3 upload)

### Integration Tests
- ✅ POST /api/ehs/events → 201 Created
- ✅ POST duplicate eventId → 200 OK (same response)
- ✅ POST invalid reporterId → 400 Bad Request
- ✅ Materialized view refresh (INCIDENT_REPORTED visible in snapshot)
- ✅ Photo presigned URL generation + upload flow

### E2E Tests (optional Sprint 1, required Sprint 2)
- ✅ Mobile offline → online sync (localStorage queue)
- ✅ Photo upload + EXIF strip end-to-end

## Migration

**Filename:** `NNNN_create_ehs_events.sql`

```sql
-- events table
CREATE TABLE ehs_events (...);
-- indexes
CREATE INDEX ...
-- materialized view
CREATE MATERIALIZED VIEW ehs_incident_snapshots AS ...
-- refresh trigger or cron setup
```

## Dependencies

- **S3 bucket:** `spaceos-ehs-photos-{env}` (encryption at rest)
- **IAM role:** Presigned URL generation permission
- **Lambda/Worker:** EXIF strip (optional Sprint 1, deploy Sprint 2 előtt)
- **Users table:** Reporter ID foreign key (már létezik Identity modulban)

## Open Questions (Conductor válasz kell)

1. **GDPR right-to-forget:** Ha user törlés → event retention policy? (Anonymizálás vagy törlés?)
2. **S3 bucket retention:** 7 év compliance vagy rövidebb?
3. **Offline conflict resolution:** Ha ugyanazt az incidentet 2 eszközről küldik (pl. WiFi visszajön), mi a UX? (UUID alapján automatikus dedup elég?)
4. **Photo storage compliance:** S3 bucket encryption + audit log ready?

## Definition of Done

- ✅ POST /api/ehs/events endpoint implementálva
- ✅ POST /api/ehs/photos/presigned-url endpoint implementálva
- ✅ ehs_events tábla + migration
- ✅ ehs_incident_snapshots materialized view
- ✅ Validation rules (reporter, timestamp, photo size)
- ✅ Idempotency handling
- ✅ Unit tests (8+) + Integration tests (5+)
- ✅ OpenAPI spec frissítve
- ✅ EXIF strip spike (4h) → Lambda deploy vagy background worker decision
- ✅ Test coverage ≥90% (új kód)

## Estimated Effort

**Backend:** 4-6 dev days (1 developer)
- Day 1: Event sourcing pattern + ehs_events tábla + migration
- Day 2: POST /api/ehs/events endpoint + validation
- Day 3: Photo presigned URL endpoint + S3 integration
- Day 4: EXIF strip spike (Lambda vs. worker)
- Day 5-6: Testing + OpenAPI spec + DoD checklist

## Next Steps (Sprint Planning)

**Sprint 1 scope:**
- Backend API ready (POST events, presigned URL)
- Frontend wizard (MSG-FRONTEND-016)
- EXIF strip spike → decision Sprint 2 előtt

**Sprint 2 scope:**
- EXIF strip deploy
- E2E testing
- Offline sync polish

---

**Conductor note:** Ha Backend kész (MSG-024 DONE), Frontend MSG-016 unblocked. Parallel development lehetséges mock API-val.
