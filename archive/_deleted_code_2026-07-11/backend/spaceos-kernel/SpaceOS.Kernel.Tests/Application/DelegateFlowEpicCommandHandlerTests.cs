using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.FlowEpics.Commands.DelegateFlowEpic;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;
using Ardalis.Result;

namespace SpaceOS.Kernel.Tests.Application;

public class DelegateFlowEpicCommandHandlerTests
{
    private readonly Mock<IFlowEpicRepository> _flowEpicRepositoryMock;
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock;
    private readonly DelegateFlowEpicCommandHandler _handler;

    public DelegateFlowEpicCommandHandlerTests()
    {
        _flowEpicRepositoryMock = new Mock<IFlowEpicRepository>();
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        _handler = new DelegateFlowEpicCommandHandler(
            _flowEpicRepositoryMock.Object,
            _tenantRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldDelegateEpic_WhenRequestIsValid()
    {
        // Arrange
        var facilityId = FacilityId.New();
        var epic = FlowEpic.Create("Kitchen Manufacturing", facilityId, TenantId.New());
        var guestTenantId = Guid.NewGuid();

        _flowEpicRepositoryMock.Setup(r => r.GetByIdAsync(epic.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tenant.Create("Guest Tenant"));

        var command = new DelegateFlowEpicCommand(epic.Id.Value, guestTenantId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(epic.Handshake);
        Assert.Equal(guestTenantId, epic.Handshake!.GuestTenantId.Value);

        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcherMock.Verify(d => d.DispatchAsync(
            It.IsAny<IEnumerable<IDomainEvent>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEpicIsInDeliveryPhase_ShouldReturnError()
    {
        // Arrange
        var epic = FlowEpic.Create("Test Epic", FacilityId.New(), TenantId.New());
        epic.StartExecution(); // Delivery fázisba lép
        var command = new DelegateFlowEpicCommand(epic.Id.Value, Guid.NewGuid());

        _flowEpicRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(epic);
        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Tenant.Create("Guest Tenant"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Error, result.Status);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenEpicDoesNotExist()
    {
        // Arrange
        var epicId = Guid.NewGuid();
        _flowEpicRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FlowEpic?)null);

        var command = new DelegateFlowEpicCommand(epicId, Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenGuestTenantNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var flowEpic = FlowEpic.Create("Epic Title", FacilityId.New(), TenantId.New());
        var command = new DelegateFlowEpicCommand(flowEpic.Id.Value, Guid.NewGuid());

        _flowEpicRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<FlowEpicId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(flowEpic);
        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
