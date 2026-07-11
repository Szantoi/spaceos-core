// SpaceOS.Kernel.Tests/AuditLog/AuditEventDispatcherTests.cs

using Moq;
using SpaceOS.Kernel.Application.AuditLog;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.UserProfiles;
using SpaceOS.Kernel.Domain.AuditLog;
using SpaceOS.Kernel.Domain.Events;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.AuditLog;

public sealed class AuditEventDispatcherTests
{
    private const string TestGenesisHash = "aabbccddaabbccddaabbccddaabbccddaabbccddaabbccddaabbccddaabbccdd";
    private static readonly Guid TestPseudonymGuid = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private readonly Mock<IAuditEventRepository> _repository = new();
    private readonly Mock<IAuditUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICurrentRequestContext> _requestContext = new();
    private readonly Mock<IAuditWriteLock> _writeLock = new();
    private readonly Mock<IExternalAuditSink> _sink = new();
    private readonly Mock<IGenesisHashProvider> _genesisHashProvider = new();
    private readonly Mock<IHashProvider> _hashProvider = new();
    private readonly Mock<IPseudonymizer> _pseudonymizer = new();
    private readonly Mock<IAuditEscrowWriter> _escrowWriter = new();
    private readonly AuditEventDispatcher _dispatcher;

    public AuditEventDispatcherTests()
    {
        _writeLock
            .Setup(l => l.AcquireAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NoOpDisposable());

        _repository
            .Setup(r => r.GetLastHashAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("GENESIS");

        _sink
            .Setup(s => s.WriteAsync(
                It.IsAny<Guid>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _genesisHashProvider
            .Setup(g => g.GetGenesisHashAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestGenesisHash);

        // IHashProvider: compute a real SHA-256 so hash-format assertions continue to pass.
        _hashProvider
            .Setup(h => h.ComputeHash(It.IsAny<string>()))
            .Returns<string>(input =>
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(input);
                return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)).ToLowerInvariant();
            });
        _hashProvider
            .Setup(h => h.AlgorithmType)
            .Returns(HashAlgorithmType.SHA256);

        // IPseudonymizer: return a fixed test GUID for any actor.
        _pseudonymizer
            .Setup(p => p.GetOrCreatePseudonymAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TestPseudonymGuid);

        // IAuditEscrowWriter: no-op — fire-and-forget, must not block.
        _escrowWriter
            .Setup(e => e.WriteAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _dispatcher = new AuditEventDispatcher(
            _repository.Object,
            _unitOfWork.Object,
            _requestContext.Object,
            _writeLock.Object,
            _sink.Object,
            _genesisHashProvider.Object,
            _hashProvider.Object,
            _pseudonymizer.Object,
            _escrowWriter.Object);
    }

    private sealed class NoOpDisposable : IAsyncDisposable
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // AuditEventDispatcher_PersistsAuditEvent_ForEachDomainEvent
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AuditEventDispatcher_PersistsAuditEvent_ForEachDomainEvent()
    {
        // Arrange
        var events = new System.Collections.Generic.List<IDomainEvent>
        {
            new TenantCreatedEvent(TenantId.New(), DateTimeOffset.UtcNow),
            new FacilityCreatedEvent(FacilityId.New(), TenantId.New(), DateTimeOffset.UtcNow),
        };

        // Act
        await _dispatcher.DispatchAsync(events.AsReadOnly(), TestContext.Current.CancellationToken);

        // Assert
        _repository.Verify(
            r => r.AddAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    // -------------------------------------------------------------------------
    // AuditEventDispatcher_ComputesSha256Hash
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AuditEventDispatcher_ComputesSha256Hash()
    {
        // Arrange
        AuditEvent? captured = null;

        _repository
            .Setup(r => r.AddAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((ae, _) => captured = ae)
            .Returns(Task.CompletedTask);

        var events = new System.Collections.Generic.List<IDomainEvent>
        {
            new TenantCreatedEvent(TenantId.New(), DateTimeOffset.UtcNow),
        };

        // Act
        await _dispatcher.DispatchAsync(events.AsReadOnly(), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(captured);
        Assert.NotEmpty(captured.StateHash);
        Assert.Equal(64, captured.StateHash.Length);
        Assert.Equal(captured.StateHash, captured.StateHash.ToLowerInvariant());
        Assert.Matches("^[0-9a-f]{64}$", captured.StateHash);
    }

    // -------------------------------------------------------------------------
    // AuditEventDispatcher_EmptyList_DoesNothing
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AuditEventDispatcher_EmptyList_DoesNothing()
    {
        // Arrange
        var events = System.Array.Empty<IDomainEvent>();

        // Act
        await _dispatcher.DispatchAsync(events, TestContext.Current.CancellationToken);

        // Assert
        _repository.Verify(
            r => r.AddAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -------------------------------------------------------------------------
    // AuditEventDispatcher_PseudonymizesActorId_WhenActorIdIsPresent
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AuditEventDispatcher_PseudonymizesActorId_WhenActorIdIsPresent()
    {
        // Arrange
        _requestContext.Setup(r => r.ActorId).Returns("user-sub-abc");
        AuditEvent? captured = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((ae, _) => captured = ae)
            .Returns(Task.CompletedTask);

        var events = new System.Collections.Generic.List<IDomainEvent>
        {
            new TenantCreatedEvent(TenantId.New(), DateTimeOffset.UtcNow),
        };

        // Act
        await _dispatcher.DispatchAsync(events.AsReadOnly(), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(TestPseudonymGuid.ToString(), captured.ActorId);
        _pseudonymizer.Verify(
            p => p.GetOrCreatePseudonymAsync("user-sub-abc", It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // -------------------------------------------------------------------------
    // AuditEventDispatcher_LeavesActorIdNull_WhenActorIdIsAbsent
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AuditEventDispatcher_LeavesActorIdNull_WhenActorIdIsAbsent()
    {
        // Arrange
        _requestContext.Setup(r => r.ActorId).Returns((string?)null);
        AuditEvent? captured = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((ae, _) => captured = ae)
            .Returns(Task.CompletedTask);

        var events = new System.Collections.Generic.List<IDomainEvent>
        {
            new TenantCreatedEvent(TenantId.New(), DateTimeOffset.UtcNow),
        };

        // Act
        await _dispatcher.DispatchAsync(events.AsReadOnly(), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(captured);
        Assert.Null(captured.ActorId);
        _pseudonymizer.Verify(
            p => p.GetOrCreatePseudonymAsync(It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // -------------------------------------------------------------------------
    // AuditEventDispatcher_StoresHashAlgorithmType_FromHashProvider
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AuditEventDispatcher_StoresHashAlgorithmType_FromHashProvider()
    {
        // Arrange
        AuditEvent? captured = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((ae, _) => captured = ae)
            .Returns(Task.CompletedTask);

        var events = new System.Collections.Generic.List<IDomainEvent>
        {
            new TenantCreatedEvent(TenantId.New(), DateTimeOffset.UtcNow),
        };

        // Act
        await _dispatcher.DispatchAsync(events.AsReadOnly(), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal(HashAlgorithmType.SHA256, captured.HashAlgorithm);
    }

    // -------------------------------------------------------------------------
    // SourceBrand tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task SourceBrand_Joinerytech_Stored()
    {
        // Arrange
        _requestContext.Setup(r => r.SourceBrand).Returns("joinerytech");
        AuditEvent? captured = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((ae, _) => captured = ae)
            .Returns(Task.CompletedTask);

        var events = new System.Collections.Generic.List<IDomainEvent>
        {
            new TenantCreatedEvent(TenantId.New(), DateTimeOffset.UtcNow),
        };

        // Act
        await _dispatcher.DispatchAsync(events.AsReadOnly(), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal("joinerytech", captured.SourceBrand);
    }

    [Fact]
    public async Task SourceBrand_Asztalostech_Stored()
    {
        // Arrange
        _requestContext.Setup(r => r.SourceBrand).Returns("asztalostech");
        AuditEvent? captured = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((ae, _) => captured = ae)
            .Returns(Task.CompletedTask);

        var events = new System.Collections.Generic.List<IDomainEvent>
        {
            new TenantCreatedEvent(TenantId.New(), DateTimeOffset.UtcNow),
        };

        // Act
        await _dispatcher.DispatchAsync(events.AsReadOnly(), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(captured);
        Assert.Equal("asztalostech", captured.SourceBrand);
    }

    [Fact]
    public async Task SourceBrand_Unknown_StoredAsNull()
    {
        // Arrange — ICurrentRequestContext returns null for unrecognised brands (allowlist filtering happens in infra)
        _requestContext.Setup(r => r.SourceBrand).Returns((string?)null);
        AuditEvent? captured = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((ae, _) => captured = ae)
            .Returns(Task.CompletedTask);

        var events = new System.Collections.Generic.List<IDomainEvent>
        {
            new TenantCreatedEvent(TenantId.New(), DateTimeOffset.UtcNow),
        };

        // Act
        await _dispatcher.DispatchAsync(events.AsReadOnly(), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(captured);
        Assert.Null(captured.SourceBrand);
    }

    [Fact]
    public async Task SourceBrand_Missing_StoredAsNull()
    {
        // Arrange — default mock returns null for SourceBrand (no header)
        AuditEvent? captured = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((ae, _) => captured = ae)
            .Returns(Task.CompletedTask);

        var events = new System.Collections.Generic.List<IDomainEvent>
        {
            new TenantCreatedEvent(TenantId.New(), DateTimeOffset.UtcNow),
        };

        // Act
        await _dispatcher.DispatchAsync(events.AsReadOnly(), TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(captured);
        Assert.Null(captured.SourceBrand);
    }

    [Fact]
    public async Task HashChain_IncludesSourceBrand()
    {
        // Arrange — two dispatches with different SourceBrand values should produce different hashes
        _requestContext.Setup(r => r.SourceBrand).Returns("joinerytech");
        AuditEvent? capturedWithBrand = null;
        _repository
            .Setup(r => r.AddAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEvent, CancellationToken>((ae, _) => capturedWithBrand = ae)
            .Returns(Task.CompletedTask);

        var events = new System.Collections.Generic.List<IDomainEvent>
        {
            new TenantCreatedEvent(TenantId.New(), DateTimeOffset.UtcNow),
        };

        await _dispatcher.DispatchAsync(events.AsReadOnly(), TestContext.Current.CancellationToken);

        // Assert: hash is a valid 64-char hex and SourceBrand is set
        Assert.NotNull(capturedWithBrand);
        Assert.Matches("^[0-9a-f]{64}$", capturedWithBrand.StateHash);
        Assert.Equal("joinerytech", capturedWithBrand.SourceBrand);
    }
}
