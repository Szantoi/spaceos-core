---
id: MSG-PROCUREMENT-015-DONE
from: procurement
to: root
type: done
priority: critical
status: READ
ref: MSG-PROCUREMENT-015
created: 2026-05-29
---

# Procurement v2 — `from-reorder-alert` RLS fix — KÉSZ

## Összefoglaló

42501 RLS hiba javítva. Build zöld, 136/136 teszt zöld.

**Commit:** `c5f1292`

---

## Fix

**Fájl:** `src/SpaceOS.Modules.Procurement.Api/Endpoints/InternalEndpoints.cs`

A `MapPost("/internal/from-reorder-alert")` lambdában `mediator.Send(command, ct)` köré
bekerült az `OpenConnectionAsync` / `set_config` / `CloseConnectionAsync` minta — ugyanaz,
amit a `delete-by-tenant` endpoint már használ.

**Root cause (összefoglaló):**
EF Core `SaveChangesAsync` → `INSERT ... RETURNING` → PostgreSQL a RETURNING sorait
a RLS USING clause-zal ellenőrzi. Ha a `app.current_tenant_id` GUC üres (DB default),
`NULLIF('','')::uuid = NULL` → `TenantId = NULL` → RLS FALSE → 42501.

`WITH CHECK (true)` csak az INSERT check-et engedi át; a RETURNING láthatóságát
a USING clause dönti el.

---

## Tesztek

```
Passed! - Failed: 0, Passed: 136, Skipped: 0, Total: 136, Duration: 11s
```

---

## VPS teendő (INFRA)

VPS-en publish + `sudo systemctl restart spaceos-procurement` szükséges.
DB schema nem változott — csak API réteg változott.

## DONE kritériumok

- [x] Build zöld
- [x] Tesztek zöldek (136/136)
- [x] Commit hash: `c5f1292`
- [ ] VPS-en publish + restart (INFRA terminál feladata)
- [ ] `from-reorder-alert` orphan materialCode → 422 smoke test (VPS után)
