using Ardalis.Result;
using SpaceOS.Modules.Sales.Abstractions.Contracts;

namespace SpaceOS.Modules.Sales.Abstractions.Ports;

/// <summary>
/// Cross-module write port for converting a Quote into a Joinery Order (ADR-039, D-11).
/// Implementation calls <c>POST http://127.0.0.1:5002/joinery/internal/orders/from-quote</c>.
/// </summary>
public interface IOrderConversionPort
{
    /// <summary>Creates a Joinery order from an accepted quote. Idempotent on QuoteId.</summary>
    Task<Result<OrderConversionResult>> CreateOrderFromQuoteAsync(
        OrderConversionRequest request, CancellationToken ct);
}
