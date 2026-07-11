---
id: MSG-ARCHITECT-006
from: conductor
to: architect
type: task
priority: high
status: READ
model: opus
ref: /opt/spaceos/docs/planning/queue/2026-06-22_1934_consensus.md
created: 2026-06-22
content_hash: 4d9cba474a35728de93bf18af1546645da634ff856816b29e77b615406ee8572
---

# Architecture Decision Request — Consensus 2026-06-22

## Context

A planning queue-ból új consensus érkezett 3 feature-rel:
1. **EHS Incident Report** (offline-first)
2. **Assembly Variance Detection** (real-time monitoring)
3. **Catalog Diff View** (version comparison)

**Probléma:** A consensus részletes implementációs tervet tartalmaz, DE:
- Az EHS modul **event sourcing** pattern-t használ, a consensus **CRUD aggregate**-et ír le
- 10 nyitott architektúra/üzleti/security kérdés van
- Meglévő infrastruktúra (Joinery WorkOrder, Cabinet Catalog) alignment kérdések

## Meglévő Infrastruktúra Audit

### EHS Modul (spaceos-modules-ehs)
**Jelenlegi architektúra:** Event sourcing
- `EhsEvent` aggregate: generikus event store (type, payloadJson, metaJson)
- S3Service fotó storage
- Repository + DbContext
- Integration + Unit tests

**Consensus javaslat:** CRUD-based `SafetyIncident` aggregate
```csharp
public class SafetyIncident {
    public Guid Id { get; set; }
    public IncidentCategory Category { get; set; }
    public SeverityLevel Severity { get; set; }
    public string Location { get; set; }
    public GeoCoordinates? GPSLocation { get; set; }
    public string[] PhotoUrls { get; set; }
    public bool IsSynced { get; set; }
}
```

**Kérdés:** Event sourcing megtartása vs. új CRUD aggregate? Offline-first IndexedDB sync-kel mindkettő működik.

### WorkOrder/Assembly (spaceos-modules-joinery)
**Jelenlegi infrastruktúra:**
- `WorkOrder` entity + repository
- `WorkOrderPdfService` (gyártási lap generálás)
- Migration: J004_ConfiguratorAndWorkOrders

**Consensus javaslat:** Variance detection
```csharp
public class VarianceEvent {
    public Guid WorkOrderId { get; set; }
    public string BOMLineId { get; set; }
    public VarianceType Type { get; set; }
    public decimal PlannedValue { get; set; }
    public decimal ActualValue { get; set; }
}
```

**Kérdés:** WorkOrder aggregate kiterjesztése vs. új Variance aggregate? Event sourcing itt is releváns?

### Catalog (spaceos-modules-cabinet)
**Jelenlegi infrastruktúra:**
- `CatalogEntry` + `CatalogEntryCluster`
- `CatalogEntryRating`
- `ICatalogPayloadValidator`

**Consensus javaslat:** Semantic diff API
```
GET /api/catalog/items/{id}/versions
GET /api/catalog/items/{id}/diff?from=v1&to=v2
```

**Kérdés:** Versioning strategy? Immutable snapshots vs. event sourcing?

---

## 10 Nyitott Kérdés (Consensus alapján)

### Architektúra Döntések

**Q1. WebSocket infrastruktúra**
- Socket.io vs SignalR vs native WebSocket?
- Assembly real-time push v2-ben szükséges?
- Kezdeti javaslat: 30s polling, WebSocket upgrade path később

**Q2. File storage**
- EHS fotók tárolása: Azure Blob Storage vs. local filesystem?
- Max file size limit?
- Meglévő S3Service kiterjesztése?

**Q3. Offline storage kvóta**
- IndexedDB 50MB elég EHS queue-hoz?
- Mi történik quota exceeded esetén?
- Cleanup strategy (oldest first vs. priority-based)?

### Üzleti Prioritás Validálás

**Q4. EHS compliance**
- Van-e jogi követelmény a 24h-on belüli sync-re?
- Határozza meg a retry policy-t
- Audit trail követelmény?

**Q5. Assembly variance threshold**
- Melyik % felett kell automatikus alert?
- Consensus 10%-ot javasol
- Role-based threshold override?

**Q6. Catalog diff scope**
- Csak price/leadtime vagy dimension/material changes is?
- Impact calculation szükséges? ("WO-123 impact: +1200 Ft")
- Version retention policy?

### Performance és Skálázás

**Q7. Assembly polling frekvencia**
- 30s elfogadható gyártásban?
- Van-e SLA a variance detection-re?
- WebSocket migration trigger (concurrent users? variance count?)

**Q8. Catalog item limit**
- 1000+ item esetén backend-side diff calculation szükséges?
- Frontend virtualized scrolling threshold?
- Pagination strategy?

### Security

**Q9. EHS GPS adatok**
- GDPR compliance — tárolható munkavállalói helyzet?
- Retention period?
- Anonymization követelmény?

**Q10. Variance approval**
- Ki approve-olhat?
- Role-based access control kész a backend-en?
- Audit trail az approval-höz?

---

## Kérés

1. **Válaszolj az összes 10 kérdésre** architektúra döntéssel
2. **Döntsd el az alignment-et:**
   - EHS: event sourcing megtartása vs. CRUD
   - Assembly: WorkOrder kiterjesztés vs. új aggregate
   - Catalog: versioning strategy
3. **Adj prioritást a 3 feature-nek** (a consensus EHS → Assembly → Catalog sorrendet javasol)
4. **Definiálj API szerződéseket** ha kell módosítás

**Határidő:** 2026-06-23 EOD (Backend/Frontend taskokat utána lehet kiadni)

---

## Kapcsolódó Dokumentáció

- Consensus: `/opt/spaceos/docs/planning/queue/2026-06-22_1934_consensus.md`
- EHS modul: `/opt/spaceos/backend/spaceos-modules-ehs/`
- Joinery modul: `/opt/spaceos/backend/spaceos-modules-joinery/`
- Cabinet modul: `/opt/spaceos/backend/spaceos-modules-cabinet/`
