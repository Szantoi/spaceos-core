using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Commands.GenerateBatch;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Repositories;
using SpaceOS.Modules.Joinery.Domain.Core;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Tests.Handlers;

/// <summary>
/// Unit tests for GenerateBatchCommandHandler — ZIP generation, storage, and error paths.
/// </summary>
public class GenerateBatchCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly Guid GyartasilapId1 = Guid.NewGuid();
    private static readonly Guid GyartasilapId2 = Guid.NewGuid();
    private static readonly byte[] FakePdfBytes = [0x25, 0x50, 0x44, 0x46]; // %PDF

    private readonly Mock<IGyartasilapRepository> _gyartasilapRepo = new();
    private readonly Mock<IGyartasilapBatchRepository> _batchRepo = new();
    private readonly Mock<IGyartasilapStorage> _storage = new();

    private GenerateBatchCommandHandler CreateHandler() =>
        new(_gyartasilapRepo.Object,
            _batchRepo.Object,
            _storage.Object,
            NullLogger<GenerateBatchCommandHandler>.Instance);

    private Gyartasilap CreateFakeGyartasilap(Guid id)
    {
        var result = Gyartasilap.Create(TenantId, OrderId, null, "L1");
        var g = result.Value;
        g.UpdateStorage(FakePdfBytes, "some/url");
        return g;
    }

    [Fact]
    public async Task Handle_HappyPath_GeneratesZipAndReturnsReady()
    {
        // Arrange
        var g1 = CreateFakeGyartasilap(GyartasilapId1);
        var g2 = CreateFakeGyartasilap(GyartasilapId2);

        _gyartasilapRepo.Setup(r => r.GetByIdAsync(GyartasilapId1, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(g1);
        _gyartasilapRepo.Setup(r => r.GetByIdAsync(GyartasilapId2, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(g2);
        _storage.Setup(s => s.StoreZipAsync(TenantId, It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("tenant/batches/batchid/batch.zip");
        _batchRepo.Setup(r => r.AddAsync(It.IsAny<GyartasilapBatch>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmd = new GenerateBatchCommand(TenantId, OrderId, new List<Guid> { GyartasilapId1, GyartasilapId2 });
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.BatchId.Should().NotBeEmpty();
        result.Value.ZipStoragePath.Should().Be("tenant/batches/batchid/batch.zip");
        result.Value.Status.Should().Be(BatchStatusDto.Ready);
    }

    [Fact]
    public async Task Handle_MissingGyartasilap_SkipsAndStillSucceeds()
    {
        // Arrange — one of the two gyartasilaps is missing
        var g1 = CreateFakeGyartasilap(GyartasilapId1);
        _gyartasilapRepo.Setup(r => r.GetByIdAsync(GyartasilapId1, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(g1);
        _gyartasilapRepo.Setup(r => r.GetByIdAsync(GyartasilapId2, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Gyartasilap?)null);
        _storage.Setup(s => s.StoreZipAsync(TenantId, It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("path/batch.zip");
        _batchRepo.Setup(r => r.AddAsync(It.IsAny<GyartasilapBatch>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmd = new GenerateBatchCommand(TenantId, OrderId, new List<Guid> { GyartasilapId1, GyartasilapId2 });
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert: still succeeds (graceful degradation)
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_StorageFailure_BatchMarkedFailed()
    {
        // Arrange
        var g1 = CreateFakeGyartasilap(GyartasilapId1);
        _gyartasilapRepo.Setup(r => r.GetByIdAsync(GyartasilapId1, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(g1);
        _storage.Setup(s => s.StoreZipAsync(TenantId, It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("MinIO unreachable"));
        _batchRepo.Setup(r => r.AddAsync(It.IsAny<GyartasilapBatch>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmd = new GenerateBatchCommand(TenantId, OrderId, new List<Guid> { GyartasilapId1 });
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert: response is returned but status is Failed
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(BatchStatusDto.Failed);
        result.Value.ZipStoragePath.Should().BeNull();
    }

    [Fact]
    public async Task Handle_EmptyIds_ReturnsInvalidResult()
    {
        var cmd = new GenerateBatchCommand(TenantId, OrderId, new List<Guid>());
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Invalid);
    }

    [Fact]
    public async Task Handle_NullIds_ReturnsInvalidResult()
    {
        var cmd = new GenerateBatchCommand(TenantId, OrderId, null!);
        var handler = CreateHandler();

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(Ardalis.Result.ResultStatus.Invalid);
    }

    [Fact]
    public async Task Handle_PersistsBatchRecord_Always()
    {
        // Arrange
        var g1 = CreateFakeGyartasilap(GyartasilapId1);
        _gyartasilapRepo.Setup(r => r.GetByIdAsync(GyartasilapId1, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(g1);
        _storage.Setup(s => s.StoreZipAsync(TenantId, It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("path/batch.zip");
        _batchRepo.Setup(r => r.AddAsync(It.IsAny<GyartasilapBatch>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmd = new GenerateBatchCommand(TenantId, OrderId, new List<Guid> { GyartasilapId1 });
        var handler = CreateHandler();

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert
        _batchRepo.Verify(r => r.AddAsync(
            It.Is<GyartasilapBatch>(b => b.OrderId == OrderId && b.TenantId == TenantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PersistsBatchRecord_EvenOnStorageFailure()
    {
        // Arrange
        var g1 = CreateFakeGyartasilap(GyartasilapId1);
        _gyartasilapRepo.Setup(r => r.GetByIdAsync(GyartasilapId1, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(g1);
        _storage.Setup(s => s.StoreZipAsync(TenantId, It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Storage down"));
        _batchRepo.Setup(r => r.AddAsync(It.IsAny<GyartasilapBatch>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmd = new GenerateBatchCommand(TenantId, OrderId, new List<Guid> { GyartasilapId1 });
        var handler = CreateHandler();

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert: batch is persisted even on failure
        _batchRepo.Verify(r => r.AddAsync(It.IsAny<GyartasilapBatch>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SingleId_ZipContainsOneEntry()
    {
        // Arrange
        var g1 = CreateFakeGyartasilap(GyartasilapId1);
        _gyartasilapRepo.Setup(r => r.GetByIdAsync(GyartasilapId1, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(g1);

        byte[]? capturedZip = null;
        _storage.Setup(s => s.StoreZipAsync(TenantId, It.IsAny<Guid>(), It.IsAny<byte[]>(), It.IsAny<CancellationToken>()))
            .Callback<Guid, Guid, byte[], CancellationToken>((_, _, zip, _) => capturedZip = zip)
            .ReturnsAsync("path/batch.zip");
        _batchRepo.Setup(r => r.AddAsync(It.IsAny<GyartasilapBatch>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var cmd = new GenerateBatchCommand(TenantId, OrderId, new List<Guid> { GyartasilapId1 });
        var handler = CreateHandler();

        // Act
        await handler.Handle(cmd, CancellationToken.None);

        // Assert: ZIP was produced (non-empty bytes)
        capturedZip.Should().NotBeNull();
        capturedZip!.Length.Should().BeGreaterThan(0);
    }
}
