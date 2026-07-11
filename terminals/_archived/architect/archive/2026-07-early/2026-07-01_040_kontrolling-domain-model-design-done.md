---
id: MSG-ARCHITECT-040
ref: MSG-ARCHITECT-037
from: architect
to: conductor
type: done
priority: high
status: READ
created: 2026-07-01
content_hash: 44c524d8e7590bc6ef17cb7ad84bc146d03089fd375e7fcc56d0885bdbd28fa7
---

# Kontrolling Domain Model Design — DONE

## Summary

Elkészítettem a JoineryTech Kontrolling modul teljes domain modelljét ADR-055 formátumban. A Kontrolling egy **calculation-heavy, read-oriented** modul, amely aggregálja a Production, HR, Finance, Warehouse és Logistics modulok adatait.

## Deliverables

**ADR Dokumentáció:** `/opt/spaceos/docs/architecture/decisions/ADR-055-joinerytech-kontrolling-domain-model.md`

**Tartalom:**
- ✅ Calculation engine modell (ProjectCostCalculation aggregate)
- ✅ Cost allocation rules (6 kategória: Material, Labor, Subcontracting, Logistics, Supplier, Overhead)
- ✅ EAC (Estimate at Completion) formula
- ✅ Integration contracts (5 modul: Production, HR, Finance, Warehouse, Logistics)
- ✅ Manual adjustments (CostAdjustment entity)
- ✅ Portfolio aggregation
- ✅ Database schema (config, adjustments)
- ✅ CQRS query handlers (6 query, 4 command)
- ✅ REST API endpoints (10 endpoint)
- ✅ Testing strategy
- ✅ Performance & scalability considerations
- ✅ 5-week implementation plan

## Architecture Highlights

### 1. Calculation-First Design

**Filozófia:**
- **Nincs stored calculation** — minden on-demand számított
- **One source of truth** — a Kontrolling NEM duplikálja az adatokat
- **Immutable results** — számítások cachelhető eredményekkel

**Aggregate:**
- `ProjectCostCalculation` (calculated, not stored)
- `CostAdjustment` (stored, manual corrections only)

### 2. Cost Categories (6 kategória)

| Kategória | Terv forrás | Tény forrás |
|-----------|-------------|-------------|
| **Material** | MfgPrep.MaterialCost | WarehouseReceipts |
| **Labor** | MfgPrep.LaborCost | HR.TimeLogs |
| **Subcontracting** | B2BHandshakes (planned) | B2BHandshakes (invoiced) |
| **Logistics** | Shipments (estimated) | Shipments (actual) |
| **Supplier** | N/A | InboundInvoices |
| **Overhead** | % of direct costs | % of actual direct |

### 3. EAC (Estimate at Completion)

**Formula:**
```
FOR EACH category:
  projected[category] = MAX(planned[category], actual[category])

costEAC = SUM(projected[category]) + overhead
eacMargin = actualRevenue - costEAC
```

**Előny:**
- Nem realizált költségek a tervük szerint szerepelnek (alsó korlát)
- Túllépések beépülnek (reális projekció)
- Stabil fedezet-előrejelzés a projekt teljes életciklusában

**Példa:**

| Kategória | Terv | Tény | Projected (EAC) |
|-----------|------|------|-----------------|
| Material | 1,000,000 HUF | 1,200,000 HUF | **1,200,000 HUF** (túllépés) |
| Labor | 500,000 HUF | 300,000 HUF | **500,000 HUF** (még nincs kész) |
| Logistics | 200,000 HUF | 0 HUF | **200,000 HUF** (még nem felmerült) |

→ EAC Total: 1,900,000 HUF (tervezett 1,700,000 HUF helyett)

### 4. Overhead Allocation

**Módszer:** DirectCostPercentage (default)
```
overhead = directCosts * overheadRate
```

**Tenant-level konfiguráció:**
- Overhead rate (default: 15%)
- Allocation method (DirectCostPercentage / LaborHours / Revenue)
- Exclusions (optional)

### 5. Manual Adjustments

**CostAdjustment Entity:**
- Scope: Project / Portfolio
- Category: Material / Labor / Subcontracting / Logistics / Supplier / Overhead
- PlannedAdjustment / ActualAdjustment
- Reason (mandatory)

**Use Case példák:**
1. Hiányzó szállítói számla (Actual +500k HUF)
2. Várható túllépés korrekciója (Planned +10%)
3. Globális rezsi-emelés (Portfolio-wide overhead)

### 6. Integration Contracts

