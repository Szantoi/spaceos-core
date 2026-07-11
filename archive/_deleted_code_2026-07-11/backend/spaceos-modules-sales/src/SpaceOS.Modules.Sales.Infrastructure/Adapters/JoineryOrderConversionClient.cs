using System.Net;
using System.Net.Http.Json;
using Ardalis.Result;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Sales.Abstractions.Contracts;
using SpaceOS.Modules.Sales.Abstractions.Ports;

namespace SpaceOS.Modules.Sales.Infrastructure.Adapters;

/// <summary>
/// HTTP adapter that calls the Joinery internal order-creation endpoint.
/// SEC-S-01: TenantId header and body field both come from the same source (req.TenantId).
/// SEC-S-10: raw response body is not propagated in error messages (PII risk).
/// </summary>
internal sealed class JoineryOrderConversionClient(
    HttpClient http,
    IConfiguration cfg,
    ILogger<JoineryOrderConversionClient> log) : IOrderConversionPort
{
    /// <inheritdoc/>
    public async Task<Result<OrderConversionResult>> CreateOrderFromQuoteAsync(
        OrderConversionRequest req, CancellationToken ct)
    {
        using var msg = new HttpRequestMessage(
            HttpMethod.Post,
            "http://127.0.0.1:5002/joinery/internal/orders/from-quote");

        msg.Headers.TryAddWithoutValidation(
            "X-SpaceOS-Internal", cfg["SpaceOS:InternalSecret"]);

        // SEC-S-01: header TenantId == body TenantId (single source)
        msg.Headers.TryAddWithoutValidation(
            "X-SpaceOS-TenantId", req.TenantId.ToString());

        msg.Headers.TryAddWithoutValidation(
            "Idempotency-Key", req.QuoteId.ToString("N"));

        msg.Content = JsonContent.Create(req);

        var resp = await http.SendAsync(msg, ct).ConfigureAwait(false);

        if (resp.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK)
        {
            var body = await resp.Content
                .ReadFromJsonAsync<OrderConversionResult>(cancellationToken: ct)
                .ConfigureAwait(false);
            return Result.Success(body!);
        }

        // SEC-S-10: discard response body — only log status code
        log.LogWarning("Joinery returned HTTP {StatusCode} for quote {QuoteId}",
            (int)resp.StatusCode, req.QuoteId);

        return Result.Error($"Joinery returned HTTP {(int)resp.StatusCode}.");
    }
}
