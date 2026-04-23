# Joinery API + Tests — Done

**Date:** 2026-04-09
**Sprint:** Phase 3C+
**Status:** COMPLETE

## Summary

`SpaceOS.Modules.Joinery.Api` and `SpaceOS.Modules.Joinery.Tests` projects implemented and verified.

## API Project

- **Program.cs** — JWT Bearer auth, ManufacturerOnly policy, Application + Infrastructure DI wired
- **Endpoints/DoorOrderEndpoints.cs** — Full Minimal API route group `/api/orders` with 7 endpoints
- **Endpoints/Requests.cs** — `CreateDoorOrderRequest`, `AddDoorItemRequest` request records
- **appsettings.json** — Jwt + ConnectionStrings config stubs
- **SpaceOS.Modules.Joinery.Api.csproj** — Added `Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11` + `FluentValidation 12.1.1`
- Default `Program.cs` (WeatherForecast) replaced

## Tests Project

**28 unit tests, all passing.**

### Domain/DoorOrderTests.cs (13 tests)
- Create_WithValidData_ReturnsSuccess
- Create_WithEmptyFlowEpicId_ReturnsInvalid
- Create_WithEmptyProjectId_ReturnsInvalid
- Create_RaisesOrderCreatedEvent
- AddItem_WhenDraft_Succeeds
- AddItem_WhenSubmitted_ReturnsError (BE-04)
- AddItem_WhenAt500Items_ReturnsError (SEC-07)
- AddItem_RaisesItemAddedEvent
- Submit_WhenDraftWithItems_Succeeds
- Submit_WhenDraftWithNoItems_ReturnsError
- Submit_WhenAlreadySubmitted_ReturnsError
- Submit_RaisesOrderSubmittedEvent
- PopDomainEvents_ClearsEvents

### Domain/DoorDimensionsTests.cs (5 tests)
- Create_WithValidDimensions_Succeeds
- Create_WithZeroWidth_ReturnsInvalid
- Create_WithDoorWiderThanWallOpening_ReturnsInvalid
- Create_WithDoorTallerThanWallOpening_ReturnsInvalid
- Create_WithNegativeThickness_ReturnsInvalid

### Calculation/DoorCalculationServiceTests.cs (10 tests)
- CalculateCuttingList_FAF_T_AppliesBkmWidthFixed
- CalculateCuttingList_AppliesCuttingOversize
- CalculateCuttingList_MultipleItems_CorrectQuantity
- CalculateCuttingList_EmptyRules_ReturnsEmpty
- CalculateCuttingList_FiltersByDoorType
- CalculateCuttingList_RoundsToOneDecimal
- CalculateFinishedDimensions_ReturnsCorrectCount
- CalculateCuttingList_IsDeterministic_SameInputSameOutput
- CalculateCuttingList_WidthMultiplierFactor_Applied
- CalculateCuttingList_LengthMultiplierFactor_Applied

## Build Results

```
dotnet build → Build succeeded. 0 Warning(s) 0 Error(s)
dotnet test  → Passed! Failed: 0, Passed: 28, Skipped: 0, Total: 28
```

## Compliance

- ConfigureAwait(false) on all async endpoint calls
- Tests in Arrange/Act/Assert structure
- Calculation tests use direct DoorCalculationService (pure function, no mock framework)
- SEC-05: cutting-list endpoint sets Cache-Control: no-store
- SEC-04: all endpoints behind ManufacturerOnly policy
- BE-04: AddItem blocked on non-Draft status (tested)
- SEC-07: MaxItems=500 enforced (tested)
- Default files removed: WeatherForecast (Program.cs replaced), UnitTest1.cs deleted
