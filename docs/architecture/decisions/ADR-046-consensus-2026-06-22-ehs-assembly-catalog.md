# ADR-046: Consensus 2026-06-22 — EHS, Assembly Variance, Catalog Diff

**Döntés:** 2026-06-22 PROPOSED
**Státusz:** IMPLEMENTÁCIÓRA KÉSZ (pending root approval)
**Készítette:** Architect terminál

---

## Executive Summary

Ez az ADR válaszol a 2026-06-22 consensus 10 nyitott kérdésére és 3 alignment döntésére:

| Feature | Alignment Döntés |
|---------|------------------|
| **EHS Incident Report** | Event sourcing MEGTARTÁSA + `SafetyIncidentProjection` view |
| **Assembly Variance Detection** | Új `VarianceEvent` aggregate (event sourcing) |
| **Catalog Diff View** | Meglévő `Version` mező + immutable snapshot strategy |

**Feature Prioritás:** EHS → Assembly Variance → Catalog Diff (consensus sorrend megerősítve)

---

## 1. EHS Alignment — Event Sourcing vs. CRUD

### Döntés: Event Sourcing MEGTARTÁSA

**Indoklás:**

A meglévő `EhsEvent` aggregate már implementálja az event sourcing pattern-t:
- Append-only, immutable events (`Sequence`, `Type`, `PayloadJson`, `MetaJson`)
- Client-generated UUID idempotency (`EventId`)
- Tenant isolation (`TenantId`)

**Előnyök az event sourcing megtartásának:**
1. **Offline-first sync kompatibilitás** — IndexedDB queue természetes illeszkedés event store-hoz
2. **Audit trail** — minden változás automatikusan megmarad (ADR-003 compliance)
3. **Idempotency** — kliens UUID garantálja a dupla submission védelmet
4. **Temporal queries** — "mi történt 2026-05-15-én?" típusú lekérdezések

**Implementációs módosítás:**

A consensus `SafetyIncident` struktúrája **projection view**-ként implementálandó:

```csharp
// Ehs.Application/Projections/SafetyIncidentProjection.cs
public sealed class SafetyIncidentProjection
{
    public Guid Id { get; init; }
    public IncidentCategory Category { get; init; }
    public SeverityLevel Severity { get; init; }
    public string Location { get; init; }
    public GeoCoordinates? GPSLocation { get; init; }
    public string[] PhotoUrls { get; init; }
    public bool IsSynced { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? ResolvedAt { get; init; }
}
```

**Event típusok bővítése:**
```csharp
public static class EhsEventTypes
{
    public const string IncidentReported = "INCIDENT_REPORTED";
    public const string IncidentUpdated = "INCIDENT_UPDATED";
    public const string IncidentResolved = "INCIDENT_RESOLVED";
    public const string PhotoAttached = "PHOTO_ATTACHED";
}
```

---

## 2. Assembly Variance Alignment — WorkOrder kiterjesztés vs. új aggregate

### Döntés: Új `VarianceEvent` Aggregate (Event Sourcing)

**Indoklás:**

A meglévő `WorkOrder` entitás CRUD-based:
- Mutable state (`BomItems`, `TotalCost`, stb.)
- Nincs audit trail az egyes módosításokhoz
- Gyártási lap generálásra optimalizált, nem real-time monitoring-ra

**Miért új aggregate?**

| Szempont | WorkOrder Kiterjesztés | Új VarianceEvent |
|----------|------------------------|------------------|
| Separation of Concerns | ❌ Mixed responsibility | ✅ Clear boundary |
| Event Sourcing | ❌ Kell refaktorálni | ✅ Natív illeszkedés |
| Real-time Query | ❌ JOIN komplexitás | ✅ Optimalizált projection |
| Backward Compatibility | ❌ Breaking change | ✅ Nincs impact |

**Domain Model:**

