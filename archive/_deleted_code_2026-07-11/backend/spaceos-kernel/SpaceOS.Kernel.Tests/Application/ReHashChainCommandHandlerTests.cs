// SpaceOS.Kernel.Tests/Application/ReHashChainCommandHandlerTests.cs

using Moq;
using SpaceOS.Kernel.Application.AuditLog.Commands;
using SpaceOS.Kernel.Domain.AuditLog;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

public sealed class ReHashChainCommandHandlerTests
{
    private static readonly Guid TestTenantId = Guid.Parse("44444444-4444-4444-4444-444444444444");

    private readonly Mock<IAuditEventRepository> _repository = new();
    private readonly ReHashChainCommandHandler _handler;

    public ReHashChainCommandHandlerTests() =>
        _handler = new ReHashChainCommandHandler(_repository.Object);

    // -------------------------------------------------------------------------
    // Handle_AllRecordsUseSameAlgorithm_ReturnsZeroAffected
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_AllRecordsUseSameAlgorithm_ReturnsZeroAffected()
    {
        // Arrange
        var events = BuildChain(count: 3, algorithm: HashAlgorithmType.SHA3_256);
        _repository
            .Setup(r => r.GetChainAsync(TestTenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var command = new ReHashChainCommand(TestTenantId, HashAlgorithmType.SHA3_256);

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.RecordsAffected);
        Assert.Equal(HashAlgorithmType.SHA3_256, result.Value.NewAlgorithm);
    }

    // -------------------------------------------------------------------------
    // Handle_AllRecordsUseDifferentAlgorithm_ReturnsAllAsAffected
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_AllRecordsUseDifferentAlgorithm_ReturnsAllAsAffected()
    {
        // Arrange
        var events = BuildChain(count: 5, algorithm: HashAlgorithmType.SHA256);
        _repository
            .Setup(r => r.GetChainAsync(TestTenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var command = new ReHashChainCommand(TestTenantId, HashAlgorithmType.SHA3_256);

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.RecordsAffected);
    }

    // -------------------------------------------------------------------------
    // Handle_EmptyChain_ReturnsZeroAffected
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_EmptyChain_ReturnsZeroAffected()
    {
        // Arrange
        _repository
            .Setup(r => r.GetChainAsync(TestTenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AuditEvent>)Array.Empty<AuditEvent>());

        var command = new ReHashChainCommand(TestTenantId, HashAlgorithmType.SHA3_256);

        // Act
        var result = await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.Value.RecordsAffected);
    }

    // -------------------------------------------------------------------------
    // Handle_DoesNotModifyAnyRecord
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Handle_DoesNotModifyAnyRecord()
    {
        // Arrange
        var events = BuildChain(count: 2, algorithm: HashAlgorithmType.SHA256);
        _repository
            .Setup(r => r.GetChainAsync(TestTenantId, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(events);

        var command = new ReHashChainCommand(TestTenantId, HashAlgorithmType.SHA3_256);

        // Act
        await _handler.Handle(command, TestContext.Current.CancellationToken);

        // Assert — no write operations were called
        _repository.Verify(r => r.AddAsync(It.IsAny<AuditEvent>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // -------------------------------------------------------------------------
    // Private helpers
    // -------------------------------------------------------------------------

    private static IReadOnlyList<AuditEvent> BuildChain(int count, HashAlgorithmType algorithm)
    {
        var events = new List<AuditEvent>(count);
        string? prev = null;
        for (var i = 0; i < count; i++)
        {
            var ae = AuditEvent.Create(
                tenantId:      TestTenantId,
                eventType:     "TestEvent",
                aggregateId:   Guid.NewGuid(),
                payload:       $"{{\"index\":{i}}}",
                stateHash:     new string((char)('a' + i), 64),
                previousHash:  prev,
                hashAlgorithm: algorithm);
            prev = ae.StateHash;
            events.Add(ae);
        }
        return events.AsReadOnly();
    }
}
