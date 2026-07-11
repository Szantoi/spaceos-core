// SpaceOS.Kernel.Tests/StageRegistry/Handlers/GetStageChainTemplateQueryHandlerTests.cs
using System;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.StageRegistry.Queries;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using Xunit;

namespace SpaceOS.Kernel.Tests.StageRegistry.Handlers;

/// <summary>Unit tests for <see cref="GetStageChainTemplateQueryHandler"/>.</summary>
public sealed class GetStageChainTemplateQueryHandlerTests
{
    private readonly Mock<IStageChainTemplateRepository> _repository = new();
    private readonly GetStageChainTemplateQueryHandler _handler;

    private static readonly Guid TenantId = Guid.NewGuid();

    public GetStageChainTemplateQueryHandlerTests()
    {
        _handler = new GetStageChainTemplateQueryHandler(_repository.Object);
    }

    [Fact]
    public async Task Handle_ExistingTemplate_ReturnsSuccessWithDetailDto()
    {
        // Arrange
        var template = StageChainTemplate.Create(TenantId, "Standard Chain");
        _repository
            .Setup(r => r.GetByIdWithStepsAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        var query = new GetStageChainTemplateQuery(template.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(template.Id, result.Value.Id);
        Assert.Equal(TenantId, result.Value.TenantId);
        Assert.Equal("Standard Chain", result.Value.Name);
    }

    [Fact]
    public async Task Handle_ExistingTemplateWithSteps_MapsStepsCorrectly()
    {
        // Arrange
        var template = StageChainTemplate.Create(TenantId, "Standard Chain");
        var definition = StageDefinition.Register(TenantId, "review_step", "Review Step", "http://127.0.0.1:5000");
        template.AddStep(definition, 1, isOptional: false);
        template.PopDomainEvents();

        _repository
            .Setup(r => r.GetByIdWithStepsAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        var query = new GetStageChainTemplateQuery(template.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        var step = Assert.Single(result.Value.Steps);
        Assert.Equal("review_step", step.StageCode);
        Assert.Equal(1, step.SortOrder);
        Assert.False(step.IsOptional);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNotFound()
    {
        // Arrange
        _repository
            .Setup(r => r.GetByIdWithStepsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StageChainTemplate?)null);
        var query = new GetStageChainTemplateQuery(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