```csharp
// Joinery.Domain/Aggregates/VarianceEvent.cs
public sealed class VarianceEvent
{
    public Guid Id { get; private set; }
    public long Sequence { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public string BOMLineId { get; private set; }
    public VarianceType Type { get; private set; }
    public decimal PlannedValue { get; private set; }
    public decimal ActualValue { get; private set; }
    public decimal DeviationPercent => (ActualValue - PlannedValue) / PlannedValue * 100;
    public Guid TenantId { get; private set; }
    public Guid ReportedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string? Notes { get; private set; }
}

public enum VarianceType
{
    Quantity,
    Dimension,
    MaterialGrade,
    Labor
}
```

---

## 3. Catalog Diff Alignment — Versioning Strategy

### Döntés: Meglévő `Version` Mező + Immutable Snapshot Strategy

**Indoklás:**

A `CatalogEntry` már rendelkezik:
- `Version` counter — monotonically increasing
- `ContentHash` — SHA-256 integrity
- `PayloadJson` — full content
- 5-state FSM lifecycle (Draft → Published → Deprecated)

**NINCS szükség event sourcing-ra itt**, mert:
1. A `Version` counter már nyomon követi a változásokat
2. `ContentHash` biztosítja az integritást
3. ADR-003 szerint "nincs UPDATE CAD adatokon" — minden verzió új snapshot

**DB séma bővítés:**

```sql
CREATE TABLE "CatalogEntryHistory" (
    "Id" uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    "CatalogEntryId" uuid NOT NULL REFERENCES "CatalogEntries"("Id"),
    "Version" int NOT NULL,
    "PayloadJson" jsonb NOT NULL,
    "ContentHash" text NOT NULL,
    "State" text NOT NULL,
    "ChangedBy" uuid NOT NULL,
    "ChangedAt" timestamptz NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "TenantId" uuid NOT NULL,
    UNIQUE ("CatalogEntryId", "Version")
);
```

---

## 4. Architektúra Döntések — 10 Kérdés Válaszai

### Q1. WebSocket infrastruktúra

**Döntés:** 30s polling MVP, SignalR Phase 2

**Választás:** **SignalR** (Phase 2)
- .NET backend natív támogatás
- ASP.NET Core middleware integrált
- Auto-reconnect és fallback

**MVP (Phase 1):** HTTP polling 30s interval

**WebSocket Migration Trigger:** >10 concurrent users VAGY >50 variance/óra

---

### Q2. File storage

**Döntés:** Meglévő S3Service kiterjesztése

- Max file size: 5MB (már implementálva)
- Content type validation: image/jpeg, image/png, image/heic, image/webp
- Azure Blob Storage prod-ban, MinIO/LocalStack dev-ben

---

### Q3. Offline storage kvóta

**Döntés:** 50MB IndexedDB + priority-based cleanup

| Aspektus | Érték |
|----------|-------|
| Kvóta | 50MB |
| Cleanup strategy | Priority-based (Severity alapú) |
| Quota exceeded | Block new photos, force sync prompt |

---

### Q4. EHS compliance

**Döntés:** 72h sync deadline, exponential backoff retry

| Aspektus | Érték |
|----------|-------|
| Sync deadline | 72 óra (magyar munkavédelmi törvény) |
| Retry policy | Exponential backoff: 1min → 5min → 15min → 1h |
| Audit trail | Kötelező (GDPR + munkavédelem) |

---

### Q5. Assembly variance threshold

**Döntés:** 10% default, role-based override

| Role | Override képesség |
|------|-------------------|
| `factory_supervisor` | 5-20% |
| `production_manager` | 0-25% |
| `tenant_admin` | Unlimited |

---

### Q6. Catalog diff scope

**Döntés:** Full semantic diff (price, leadtime, dimensions, materials)

| Diff Category | Severity |
|---------------|----------|
| Price change | Warning |
| Lead time change | Warning |
| Dimension change | Critical |
| Material change | Critical |

**Version retention:** 365 nap

---

### Q7. Assembly polling frekvencia

**Döntés:** 30s elfogadható, SLA = 60s variance detection

| Metrika | Érték |
|---------|-------|
| Polling interval | 30 seconds |
| Variance detection SLA | 60 seconds |
| WebSocket trigger | >10 users OR >50 variance/hour |

---

