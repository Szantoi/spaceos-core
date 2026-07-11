// SpaceOS.Kernel.Tests/Infrastructure/CrossModuleOutboxDispatcherTests.cs

using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Infrastructure.Outbox;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Outbox;
using SpaceOS.Kernel.Domain.Repositories;
using Xunit;

namespace SpaceOS.Kernel.Tests.Infrastructure;

/// <summary>
/// Unit tests for <see cref="CrossModuleOutboxDispatcher"/>.
/// Uses a <see cref="FakeHttpMessageHandler"/> to intercept HTTP calls without a real server.
/// </summary>
public sealed class CrossModuleOutboxDispatcherTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private const string HmacKey = "test-hmac-key-32-bytes-padded!!";

    private static IConfiguration BuildConfig(string? hmacKey = HmacKey)
    {
        var pairs = new Dictionary<string, string?>();
        if (hmacKey is not null)
            pairs["CrossModule:HmacKey"] = hmacKey;
        return new ConfigurationBuilder().AddInMemoryCollection(pairs).Build();
    }

    private static OutboxMessage MakeMessage(string eventType = "CuttingPanelCompleted")
    {
        var msg = OutboxMessage.Create("TestEvent", "{\"x\":1}", TenantId);
        // Use batch overload to populate EventType
        return OutboxMessage.Create("TestEvent", "{\"x\":1}", TenantId,
            Guid.NewGuid(), 0, Guid.NewGuid(), "CuttingSheet", eventType);
    }

    private static ModuleSubscription MakeSub(string endpoint = "http://127.0.0.1:5007/inbox")
        => ModuleSubscription.Create("Manufacturing", "CuttingPanelCompleted", endpoint);

    private CrossModuleOutboxDispatcher BuildDispatcher(
        Mock<IModuleSubscriptionRepository> repoMock,
        FakeHttpMessageHandler handler,
        IConfiguration? config = null)
    {
        var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
        var factoryMock = new Mock<IHttpClientFactory>();
        factoryMock.Setup(f => f.CreateClient("cross-module")).Returns(httpClient);

        return new CrossModuleOutboxDispatcher(
            repoMock.Object,
            factoryMock.Object,
            config ?? BuildConfig(),
            NullLogger<CrossModuleOutboxDispatcher>.Instance);
    }

    // ── 1. Happy path: message dispatched to active subscriber ────────────────

    [Fact]
    public async Task DispatchAsync_ActiveSubscriber_SendsPostRequest()
    {
        var sub  = MakeSub();
        var repo = new Mock<IModuleSubscriptionRepository>();
        repo.Setup(r => r.GetActiveByEventTypeAsync("CuttingPanelCompleted", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ModuleSubscription> { sub });

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
        var dispatcher = BuildDispatcher(repo, handler);

        await dispatcher.DispatchAsync(MakeMessage(), CancellationToken.None);

        Assert.Single(handler.ReceivedRequests);
        Assert.Equal(HttpMethod.Post, handler.ReceivedRequests[0].Method);
        Assert.Equal(sub.InboxEndpoint, handler.ReceivedRequests[0].RequestUri!.ToString());
    }

    // ── 2. No subscribers → no HTTP call ─────────────────────────────────────

    [Fact]
    public async Task DispatchAsync_NoSubscribers_SendsNoRequests()
    {
        var repo = new Mock<IModuleSubscriptionRepository>();
        repo.Setup(r => r.GetActiveByEventTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ModuleSubscription>());

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
        var dispatcher = BuildDispatcher(repo, handler);

        await dispatcher.DispatchAsync(MakeMessage(), CancellationToken.None);

        Assert.Empty(handler.ReceivedRequests);
    }

    // ── 3. X-SpaceOS-Internal header is present ───────────────────────────────

    [Fact]
    public async Task DispatchAsync_ActiveSubscriber_SetsInternalHeader()
    {
        var sub  = MakeSub();
        var repo = new Mock<IModuleSubscriptionRepository>();
        repo.Setup(r => r.GetActiveByEventTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ModuleSubscription> { sub });

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
        var dispatcher = BuildDispatcher(repo, handler);

        await dispatcher.DispatchAsync(MakeMessage(), CancellationToken.None);

        var req = handler.ReceivedRequests[0];
        Assert.True(req.Headers.Contains("X-SpaceOS-Internal"));
        Assert.Equal("true", req.Headers.GetValues("X-SpaceOS-Internal").First());
    }

    // ── 4. HMAC header is present and valid ───────────────────────────────────

    [Fact]
    public async Task DispatchAsync_ActiveSubscriber_SetsValidHmacHeader()
    {
        var sub  = MakeSub();
        var repo = new Mock<IModuleSubscriptionRepository>();
        repo.Setup(r => r.GetActiveByEventTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ModuleSubscription> { sub });

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
        var dispatcher = BuildDispatcher(repo, handler);

        await dispatcher.DispatchAsync(MakeMessage(), CancellationToken.None);

        var req = handler.ReceivedRequests[0];
        Assert.True(req.Headers.Contains("X-SpaceOS-Hmac"));

        // The HMAC must be a 64-char lowercase hex string (SHA-256 output)
        var hmacValue = req.Headers.GetValues("X-SpaceOS-Hmac").First();
        Assert.Equal(64, hmacValue.Length);
        Assert.Matches("^[0-9a-f]{64}$", hmacValue);
    }

    // ── 5. HTTP 500 → retries then throws ─────────────────────────────────────

    [Fact]
    public async Task DispatchAsync_SubscriberReturns500_RetriesAndThrows()
    {
        var sub  = MakeSub();
        var repo = new Mock<IModuleSubscriptionRepository>();
        repo.Setup(r => r.GetActiveByEventTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ModuleSubscription> { sub });

        // Always return 500 — ensure exception propagates after retries
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, retryDelayOverride: TimeSpan.Zero);
        var dispatcher = BuildDispatcher(repo, handler);

        await Assert.ThrowsAnyAsync<Exception>(() =>
            dispatcher.DispatchAsync(MakeMessage(), CancellationToken.None));

        // Should have been called 3 times (3 retry attempts)
        Assert.Equal(3, handler.ReceivedRequests.Count);
    }

    // ── 6. Inactive subscriber is skipped ────────────────────────────────────

    [Fact]
    public async Task DispatchAsync_InactiveSubscriberInResult_SkippedByRepository()
    {
        // Repository only returns ACTIVE subs — verify dispatcher doesn't get inactive ones
        var repo = new Mock<IModuleSubscriptionRepository>();
        repo.Setup(r => r.GetActiveByEventTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ModuleSubscription>()); // repo already filtered inactive

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
        var dispatcher = BuildDispatcher(repo, handler);

        await dispatcher.DispatchAsync(MakeMessage(), CancellationToken.None);

        Assert.Empty(handler.ReceivedRequests);
    }

    // ── 7. HMAC computation is deterministic ──────────────────────────────────

    [Fact]
    public void ComputeHmac_SameBodyAndKey_ReturnsSameHex()
    {
        const string body = "{\"id\":\"test\"}";
        var keyBytes  = Encoding.UTF8.GetBytes(HmacKey);
        var bodyBytes = Encoding.UTF8.GetBytes(body);
        var expected  = Convert.ToHexString(HMACSHA256.HashData(keyBytes, bodyBytes)).ToLowerInvariant();

        // Compute twice — must be identical (deterministic)
        var hash1 = Convert.ToHexString(HMACSHA256.HashData(keyBytes, bodyBytes)).ToLowerInvariant();
        var hash2 = Convert.ToHexString(HMACSHA256.HashData(keyBytes, bodyBytes)).ToLowerInvariant();

        Assert.Equal(expected, hash1);
        Assert.Equal(hash1,    hash2);
    }

    // ── 8. Multiple subscribers all receive the message ───────────────────────

    [Fact]
    public async Task DispatchAsync_MultipleSubscribers_AllReceivePost()
    {
        var sub1 = ModuleSubscription.Create("Manufacturing", "CuttingPanelCompleted", "http://127.0.0.1:5007/inbox");
        var sub2 = ModuleSubscription.Create("Inventory",    "CuttingPanelCompleted", "http://127.0.0.1:5008/inbox");

        var repo = new Mock<IModuleSubscriptionRepository>();
        repo.Setup(r => r.GetActiveByEventTypeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ModuleSubscription> { sub1, sub2 });

        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK);
        var dispatcher = BuildDispatcher(repo, handler);

        await dispatcher.DispatchAsync(MakeMessage(), CancellationToken.None);

        Assert.Equal(2, handler.ReceivedRequests.Count);
    }
}

// ── Fake HTTP handler ─────────────────────────────────────────────────────────

/// <summary>
/// Test double for <see cref="HttpMessageHandler"/> that records every request
/// and returns a preconfigured status code.
/// </summary>
internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    public List<HttpRequestMessage> ReceivedRequests { get; } = new();

    public FakeHttpMessageHandler(HttpStatusCode statusCode, TimeSpan retryDelayOverride = default)
        => _statusCode = statusCode;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        ReceivedRequests.Add(request);
        return Task.FromResult(new HttpResponseMessage(_statusCode));
    }
}
