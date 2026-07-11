using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.FlowEpics.Commands;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

public class UpdateFlowEpicTitleCommandHandlerTests
{
    private readonly Mock<IFlowEpicRepository> _flowEpicRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock;
    private readonly UpdateFlowEpicTitleCommandHandler _handler;

    public UpdateFlowEpicTitleCommandHandlerTests()
    {
        _flowEpicRepositoryMock = new Mock<IFlowEpicRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        _handler = new UpdateFlowEpicTitleCommandHandler(
            _flowEpicRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var flowEpic = FlowEpic.Create("Old Title", FacilityId.New(), TenantId.New());
        flowEpic.PopDomainEvents(); // clear creation event
        var command = new UpdateFlowEpicTitleCommand(flowEpic.Id.Value, "New Title");

        _flowEpicRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(flowEpic);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("New Title", flowEpic.Title.Value);
        _flowEpicRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<FlowEpic>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenFlowEpicNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = new UpdateFlowEpicTitleCommand(Guid.NewGuid(), "New Title");

        _flowEpicRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlowEpic?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
