---
id: MSG-KERNEL-070-DONE
from: kernel
to: architect
type: response
status: DONE
priority: high
ref: MSG-KERNEL-070
---

## Összefoglaló

MSG-KERNEL-070 — Ecosystem Actor Architecture v4 implementálva.

### Érintett fájlok

**Domain — új:**
- `SpaceOS.Kernel.Domain/Enums/TenantType.cs` — 6 aktor-típus enum
- `SpaceOS.Kernel.Domain/Enums/ModuleType.cs` — 9 modul enum
- `SpaceOS.Kernel.Domain/Services/IModuleRegistryService.cs` — domain service interfész
- `SpaceOS.Kernel.Domain/Services/ModuleRegistryService.cs` — statikus implementáció (SEC-02 defense-in-depth)
- `SpaceOS.Kernel.Domain/Services/ModuleValidationResult.cs` — lightweight result VO (Ardalis.Result nélkül)
- `SpaceOS.Kernel.Domain/Events/TenantModulesUpdatedEvent.cs` — új domain event
- `SpaceOS.Kernel.Domain/Specifications/AllTenantsByTypeSpec.cs` — TenantType filter spec
- `SpaceOS.Kernel.Domain/Specifications/AllTenantsByTypePagedSpec.cs` — paged variant

**Domain — módosított:**
- `SpaceOS.Kernel.Domain/Entities/Tenant.cs` — `TenantType` property, `Register()` factory, `UpdateEnabledModules()` method (backwards compat: `Create()` és `SetEnabledModules()` megtartva)

**Application — módosított:**
- `Tenants/TenantDto.cs` — `TenantType` + `EnabledModules` mezők hozzáadva
- `Tenants/Commands/CreateTenantCommand.cs` — `TenantType` + `EnabledModules` paraméterek
- `Tenants/Commands/CreateTenantCommandHandler.cs` — `IModuleRegistryService` injektálva, `Tenant.Register()` hívás
- `Tenants/Commands/CreateTenantCommandValidator.cs` — `IsInEnum()` validáció
- `Tenants/Queries/GetAllTenantsQuery.cs` — `TenantTypeFilter?` opcionális paraméter
- `Tenants/Queries/GetAllTenantsQueryHandler.cs` — filter ág + TenantDto projekció bővítve
- `Tenants/Queries/GetTenantByIdQueryHandler.cs` — TenantDto projekció bővítve

**Application — új:**
- `Tenants/Commands/UpdateTenantModulesCommand.cs`
- `Tenants/Commands/UpdateTenantModulesCommandHandler.cs`
- `Tenants/Commands/UpdateTenantModulesCommandValidator.cs`
- `ModuleRegistry/GetModuleRegistryQuery.cs` + `ModuleRegistryDto`
- `ModuleRegistry/GetModuleRegistryQueryHandler.cs`

**Infrastructure — módosított:**
- `Data/Configurations/TenantConfiguration.cs` — `TenantType` varchar(32) mapping (BE-02: explicit conversion)
- `DependencyInjection.cs` — `AddSingleton<IModuleRegistryService, ModuleRegistryService>()`
- `Migrations/AppDbContextModelSnapshot.cs` — `TenantType` + `_enabledModules` shadow property hozzáadva

**Infrastructure — új:**
- `Migrations/20260415090000_Migration_0029_EcosystemActorTypes.cs` — teljes migration SQL (10 lépés: column, constraints, triggers, seed)

**API — módosított:**
- `Endpoints/TenantEndpoints.cs` — `tenantType` query param (GET), TenantType parse (POST), `PUT /{id}/modules` endpoint
- `Program.cs` — `app.MapModuleRegistryEndpoints()` regisztráció

**API — új:**
- `Endpoints/ModuleRegistryEndpoints.cs` — `GET /api/module-registry/{tenantType}`

**Tesztek — módosított:**
- `Entities/TenantTests.cs` — 10 új teszt (Register, UpdateEnabledModules)
- `Application/CreateTenantCommandHandlerTests.cs` — IModuleRegistryService mock + 2 új teszt

**Tesztek — új:**
- `Domain/ModuleRegistryServiceTests.cs` — 14 teszt (6 TenantType × valid + invalid + GetAllowed + GetRequired)
- `Application/UpdateTenantModulesCommandHandlerTests.cs` — 3 teszt

## Tesztek

- Előző baseline: **1077 passing**
- Jelenlegi: **1104 passing** (910 unit + 101 integration + 93 API, 4 skipped)
- Új tesztek: **27 db**
- Eredmény: **0 failed, 0 skipped (új)**

## Security review

- **SEC-01**: `TenantType` immutability — DB trigger `TR_Tenants_ImmutableTenantType` (Migration 0029)
- **SEC-02**: EnabledModules per-type validation — DB trigger `TR_Tenants_ValidateModulesForType` + Application-layer `IModuleRegistryService` (defense-in-depth)
- **SEC-03**: `GET /api/tenants/by-type` endpoint NEM létezik — csak `TenantHandshakeAllowlist`-en keresztül lehetséges partner-keresés
- **Input validation**: `Enum.TryParse` a TenantType API-stringre, FluentValidation `IsInEnum()`
- **Authorization**: minden endpoint `RequireAuthorization("ReadPolicy"/"AdminPolicy")` védett
- **RLS**: nem érintett (TenantType nem RLS policy része)
- **No hardcoded secrets**: nincs

## Kockázatok / kérdések

Nincsenek. A implementáció az ADR-018, ADR-019 döntéseknek megfelelő.

Migration 0029 `suppressTransaction: true` — PostgreSQL DDL + trigger create tranzakción kívül futnak (konzisztens a 0025/0026 mintával).
