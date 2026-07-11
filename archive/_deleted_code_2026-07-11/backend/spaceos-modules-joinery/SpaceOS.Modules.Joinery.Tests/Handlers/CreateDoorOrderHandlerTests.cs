using FluentAssertions;
using MediatR;
using Moq;
using SpaceOS.Modules.Joinery.Application.Orders.Commands.CreateDoorOrder;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Aggregates;

namespace SpaceOS.Modules.Joinery.Tests.Handlers;

public class CreateDoorOrderHandlerTests
{
    private readonly Mock<IDoorOrderRepository> _repo = new();
    private readonly Mock<IMediator> _mediator = new();
    private readonly CreateDoorOrderCommandHandler _sut;

    public CreateDoorOrderHandlerTests()
    {
        _repo.Setup(r => r.AddAsync(It.IsAny<DoorOrder>(), It.IsAny<CancellationToken>()))
             .Returns(Task.CompletedTask);
        _sut = new CreateDoorOrderCommandHandler(_repo.Object, _mediator.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccessWithGuid()
    {
        var cmd = new CreateDoorOrderCommand(
            Guid.NewGuid(), Guid.NewGuid(), "PRJ-001", "Test Project",
            null, null, null, null);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task Handle_EmptyProjectId_ReturnsInvalid()
    {
        var cmd = new CreateDoorOrderCommand(
            Guid.NewGuid(), Guid.NewGuid(), "", "Test Project",
            null, null, null, null);

        var result = await _sut.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.ValidationErrors.Should().Contain(e => e.Identifier == "ProjectId");
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsRepositoryAddOnce()
    {
        var cmd = new CreateDoorOrderCommand(
            Guid.NewGuid(), Guid.NewGuid(), "PRJ-002", "Project",
            null, null, null, null);

        await _sut.Handle(cmd, CancellationToken.None);

        _repo.Verify(r => r.AddAsync(It.IsAny<DoorOrder>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
