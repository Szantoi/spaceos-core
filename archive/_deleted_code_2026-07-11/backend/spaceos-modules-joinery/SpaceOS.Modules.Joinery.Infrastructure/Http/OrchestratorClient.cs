using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Ardalis.Result;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Infrastructure.Http;

/// <summary>
/// HTTP implementation of <see cref="IOrchestratorClient"/> that posts calculation
/// requests to the Orchestrator service with up to 3 attempts and per-attempt timeouts.
/// </summary>
public sealed class OrchestratorClient : IOrchestratorClient
{
    private static readonly TimeSpan[] RetryDelays =
        [TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5)];

    private readonly HttpClient _httpClient;
    private readonly ILogger<OrchestratorClient> _logger;

    public OrchestratorClient(HttpClient httpClient, ILogger<OrchestratorClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<Result<CalculationResponse>> CalculateAsync(JoineryOutboxEntry entry, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(entry);

        foreach (var (delay, attempt) in RetryDelays.Select((d, i) => (d, i + 1)))
        {
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, ct).ConfigureAwait(false);

            try
            {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                cts.CancelAfter(TimeSpan.FromSeconds(10));

                var payload = JsonSerializer.Deserialize<object>(entry.PayloadJson)!;
                var response = await _httpClient
                    .PostAsJsonAsync("/internal/abstractions/calculate", payload, cts.Token)
                    .ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                    return Result.Success(new CalculationResponse(true, null));

                if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
                    return Result.Error($"Calculation failed: {response.StatusCode}");

                // 5xx → retry
                _logger.LogWarning(
                    "Attempt {Attempt}: orchestrator returned {Status}",
                    attempt, response.StatusCode);
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                _logger.LogWarning("Attempt {Attempt}: timeout", attempt);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Attempt {Attempt}: HTTP error", attempt);
            }
        }

        return Result.Error("Orchestrator unreachable after 3 attempts");
    }
}
