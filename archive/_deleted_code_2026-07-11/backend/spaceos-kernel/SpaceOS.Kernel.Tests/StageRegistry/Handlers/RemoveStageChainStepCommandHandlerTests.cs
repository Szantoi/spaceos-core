// SpaceOS.Kernel.Tests/StageRegistry/Handlers/RemoveStageChainStepCommandHandlerTests.cs
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

/// <summary>Unit tests for <see cref="RemoveStageChainStepCommandHandler"/>.</summary>
public sealed class RemoveStageChainStepCommandHandlerTests
{
    private readonly Mock<IStageChainTemplateRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcher = new();
    private readonly RemoveStageChainStepCommandHandler _handler;

    private static readonly Guid TenantId = Guid.NewGuid();

    private static StageChainTemplate BuildTemplateWithStep()
    {
        var template = StageChainTemplate.Create(TenantId, "Standard Chain");
        var definition = StageDefinition.Register(TenantId, "review_step", "Review Step", "http://127.0.0.1:5000");
        template.AddStep(definition, 1);
        template.PopDomainEvents(); // clear creation events
        return template;
    }

    public RemoveStageChainStepCommandHandlerTests()
    {
        _handler = new RemoveStageChainStepCommandHandler(
            _repository.Object,
            _unitOfWork.Object,
            _dispatcher.Object);
    }

    [Fact]
    public async Task Handle_ExistingStep_ReturnsSuccess()
    {
        // Arrange
        var template = BuildTemplateWithStep();
        _repository
            .Setup(r => r.GetByIdWithStepsAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        var command = new RemoveStageChainStepCommand(template.Id, "review_step");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ExistingStep_CallsUpdateAsync_Once()
    {
        // Arrange
        var template = BuildTemplateWithStep();
        _repository
            .Setup(r => r.GetByIdWithStepsAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        var command = new RemoveStageChainStepCommand(template.Id, "review_step");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repository.Verify(r =>
            r.UpdateAsync(It.IsAny<StageChainTemplate>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingStep_CallsSaveChangesAsync_Once()
    {
        // Arrange
        var template = BuildTemplateWithStep();
        _repository
            .Setup(r => r.GetByIdWithStepsAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        var command = new RemoveStageChainStepCommand(template.Id, "review_step");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWork.Verify(u =>
            u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingStep_DispatchesDomainEvents_Once()
    {
        // Arrange
        var template = BuildTemplateWithStep();
        _repository
            .Setup(r => r.GetByIdWithStepsAsync(template.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(template);
        var command = new RemoveStageChainStepCommand(template.Id, "review_step");

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
        _repository
            .Setup(r => r.GetByIdWithStepsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StageChainTemplate?)null);
        var command = new RemoveStageChainStepCommand(Guid.NewGuid(), "review_step");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
