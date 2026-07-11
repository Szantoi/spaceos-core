// SpaceOS.Infrastructure/Outbox/CrossModuleOutboxDispatcher.cs

using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Outbox;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Infrastructure.Outbox;

/// <summary>
/// Dispatches processed <see cref="OutboxMessage"/> records to all active cross-module
/// subscribers via HTTP POST. Each delivery is HMAC-signed so recipients can verify the
/// message originated from this Kernel node.
/// </summary>
/// <remarks>
/// Retry policy: up to 3 attempts with exponential back-off (1 s → 2 s → 4 s).
/// If all attempts fail the exception propagates to the caller, which marks the message
/// as <see cref="OutboxStatus.Failed"/> and saves the attempt counter.
/// </remarks>
internal sealed class CrossModuleOutboxDispatcher : ICrossModuleOutboxDispatcher
{
    private static readonly TimeSpan[] RetryDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(4),
    ];

    private const string HttpClientName = "cross-module";
    private const string HmacConfigKey  = "CrossModule:HmacKey";
    private const string DefaultHmacKey = "dev-hmac-key";

    private readonly IModuleSubscriptionRepository _subscriptions;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CrossModuleOutboxDispatcher> _logger;

    /// <summary>
    /// Initialises a new <see cref="CrossModuleOutboxDispatcher"/>.
    /// </summary>
    /// <param name="subscriptions">Repository for resolving active module subscriptions.</param>
    /// <param name="httpClientFactory">Factory used to create the named HTTP client.</param>
    /// <param name="configuration">Application configuration for HMAC key resolution.</param>
    /// <param name="logger">Structured logger.</param>
    public CrossModuleOutboxDispatcher(
        IModuleSubscriptionRepository subscriptions,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<CrossModuleOutboxDispatcher> logger)
    {
        ArgumentNullException.ThrowIfNull(subscriptions);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(logger);

        _subscriptions    = subscriptions;
        _httpClientFactory = httpClientFactory;
        _configuration    = configuration;
        _logger           = logger;
    }

    /// <inheritdoc/>
    public async Task DispatchAsync(OutboxMessage message, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(message);

        var eventType = message.EventType ?? message.Type;

        var subscribers = await _subscriptions
            .GetActiveByEventTypeAsync(eventType, ct)
            .ConfigureAwait(false);

        if (subscribers.Count == 0)
            return;

        _logger.LogDebug(
            "CrossModuleOutboxDispatcher: dispatching message {MessageId} (type={EventType}) to {SubscriberCount} subscriber(s).",
            message.Id, eventType, subscribers.Count);

        foreach (var subscriber in subscribers)
        {
            await PostToSubscriberAsync(subscriber, message, ct).ConfigureAwait(false);
        }
    }

    private async Task PostToSubscriberAsync(
        ModuleSubscription subscriber,
        OutboxMessage message,
        CancellationToken ct)
    {
        var body = SerializePayload(message);
        var hmacKey = _configuration[HmacConfigKey] ?? DefaultHmacKey;
        var hmacHex = ComputeHmac(body, hmacKey);

        Exception? lastException = null;

        for (var attempt = 0; attempt < RetryDelays.Length; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                var httpClient = _httpClientFactory.CreateClient(HttpClientName);

                using var request = new HttpRequestMessage(HttpMethod.Post, subscriber.InboxEndpoint);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                request.Headers.TryAddWithoutValidation("X-SpaceOS-Internal", "true");
                request.Headers.TryAddWithoutValidation("X-SpaceOS-Hmac", hmacHex);

                using var response = await httpClient
                    .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct)
                    .ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                _logger.LogInformation(
                    "CrossModuleOutboxDispatcher: delivered message {MessageId} to {Subscriber} at {Endpoint} (attempt {Attempt}).",
                    message.Id, subscriber.SubscriberModule, subscriber.InboxEndpoint, attempt + 1);

                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;

                _logger.LogWarning(
                    ex,
                    "CrossModuleOutboxDispatcher: attempt {Attempt}/{MaxAttempts} failed for message {MessageId} → {Endpoint}.",
                    attempt + 1, RetryDelays.Length, message.Id, subscriber.InboxEndpoint);

                if (attempt < RetryDelays.Length - 1)
                {
                    await Task.Delay(RetryDelays[attempt], ct).ConfigureAwait(false);
                }
            }
        }

        // All retries exhausted — propagate to caller so the message is marked Failed.
        throw lastException!;
    }

    private static string SerializePayload(OutboxMessage message)
    {
        // Minimal projection — subscribers receive only the fields they need for routing.
        var envelope = new
        {
            id        = message.Id,
            type      = message.Type,
            payload   = message.Payload,
            tenantId  = message.TenantId,
            createdAt = message.CreatedAt,
        };

        return JsonSerializer.Serialize(envelope);
    }

    private static string ComputeHmac(string body, string key)
    {
        var keyBytes  = Encoding.UTF8.GetBytes(key);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var hashBytes = HMACSHA256.HashData(keyBytes, bodyBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
