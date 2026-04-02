---
id: MSG-006
from: root
to: kernel
type: bug-report
priority: P1
status: DONE
created: 2026-04-01T12:30:00
---

## Tárgy

BUG — WorkStation status enum intként serializálódik, nem stringként

## Probléma

A `GET /api/facilities/:id/work-stations` válasza:
```json
{ "status": 0 }
```

A frontend stringet vár:
```json
{ "status": "Idle" }
```

## Elvárt megoldás

A JSON serializer adja vissza az enum értékeket stringként. ASP.NET Core-ban:

```csharp
builder.Services.Configure<JsonOptions>(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
```

Vagy a DTO-n:
```csharp
[JsonConverter(typeof(JsonStringEnumConverter))]
public WorkStationStatus Status { get; init; }
```

Ez valószínűleg minden enumot érint (WorkStationStatus, FsmState, TradeType).

## Várt válasz

Javítás + Kernel újraindítás + outbox status-update.
