// SpaceOS.Kernel.Tests/StageRegistry/Handlers/ListStageDefinitionsQueryHandlerTests.cs
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

/// <summary>Unit tests for <see cref="ListStageDefinitionsQueryHandler"/>.</summary>
public sealed class ListStageDefinitionsQueryHandlerTests
{
    private readonly Mock<IStageDefinitionRepository> _repository = new();
    private readonly ListStageDefinitionsQueryHandler _handler;

    private static readonly Guid TenantId = Guid.NewGuid();

    public ListStageDefinitionsQueryHandlerTests()
    {
        _handler = new ListStageDefinitionsQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_WithActiveDefinitions_ReturnsSuccessWithDtoList()
    {
        // Arrange
        var definitions = new List<StageDefinition>
        {
            StageDefinition.Register(TenantId, "stage_one", "Stage One", "http://127.0.0.1:5000"),
            StageDefinition.Register(TenantId, "stage_two", "Stage Two", "http://127.0.0.1:5001"),
        };
        _repository
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<StageDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(definitions);

        // Act
        var result = await _handler.Handle(new ListStageDefinitionsQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        _repository
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<StageDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StageDefinition>());

        // Act
        var result = await _handler.Handle(new ListStageDefinitionsQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_WithDefinitions_MapsFieldsCorrectly()
    {
        // Arrange
        var definition = StageDefinition.Register(TenantId, "review_step", "Review Step", "http://127.0.0.1:5000");
        _repository
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<StageDefinition>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StageDefinition> { definition });

        // Act
        var result = await _handler.Handle(new ListStageDefinitionsQuery(), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value);
        Assert.Equal(definition.Id, dto.Id);
        Assert.Equal(definition.TenantId, dto.TenantId);
        Assert.Equal("review_step", dto.StageCode);
        Assert.Equal("Review Step", dto.DisplayName);
        Assert.True(dto.IsActive);
    }
}
