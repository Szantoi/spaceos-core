// SpaceOS.Kernel.Tests/StageRegistry/Handlers/ListStageChainTemplatesQueryHandlerTests.cs
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

/// <summary>Unit tests for <see cref="ListStageChainTemplatesQueryHandler"/>.</summary>
public sealed class ListStageChainTemplatesQueryHandlerTests
{
    private readonly Mock<IStageChainTemplateRepository> _repository = new();
    private readonly ListStageChainTemplatesQueryHandler _handler;

    private static readonly Guid TenantId = Guid.NewGuid();

    public ListStageChainTemplatesQueryHandlerTests()
    {
        _handler = new ListStageChainTemplatesQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_WithTemplates_ReturnsSuccessWithDtoList()
    {
        // Arrange
        var templates = new List<StageChainTemplate>
        {
            StageChainTemplate.Create(TenantId, "Standard Chain"),
            StageChainTemplate.Create(TenantId, "Express Chain"),
        };
        _repository
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<StageChainTemplate>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(templates);

        // Act
        var result = await _handler.Handle(new ListStageChainTemplatesQuery(TenantId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        _repository
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<StageChainTemplate>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StageChainTemplate>());

        // Act
        var result = await _handler.Handle(new ListStageChainTemplatesQuery(TenantId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_WithTemplate_MapsFieldsCorrectly()
    {
        // Arrange
        var template = StageChainTemplate.Create(TenantId, "Standard Chain", isDefault: true);
        _repository
            .Setup(r => r.ListAsync(It.IsAny<ISpecification<StageChainTemplate>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StageChainTemplate> { template });

        // Act
        var result = await _handler.Handle(new ListStageChainTemplatesQuery(TenantId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var dto = Assert.Single(result.Value);
        Assert.Equal(template.Id, dto.Id);
        Assert.Equal(TenantId, dto.TenantId);
        Assert.Equal("Standard Chain", dto.Name);
        Assert.True(dto.IsDefault);
        Assert.Equal(0, dto.StepCount);
    }
}
