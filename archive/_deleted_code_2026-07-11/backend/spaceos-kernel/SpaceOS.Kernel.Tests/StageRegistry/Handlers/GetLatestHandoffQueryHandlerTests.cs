// SpaceOS.Kernel.Tests/StageRegistry/Handlers/GetLatestHandoffQueryHandlerTests.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using Ardalis.Specification;
using Moq;
using SpaceOS.Kernel.Application.StageRegistry.Queries;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using Xunit;

namespace SpaceOS.Kernel.Tests.StageRegistry.Handlers;

/// <summary>Unit tests for <see cref="GetLatestHandoffQueryHandler"/>.</summary>
public sealed class GetLatestHandoffQueryHandlerTests
{
    private readonly Mock<IStageHandoffRepository> _repository = new();
    private readonly GetLatestHandoffQueryHandler _handler;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid EpicId = Guid.NewGuid();

    private static StageHandoff BuildHandoff() =>
        StageHandoff.Create(
            TenantId,
            EpicId,
            "stage_a",
            "stage_b",
            nextVersion: 1,
            idempotencyKey: Guid.NewGuid(),
            payloadJson: "{}",
            sourceActorId: null,
            targetActorId: null);

    public GetLatestHandoffQueryHandlerTests()
    {
        _handler = new GetLatestHandoffQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_HandoffExists_ReturnsSuccessWithDto()
    {
        // Arrange
        var handoff = BuildHandoff();
        _repository
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<ISingleResultSpecification<StageHandoff>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handoff);
        var query = new GetLatestHandoffQuery(EpicId, "stage_a", "stage_b");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(handoff.Id, result.Value.Id);
        Assert.Equal("stage_a", result.Value.SourceStageCode);
        Assert.Equal("stage_b", result.Value.TargetStageCode);
    }

    [Fact]
    public async Task Handle_HandoffExists_MapsAllFieldsCorrectly()
    {
        // Arrange
        var handoff = BuildHandoff();
        _repository
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<ISingleResultSpecification<StageHandoff>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handoff);
        var query = new GetLatestHandoffQuery(EpicId, "stage_a", "stage_b");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TenantId, result.Value.TenantId);
        Assert.Equal(EpicId, result.Value.FlowEpicId);
        Assert.Equal(1, result.Value.Version);
        Assert.Equal("SHA-256", result.Value.HashAlgorithm);
        Assert.NotEmpty(result.Value.PayloadHash);
    }

    [Fact]
    public async Task Handle_NoHandoffFound_ReturnsNotFound()
    {
        // Arrange
        _repository
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<ISingleResultSpecification<StageHandoff>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StageHandoff?)null);
        var query = new GetLatestHandoffQuery(EpicId, "stage_a", "stage_b");

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