**5 integráció:**

1. **Kontrolling ← Production:**
   - `IMfgPrepService.DeriveForProjectAsync()`
   - Planned Material, Labor

2. **Kontrolling ← HR:**
   - `ITimeLogService.GetTimeLogsForProjectAsync()`
   - Actual Labor

3. **Kontrolling ← Finance:**
   - `IInvoiceService.GetInvoicesForProjectAsync()`
   - Actual Revenue, Supplier costs

4. **Kontrolling ← Warehouse:**
   - `IWarehouseReceiptService.GetReceiptsForProjectAsync()`
   - Actual Material

5. **Kontrolling ← Logistics:**
   - `IShipmentCostService.GetShipmentCostsForProjectAsync()`
   - Planned/Actual Logistics

### 7. Database Schema

**2 táblák:**
- `controlling.config` — Tenant-level overhead config
- `controlling.cost_adjustments` — Manual corrections

**Nincs tábla ProjectCostCalculation-höz** — minden on-demand számított!

**RLS Policies:** Tenant isolation mindkét táblán

**Indexes:**
- `(project_id)` on adjustments
- `(scope, category)` on adjustments
- `(created_at DESC)` on adjustments

### 8. Performance Optimization

**Caching:**
- Project costs: 5 min cache
- Portfolio aggregation: 10 min cache
- Config: Indefinite cache (invalidate on update)

**Materialized View (optional):**
```sql
CREATE MATERIALIZED VIEW controlling.project_cost_summary AS
SELECT project_id, tenant_id, /* ... aggregated costs */
FROM projects.projects
-- ... joins
GROUP BY project_id, tenant_id;
```

Refresh: Hourly cron vagy on-demand

**Archival:**
- Old adjustments: Archive after 2 years
- Historical snapshots: Monthly for trend analysis

### 9. API Endpoints

**6 Query Endpoints:**
- GET `/api/controlling/projects/{id}/costs`
- GET `/api/controlling/portfolio/costs`
- GET `/api/controlling/portfolio/top-projects`
- GET `/api/controlling/projects/{id}/variance`
- GET `/api/controlling/adjustments`
- GET `/api/controlling/config`

**4 Command Endpoints:**
- POST `/api/controlling/adjustments`
- PUT `/api/controlling/adjustments/{id}`
- DELETE `/api/controlling/adjustments/{id}`
- PUT `/api/controlling/config`

**Permissions:**
- `controlling.view` — Read access
- `controlling.manage` — Create/update adjustments
- `controlling.admin` — Config changes

## Implementation Plan

**Week 1:** Core Calculation Engine (ProjectCostCalculation, CategoryCost, EAC logic)
**Week 2:** Integration Contracts (5 service interfaces + mocks)
**Week 3:** Manual Adjustments (CostAdjustment entity, CRUD, events)
**Week 4:** Query Handlers (6 query handlers + caching layer)
**Week 5:** API Layer & Integration (Controllers, real module integration, E2E tests)

## Acceptance Criteria Status

- [x] Calculation engine modell
- [x] Cost allocation rules
- [x] Integration contract Kontrolling←Production
- [x] ADR dokumentáció

## Files Changed

- `/opt/spaceos/docs/architecture/decisions/ADR-055-joinerytech-kontrolling-domain-model.md` (NEW)

## Technical Debt

Nincs.

## Next Steps

1. **Backend Terminal:** Implementálja az ADR-055 alapján a Kontrolling modult
2. **Production Terminal:** Implementálja az `IMfgPrepService` interface-t
3. **HR Terminal:** Implementálja az `ITimeLogService` interface-t
4. **Finance Terminal:** Implementálja az `IInvoiceService` interface-t
5. **Warehouse Terminal:** Implementálja az `IWarehouseReceiptService` interface-t
6. **Logistics Terminal:** Implementálja az `IShipmentCostService` interface-t
7. **Frontend Terminal:** Kontrolling dashboard (project costs, portfolio view, variance analysis)

## Notes

Az ADR-055 követi a SpaceOS architektúra alapelveit:
- **One Source of Truth** — nem duplikál adatokat
- **Calculation-First** — minden on-demand számított
- **Immutability** — csak manual adjustments írhatók
- **Multi-Tenant Safe** — RLS policies
- **Performance-Aware** — caching + optional materialized view

A Kontrolling modul **calculation-heavy, read-oriented** architektúrával készült, ami minimalizálja az írási műveleteket és maximalizálja a számítási rugalmasságot.

A dokumentáció készen áll a backend implementációra.
