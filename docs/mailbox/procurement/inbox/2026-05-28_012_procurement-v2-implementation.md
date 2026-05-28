---
id: MSG-PROCUREMENT-012
from: root
to: procurement
type: task
priority: high
status: READ
ref: PROCUREMENT-V2
created: 2026-05-28
---

# Procurement v2 — Teljes implementáció (Track A–H)

**Spec:** `/opt/spaceos/docs/tasks/active/SpaceOS_Modules_Procurement_v2_Architecture_v4.md`

Teljes review pipeline lezárva (v4): 35 finding absorbed (DB ✅ · Security ✅ · Backend ✅).
A spec **implementációra kész** — ne módosítsd, csak implementáld.

---

## Kontextus

**Meglévő:** Procurement Core v1 — `Supplier`, `PurchaseOrder` (FSM), `Delivery`, `IProcurementProvider` — **érintetlen marad**.

**Új v2 aggregates:** `PurchaseRequisition`, `SupplierInvoice`, `PriceList` + Three-Way Match engine.

**Repo:** `spaceos-modules-procurement` (meglévő, bővítés — nem új polyrepo)
**Port:** 5006 (meglévő systemd service)
**Schema:** `spaceos_procurement` (additív bővítés, meglévő táblák érintetlenek)

---

## Track sorrend (A–H)

```
A (Schema/Migration) → B (Domain) → C (Application/CQRS)
                                   → D (Outbox/Worker)    ← blokkolva: Inventory-059
                                   → E (Receiver)         ← blokkolva: Inventory-059
                                   → F (Audit/Observability)
                                   → G (Delivery-hook)
                                   → H (Contracts)
```

**Track A + B azonnal indítható.** Track C, F, G, H blokkoló nélkül megvalósítható.
**Track D + E blokkolva: Inventory MSG-059** (`/inventory/internal/inbound` + `reorder_alert` outbox). Ha az Inventory terminál kész → folytasd D+E-vel.

### Track A — Schema/Migration (PR-M1…M8)
A spec §3 tartalmazza a teljes DDL-t. Minden migration `IF NOT EXISTS` + `DO $$` idempotens.

