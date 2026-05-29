---
id: MSG-PROCUREMENT-014
from: root
to: procurement
type: task
priority: high
status: READ
ref: MSG-PROCUREMENT-012-DONE
created: 2026-05-29
---

# Procurement v2 — VPS deploy bugok (2 javítás szükséges)

A v2 deploy lefutott, service él (healthz 200), de a `from-reorder-alert` smoke test
és az Integration Worker 2 kódbeli hibába ütközik.

**Skill:** `/spaceos-terminal`

---

## Bug 1: `SqlQueryRaw<string>` — `column t.Value does not exist`

**Hol:** `ProcurementV2Repository.GenerateRequisitionNumberAsync`  
**Hiba:** EF Core 8 `SqlQueryRaw<string>` a kapott SQL-t becsomagolja:
```sql
SELECT t."Value" FROM (SELECT fn_next_requisition_number(...)) t
```
PostgreSQL a függvény visszatérési oszlopát `fn_next_requisition_number`-nek nevezi,
nem `Value`-nak → `42703: column t.Value does not exist`.

**Fix:**
```csharp
// /opt/spaceos/backend/spaceos-modules-procurement/src/
// SpaceOS.Modules.Procurement.Infrastructure/Repositories/ProcurementV2Repository.cs
// GenerateRequisitionNumberAsync metódusban:

var result = await _db.Database
    .SqlQueryRaw<string>(
        "SELECT spaceos_procurement.fn_next_requisition_number({0}, {1}) AS \"Value\"",
        tenantId.ToString(), DateTime.UtcNow.Year)
    .FirstAsync(ct).ConfigureAwait(false);
```

---

## Bug 2: Worker `app.current_tenant_id` GUC nem állítódik be

**Hol:** `ProcurementIntegrationWorker.ProcessBatchAsync`  
**Hiba:** A worker BYPASSRLS role-lal csatlakozik (`spaceos_procurement_worker`),
de az EF Core `ProcurementDbContext` (amelyen a worker lekér) a **főbb** connection stringet
használja (`spaceos` user), és a `TenantSessionInterceptor` nem állítja be a GUC-ot
(mert nincs JWT claim a háttérfolyamatban).

**Jelenleg a worker ugyanazon DbContext példányon dolgozik mint az app.**  
A worker LogoN DB hívásai `procurement_outbox` RLS policy-ba ütköznek.

**Vizsgálandó:** `ServiceCollectionExtensions.cs` — a worker hogyan kap `DbContext`-et.  
Ha a worker a főbb connectiont kapja (nem a worker connection-t), az `spaceos` user
RLS-be ütközik. A fix: worker-nek külön `NpgsqlDataSource` a `ProcurementWorkerConnectionString`-ből,
BYPASSRLS user-rel — ugyanúgy ahogy az Identity és Inventory worker teszi.

**Megjegyzés:** A `ProcurementWorkerConnectionString` env már be van állítva a VPS-en:
```
ProcurementWorkerConnectionString=Host=127.0.0.1;Port=5433;Database=spaceos_procurement;Username=spaceos_procurement_worker;Password=<jelszó>
```
(Megtalálható `/etc/spaceos/cutting.env`-ben.)

---

## DB állapot a VPS-en (referencia)

A 2 kód bug kivételével a DB schema teljes:
- PR-M1..M8 migráció lefutott (javításokkal)
- RLS policy-k: `(NULLIF(current_setting('app.current_tenant_id', true), ''))::uuid`
- `fn_next_requisition_number` signature: `(text, integer)` — EF Core-nak text kell
- `procurement_inbox` helyes schema: `Id` UUID PK, `IdempotencyKey varchar(512)`, `CreatedAt`
- `procurement_audit_log.SourceIp` típus: `varchar(45)` (inet → varchar javítva)

---

## Tesztelési instrukciók (deploy után)

```bash
INTERNAL_SECRET=$(cat /tmp/spaceos_internal_secret.txt)

# from-reorder-alert smoke test (orphan → 422)
curl -s -w "\nHTTP:%{http_code}" \
  -X POST http://127.0.0.1:5006/internal/from-reorder-alert \
  -H "Authorization: Bearer $INTERNAL_SECRET" \
  -H "X-SpaceOS-TenantId: 63ef28b6-a43b-4d3f-a076-759a47911559" \
  -H "Content-Type: application/json" \
  -d '{"tenantId":"63ef28b6-a43b-4d3f-a076-759a47911559","materialCode":"NONEXISTENT","currentStock":0,"reorderPoint":5,"suggestedQuantity":10,"unitOfMeasure":"pcs","alertedAt":"2026-05-29T00:00:00Z"}'
# → 422 helyes

# Worker log ellenőrzés (ne legyen 42704 vagy RLS hiba)
sudo journalctl -u spaceos-procurement -n 20 --no-pager | grep -v "42704\|tenant_id"
```

---

## Pipeline
- Mindkét bug javítva → `dotnet test` zöld → publish + systemd restart
- Outbox: `MSG-PROCUREMENT-014-DONE`
