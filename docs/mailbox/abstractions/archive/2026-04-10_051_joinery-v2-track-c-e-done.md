---
id: MSG-ABSTRACTIONS-051-DONE
from: abstractions
to: root
type: response
status: DONE
ref: MSG-ABSTRACTIONS-051
created: 2026-04-10
---

## Összefoglaló

MSG-ABSTRACTIONS-051 mindkét track-je implementálva, build + tesztek zöldek.

### Track C-Abs — POST /api/templates/{name}/calculate

**Új fájlok:**
- `Application/Calculation/Commands/CalculateByNameCommand.cs` — command, validator, handler

**Módosított fájlok:**
- `Application/IAbstractionsRepository.cs` — `GetTemplateByNameWithAllAsync(name, tenantId, ct)` metódus hozzáadva
- `Infrastructure/Persistence/AbstractionsRepository.cs` — implementáció: name+tenantId+IsActive+!IsArchived filter, LoadCollections pattern
- `Api/Endpoints/ProductTemplateEndpoints.cs` — új `/api/templates` csoport (ManufacturerOnly), `POST {name}/calculate` endpoint

Tenant check: ha a template.TenantId != jwtTenantId → Forbidden (dupla védelem RLS mellett).
404 ha template nem létezik (name+tenantId kombináció).

### Track E-Seed — FAF_T + FAF_Ü + BFAJ

**Új fájlok:**
- `Infrastructure/Seeding/FafUTemplateSeeder.cs` — FAF_Ü (7 slot, 12 connection, üveg-slot Virtual típussal)
- `Infrastructure/Seeding/BfajTemplateSeeder.cs` — BFAJ (5 slot, 8 connection)
- `Infrastructure/Seeding/CompositeTemplateSeeder.cs` — FAF_T + FAF_Ü + BFAJ sorban

**Módosított fájlok:**
- `Infrastructure/InfrastructureServiceExtensions.cs` — CompositeTemplateSeeder regisztrálva mint ITemplateSeeder

Mindhárom seeder idempotens (name check előtt).

## Tesztek

- Baseline: 61 teszt (mind zöld maradt)
- Új Track C-Abs tesztek (5 db): `Tests/Calculation/CalculateByNameTests.cs`
  - TemplateNotFound → NotFound
  - WrongTenant → Forbidden
  - ValidRequest → Success
  - ValidRequest → helyes DimensionInput továbbítva az engine-nek
  - TenantId → repo a helyes tenantId-vel hívva
- Új Track E-Seed tesztek (3 db): `Tests/Seeding/TemplateSeederValidationTests.cs`
  - FafT ITemplateValidator.Validate() → Success
  - FafU ITemplateValidator.Validate() → Success
  - Bfaj ITemplateValidator.Validate() → Success

**Végeredmény: 69/69 teszt zöld, 0 error, 0 warning**

## Security review

- **ManufacturerOnly policy**: az új `/api/templates` csoport RequireAuthorization("ManufacturerOnly") alatt — OK
- **RLS**: az új endpoint a meglévő AbstractionsRepository-t használja, ahol a TenantSessionInterceptor SET app.tenant_id-t injektál — OK
- **Tenant check**: GetTemplateByNameWithAllAsync(name, tenantId) + handler szintű template.TenantId != request.TenantId ellenőrzés — OK
- **DAG cycle detection**: nem érintett (nem graph mutation) — N/A
- **FileReference**: nem érintett — N/A
- **OWASP**: name útvonal paraméter csak string, nincs SQL injection (EF paraméteres lekérdezés) — OK

## Kockázatok / kérdések

Nincs. A glass slot "Virtual" componentType-ot kapott, mert a domain csak a ComponentSlot.Create whitelist-jén lévő típusokat fogadja el — "Glass" nem szerepelt benne. Ha szükséges, a whitelist bővíthető domain szinten.
