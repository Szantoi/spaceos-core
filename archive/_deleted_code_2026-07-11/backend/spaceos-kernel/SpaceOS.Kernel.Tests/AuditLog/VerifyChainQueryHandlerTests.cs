// SpaceOS.Kernel.Tests/AuditLog/VerifyChainQueryHandlerTests.cs

using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Application.AuditLog.Queries;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.Services;
using Xunit;

namespace SpaceOS.Kernel.Tests.AuditLog;

/// <summary>Unit tests for <see cref="VerifyChainQueryHandler"/>.</summary>
public sealed class VerifyChainQueryHandlerTests
{
    private const string GenesisHash = "aabbccddaabbccddaabbccddaabbccddaabbccddaabbccddaabbccddaabbccdd";

    private readonly Mock<IAuditEventRepository> _repository = new();
    private readonly Mock<IExternalAuditSink> _sink = new();
    private readonly Mock<IGenesisHashProvider> _genesisHashProvider = new();
    private readonly Mock<IProofStorageService> _proofStorage = new();
    private readonly VerifyChainQueryHandler _sut;

    public VerifyChainQueryHandlerTests()
    {
        _genesisHashProvider
            .Setup(g => g.GetGenesisHashAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(GenesisHash);

        _sink
            .Setup(s => s.ReadHashesAsync(
                It.IsAny<Guid>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<DateTimeOffset?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<ExternalAuditHashRecord>().ToList().AsReadOnly() as IReadOnlyList<ExternalAuditHashRecord>);

        // Default: WORM storage is available
        _proofStorage
            .Setup(p => p.IsAvailableAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _proofStorage.Setup(p => p.ProviderName).Returns("local");

        _sut = new VerifyChainQueryHandler(
            _repository.Object,
            _sink.Object,
            _genesisHashProvider.Object,
            _proofStorage.Object);
    }

    // ── Empty chain ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_EmptyChain_ReturnsIsValidTrue_AndZeroRecords()
    {
        // Arrange
        var tenantId = Guid.NewGuid();

        _repository
            .Setup(r => r.GetChainAsync(tenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AuditEvent>().AsReadOnly() as IReadOnlyList<AuditEvent>);

        var query = new VerifyChainQuery(tenantId, null, null);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.True(result.Value.IsValid);
        Assert.Equal(0, result.Value.TotalRecordsChecked);
        Assert.Null(result.Value.FirstBrokenAt);
        Assert.True(result.Value.ExternalSinkMatch);
    }

    // ── Valid chain ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ValidChain_ReturnsIsValidTrue()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Build two linked events: first event's PreviousHash = genesis, second's = first StateHash
        var firstHash = "1111111111111111111111111111111111111111111111111111111111111111";
        var secondHash = "2222222222222222222222222222222222222222222222222222222222222222";

        var first = AuditEvent.Create(
            tenantId, "TenantCreatedEvent", Guid.NewGuid(), "{}", firstHash, GenesisHash);

        // Force OccurredAt ordering via Reflection so the chain is deterministic
        SetOccurredAt(first, now.AddSeconds(-2));

        var second = AuditEvent.Create(
            tenantId, "FacilityCreatedEvent", Guid.NewGuid(), "{}", secondHash, firstHash);

        SetOccurredAt(second, now.AddSeconds(-1));

        var chain = new List<AuditEvent> { first, second }.AsReadOnly() as IReadOnlyList<AuditEvent>;

        _repository
            .Setup(r => r.GetChainAsync(tenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chain!);

        // Sink returns matching records
        var sinkRecords = new List<ExternalAuditHashRecord>
        {
            new(first.OccurredAt, tenantId, "TenantCreatedEvent", GenesisHash, firstHash),
            new(second.OccurredAt, tenantId, "FacilityCreatedEvent", firstHash, secondHash),
        }.AsReadOnly() as IReadOnlyList<ExternalAuditHashRecord>;

        _sink
            .Setup(s => s.ReadHashesAsync(tenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sinkRecords!);

        var query = new VerifyChainQuery(tenantId, null, null);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.True(result.Value.IsValid);
        Assert.Equal(2, result.Value.TotalRecordsChecked);
        Assert.Null(result.Value.FirstBrokenAt);
        Assert.True(result.Value.ExternalSinkMatch);
    }

    // ── Broken chain ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_BrokenChain_WrongPreviousHash_ReturnsIsValidFalse_AndFirstBrokenAtSet()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var brokenAt = DateTimeOffset.UtcNow;

        // First event has wrong PreviousHash — does not match genesis
        const string wrongPreviousHash = "deadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeefdeadbeef";
        const string stateHash         = "cafecafecafecafecafecafecafecafecafecafecafecafecafecafecafecafe";

        var brokenEvent = AuditEvent.Create(
            tenantId, "TenantCreatedEvent", Guid.NewGuid(), "{}", stateHash, wrongPreviousHash);

        SetOccurredAt(brokenEvent, brokenAt);

        var chain = new List<AuditEvent> { brokenEvent }.AsReadOnly() as IReadOnlyList<AuditEvent>;

        _repository
            .Setup(r => r.GetChainAsync(tenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chain!);

        var query = new VerifyChainQuery(tenantId, null, null);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.False(result.Value.IsValid);
        Assert.Equal(1, result.Value.TotalRecordsChecked);
        Assert.NotNull(result.Value.FirstBrokenAt);
        Assert.Equal(brokenAt, result.Value.FirstBrokenAt!.Value);
    }

    // ── External sink mismatch ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_SinkHashMismatch_ReturnsExternalSinkMatchFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        const string correctHash  = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        const string tamperedHash = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";

        var ev = AuditEvent.Create(
            tenantId, "TenantCreatedEvent", Guid.NewGuid(), "{}", correctHash, GenesisHash);

        SetOccurredAt(ev, now);

        var chain = new List<AuditEvent> { ev }.AsReadOnly() as IReadOnlyList<AuditEvent>;

        _repository
            .Setup(r => r.GetChainAsync(tenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chain!);

        // Sink returns a different StateHash for the same timestamp
        var sinkRecords = new List<ExternalAuditHashRecord>
        {
            new(ev.OccurredAt, tenantId, "TenantCreatedEvent", GenesisHash, tamperedHash),
        }.AsReadOnly() as IReadOnlyList<ExternalAuditHashRecord>;

        _sink
            .Setup(s => s.ReadHashesAsync(tenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sinkRecords!);

        var query = new VerifyChainQuery(tenantId, null, null);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.False(result.Value.ExternalSinkMatch);
    }

    // ── External sink empty ───────────────────────────────────────────────────

    [Fact]
    public async Task Handle_SinkEmpty_WhenChainHasEvents_ReturnsExternalSinkMatchFalse()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        const string hash = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

        var ev = AuditEvent.Create(
            tenantId, "TenantCreatedEvent", Guid.NewGuid(), "{}", hash, GenesisHash);

        SetOccurredAt(ev, now);

        var chain = new List<AuditEvent> { ev }.AsReadOnly() as IReadOnlyList<AuditEvent>;

        _repository
            .Setup(r => r.GetChainAsync(tenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(chain!);

        // Sink returns empty list (stub / unavailable)
        _sink
            .Setup(s => s.ReadHashesAsync(tenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ExternalAuditHashRecord>().AsReadOnly() as IReadOnlyList<ExternalAuditHashRecord>);

        var query = new VerifyChainQuery(tenantId, null, null);

        // Act
        var result = await _sut.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.Value.ExternalSinkMatch);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Forces <see cref="AuditEvent.OccurredAt"/> to a specific value so tests can build
    /// deterministic chains without depending on wall-clock ordering.
    /// </summary>
    private static void SetOccurredAt(AuditEvent auditEvent, DateTimeOffset value)
    {
        var prop = typeof(AuditEvent).GetProperty(nameof(AuditEvent.OccurredAt));
        prop!.SetValue(auditEvent, value);
    }
}
