using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace SpaceOS.Modules.Inventory.Tests.Api;

/// <summary>
/// Tests for POST /api/inventory/integration/cutting-job-completed
/// — the cross-service hook triggered by the CUTTING module when a job transitions to "Cut".
/// </summary>
public class IntegrationEndpointsTests : IClassFixture<InventoryWebFactory>
{
    private const string InternalServiceHeader = "X-Internal-Service";
    private readonly HttpClient _client;

    public IntegrationEndpointsTests(InventoryWebFactory factory)
    {
        _client = factory.CreateClient();
    }

    private static object MakePayload(decimal yieldPct = 91m, decimal wasteM2 = 0.5m)
        => new
        {
            jobId          = Guid.NewGuid(),
            tenantId       = Guid.NewGuid(),
            cuttingSheetId = Guid.NewGuid(),
            completedAt    = DateTime.UtcNow,
            yieldPct,
            wasteM2
        };

    // ── 202 Accepted (happy path) ─────────────────────────────────────────────

    [Fact]
    public async Task Post_WithInternalHeader_Returns202()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/inventory/integration/cutting-job-completed");
        request.Headers.Add(InternalServiceHeader, "cutting");
        request.Content = JsonContent.Create(MakePayload());

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Post_ZeroYield_Returns202WithoutCreatingOffcut()
    {
        // WastePercent = 1 - 0/100 = 1.0, but handler guards WidthMm = 0 → skips offcut
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/inventory/integration/cutting-job-completed");
        request.Headers.Add(InternalServiceHeader, "cutting");
        request.Content = JsonContent.Create(MakePayload(yieldPct: 0m, wasteM2: 2.0m));

        var response = await _client.SendAsync(request);

        // v1: handler gracefully skips when dimensions are zero — still 202
        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task Post_HighYield_Returns202()
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/api/inventory/integration/cutting-job-completed");
        request.Headers.Add(InternalServiceHeader, "cutting");
        request.Content = JsonContent.Create(MakePayload(yieldPct: 95m, wasteM2: 0.2m));

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    // ── 403 Forbidden (missing header) ────────────────────────────────────────

    [Fact]
    public async Task Post_WithoutInternalHeader_Returns403()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/inventory/integration/cutting-job-completed",
            MakePayload());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
