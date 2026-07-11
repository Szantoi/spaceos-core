using Ardalis.Result;
using FluentAssertions;
using Moq;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Commands.GenerateBatch;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Queries.GetBatchStatus;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Repositories;
using SpaceOS.Modules.Joinery.Domain.Core;

namespace SpaceOS.Modules.Joinery.Tests.Handlers;

/// <summary>
/// Unit tests for GetBatchStatusQueryHandler.
/// </summary>
public class GetBatchStatusQueryHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid OrderId = Guid.NewGuid();
    private static readonly IReadOnlyList<Guid> ValidIds = new List<Guid> { Guid.NewGuid() };

    private readonly Mock<IGyartasilapBatchRepository> _batchRepo = new();

    private GetBatchStatusQueryHandler CreateHandler() =>
        new(_batchRepo.Object);

    private GyartasilapBatch CreatePendingBatch()
    {
        var result = GyartasilapBatch.Create(OrderId, TenantId, ValidIds);
        return result.Value;
    }

    [Fact]
    public async Task Handle_PendingBatch_ReturnsPendingStatus()
    {
        // Arrange
        var batch = CreatePendingBatch();
        _batchRepo.Setup(r => r.GetByIdAsync(batch.Id, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);

        var query = new GetBatchStatusQuery(TenantId, batch.Id);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(BatchStatusDto.Pending);
        result.Value.ZipStoragePath.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ReadyBatch_ReturnsReadyStatusAndPath()
    {
        // Arrange
        const string zipPath = "tenant/batches/id/batch.zip";
        var batch = CreatePendingBatch();
        batch.MarkGenerating();
        batch.MarkReady(zipPath);

        _batchRepo.Setup(r => r.GetByIdAsync(batch.Id, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);

        var query = new GetBatchStatusQuery(TenantId, batch.Id);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(BatchStatusDto.Ready);
        result.Value.ZipStoragePath.Should().Be(zipPath);
    }

    [Fact]
    public async Task Handle_BatchNotFound_ReturnsNotFound()
    {
        // Arrange
        var unknownId = Guid.NewGuid();
        _batchRepo.Setup(r => r.GetByIdAsync(unknownId, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GyartasilapBatch?)null);

        var query = new GetBatchStatusQuery(TenantId, unknownId);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_WrongTenant_ReturnsNotFound()
    {
        // Arrange — repository returns null when tenant doesn't match (RLS simulation)
        var batchId = Guid.NewGuid();
        var wrongTenantId = Guid.NewGuid();
        _batchRepo.Setup(r => r.GetByIdAsync(batchId, wrongTenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((GyartasilapBatch?)null);

        var query = new GetBatchStatusQuery(wrongTenantId, batchId);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_FailedBatch_ReturnsFailedStatus()
    {
        // Arrange
        var batch = CreatePendingBatch();
        batch.MarkGenerating();
        batch.MarkFailed();

        _batchRepo.Setup(r => r.GetByIdAsync(batch.Id, TenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(batch);

        var query = new GetBatchStatusQuery(TenantId, batch.Id);
        var handler = CreateHandler();

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Status.Should().Be(BatchStatusDto.Failed);
    }
}
