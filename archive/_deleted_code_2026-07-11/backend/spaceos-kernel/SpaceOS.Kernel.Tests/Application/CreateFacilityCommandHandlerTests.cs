using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Facilities;
using SpaceOS.Kernel.Application.Facilities.Commands;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;
using Ardalis.Result;

namespace SpaceOS.Kernel.Tests.Application;

public class CreateFacilityCommandHandlerTests
{
    private readonly Mock<IFacilityRepository> _facilityRepositoryMock;
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock;
    private readonly CreateFacilityCommandHandler _handler;

    public CreateFacilityCommandHandlerTests()
    {
        _facilityRepositoryMock = new Mock<IFacilityRepository>();
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        _handler = new CreateFacilityCommandHandler(
            _facilityRepositoryMock.Object,
            _tenantRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var tenant = Tenant.Create("Test Tenant");
        var tenantId = tenant.Id;
        var command = new CreateFacilityCommand(tenantId.Value, "New Facility");

        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _facilityRepositoryMock.Setup(r => r.ExistsByNameAsync(tenantId, command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _facilityRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Facility>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var tenantId = TenantId.New();
        var command = new CreateFacilityCommand(tenantId.Value, "New Facility");

        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_WhenNameNotUnique_ShouldReturnError()
    {
        // Arrange
        var tenant = Tenant.Create("Test Tenant");
        var tenantId = tenant.Id;
        var command = new CreateFacilityCommand(tenantId.Value, "Existing Facility");

        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenantId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        _facilityRepositoryMock.Setup(r => r.ExistsByNameAsync(tenantId, command.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.Error, result.Status);
    }
}
