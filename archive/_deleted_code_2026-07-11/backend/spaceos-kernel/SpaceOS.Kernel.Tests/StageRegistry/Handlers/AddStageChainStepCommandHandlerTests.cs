// SpaceOS.Kernel.Tests/StageRegistry/Handlers/AddStageChainStepCommandHandlerTests.cs
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.StageRegistry.Commands;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using Xunit;

namespace SpaceOS.Kernel.Tests.StageRegistry.Handlers;

/// <summary>Unit tests for <see cref="AddStageChainStepCommandHandler"/>.</summary>
public sealed class AddStageChainStepCommandHandlerTests
{
    private readonly Mock<IStageChainTemplateRepository> _chainRepository = new();
    private readonly Mock<IStageDefinitionRepository> _definitionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcher = new();
    private readonly AddStageChainStepCommandHandler _handler;

    private static readonly Guid TenantId = Guid.NewGuid();

    private static StageChainTemplate BuildTemplate() =>
        StageChainTemplate.Create(TenantId, "Standard Chain");

    private static StageDefinition BuildDefinition() =>
        StageDefinition.Register(TenantId, "review_step", "Review Step", "http://127.0.0.1:5000");

    public AddStageChainStepCommandHandlerTests()
    {
        _handler = new AddStageChainStepCommandHandler(
            _chainRepository.Object,
            _definitionRepository.Object,
            _unitOfWork.Object,
            _dispatcher.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var template = BuildTemplate();
        var definition = BuildDefinition();
        _chainRepository
            .Setup(r => r.GetByIdWithStepsAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _definitionRepository
            .Setup(r => r.GetByIdAsync(definition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);
        var command = new AddStageChainStepCommand(template.Id, definition.Id, 1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsUpdateAsync_Once()
    {
        // Arrange
        var template = BuildTemplate();
        var definition = BuildDefinition();
        _chainRepository
            .Setup(r => r.GetByIdWithStepsAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _definitionRepository
            .Setup(r => r.GetByIdAsync(definition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);
        var command = new AddStageChainStepCommand(template.Id, definition.Id, 1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _chainRepository.Verify(r =>
            r.UpdateAsync(It.IsAny<StageChainTemplate>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSaveChangesAsync_Once()
    {
        // Arrange
        var template = BuildTemplate();
        var definition = BuildDefinition();
        _chainRepository
            .Setup(r => r.GetByIdWithStepsAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _definitionRepository
            .Setup(r => r.GetByIdAsync(definition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);
        var command = new AddStageChainStepCommand(template.Id, definition.Id, 1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWork.Verify(u =>
            u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_DispatchesDomainEvents_Once()
    {
        // Arrange
        var template = BuildTemplate();
        var definition = BuildDefinition();
        _chainRepository
            .Setup(r => r.GetByIdWithStepsAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _definitionRepository
            .Setup(r => r.GetByIdAsync(definition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);
        var command = new AddStageChainStepCommand(template.Id, definition.Id, 1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dispatcher.Verify(d =>
            d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ChainNotFound_ReturnsNotFound()
    {
        // Arrange
        _chainRepository
            .Setup(r => r.GetByIdWithStepsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StageChainTemplate?)null);
        var command = new AddStageChainStepCommand(Guid.NewGuid(), Guid.NewGuid(), 1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_DefinitionNotFound_ReturnsNotFound()
    {
        // Arrange
        var template = BuildTemplate();
        _chainRepository
            .Setup(r => r.GetByIdWithStepsAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        _definitionRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StageDefinition?)null);
        var command = new AddStageChainStepCommand(template.Id, Guid.NewGuid(), 1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
