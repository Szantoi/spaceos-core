---
id: MSG-BACKEND-024-DONE
from: backend
to: conductor
type: done
status: READ
ref: MSG-BACKEND-024
created: 2026-06-22
content_hash: 18afc240937138f8eec75b0821084bf70fcb85f2204f1b468a69cf6560db0f72
---

# EHS Incident Reporting API — Sprint 1 Backend DONE

## Összefoglaló

Teljes **EHS (Environment, Health, Safety) Incident Reporting API** implementálva event sourcing pattern-nel:

### Implementált modulok

1. **Domain layer** (Ehs.Domain)
   - `EhsEvent` aggregate (immutable event sourcing)
   - Value objects: `EventId`, `IncidentType`, `PhotoS3Key`, `ReporterId`
   - Interfaces: `IEhsEventRepository`, `IS3Service`

2. **Application layer** (Ehs.Application)
   - `ReportIncidentCommand` + Handler + Validator
   - `GeneratePresignedUrlQuery` + Handler + Validator
   - DTOs: `IncidentPayload`, `EventMeta`, `EventResponse`, `PresignedUrlResponse`
   - `ICurrentUserContext` interface

3. **Infrastructure layer** (Ehs.Infrastructure)
   - `EhsDbContext` - EF Core DbContext
   - `EhsEventRepository` - Repository implementation
   - `S3Service` - AWS S3 presigned URL generation
   - `CurrentUserContextService` - JWT claims-based user context
   - DI extension methods

4. **API layer** (Ehs.Api)
   - `EventsController` - POST /api/ehs/events (incident reporting)
   - `PhotosController` - POST /api/ehs/photos/presigned-url (S3 photo upload)
   - Program.cs - API startup configuration
   - appsettings.json - configuration template

### API Endpoints

**POST /api/ehs/events**
- Incident event reporting (event sourcing)
- Idempotency: client-generated UUID (duplicate POST = 200 OK, not 201)
- Validation: reporter ID, timestamp drift (max 2h), photo S3 key
- Response: `{ eventId, sequence, status, serverTimestamp }`

**POST /api/ehs/photos/presigned-url**
- S3 presigned URL generation (15 min TTL)
- Validation: file size (max 5MB), MIME type (image/jpeg, image/png)
- Response: `{ uploadUrl, s3Key, expiresAt }`

### Database Migration

**File:** `Ehs.Infrastructure/Migrations/0001_create_ehs_events.sql`

```sql
CREATE TABLE ehs_events (
  event_id UUID PRIMARY KEY,
  sequence BIGSERIAL,
  type VARCHAR(50) NOT NULL,
  payload JSONB NOT NULL,
  meta JSONB,
  created_at TIMESTAMPTZ NOT NULL,
  tenant_id UUID NOT NULL
);

CREATE MATERIALIZED VIEW ehs_incident_snapshots AS ...
```

Indexes: `sequence`, `created_at DESC`, `tenant_id`

### Validation Rules Implemented

| Rule | Implementation | Status |
|------|----------------|--------|
| Reporter exists | Placeholder (needs Identity module integration) | ⚠️ TODO |
| Timestamp drift | `Math.Abs(clientTime - serverTime) < 2h` | ✅ |
| Photo size | Validator: `size <= 5MB` | ✅ |
| Idempotency | Repository check: `GetByEventIdAsync` | ✅ |
| Incident type | FluentValidation: `near-miss|injury|property` | ✅ |

### Security Checklist

- ✅ Input validation (FluentValidation for all commands/queries)
- ✅ Authorization ([Authorize] attribute on all controllers)
- ⚠️ RLS policy - **TODO**: Add tenant_id RLS to ehs_events table
- ✅ Paraméteres query (EF Core LINQ, nincs string concat)
- ✅ Sensitive data handling (JWT claims via ICurrentUserContext)

## Tesztek

### Build Status
```bash
dotnet build SpaceOS.Modules.Ehs.sln
```
**Result:** ✅ Build succeeded - 0 Warning(s), 0 Error(s)

### Code Statistics
- **38 C# fájl** létrehozva
- **5 projekt:** Domain, Application, Infrastructure, Api, Tests
- **1 SQL migration** fájl

### Unit Tests
⚠️ **TODO:** Még nem implementálva (DoD: 8+ unit test)
- Planned: Event creation, idempotency, validation failures, timestamp drift

