# Joinery Application Layer — Done

**Date:** 2026-04-09
**Task:** SpaceOS.Modules.Joinery.Application projekt implementálása

## Build eredmény

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

## Létrehozott fájlok

### Commands + Handlers + Validators
- `Orders/Commands/CreateDoorOrder/CreateDoorOrderCommand.cs`
- `Orders/Commands/CreateDoorOrder/CreateDoorOrderCommandHandler.cs`
- `Orders/Commands/CreateDoorOrder/CreateDoorOrderCommandValidator.cs`
- `Orders/Commands/AddDoorItem/AddDoorItemCommand.cs`
- `Orders/Commands/AddDoorItem/AddDoorItemCommandHandler.cs`
- `Orders/Commands/AddDoorItem/AddDoorItemCommandValidator.cs`
- `Orders/Commands/CalculateDoorOrder/CalculateDoorOrderCommand.cs`
- `Orders/Commands/CalculateDoorOrder/CalculateDoorOrderCommandHandler.cs`
- `Orders/Commands/SubmitDoorOrder/SubmitDoorOrderCommand.cs`
- `Orders/Commands/SubmitDoorOrder/SubmitDoorOrderCommandHandler.cs`

### Queries + Handlers
- `Orders/Queries/GetCuttingList/GetCuttingListQuery.cs`
- `Orders/Queries/GetCuttingList/GetCuttingListQueryHandler.cs`
- `Orders/Queries/GetCuttingList/CuttingListResponse.cs`
- `Orders/Queries/GetDoorOrder/GetDoorOrderQuery.cs`
- `Orders/Queries/GetDoorOrder/GetDoorOrderQueryHandler.cs`
- `Orders/Queries/ListDoorOrders/ListDoorOrdersQuery.cs`
- `Orders/Queries/ListDoorOrders/ListDoorOrdersQueryHandler.cs`
- `Orders/Queries/ListDoorOrders/PagedList.cs`

### DTOs + Interfaces
- `Orders/DTOs/DoorOrderDto.cs`
- `Orders/Repositories/IDoorOrderRepository.cs`
- `Orders/Repositories/IDoorRulesRepository.cs`
- `Seeding/IDataSeeder.cs`

### Infrastructure
- `Common/DomainEventNotification.cs` — wrapper IDomainEvent → INotification
- `Common/DomainEventDispatcher.cs` — reflection-alapú dispatch segédosztály
- `DependencyInjection.cs` — AddApplication() extension method

## Technikai döntések

1. **IDomainEvent → INotification bridge**: A Domain réteg `IDomainEvent`-jei nem implementálják az MediatR `INotification`-t (dependency rule: Domain-nek nincs MediatR ref). `DomainEventNotification<TDomainEvent>` wrapper + `DomainEventDispatcher.DispatchAsync()` oldja meg reflection-nel.

2. **IDoorRulesRepository**: Új repository interface a `DoorTypeRule`, `PartDimensionRule[]`, `GlobalConstant` lekérdezéséhez — ezeket az `IDoorCalculationService` igényli, implementálni az Infrastructure rétegben kell.

3. **FluentValidation.DependencyInjectionExtensions**: A csproj-ba fel kellett venni az `AddValidatorsFromAssembly()` extension method miatt.

4. **SEC-05**: `GetCuttingListQueryHandler` kommenttel jelölve — soha nem cache-elhető, mindig on-demand számítás.

5. **CancellationToken**: Minden handler `CancellationToken ct` névvel, `ConfigureAwait(false)` minden async callban.

## Következő lépés

Infrastructure projekt: `IDoorOrderRepository`, `IDoorRulesRepository`, `IDoorCalculationService` implementálása EF Core + PostgreSQL-lel.
