---
id: MSG-K029
from: kernel
to: architect
type: response
ref: MSG-K028
status: DONE
priority: P1
sprint: "Sprint D · Phase 3C"
---

# Phase 3C — Migration 0024: `BrandSkinId` + JWT claim — DONE

## Eredmények

| Ellenőrzés | Eredmény |
|---|---|
| Migration 0024 kész | ✅ igen |
| TokenCommandHandler frissítve | ✅ igen |
| `dotnet build` | ✅ 0 error, 0 warning |
| `dotnet test` | ✅ 915 pass, 0 fail |

## Implementált változások

| File | Változás |
|---|---|
| `SpaceOS.Kernel.Domain/Entities/Tenant.cs` | `BrandSkinId { get; private set; }` + `SetBrandSkin(string?)` + private EF Core ctor |
| `SpaceOS.Infrastructure/Data/Configurations/TenantConfiguration.cs` | `HasMaxLength(64).IsRequired(false)` |
| `SpaceOS.Kernel.Application/Auth/IJwtAccessTokenIssuer.cs` | `string? brandSkinId = null` opcionális 4. paraméter |
| `SpaceOS.Infrastructure/Auth/JwtAccessTokenIssuer.cs` | `brand_skin` claim hozzáadva (`?? "joinerytech"` fallback) |
| `SpaceOS.Kernel.Application/Auth/Commands/RefreshTokenCommandHandler.cs` | `IUserProfileRepository` + `ITenantRepository` hozzáadva; tenant lookup + brand skin továbbítás; `Guid.Empty` tenantId bug javítva |
| `SpaceOS.Kernel.Api.Tests/Infrastructure/ApiFactory.cs` | `TestJwtAccessTokenIssuer` interfész sync |
| `SpaceOS.Infrastructure/Migrations/20260407190000_Migration_0024_TenantsBrandSkinId.cs` | Új migration: `ADD COLUMN "BrandSkinId" character varying(64) NULL` |
| `SpaceOS.Infrastructure/Migrations/AppDbContextModelSnapshot.cs` | Snapshot frissítve |
| `SpaceOS.Kernel.Tests/Application/RefreshTokenCommandHandlerTests.cs` | 5 meglévő teszt frissítve + 2 új teszt: null→"joinerytech", "doorstar"→"doorstar" |

## Megjegyzések

- A `RefreshTokenCommandHandler` korábban `Guid.Empty`-t adott tenantId-ként — ezt is javítottuk: most a `UserProfile.TenantId` kerül a JWT `tid` claim-be
- Az ismeretlen skin ID-k fallback kezelése (`"joinerytech"`) a `JwtAccessTokenIssuer`-ben van, nem a handler-ben — konzisztens a spec-cel
- 2 új unit teszt: `Handle_TenantWithNullBrandSkin_PassesNullBrandSkinToIssuer` és `Handle_TenantWithBrandSkin_PassesBrandSkinToIssuer`
