// SpaceOS.Kernel.Tests/StageRegistry/Handlers/GetStageHandoffsQueryHandlerTests.cs
using System;
using System.Collections.Generic;
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

/// <summary>Unit tests for <see cref="GetStageHandoffsQueryHandler"/>.</summary>
public sealed class GetStageHandoffsQueryHandlerTests
{
    private readonly Mock<IStageHandoffRepository> _repository = new();
    private readonly GetStageHandoffsQueryHandler _handler;

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

    public GetStageHandoffsQueryHandlerTests()
    {
        _handler = new GetStageHandoffsQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_WithHandoffs_ReturnsSuccessWithDtoList()
    {
        // Arrange
        var handoffs = new List<StageHandoff> { BuildHandoff(), BuildHandoff() };
        _repository
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<StageHandoff>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(handoffs);

        // Act
        var result = await _handler.Handle(new GetStageHandoffsQuery(EpicId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task Handle_NoHandoffs_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        _repository
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<StageHandoff>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StageHandoff>());

        // Act
        var result = await _handler.Handle(new GetStageHandoffsQuery(EpicId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_WithHandoff_MapsFieldsCorrectly()
    {
        // Arrange
        var handoff = BuildHandoff();
        _repository
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<StageHandoff>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StageHandoff> { handoff });

        // Act
        var result = await _handler.Handle(new GetStageHandoffsQuery(EpicId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value);
        Assert.Equal(handoff.Id, dto.Id);
        Assert.Equal(TenantId, dto.TenantId);
        Assert.Equal(EpicId, dto.FlowEpicId);
        Assert.Equal("stage_a", dto.SourceStageCode);
        Assert.Equal("stage_b", dto.TargetStageCode);
        Assert.Equal(1, dto.Version);
        Assert.Equal("SHA-256", dto.HashAlgorithm);
    }
}
