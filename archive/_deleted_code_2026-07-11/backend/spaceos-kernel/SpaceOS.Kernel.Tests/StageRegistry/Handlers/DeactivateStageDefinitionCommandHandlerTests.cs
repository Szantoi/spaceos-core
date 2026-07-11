// SpaceOS.Kernel.Tests/StageRegistry/Handlers/DeactivateStageDefinitionCommandHandlerTests.cs
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

/// <summary>Unit tests for <see cref="DeactivateStageDefinitionCommandHandler"/>.</summary>
public sealed class DeactivateStageDefinitionCommandHandlerTests
{
    private readonly Mock<IStageDefinitionRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcher = new();
    private readonly DeactivateStageDefinitionCommandHandler _handler;

    private static StageDefinition BuildDefinition() =>
        StageDefinition.Register(Guid.NewGuid(), "review_step", "Review Step", "http://127.0.0.1:5000");

    public DeactivateStageDefinitionCommandHandlerTests()
    {
        _handler = new DeactivateStageDefinitionCommandHandler(
            _repository.Object,
            _unitOfWork.Object,
            _dispatcher.Object);
    }

    [Fact]
    public async Task Handle_ExistingDefinition_ReturnsSuccess()
    {
        // Arrange
        var definition = BuildDefinition();
        _repository
            .Setup(r => r.GetByIdAsync(definition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);
        var command = new DeactivateStageDefinitionCommand(definition.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Handle_ExistingDefinition_CallsUpdateAsync_Once()
    {
        // Arrange
        var definition = BuildDefinition();
        _repository
            .Setup(r => r.GetByIdAsync(definition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);
        var command = new DeactivateStageDefinitionCommand(definition.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repository.Verify(r =>
            r.UpdateAsync(It.IsAny<StageDefinition>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingDefinition_CallsSaveChangesAsync_Once()
    {
        // Arrange
        var definition = BuildDefinition();
        _repository
            .Setup(r => r.GetByIdAsync(definition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);
        var command = new DeactivateStageDefinitionCommand(definition.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _unitOfWork.Verify(u =>
            u.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ExistingDefinition_DispatchesDomainEvents_Once()
    {
        // Arrange
        var definition = BuildDefinition();
        _repository
            .Setup(r => r.GetByIdAsync(definition.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(definition);
        var command = new DeactivateStageDefinitionCommand(definition.Id);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dispatcher.Verify(d =>
            d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownId_ReturnsNotFound()
    {
        // Arrange
        _repository
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((StageDefinition?)null);
        var command = new DeactivateStageDefinitionCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
