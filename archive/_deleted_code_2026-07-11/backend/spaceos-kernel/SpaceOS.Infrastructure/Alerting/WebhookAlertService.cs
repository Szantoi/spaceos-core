// SpaceOS.Infrastructure/Alerting/WebhookAlertService.cs

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SpaceOS.Kernel.Application.Common;
using System.Net.Http.Json;

namespace SpaceOS.Infrastructure.Alerting;

/// <summary>
/// Production stub implementation of <see cref="IAlertService"/> that posts alert payloads
/// to a configured webhook URL. If <c>Alerting:WebhookUrl</c> is absent or empty, the alert
/// is logged as a warning instead. HTTP errors are swallowed and logged — a delivery failure
/// must never propagate to the calling anomaly detector.
/// </summary>
internal sealed class WebhookAlertService : IAlertService
{
    private const string WebhookUrlKey = "Alerting:WebhookUrl";

    private readonly ILogger<WebhookAlertService> _logger;
    private readonly string? _webhookUrl;
    private readonly IHttpClientFactory _httpClientFactory;

    /// <summary>Initialises a new <see cref="WebhookAlertService"/>.</summary>
    /// <param name="configuration">Application configuration used to read the webhook URL.</param>
    /// <param name="httpClientFactory">Factory used to create outbound HTTP clients.</param>
    /// <param name="logger">Logger for warnings and delivery errors.</param>
    public WebhookAlertService(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<WebhookAlertService> logger)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(logger);

        _webhookUrl        = configuration[WebhookUrlKey];
        _httpClientFactory = httpClientFactory;
        _logger            = logger;

        if (string.IsNullOrWhiteSpace(_webhookUrl))
        {
            _logger.LogWarning(
                "WebhookAlertService: '{Key}' is not configured. Alerts will be logged only.",
                WebhookUrlKey);
        }
    }

    /// <inheritdoc/>
    public async Task SendAlertAsync(string alertType, string message, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_webhookUrl))
        {
            _logger.LogWarning("[ALERT:{AlertType}] {Message} (webhook not configured)", alertType, message);
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(nameof(WebhookAlertService));
            var payload = new { alertType, message, timestamp = DateTimeOffset.UtcNow };
            using var response = await client
                .PostAsJsonAsync(_webhookUrl, payload, ct)
                .ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "WebhookAlertService: delivery failed for alert '{AlertType}'. HTTP {StatusCode}.",
                    alertType, (int)response.StatusCode);
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "WebhookAlertService: exception delivering alert '{AlertType}'.", alertType);
        }
    }
}
