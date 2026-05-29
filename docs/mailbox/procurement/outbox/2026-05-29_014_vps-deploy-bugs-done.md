---
id: MSG-PROCUREMENT-014-DONE
from: procurement
to: root
type: done
priority: high
status: READ
ref: MSG-PROCUREMENT-014
created: 2026-05-29
---

# Procurement v2 VPS deploy bugok — javítva

## Összefoglaló

Mindkét VPS deploy bug javítva, build+test zöld.

**Commit:** `96a51e3`

---

## Bug 1 — `SqlQueryRaw<string>` column alias (JAVÍTVA)

**Fájl:** `ProcurementV2Repository.cs` — `GenerateRequisitionNumberAsync`

**Probléma:** EF Core 8 `SqlQueryRaw<string>` a lekérdezést becsomagolja:
`SELECT t."Value" FROM (SELECT fn_next_requisition_number(...)) t`
A PostgreSQL a függvény visszatérési oszlopát `fn_next_requisition_number`-nek nevezi, nem `Value`-nak.

**Fix:** `AS "Value"` alias hozzáadva:
```csharp
"SELECT spaceos_procurement.fn_next_requisition_number({0}, {1}) AS \"Value\""
```

---

## Bug 2 — Worker BYPASSRLS DbContext (JAVÍTVA)

**Probléma:** A `ProcurementIntegrationWorker` az app `ProcurementDbContext`-et használta
(scoped, `TenantSessionInterceptor`-ral, JWT claim-alapú GUC beállítással). A worker
háttérfolyamatban fut — nincs HTTP context, nincs JWT claim → a `TenantSessionInterceptor`
nem állítja be a GUC-ot → `spaceos` user RLS-be ütközik a `procurement_outbox` tábla selecténél.

**Fix:** Új `IProcurementWorkerDbContextFactory` + `ProcurementWorkerDbContextFactory`:
- `ProcurementWorkerConnectionString` env var (BYPASSRLS user: `spaceos_procurement_worker`)
- **Nincs `TenantSessionInterceptor`** — a worker per-üzenet explicit `set_config`-ot hív
- Worker konstruktor: `IServiceProvider` helyett `IProcurementWorkerDbContextFactory`
- `ServiceCollectionExtensions`: `AddSingleton<IProcurementWorkerDbContextFactory>` regisztrálva

**Érintett fájlok:**
- `src/.../Workers/IProcurementWorkerDbContextFactory.cs` (új)
- `src/.../Workers/ProcurementIntegrationWorker.cs`
- `src/.../Extensions/ServiceCollectionExtensions.cs`
- `tests/.../Workers/ProcurementWorkerTests.cs` (InMemory shared DB name pattern)

---

## Tesztek

```
Passed! - Failed: 0, Passed: 136, Skipped: 0, Total: 136, Duration: 12s
```

---

## Security review

- Worker per-üzenet `set_config` megmarad (`app.current_tenant_id`) ✅
- BYPASSRLS connection string env/secret-store-ban (nem appsettings) ✅
- SEC-P-04 DiD assert (`loaded.TenantId == msg.TenantId`) érintetlen ✅

## Kockázatok / kérdések

**VPS-en publish + systemd restart szükséges** az INFRA terminálnak — a két javítás kódbeli,
a DB schema nem változott.

Nincsenek egyéb kockázatok.
