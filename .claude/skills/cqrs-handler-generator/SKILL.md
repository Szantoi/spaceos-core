# CQRS Handler Generator Skill

> .NET CQRS Query és Command handler-ek automatikus generálása (MediatR + Ardalis.Result pattern).

## Mikor használd

- Új Query handler kell (pl. GetOrderStatus, GetOrders)
- Új Command handler kell (pl. CreateOrder, WithdrawComplaint)
- Batch handler generálás (5+ handler egyszerre)
- Boilerplate kód csökkentése (90% időmegtakarítás)

## Használat

### Query Handler Generálás

```bash
/opt/spaceos/scripts/codegen/generate-handler.sh GetOrderStatus \
  --type query \
  --module Procurement \
  --repository IProcurementRepository \
  --aggregate Order \
  --properties '[{"name":"OrderId","type":"Guid"}]'
```

**Generált fájlok (3):**
```
Queries/GetOrderStatus/GetOrderStatusQuery.cs
Queries/GetOrderStatus/GetOrderStatusQueryHandler.cs
Queries/GetOrderStatus/GetOrderStatusResponse.cs
```

### Command Handler Generálás

```bash
/opt/spaceos/scripts/codegen/generate-handler.sh WithdrawComplaint \
  --type command \
  --module Procurement \
  --repository IComplaintRepository \
  --aggregate Complaint \
  --properties '[
    {"name":"ComplaintId","type":"Guid"},
    {"name":"Reason","type":"string"}
  ]'
```

**Generált fájlok (2):**
```
Commands/WithdrawComplaint/WithdrawComplaintCommand.cs
Commands/WithdrawComplaint/WithdrawComplaintCommandHandler.cs
```

## Paraméterek

| Paraméter | Kötelező | Leírás |
|-----------|----------|--------|
| `name` | Igen | Handler neve (pl. GetOrderStatus) |
| `--type` | Igen | `query` vagy `command` |
| `--module` | Igen | Modul neve (pl. Procurement, Joinery) |
| `--repository` | Igen | Repository interface (pl. IProcurementRepository) |
| `--aggregate` | Igen | Aggregate root neve (pl. Order, Complaint) |
| `--properties` | Nem | JSON array property-kkel |

## Properties JSON Formátum

```json
[
  {"name": "OrderId", "type": "Guid"},
  {"name": "CustomerId", "type": "Guid", "nullable": true},
  {"name": "Notes", "type": "string"}
]
```

**Támogatott típusok:** `Guid`, `string`, `int`, `decimal`, `bool`, `DateTime`

## Generált Kód Példa

### Query.cs
```csharp
using Ardalis.Result;
using MediatR;

namespace SpaceOS.Modules.Procurement.Application.Queries.GetOrderStatus;

public sealed record GetOrderStatusQuery(
    Guid OrderId
) : IRequest<Result<GetOrderStatusResponse>>;
```

### QueryHandler.cs
```csharp
using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Procurement.Domain.Interfaces;

namespace SpaceOS.Modules.Procurement.Application.Queries.GetOrderStatus;

public sealed class GetOrderStatusQueryHandler
    : IRequestHandler<GetOrderStatusQuery, Result<GetOrderStatusResponse>>
{
    private readonly IProcurementRepository _repository;

    public GetOrderStatusQueryHandler(IProcurementRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<GetOrderStatusResponse>> Handle(
        GetOrderStatusQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Implement query logic
        var order = await _repository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
            return Result.NotFound($"Order {request.OrderId} not found");

        return new GetOrderStatusResponse
        {
            // TODO: Map properties
        };
    }
}
```

## Batch Generálás (Conductor + Haiku)

A Conductor párhuzamosan generálhat több handler-t:

```yaml
parallel_dispatch:
  - terminal: backend
    model: haiku
    prompt: "generate_handler GetOrderStatus --type query ..."
  - terminal: backend
    model: haiku
    prompt: "generate_handler GetOrders --type query ..."
  - terminal: backend
    model: haiku
    prompt: "generate_handler CreateOrder --type command ..."
```

**ROI:** 5 handler × 20 perc = 100 perc → 5 handler × 2 perc = 10 perc (**90% megtakarítás**)

## Referencia

- **ADR-051:** `/opt/spaceos/docs/architecture/decisions/ADR-051-cqrs-handler-generator.md`
- **Script:** `/opt/spaceos/scripts/codegen/generate-handler.sh`
- **Dokumentáció:** `/opt/spaceos/docs/knowledge/patterns/CODE_GENERATOR_CATALOGUE.md`