### Q8. Catalog item limit

**Döntés:** Backend-side diff >500 items, frontend virtualized >100

| Threshold | Action |
|-----------|--------|
| <100 items | Full client-side diff |
| 100-500 items | Client-side + lazy load |
| >500 items | Server-side diff |

---

### Q9. EHS GPS adatok

**Döntés:** Opt-in, 90 nap retention, anonymization

| Aspektus | Döntés | GDPR alap |
|----------|--------|-----------|
| GPS gyűjtés | Opt-in | Art. 6(1)(a) |
| Retention | 90 nap | Art. 5(1)(e) |
| Anonymization | 90 nap után → ~1km precision | |

---

### Q10. Variance approval

**Döntés:** Role-based approval + audit trail

| Role | Approve képesség | Max deviation |
|------|------------------|---------------|
| `factory_worker` | ❌ | - |
| `factory_supervisor` | ✅ | <15% |
| `production_manager` | ✅ | <25% |
| `tenant_admin` | ✅ | Unlimited |

---

## 5. API Szerződések

### 5.1 EHS API (bővítés)

```yaml
POST /api/ehs/events/batch
  Request: { events: EhsEvent[] }  # Max 50
  Response: { results: { eventId, success, error? }[] }

GET /api/ehs/incidents?status=open&severity=critical
  Response: { items: SafetyIncidentProjection[], totalCount }
```

### 5.2 Assembly Variance API (új)

```yaml
POST /api/joinery/work-orders/{id}/variances
  Request: { bomLineId, type, plannedValue, actualValue, notes? }
  Response: { id, deviationPercent, alertTriggered }

GET /api/joinery/work-orders/{id}/variances?since={timestamp}
  Response: { variances: VarianceEvent[] }

POST /api/joinery/variances/{id}/approve
  Request: { justification? }
  Response: 200 OK
```

### 5.3 Catalog Diff API (új)

```yaml
GET /api/catalog/items/{id}/versions?cursor={cursor}&limit=20
  Response: { versions, nextCursor?, totalCount }

GET /api/catalog/items/{id}/diff?from={v1}&to={v2}
  Response: { changes, priceImpact?, leadTimeImpact?, affectedWorkOrders[] }
```

---

## 6. Feature Prioritás

| # | Feature | Prioritás | Effort | Dependencies |
|---|---------|-----------|--------|--------------|
| 1 | **EHS Incident Report** | P0 | 3-4 nap | S3Service (done) |
| 2 | **Assembly Variance** | P1 | 4-5 nap | EHS done |
| 3 | **Catalog Diff** | P2 | 3-4 nap | Nincs blocker |

---

## 7. Implementációs Roadmap

### Phase 1: EHS Enhancement (3-4 nap)
- [ ] `EhsEventTypes` konstansok
- [ ] `SafetyIncidentProjection` service
- [ ] `/api/ehs/incidents` endpoint
- [ ] `/api/ehs/events/batch` endpoint
- [ ] Frontend: IndexedDB sync logic

### Phase 2: Assembly Variance (4-5 nap)
- [ ] `VarianceEvent` aggregate
- [ ] Migration: variance_events table
- [ ] API endpoints (3)
- [ ] Frontend: polling hook + variance list

### Phase 3: Catalog Diff (3-4 nap)
- [ ] `CatalogEntryHistory` table
- [ ] `ICatalogDiffService` implementation
- [ ] API endpoints (2)
- [ ] Frontend: diff view component

---

## Referenciák

- EHS Domain: `/opt/spaceos/backend/spaceos-modules-ehs/Ehs.Domain/Aggregates/EhsEvent.cs`
- WorkOrder: `/opt/spaceos/backend/spaceos-modules-joinery/SpaceOS.Modules.Joinery.Domain/Entities/WorkOrder.cs`
- CatalogEntry: `/opt/spaceos/backend/spaceos-modules-cabinet/src/SpaceOS.Cabinet.Catalog/CatalogEntry.cs`
- ADR Catalogue: `/opt/spaceos/docs/knowledge/architecture/ADR_CATALOGUE.md`
