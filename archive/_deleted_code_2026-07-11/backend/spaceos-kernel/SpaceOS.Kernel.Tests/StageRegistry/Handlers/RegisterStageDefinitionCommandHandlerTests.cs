// SpaceOS.Kernel.Tests/StageRegistry/Handlers/RegisterStageDefinitionCommandHandlerTests.cs
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

/// <summary>Unit tests for <see cref="RegisterStageDefinitionCommandHandler"/>.</summary>
public sealed class RegisterStageDefinitionCommandHandlerTests
{
    private readonly Mock<IStageDefinitionRepository> _repository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcher = new();
    private readonly RegisterStageDefinitionCommandHandler _handler;

    public RegisterStageDefinitionCommandHandlerTests()
    {
        _handler = new RegisterStageDefinitionCommandHandler(
            _repository.Object,
            _unitOfWork.Object,
            _dispatcher.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithNewId()
    {
        // Arrange
        var command = new RegisterStageDefinitionCommand(
            Guid.NewGuid(), "sales_stage", "Sales Stage", "http://127.0.0.1:5004");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsAddAsync_Once()
    {
        // Arrange
        var command = new RegisterStageDefinitionCommand(
            Guid.NewGuid(), "sales_stage", "Sales Stage", "http://127.0.0.1:5004");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _repository.Verify(r =>
            r.AddAsync(It.IsAny<StageDefinition>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsSaveChangesAsync_Once()
    {
        // Arrange
        var command = new RegisterStageDefinitionCommand(
            Guid.NewGuid(), "sales_stage", "Sales Stage", "http://127.0.0.1:5004");

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
        var command = new RegisterStageDefinitionCommand(
            Guid.NewGuid(), "sales_stage", "Sales Stage", "http://127.0.0.1:5004");

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dispatcher.Verify(d =>
            d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
