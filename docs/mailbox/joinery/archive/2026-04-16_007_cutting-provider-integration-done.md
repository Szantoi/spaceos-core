---
id: MSG-J007-DONE
from: joinery
to: root
type: done
status: READ
processed: 2026-04-16
---

## Összefoglaló

ICuttingProvider integráció implementálva a SubmitDoorOrderCommandHandler-ben.

Módosított/létrehozott fájlok:
- `NuGet.Config` — LocalCutting forrás hozzáadva
- `SpaceOS.Modules.Joinery.Application/SpaceOS.Modules.Joinery.Application.csproj` — SpaceOS.Modules.Cutting.Contracts 1.0.0 + Microsoft.Extensions.Logging.Abstractions 8.0.2 csomagok
- `SpaceOS.Modules.Joinery.Infrastructure/Cutting/CuttingProviderStub.cs` — Soft Launch stub implementáció
- `SpaceOS.Modules.Joinery.Infrastructure/DependencyInjection.cs` — ICuttingProvider → CuttingProviderStub regisztráció
- `SpaceOS.Modules.Joinery.Application/Orders/Commands/SubmitDoorOrder/SubmitDoorOrderCommandHandler.cs` — ICuttingProvider + ILogger injektálás, BuildCuttingSheet() helper, graceful degradation try/catch
- `SpaceOS.Modules.Joinery.Tests/Handlers/SubmitDoorOrderHandlerTests.cs` — új mock-ok hozzáadva (ICuttingProvider, ILogger)
- `SpaceOS.Modules.Joinery.Tests/Handlers/SubmitDoorOrderOutboxTests.cs` — új mock-ok hozzáadva
- `SpaceOS.Modules.Joinery.Tests/Security/ApiSecurityTests.cs` — SubmitHandler instantiation javítva
- `SpaceOS.Modules.Joinery.Tests/Handlers/CuttingIntegrationTests.cs` — 11 új teszt

## Tesztek

**214 teszt fut, mind zöld.** (volt: 202, új: +12)

CuttingIntegrationTests — 11 teszt:
- Handle_ValidOrder_CallsCuttingProviderOnce
- Handle_ValidOrder_PassesCorrectOrderId
- Handle_ValidOrder_PassesCorrectLineCount
- Handle_ValidOrder_MapsItemDimensionsCorrectly
- Handle_ValidOrder_SetsCanRotateFalse
- Handle_CuttingProviderThrows_OrderSubmitStillSucceeds
- Handle_CuttingProviderThrows_WarningIsLogged
- Handle_CuttingProviderReturnsId_OrderSubmitSucceeds
- Handle_OrderWithMultipleItems_CuttingSheetHasAllLines
- Handle_ValidOrder_CuttingSheetHasTenantId
- Handle_OrderNotFound_CuttingProviderNeverCalled
- Handle_ValidOrder_CuttingSheetMaterialTypeIsDoor (bónusz)

## Security review

- ICuttingProvider hiba nem töri meg a submit flowt — graceful degradation LogWarning-gal
- CuttingSheetDto-ba TenantId átadva (multi-tenant izoláció)
- BuildCuttingSheet() statikus pure helper — nincs I/O vagy side effect
- Stub implementáció nem cache-el, nem tárol állapotot

## Kockázatok / kérdések

Nincsenek. A stub Q3-ban cserélhető HttpAdapter-re a CuttingProviderStub-ot felváltva, DI regisztráció egyetlen sor csere.
