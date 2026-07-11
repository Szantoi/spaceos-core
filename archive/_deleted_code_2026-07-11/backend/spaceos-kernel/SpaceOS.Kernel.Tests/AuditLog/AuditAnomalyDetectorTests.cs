// SpaceOS.Kernel.Tests/AuditLog/AuditAnomalyDetectorTests.cs

using Ardalis.Specification;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Kernel.Application.AuditLog.Anomaly;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.AuditLog;
using Xunit;

namespace SpaceOS.Kernel.Tests.AuditLog;

/// <summary>Unit tests for <see cref="AuditAnomalyDetector"/>.</summary>
public sealed class AuditAnomalyDetectorTests
{
    private const string GenesisHash = "aabbccddaabbccddaabbccddaabbccddaabbccddaabbccddaabbccddaabbccdd";

    private readonly Mock<IAuditEventRepository> _repository = new();
    private readonly Mock<IAlertService> _alertService = new();
    private readonly Mock<IGenesisHashProvider> _genesisHashProvider = new();
    private readonly AuditAnomalyDetector _sut;

    public AuditAnomalyDetectorTests()
    {
        _genesisHashProvider
            .Setup(g => g.GetGenesisHashAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenesisHash);

        _alertService
            .Setup(a => a.SendAlertAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Default: no events in chain, non-zero recent events, count below burst threshold
        _repository
            .Setup(r => r.GetChainAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuditEvent>().AsReadOnly() as IReadOnlyList<AuditEvent>);

        _repository
            .Setup(r => r.CountAsync(
                It.IsAny<ISpecification<AuditEvent>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        _sut = new AuditAnomalyDetector(
            _repository.Object,
            _alertService.Object,
            _genesisHashProvider.Object,
            NullLogger<AuditAnomalyDetector>.Instance);
    }

    // ── AuditGap alert ────────────────────────────────────────────────────────

    [Fact]
    public async Task DetectAnomaliesAsync_NoRecentEvents_SendsAuditGapAlert()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Both specs (AuditGap window and BurstClosedDone window) return 0.
        // This is fine for the test — the AuditGap check fires because count == 0.
        _repository
            .Setup(r => r.CountAsync(
                It.IsAny<ISpecification<AuditEvent>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _sut.DetectAnomaliesAsync(tenantId, CancellationToken.None);

        // Assert — AuditGap alert must have been sent exactly once
        _alertService.Verify(
            a => a.SendAlertAsync(
                "AuditGap",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DetectAnomaliesAsync_NormalActivityInWindow_DoesNotSendAuditGapAlert()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // count > 0 means events exist in the gap window → no AuditGap alert
        _repository
            .Setup(r => r.CountAsync(
                It.IsAny<ISpecification<AuditEvent>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        // Act
        await _sut.DetectAnomaliesAsync(tenantId, CancellationToken.None);

        // Assert
        _alertService.Verify(
            a => a.SendAlertAsync(
                "AuditGap",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── BurstClosedDone alert ─────────────────────────────────────────────────

    [Fact]
    public async Task DetectAnomaliesAsync_ClosedDoneCountExceedsThreshold_SendsBurstClosedDoneAlert()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // First call (AuditGap spec) returns 1 to suppress the AuditGap alert.
        // Second call (BurstClosedDone spec) returns threshold + 1.
        var callIndex = 0;
        _repository
            .Setup(r => r.CountAsync(
                It.IsAny<ISpecification<AuditEvent>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callIndex++;
                return callIndex == 1
                    ? 1                                          // AuditGap window: has events
                    : AuditAnomalyDetector.BurstClosedDoneThreshold + 1; // burst window: over threshold
            });

        // Act
        await _sut.DetectAnomaliesAsync(tenantId, CancellationToken.None);

        // Assert
        _alertService.Verify(
            a => a.SendAlertAsync(
                "BurstClosedDone",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task DetectAnomaliesAsync_ClosedDoneCountAtThreshold_DoesNotSendBurstAlert()
    {
        // Arrange — exactly at the threshold (not exceeding it) must not trigger
        var tenantId = Guid.NewGuid();
        var callIndex = 0;

        _repository
            .Setup(r => r.CountAsync(
                It.IsAny<ISpecification<AuditEvent>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                callIndex++;
                return callIndex == 1
                    ? 1
                    : AuditAnomalyDetector.BurstClosedDoneThreshold; // exactly at threshold, not above
            });

        // Act
        await _sut.DetectAnomaliesAsync(tenantId, CancellationToken.None);

        // Assert
        _alertService.Verify(
            a => a.SendAlertAsync(
                "BurstClosedDone",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ── ChainBreak alert ──────────────────────────────────────────────────────

    [Fact]
    public async Task DetectAnomaliesAsync_ChainBreakDetected_SendsChainBreakAlert()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // Suppress AuditGap and BurstClosedDone
        _repository
            .Setup(r => r.CountAsync(
                It.IsAny<ISpecification<AuditEvent>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Chain has one event whose PreviousHash does NOT match genesis
        const string wrongPreviousHash = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeef";
        const string stateHash         = "cafecafecafecafecafecafecafecafecafecafecafecafecafecafecafecafe";

        var brokenEvent = AuditEvent.Create(
            tenantId, "TenantCreatedEvent", Guid.NewGuid(), "{}", stateHash, wrongPreviousHash);

        var chain = new List<AuditEvent> { brokenEvent }.AsReadOnly() as IReadOnlyList<AuditEvent>;

        _repository
            .Setup(r => r.GetChainAsync(
                tenantId,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(chain!);

        // Act
        await _sut.DetectAnomaliesAsync(tenantId, CancellationToken.None);

        // Assert
        _alertService.Verify(
            a => a.SendAlertAsync(
                "ChainBreak",
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // ── Normal activity — no alerts ───────────────────────────────────────────

    [Fact]
    public async Task DetectAnomaliesAsync_NormalActivity_NoAlertsAreSent()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        // count > 0 for all windows, below burst threshold
        _repository
            .Setup(r => r.CountAsync(
                It.IsAny<ISpecification<AuditEvent>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Chain is empty → no chain-break check runs
        _repository
            .Setup(r => r.GetChainAsync(
                tenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuditEvent>().AsReadOnly() as IReadOnlyList<AuditEvent>);

        // Act
        await _sut.DetectAnomaliesAsync(tenantId, CancellationToken.None);

        // Assert
        _alertService.Verify(
            a => a.SendAlertAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