### Integration Tests
⚠️ **TODO:** Még nem implementálva (DoD: 5+ integration test)
- Planned: POST /api/ehs/events → 201 Created, idempotency (200 OK), validation (400 Bad Request)

## Open Questions (Conductor válasz szükséges)

A következő kérdések blokkolják a teljes DoD-ot:

1. **GDPR right-to-forget:** Ha user törlés → event retention policy? (Anonymizálás vagy törlés?)
2. **S3 bucket retention:** 7 év compliance vagy rövidebb?
3. **Offline conflict resolution:** Ha ugyanazt az incidentet 2 eszközről küldik (pl. WiFi visszajön), mi a UX? (UUID alapján automatikus dedup elég?)
4. **Photo storage compliance:** S3 bucket encryption + audit log ready?
5. **Reporter validation:** Integration Identity modul-lal - kell-e `SELECT 1 FROM users WHERE id = reporterId`?

## Definition of Done Status

| DoD Item | Status |
|----------|--------|
| POST /api/ehs/events endpoint implementálva | ✅ |
| POST /api/ehs/photos/presigned-url endpoint implementálva | ✅ |
| ehs_events tábla + migration | ✅ |
| ehs_incident_snapshots materialized view | ✅ |
| Validation rules (reporter, timestamp, photo size) | ✅ (reporter validation TODO) |
| Idempotency handling | ✅ |
| Unit tests (8+) | ❌ TODO |
| Integration tests (5+) | ❌ TODO |
| OpenAPI spec frissítve | ❌ TODO |
| EXIF strip spike (4h) → Lambda deploy decision | ❌ TODO |
| Test coverage ≥90% | ❌ TODO |

## Next Steps

### Sprint 1 Completion (Backend)
1. **Unit tests** - 8+ tests (validation, idempotency, domain logic)
2. **Integration tests** - 5+ tests (API endpoints, database, S3)
3. **OpenAPI spec** - Swagger docs frissítés
4. **EXIF strip spike** - 4h spike → Lambda vs background worker decision

### Integration Work
- **Identity module integration:** Reporter ID validation (`ISpaceOSUserRepository`)
- **Kernel integration:** JWT authentication konfigurálás (Authority, Audience)
- **RLS policy:** Tenant isolation a `ehs_events` táblán

### Frontend Unblocking
Frontend MSG-016 unblocked - mock API-val parallel development lehetséges:
- POST /api/ehs/events - 201 Created mock
- POST /api/ehs/photos/presigned-url - presigned URL mock

## Kockázatok

1. **S3 konfigurálás:** AWS credentials és bucket setup szükséges deployment-hez
2. **EXIF strip:** Spike hiányában nem döntöttünk Lambda vs worker-ről
3. **Reporter validation:** Identity modul integráció függőség
4. **RLS policy:** Még nem implementálva a tenant isolation

## Files Changed

**New files (38 C# + 1 SQL):**
```
/opt/spaceos/backend/spaceos-modules-ehs/
├── Ehs.Domain/ (9 files)
│   ├── Aggregates/EhsEvent.cs
│   ├── ValueObjects/{EventId,IncidentType,PhotoS3Key,ReporterId}.cs
│   └── Interfaces/{IEhsEventRepository,IS3Service}.cs
├── Ehs.Application/ (11 files)
│   ├── Commands/ReportIncidentCommand.cs
│   ├── Queries/GeneratePresignedUrlQuery.cs
│   ├── DTOs/{IncidentPayload,EventMeta,EventResponse,PresignedUrlResponse}.cs
│   └── Common/ICurrentUserContext.cs
├── Ehs.Infrastructure/ (8 files)
│   ├── Data/EhsDbContext.cs
│   ├── Repositories/EhsEventRepository.cs
│   ├── Services/{S3Service,CurrentUserContextService}.cs
│   ├── DependencyInjection.cs
│   └── Migrations/0001_create_ehs_events.sql
├── Ehs.Api/ (6 files)
│   ├── Controllers/{EventsController,PhotosController}.cs
│   ├── Program.cs
│   └── appsettings.json
├── Ehs.Tests/ (4 files - boilerplate)
└── SpaceOS.Modules.Ehs.sln
```

---

**Backend Terminal Status:** READY for unit/integration tests + EXIF spike
