using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace SpaceOS.Modules.Joinery.Tests.Api;

/// <summary>
/// Integration tests for POST /joinery/internal/orders/from-quote.
/// Each test creates its own isolated JoineryWebFactory (in-memory DB).
/// </summary>
public sealed class InternalOrderConversionApiTests
{
    // Matches the default set in JoineryWebFactory.ConfigureAppConfiguration
    private const string TestSecret = "joinery-integration-test-internal-secret";
    private static readonly Guid TenantId = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

    private static HttpClient MakeClient(
        JoineryWebFactory factory,
        string? secret = TestSecret,
        string? tenantId = null)
    {
        var client = factory.CreateClient();
        if (secret is not null)
            client.DefaultRequestHeaders.Add("X-SpaceOS-Internal", secret);
        if (tenantId is not null)
            client.DefaultRequestHeaders.Add("X-SpaceOS-TenantId", tenantId);
        return client;
    }

    private static object ValidBody(Guid? tenantId = null, Guid? quoteId = null) => new
    {
        quoteId = quoteId ?? Guid.NewGuid(),
        tenantId = tenantId ?? TenantId,
        customerId = Guid.NewGuid(),
        linkedTenantId = (Guid?)null,
        currency = "HUF",
        totalNet = 10000m,
        totalVat = 2700m,
        totalGross = 12700m,
        lines = new[]
        {
            new
            {
                sourceTemplateId = (Guid?)null,
                description = "Standard door",
                quantity = 1m,
                unitPriceNet = 10000m,
                vatRate = 0.27m,
                discountPercent = (decimal?)null,
                sortOrder = 0
            }
        },
        contentHash = "test-content-hash-abc123"
    };

    // ── 1. Missing secret header → 401 ──────────────────────────────────────

    [Fact]
    public async Task PostFromQuote_MissingSecretHeader_Returns401()
    {
        // Arrange
        using var factory = new JoineryWebFactory();
        var client = MakeClient(factory, secret: null, tenantId: TenantId.ToString());

        // Act
        var resp = await client.PostAsJsonAsync("/joinery/internal/orders/from-quote", ValidBody());

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── 2. Wrong secret → 401 ────────────────────────────────────────────────

    [Fact]
    public async Task PostFromQuote_InvalidSecretHeader_Returns401()
    {
        // Arrange
        using var factory = new JoineryWebFactory();
        var client = MakeClient(factory, secret: "wrong-secret", tenantId: TenantId.ToString());

        // Act
        var resp = await client.PostAsJsonAsync("/joinery/internal/orders/from-quote", ValidBody());

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── 3. Missing tenant ID header → 400 ───────────────────────────────────

    [Fact]
    public async Task PostFromQuote_MissingTenantIdHeader_Returns400()
    {
        // Arrange
        using var factory = new JoineryWebFactory();
        var client = MakeClient(factory, secret: TestSecret, tenantId: null);

        // Act
        var resp = await client.PostAsJsonAsync("/joinery/internal/orders/from-quote", ValidBody());

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── 4. TenantId header != body TenantId → 400 ───────────────────────────

    [Fact]
    public async Task PostFromQuote_TenantIdMismatch_Returns400()
    {
        // Arrange
        using var factory = new JoineryWebFactory();
        var differentTenantId = Guid.NewGuid();
        var client = MakeClient(factory, secret: TestSecret, tenantId: differentTenantId.ToString());

        // Act — header has differentTenantId but body has TenantId
        var resp = await client.PostAsJsonAsync(
            "/joinery/internal/orders/from-quote", ValidBody(tenantId: TenantId));

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── 5. Valid request → 200 with orderId ──────────────────────────────────

    [Fact]
    public async Task PostFromQuote_ValidRequest_Returns200WithOrderId()
    {
        // Arrange
        using var factory = new JoineryWebFactory();
        var client = MakeClient(factory, secret: TestSecret, tenantId: TenantId.ToString());

        // Act
        var resp = await client.PostAsJsonAsync(
            "/joinery/internal/orders/from-quote", ValidBody(tenantId: TenantId));

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<OrderConversionResponse>();
        body!.OrderId.Should().NotBeEmpty();
        body.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromMinutes(1));
    }

    // ── 6. Idempotent replay → same orderId ──────────────────────────────────

    [Fact]
    public async Task PostFromQuote_IdempotentReplay_Returns200WithSameOrderId()
    {
        // Arrange
        using var factory = new JoineryWebFactory();
        var quoteId = Guid.NewGuid();
        var client = MakeClient(factory, secret: TestSecret, tenantId: TenantId.ToString());

        // Act — send same quote twice
        var resp1 = await client.PostAsJsonAsync(
            "/joinery/internal/orders/from-quote", ValidBody(TenantId, quoteId));
        var resp2 = await client.PostAsJsonAsync(
            "/joinery/internal/orders/from-quote", ValidBody(TenantId, quoteId));

        // Assert
        resp1.StatusCode.Should().Be(HttpStatusCode.OK);
        resp2.StatusCode.Should().Be(HttpStatusCode.OK);

        var body1 = await resp1.Content.ReadFromJsonAsync<OrderConversionResponse>();
        var body2 = await resp2.Content.ReadFromJsonAsync<OrderConversionResponse>();
        body1!.OrderId.Should().Be(body2!.OrderId);
    }

    // ── 7. Empty lines → 400 ────────────────────────────────────────────────

    [Fact]
    public async Task PostFromQuote_EmptyLines_Returns400()
    {
        // Arrange
        using var factory = new JoineryWebFactory();
        var client = MakeClient(factory, secret: TestSecret, tenantId: TenantId.ToString());

        var body = new
        {
            quoteId = Guid.NewGuid(),
            tenantId = TenantId,
            customerId = Guid.NewGuid(),
            linkedTenantId = (Guid?)null,
            currency = "HUF",
            totalNet = 10000m,
            totalVat = 2700m,
            totalGross = 12700m,
            lines = Array.Empty<object>(),
            contentHash = "hash"
        };

        // Act
        var resp = await client.PostAsJsonAsync("/joinery/internal/orders/from-quote", body);

        // Assert
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private sealed record OrderConversionResponse(Guid OrderId, DateTimeOffset CreatedAt);
}
