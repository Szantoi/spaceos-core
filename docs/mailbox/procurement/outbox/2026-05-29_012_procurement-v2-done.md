---
id: MSG-PROCUREMENT-012-DONE
from: procurement
to: root
type: done
priority: high
status: READ
ref: MSG-PROCUREMENT-012
created: 2026-05-29
---

# Procurement v2 — Implementáció kész (Track A–H)

## Összefoglaló

A Procurement v2 teljes implementációja elkészült, beleértve a frissen unblocked Track D + E-t (MSG-PROCUREMENT-013).

**Commit:** `26a05d1`  
**Branch:** `main`

### Változott/létrejött fájlok (129 fájl, +7027 sor)

**Track A — DB schema (ManualMigrations/)**
- `PR-M1_worker_role.sql` — `spaceos_procurement_worker` BYPASSRLS role
- `PR-M2_purchase_requisition.sql` — PR tábla + composite FK + RLS FORCE + indexek
- `PR-M3_supplier_invoice.sql` — invoice tábla + composite FK + RLS + indexek
- `PR-M4_invoice_match.sql` — append-only trigger + REVOKE (DB-P-02)
- `PR-M5_price_list.sql` — price_list + price_list_entry + partial unique
- `PR-M6_delivery_line.sql` — delivery_line.InventorySyncStatus + outbox
- `PR-M7_outbox_inbox.sql` — procurement_outbox + procurement_inbox + retention indexek
- `PR-M8_audit_log.sql` — procurement_audit_log + append-only trigger + REVOKE (SEC-P-05)

**Track B — Domain**
- `PurchaseRequisition` + `PurchaseRequisitionLine` (FSM, SoD, domain events)
- `SupplierInvoice` + `SupplierInvoiceLine` (FSM, amount-integrity, SoD)
- `PriceList` + `PriceListEntry` (FSM, overlap guard)
- `IMatchPolicy` / `DefaultMatchPolicy` (three-way match, kumulatív ReceivedQuantity, null-ár fallback, div-zero guard)
- `MatchResult`, `MatchLineResult`, `MatchPolicyThresholds`, `ThreeWayMatchInput` VOs
- `ProcurementOutboxMessage`, `ProcurementInboxMessage`, `ProcurementAuditLog`, `InvoiceMatchEntity`, `MatchPolicyEntity`
- Enums: `RequisitionSource`, `RequisitionStatus`, `InvoiceStatus`, `MatchOutcome`, `PriceListStatus`, `OutboxStatus`, `InventorySyncStatus`
- Domain events: 13 új event

**Track C — Application CQRS**
- 12 command + handler (CreatePurchaseRequisition, Approve/Reject/Convert Requisition, ReceiveInvoice, RunMatch, Approve/ApproveWithVariance/Dispute Invoice, CreatePriceList, ActivatePriceList, UpdateMatchPolicy)
- 8 query + handler (GetRequisitions, GetRequisitionById, GetInvoices, GetInvoiceById, GetPriceLists, GetBestPrice, GetMatchPolicy, + meglévők)
- FluentValidation minden command-en
- `ResultToHttp` egységes map (BE-P-05): Forbidden→403, Invalid→422, Conflict→409, NotFound→404, transient→503

**Track D — ProcurementIntegrationWorker**
- ADR-039 három-fázis: CLAIM (`FOR UPDATE SKIP LOCKED`) → PROCESS → COMPLETE
- Manual retry csak tranziensre (3×, exp. back-off 5/10/20s) — Polly nincs approved packages-ben
- Circuit-breaker: 3 egymást követő tranziens után 60s cooldown
- SEC-P-04 DiD: `loaded.TenantId == msg.TenantId` assert mutáció előtt
- BE-P-08: idempotens 200 → Completed (nem Failed)
- BE-P-10: bounded worker pool (env `ProcurementWorkerConnectionString`)
- SEC-P-11: `LastError` scrub — csak hiba típusa, nincs payload/ex.Message

**Track E — from-reorder-alert receiver**
- `POST /procurement/internal/from-reorder-alert`
- Bearer constant-time auth (SEC-P-01), `InternalBearerEndpointFilter`
- `X-SpaceOS-TenantId` header == body strict equal (DB-P-07) → 400 ha nem egyezik
- Inbox dedup: `procurement_inbox ON CONFLICT DO NOTHING` (BE-P-01 egy-tx UoW)
- Dup → 200 meglévő requisitionId; új → 201 + `PurchaseRequisition.Create(Source=ReorderAlert)`
- Orphan materialCode → 422 (SEC-P-10, NEM 5xx)

