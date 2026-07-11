// SpaceOS.Kernel.Tests/AuditLog/VerifyChainWormStorageTests.cs

using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Application.AuditLog.Queries;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.Services;
using Xunit;

namespace SpaceOS.Kernel.Tests.AuditLog;

/// <summary>
/// Tests verifying SEC-P3B-05: VerifyChain endpoint returns HTTP 200 (not 500)
/// when WORM proof storage is unavailable, and reports <c>WormStorageAvailable: false</c>.
/// </summary>
public sealed class VerifyChainWormStorageTests
{
    private const string GenesisHash = "aabbccddaabbccddaabbccddaabbccddaabbccddaabbccddaabbccddaabbccdd";

    private readonly Mock<IAuditEventRepository>   _repository        = new();
    private readonly Mock<IExternalAuditSink>       _sink              = new();
    private readonly Mock<IGenesisHashProvider>     _genesisProvider   = new();
    private readonly Mock<IProofStorageService>     _proofStorage      = new();

    private VerifyChainQueryHandler BuildSut() => new(
        _repository.Object,
        _sink.Object,
        _genesisProvider.Object,
        _proofStorage.Object);

    public VerifyChainWormStorageTests()
    {
        _genesisProvider
            .Setup(g => g.GetGenesisHashAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenesisHash);

        _sink
            .Setup(s => s.ReadHashesAsync(
                It.IsAny<Guid>(), It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ExternalAuditHashRecord>().ToList().AsReadOnly() as IReadOnlyList<ExternalAuditHashRecord>);

        _repository
            .Setup(r => r.GetChainAsync(It.IsAny<Guid>(), It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuditEvent>().AsReadOnly() as IReadOnlyList<AuditEvent>);

        _proofStorage.Setup(p => p.ProviderName).Returns("s3");
    }

    // ── WORM unavailable — returns 200 with flag set ──────────────────────────

    [Fact]
    public async Task Handle_WormStorageUnavailable_ReturnsOkStatus()
    {
        // Arrange
        _proofStorage
            .Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut   = BuildSut();
        var query = new VerifyChainQuery(Guid.NewGuid(), null, null);

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert: not a failure result — endpoint returns 200
        Assert.Equal(ResultStatus.Ok, result.Status);
    }

    [Fact]
    public async Task Handle_WormStorageUnavailable_SetsWormStorageAvailableFalse()
    {
        // Arrange
        _proofStorage
            .Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut   = BuildSut();
        var query = new VerifyChainQuery(Guid.NewGuid(), null, null);

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.Value.WormStorageAvailable);
    }

    [Fact]
    public async Task Handle_WormStorageUnavailable_SetsDiagnosticMessage()
    {
        // Arrange
        _proofStorage
            .Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut   = BuildSut();
        var query = new VerifyChainQuery(Guid.NewGuid(), null, null);

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.NotNull(result.Value.DiagnosticMessage);
        Assert.NotEmpty(result.Value.DiagnosticMessage!);
    }

    // ── WORM check throws — returns 200 (not 500) ────────────────────────────

    [Fact]
    public async Task Handle_WormStorageThrows_ReturnsOkStatus()
    {
        // Arrange
        _proofStorage
            .Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("S3 connection timed out."));

        var sut   = BuildSut();
        var query = new VerifyChainQuery(Guid.NewGuid(), null, null);

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.False(result.Value.WormStorageAvailable);
    }

    // ── WORM available — flag is true ─────────────────────────────────────────

    [Fact]
    public async Task Handle_WormStorageAvailable_SetsWormStorageAvailableTrue()
    {
        // Arrange
        _proofStorage
            .Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut   = BuildSut();
        var query = new VerifyChainQuery(Guid.NewGuid(), null, null);

        // Act
        var result = await sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.Value.WormStorageAvailable);
    }
}
