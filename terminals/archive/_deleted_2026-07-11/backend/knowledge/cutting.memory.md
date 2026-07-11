# Cutting Domain Memory

> Automatikusan betöltődik ha a feladat Cutting/Szabászat modulhoz kapcsolódik.

## Domain Scope

- **Modul:** `spaceos-modules-cutting`
- **Felelősség:** Lapszabászat, nesting, CNC optimalizáció, anyagfelhasználás
- **Tech stack:** .NET 8, PostgreSQL, CAD/CAM integráció

## Aktív Patterns

### 1. Public Quote Request (Q3 Track A)
```csharp
// Anonymous endpoint - nincs auth
[AllowAnonymous]
app.MapPost("/api/public/cutting/quote-request", async (
    PublicQuoteRequestDto request,
    ICuttingRepository repo) =>
{
    var quoteRequest = PublicQuoteRequest.Create(
        request.CustomerEmail,
        request.Material,
        request.Dimensions,
        request.Quantity,
        request.Urgency
    );

    await repo.AddPublicQuoteRequestAsync(quoteRequest);
    return Results.Created($"/api/public/cutting/quote-status/{quoteRequest.Id}",
        new { quoteRequest.Id, quoteRequest.TrackingToken });
});
```

### 2. Tracking Token Pattern
```csharp
// Publikus tracking - nem GUID, hanem user-friendly token
public string TrackingToken { get; private set; } = GenerateTrackingToken();

private static string GenerateTrackingToken()
{
    // Format: CUT-XXXXXX (6 alphanumeric)
    return $"CUT-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";
}
```

### 3. Urgency-Based Deadline
```csharp
public DateOnly CalculateDeadline(string urgency) => urgency.ToLower() switch
{
    "express" => DateOnly.FromDateTime(DateTime.UtcNow.AddBusinessDays(1)),
    "standard" => DateOnly.FromDateTime(DateTime.UtcNow.AddBusinessDays(2)),
    _ => DateOnly.FromDateTime(DateTime.UtcNow.AddBusinessDays(3))
};
```

## API Endpoints

| Endpoint | Method | Auth | Státusz |
|----------|--------|------|---------|
| `/api/public/cutting/quote-request` | POST | Anonymous | ✅ Live |
| `/api/public/cutting/quote-status/{id}` | GET | Anonymous | ✅ Live |
| `/api/public/cutting/quotes/track/{token}` | GET | Anonymous | 🔄 Phase 2 |
| `/api/public/cutting/quotes/track/{token}/accept` | POST | Anonymous | 🔄 Phase 2 |
| `/api/cutting/materials` | GET | JWT | ✅ Live |
| `/api/cutting/calculate` | POST | JWT | ✅ Live |

## Legutóbbi Tanulságok

- **Rate limiting** kell a public endpointokra (50 req/hour per IP)
- **FluentValidation** email, dimensions, quantity validáláshoz
- **Audit logging** minden public request-re (IP, timestamp)
- **CORS** beállítás a frontend domain-ekhez

## Test Coverage

- Unit tests: `CreatePublicQuoteRequestCommandHandlerTests` (10 teszt)
- Integration tests: `PublicQuoteRequestEndpointTests` (7 teszt)
- Total: 17 teszt a Quote Request feature-re

## Kapcsolódó Fájlok

- `src/SpaceOS.Modules.Cutting/` - Domain
- `src/SpaceOS.Modules.Cutting.Application/` - CQRS handlers
- `src/SpaceOS.Modules.Cutting.Api/` - Endpoints
- `tests/SpaceOS.Modules.Cutting.Tests/` - Tests