**Track F — Audit/Observability**
- `ProcurementAuditLog` append-only in-tx writer
- `OutboxRetentionCleanupJob` (Completed > 30 nap)
- OpenTelemetry outbox lag metric

**Track G — Delivery-hook**
- `RecordDeliveryCommandHandler` módosítva: egy-tx outbox INSERT (BE-P-01)

**Track H — Contracts**
- `IProcurementProvider` bővítve: `GetRequisitionByIdAsync`, `GetRequisitionsByTenantAsync`, `GetInvoiceByIdAsync`, `GetBestPriceAsync`

**EF Core (BE-P-04)**
- `HasAlternateKey("Id","TenantId")` + `WithOwner().HasForeignKey().HasPrincipalKey("Id","TenantId")` — composite FK owned entities-en

## Tesztek

| Kategória | Szám |
|-----------|------|
| Meglévő v1 tesztek (mind zöld) | 53 |
| Domain (Requisition, Invoice, Match, PriceList) | 42 |
| Handler tesztek | 8 |
| API endpoint tesztek | 18 |
| Worker tesztek (Track D) | 10 |
| Backend atomicitás (BE-P-01) | 4 |
| **Összesen** | **136** |

```
Passed! - Failed: 0, Passed: 136, Skipped: 0, Total: 136, Duration: 11s
```

## Security review

- **SEC-P-01**: Bearer constant-time (`CryptographicOperations.FixedTimeEquals`) minden internal endpointon ✅
- **SEC-P-02/03**: RBAC `procurement.approver`/`manager` + SoD invariánsok (ApprovedBy≠RequestedBy, VarianceApprovedBy≠RecordedBy) ✅
- **SEC-P-04**: Worker fail-closed DiD assert ✅
- **SEC-P-05**: `procurement_audit_log` append-only (trigger+REVOKE, PR-M8) ✅
- **SEC-P-06**: `SPACEOS_INTERNAL_SECRET` env/secret-store (nem appsettings) ✅
- **SEC-P-07**: Amount-integrity invariant `LineNetAmount==round(Qty×UnitPrice,4)` + `Total*==Σlines` ✅
- **SEC-P-10**: Orphan materialCode → 422 (nem 5xx, peer nem retry-zik) ✅
- **SEC-P-11**: `LastError` scrub ✅
- **DB-P-01**: Nincs user `xmin` oszlop; EF shadow property ✅
- **DB-P-02**: `invoice_match` append-only trigger + REVOKE ✅
- **DB-P-03**: Composite FK `(ParentId,TenantId)` minden line-on ✅
- **DB-P-07**: `set_config('app.tenant_id', ...)` receiver-ben explicit (TenantSessionInterceptor) ✅
- **BE-P-01**: Transactional UoW — domain mutáció + outbox/inbox + audit egy SaveChanges ✅
- **BE-P-04**: EF composite-FK `HasAlternateKey` + `HasPrincipalKey` ✅

## Kockázatok / nyitott pontok

1. **Manuális migration (PR-M1..M8)**: A `ManualMigrations/` mappában lévő SQL fájlok még nem futottak a VPS-en. **INFRA feladat kell** a futtatáshoz.

2. **Polly nincs approved packages-ben**: Manual retry + circuit-breaker implementálva ekvivalens viselkedéssel. Ha Polly engedélyezve lesz → könnyű migrálni.

3. **Rate-limit (SEC-P-08)**: Per-tenant rate-limiting a portal write-endpointokon TODO. Jelölve a kódban — ha szükséges, root adja ki Infrastructure feladatként.

4. **SEC-P-09 (Stored XSS)**: Portal-réteg felelőssége (output-encoding renderingkor) — nem Procurement-oldalon kezelendő.

5. **Performance budget (BE-P-11)**: Load-test gate (`RunMatch` < 200ms stb.) nincs automatizálva — manuális méréssel ellenőrizendő a VPS-en az éles migration után.

6. **delivery_line backfill**: A meglévő Delivery sorokon `InventorySyncStatus` alapértelmezett értéke a migration-ban `NotApplicable` (az új sorokon `Pending`). Az INFRA tasknál jelezni kell.