Kritikus pontok:
- **(DB-P-01)** Semmi user-`xmin` oszlop — EF az implicit rendszer-`xmin`-re mapel
- **(DB-P-02)** `invoice_match` append-only trigger (`fn_prevent_invoice_match_mutation()`) + `REVOKE UPDATE, DELETE FROM spaceos_procurement_app`
- **(DB-P-03)** Szülő aggregate-eken `UNIQUE ("Id","TenantId")` + minden `*_line`-on composite FK `("ParentId","TenantId")`
- **(SEC-P-05)** PR-M8: `procurement_audit_log` tábla append-only (Sales `sales_audit_log` minta)
- RLS FORCE minden új táblán; `current_setting('app.tenant_id')`
- **Migration futtatása: manuális SQL** (dotnet-ef v10 inkompatibilis .NET 8-cal — lásd `docs/knowledge/deployment/KNOWN_GOTCHAS.md` #1)

### Track B — Domain
A spec §2 tartalmazza az aggregate-eket, FSM-guardokat, VO-kat.

Kritikus pontok:
- **(BE-P-01)** Egyetlen UoW: domain mutáció + outbox/inbox INSERT + audit egy DB-tx-ben
- **(SEC-P-07)** Amount-integritás invariáns: `LineNetAmount == round(Qty×UnitPrice, 4)` + `Total* == Σlines`
- **(BE-P-06)** `ReceivedQuantity` kumulált egy `GROUP BY` query-vel (no N+1)
- **(DB-P-08)** Null-ár fallback: PO `UnitPrice` null → aktív PriceList-ár; ha nincs/0 → `Exception`
- SoD: `ApprovedBy ≠ RequestedBy`, `VarianceApprovedBy ≠ RecordedBy`

### Track C — Application/CQRS
A spec §4 tartalmazza az API surface-t.

Kritikus pontok:
- **(SEC-P-02/03)** RBAC + SoD guard minden money-kapun (`procurement.approver` role-claim)
- **(BE-P-05)** Egységes `Result`→HTTP map: Forbidden→403, Invalid→422, Conflict→409, NotFound→404, tranziens→503
- FluentValidation minden command-en

### Track D — Outbox/Worker *(blokkolva: Inventory-059)*
- **(BE-P-02)** Polly: retry csak tranziensre + circuit-breaker
- **(BE-P-03)** Lease/reclaim: `Status='Pending' AND NextAttemptAt<=now()` OR `Status='InFlight' AND LeaseUntil<now()`, `FOR UPDATE SKIP LOCKED`
- **(SEC-P-04)** Worker fail-closed: `loaded.TenantId == msg.TenantId` assert mutáció előtt
- **(BE-P-10)** Bounded külön connection-pool a worker-nek

### Track E — Receiver *(blokkolva: Inventory-059)*
`POST /procurement/internal/from-reorder-alert`
- Bearer-auth (constant-time), loopback assert, Inbox dedup (`ON CONFLICT DO NOTHING`)
- `set_config('app.tenant_id', ...)` a business-tx elején
- Orphan `MaterialCode`/`SupplierId` → `Result.Invalid` (422, nem 5xx)

### Track F — Audit/Observability
- `procurement_audit_log` writer in-tx (minden pénzügyi mutáció + minden `Forbidden` kísérlet)
- Outbox lag-metric OpenTelemetry-n
- Retention cleanup-job (Completed/inbox > 30 nap)

### Track G — Delivery-hook
`RecordDelivery` (meglévő) outbox-enqueue bővítése egy-tx-ben + `InventorySyncStatus` mező.

### Track H — Contracts
`IProcurementProvider` additív bővítés — requisition + invoice + best-price DTO-k.

---

## Shell discovery (első lépés — kötelező)

```bash
cd backend/spaceos-modules-procurement

# Meglévő aggregates és EF-config
ls src/SpaceOS.Procurement.Domain/Aggregates/
ls src/SpaceOS.Procurement.Infrastructure/Configurations/

# Migration előzmények
ls src/SpaceOS.Procurement.Infrastructure/Migrations/ | tail -5

# Delivery / DeliveryLine jelenlegi alakja
grep -rn "class Delivery\b\|class DeliveryLine\b" src/SpaceOS.Procurement.Domain/

# Bearer/internal-auth precedens a Sales-ből
grep -rn "SPACEOS_INTERNAL_SECRET\|IInternalAuth\|InternalSecretMiddleware" \
  ../spaceos-modules-sales/src/

# Nyitott pont: delivery_line backfill ('Synced' vs 'NotApplicable')
grep -rn "InventorySyncStatus\|SyncStatus" src/SpaceOS.Procurement.Domain/
```

---

## DoD (összesített — spec §7)

- [ ] Meglévő **53** Procurement teszt + **3 761+** összesített backend teszt zöld
- [ ] Procurement v2 **≥ 82 új teszt** (22 domain + 14 handler + 10 API + 18 security + 8 DB-constraint + 10 backend)
- [ ] 0 build warning
- [ ] `ConfigureAwait(false)` minden production async callban
- [ ] Golden Rules #1–12 teljesül
- [ ] `dotnet list package --vulnerable` → 0 high/critical

---

## Precedensek (tanulmányozásra)

| Mit | Hol |
|---|---|
| Bearer internal-auth minta | `../spaceos-modules-sales/src/` (SEC-S-01) |
| Outbox + worker + Polly + BYPASSRLS | `../spaceos-modules-sales/src/SpaceOS.Modules.Sales.Infrastructure/Outbox/` |
| Receiver + Inbox dedup + egy-tx | `../spaceos-modules-joinery/src/` (J-003 minta) |
| EF owned composite-FK | spec §6 |

---

## Skill-ek

`/spaceos-terminal` — inbox/outbox protokoll (kötelező olvasni session elején)
`/senior-backend` — implementation guidance
`/ef-core` — EF Core patterns
`/spaceos-deploy` — migration futtatás, KNOWN_GOTCHAS

## Ha elakadsz

BLOCKED outbox üzenet — konkrét leírással mi blokkol. Track D/E esetén jelezd, hogy az Inventory-blokkolóra vársz.
