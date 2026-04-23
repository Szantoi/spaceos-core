# Joinery Terminal — Hidegindítási Kontextus

> Stack: .NET 8, EF Core 8, QuestPDF, PostgreSQL
> Repo: `/opt/spaceos/spaceos-modules-joinery`
> Branch: `develop`

---

## Felelősség

- Ajtórendelés (DoorOrder) teljes lifecycle
- FSM: Draft → Submitted → Calculating → Calculated → CalculationFailed → Reverted
- Cutting list generálás (BOM)
- PDF gyártási lap (QuestPDF Community)
- Joinery Outbox Worker (FOR UPDATE SKIP LOCKED, 5s tick)
- Abstractions kalkuláció hívás (IOrchestratorClient, 3× retry)
- CuttingListSnapshot (SHA-256 ContentHash, IsLatest)

---

## Jelenlegi állapot (2026-04-20)

| Metrika | Érték |
|---------|-------|
| Tesztek | **249 pass** (basis + v2 + JOINERY-015/016 PDF) |
| Commit | `35a8723` (feat: hardware-list-pdf + material-req-pdf) |
| VPS státusz | `DEPLOYED` ✅ — `systemctl is-active spaceos-joinery` → active |
| Port | **5002** (loopback-only) |
| Health | `GET http://127.0.0.1:5002/health` → 200 |
| Migration | `20260409000001_Migration_0001_InitialSchema` applied |
| ⚠️ | publish chown fix szükséges (root:root → spaceos:spaceos) |

---

## Nyitott migration-ök (pending!)

- `Migration_0002_SeedStaticData` — pending (nincs alkalmazva)
- `J0002_V2_CuttingListSnapshot` — pending (nincs alkalmazva)

**Megjegyzés:** Az E2E 29-joinery-order teszt az alapséma alapján működik — a pending migrációk nem blokkolják. De deploy előtt ellenőrizni!

---

## Env config (`/etc/spaceos/joinery.env`)

```ini
ConnectionStrings__JoineryDb=Host=127.0.0.1;Port=5433;Database=spaceos_joinery;Username=spaceos;Password=spaceos_db_pass
ASPNETCORE_URLS=http://127.0.0.1:5002
Jwt__Authority=https://joinerytech.hu/auth/realms/spaceos
Jwt__Audience=kernel-api
```

---

## Contracts NuGet függőségek

- `SpaceOS.Modules.Cutting.Contracts` — ICuttingProvider
- `SpaceOS.Modules.Inventory.Contracts` — IInventoryProvider (hardverlista)

---

## Kritikus kód helyek

| Komponens | Helyszín | Fontosság |
|-----------|----------|-----------|
| `Infrastructure/Outbox/JoineryOutboxWorker.cs` | FOR UPDATE SKIP LOCKED | SEC-02 — concurrent safe |
| `Infrastructure/Http/OrchestratorClient.cs` | 3× exponential backoff | BE-02 — retry pattern |
| `Infrastructure/Pdf/ProductionSheetGenerator.cs` | QuestPDF Community | PDF layout |
| `DoorOrderEndpoints.cs` | `/health`, `/api/orders/*`, `/internal/results` | Route definiíciók |

---

## SEC-05 — PDF endpoint fejlécek

```csharp
// GET /api/orders/:id/sheet
context.Response.Headers["X-Content-Type-Options"] = "nosniff";
context.Response.Headers["Cache-Control"] = "private, no-store";
```

---

## PDF endpointok (JOINERY-015/016)

### JOINERY-015 — Gyártási lap PDF
```
GET /api/orders/{id}/sheet
```
Fejlécek: `X-Content-Type-Options: nosniff`, `Cache-Control: private, no-store`

### JOINERY-016 — Hardverlista + Anyagnorma PDF (commit 35a8723)
```
GET /api/orders/{id}/hardware-list-pdf
GET /api/orders/{id}/material-req-pdf
```
- **Hardverlista:** fejléc + tábla (Ssz., Megnevezés, Típus, Db, Szín, Megjegyzés) + összesítő
- **Anyagnorma:** fejléc + tábla (Anyag, Vastagság, Terület m², Él fm) + összesítő
- Auth: `ManufacturerOnly` + `TryGetTenantId()` 401 guard
- Tenant isolation: repo lekérdezés `TenantId AND OrderId` → 404 ha más tenant

[MSG-JOINERY-016-DONE]

---

## Ismert tech debt

- ⚠️ publish chown fix szükséges (root:root → spaceos:spaceos)
- Pending migration-ök alkalmazása (J-0002)
- `MapInboundClaims = false` — ellenőrizni (Kernel tanulság alapján)
- GUC key: `app.current_tenant_id` (nem `app.tenant_id`) — ellenőrizni

---

## Indítás előtt

1. `dotnet build → 0 error, 0 warning`
2. `dotnet test → 249 pass, 0 fail`
3. `MapInboundClaims = false` megvan-e a `Program.cs`-ben?
4. `TenantSessionInterceptor` GUC kulcs: `app.current_tenant_id`?
