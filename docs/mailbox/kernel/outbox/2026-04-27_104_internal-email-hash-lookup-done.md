---
id: MSG-KERNEL-104-DONE
from: kernel
to: root
type: done
status: READ
ref: MSG-KERNEL-104
created: 2026-04-27
---

# KERNEL-104 — Internal email-hash lookup endpoint DONE

## Összefoglaló

A PartnerTier `AttributionWorker` által igényelt `GET /internal/tenants/by-email-hash` endpoint implementálva.

### Implementált változások

| Fájl | Változás |
|---|---|
| `SpaceOS.Kernel.Domain/Entities/Tenant.cs` | +`EmailHash` property (nullable string, max 64 char); +`SetEmailHash(string?)` metódus (SHA-256 hex validációval) |
| `SpaceOS.Kernel.Domain/Repositories/ITenantRepository.cs` | +`GetByEmailHashAsync(string, CancellationToken)` |
| `SpaceOS.Infrastructure/Data/Configurations/TenantConfiguration.cs` | +EmailHash EF mapping (max 64) + UNIQUE partial index |
| `SpaceOS.Infrastructure/Data/Repositories/TenantRepository.cs` | +`GetByEmailHashAsync` implementáció (AsNoTracking, IsArchived=false filter) |
| `SpaceOS.Kernel.Api/Endpoints/InternalEndpoints.cs` | +`GET /internal/tenants/by-email-hash?hash={sha256_hex}` endpoint |
| `SpaceOS.Infrastructure/Migrations/20260427051342_Tenant_EmailHash.cs` | ÚJ — PostgreSQL típusokkal (character varying(64)) |

### Endpoint spec

```
GET /internal/tenants/by-email-hash?hash={sha256_hex}
Header: X-SpaceOS-Internal: true

→ 200: { "tenantId": "uuid" }        (tenant found)
→ 404: { "error": "Not found" }      (no matching tenant)
→ 403: { "error": "Forbidden" }      (missing/invalid header)
→ 400: { "error": "Bad request" }    (missing hash param)
```

### Séma változás (VPS-en alkalmazva)

```sql
ALTER TABLE "Tenants" ADD COLUMN "EmailHash" character varying(64);
CREATE UNIQUE INDEX "IX_Tenants_EmailHash" ON "Tenants" ("EmailHash") WHERE "EmailHash" IS NOT NULL;
```

## Tesztek

- **Összesen: 1161 pass** (946 unit + 108 integration + 107 API)
- **DoD: ≥ 1161** ✅ (+3 új API teszt)

### Új tesztek
- `SpaceOS.Kernel.Api.Tests/Endpoints/InternalEmailHashLookupEndpointTests.cs`:
  1. Valid hash → 200 + tenantId
  2. Unknown hash → 404
  3. Missing X-SpaceOS-Internal → 403

## Security review

- A nyers email cím soha nem kerül a Kernelbe — csak a SHA-256 hash
- `SetEmailHash` validálja, hogy pontosan 64 karakter hexadecimális string
- Az endpoint X-SpaceOS-Internal header gáttal védett (403 ha hiányzik)
- SQL injection nem lehetséges — EF Core paraméteres query
- `IsArchived=false` filter — archivált tenantok nem kereshetők fel
- Az endpoint `.AllowAnonymous()` + `.ExcludeFromDescription()` — nincs Swagger dokumentáció

## Kockázatok / kérdések

Nincs blokkoló. A `SetEmailHash` metódus jelenleg szabályos Regex validációval ellenőriz — ha a PartnerTier különleges hash formátumot igényel (pl. uppercase), ez könnyen módosítható.
