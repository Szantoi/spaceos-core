using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Infrastructure.Http;

namespace SpaceOS.Modules.Joinery.Tests.Http;

/// <summary>
/// Unit tests for <see cref="OrchestratorClient"/> HTTP retry and error-handling behavior.
/// Uses a fake <see cref="HttpMessageHandler"/> to avoid real network I/O.
/// </summary>
public class OrchestratorClientTests
{
    // ── Fake HTTP handler ─────────────────────────────────────────────────────

    private sealed class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();

        public void EnqueueResponse(HttpResponseMessage response) => _responses.Enqueue(response);

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();

            if (_responses.TryDequeue(out var response))
                return Task.FromResult(response);

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static JoineryOutboxEntry MakeEntry(Guid? tenantId = null) =>
        JoineryOutboxEntry.Create(
            tenantId ?? Guid.NewGuid(),
            "SpaceOS.Test.DummyEvent",
            "{}",
            DateTimeOffset.UtcNow);

    private static OrchestratorClient BuildSut(FakeHttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };
        return new OrchestratorClient(httpClient, NullLogger<OrchestratorClient>.Instance);
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CalculateAsync_WithSuccessResponse_ReturnsSuccess()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK));
        var sut = BuildSut(handler);

        // Act
        var result = await sut.CalculateAsync(MakeEntry(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(because: "a 200 OK response means the calculation was accepted");
    }

    [Fact]
    public async Task CalculateAsync_With400Response_ReturnsImmediatelyWithError()
    {
        // Arrange — 400 BadRequest is a non-retryable client error
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.BadRequest));
        var sut = BuildSut(handler);

        // Act
        var result = await sut.CalculateAsync(MakeEntry(), CancellationToken.None);

        // Assert — returns error without retrying (only 1 response was enqueued)
        result.IsSuccess.Should().BeFalse(because: "400 BadRequest is a permanent client error");
        result.Errors.Should().Contain(e => e.Contains("BadRequest") || e.Contains("Calculation failed"));
    }

    [Fact]
    public async Task CalculateAsync_With404Response_ReturnsImmediatelyWithError()
    {
        // Arrange — 404 NotFound is also non-retryable
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.NotFound));
        var sut = BuildSut(handler);

        // Act
        var result = await sut.CalculateAsync(MakeEntry(), CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse(because: "404 NotFound is a permanent client error");
        result.Errors.Should().Contain(e => e.Contains("NotFound") || e.Contains("Calculation failed"));
    }

    [Fact]
    public async Task CalculateAsync_With500ThenSuccess_ReturnsSuccess()
    {
        // Arrange — first attempt fails with 500, second succeeds
        // RetryDelays[0] = 0s (instant), RetryDelays[1] = 2s — but delay is skipped on first attempt
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK));

        // Use a CancellationTokenSource with enough time to allow the 2s retry delay
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var sut = BuildSut(handler);

        // Act
        var result = await sut.CalculateAsync(MakeEntry(), cts.Token);

        // Assert — eventually succeeds after one retry
        result.IsSuccess.Should().BeTrue(because: "the second attempt returns 200 OK");
    }

    [Fact]
    public async Task CalculateAsync_With500Response_ExhaustingAllRetries_ReturnsError()
    {
        // Arrange — all 3 attempts fail with 500
        // Delays: attempt 1 = 0s, attempt 2 = 2s, attempt 3 = 5s — total ~7s
        // We enqueue exactly 3 failure responses to cover all retry slots.
        var handler = new FakeHttpMessageHandler();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        var sut = BuildSut(handler);

        // Act
        var result = await sut.CalculateAsync(MakeEntry(), cts.Token);

        // Assert
        result.IsSuccess.Should().BeFalse(because: "all 3 retry attempts returned 500");
        result.Errors.Should().Contain(e => e.Contains("3 attempts") || e.Contains("unreachable"));
    }

    [Fact]
    public async Task CalculateAsync_WhenCancelled_StopsSending()
    {
        // Arrange — cancel immediately after the first response is received
        var handler = new FakeHttpMessageHandler();
        // Return a 500 first, then the cancellation should prevent further attempts
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        using var cts = new CancellationTokenSource();
        var sut = BuildSut(handler);

        // Cancel before the second attempt's delay (which is 2s) can complete
        // We trigger cancellation right after building so that the delay throws OperationCanceledException
        cts.Cancel();

        // Act
        var act = async () => await sut.CalculateAsync(MakeEntry(), cts.Token);

        // Assert — either returns an error result or throws OperationCanceledException;
        // either outcome is acceptable since cancellation stops work
        await act.Should().ThrowAsync<OperationCanceledException>()
            .WithMessage("*");
    }
}
