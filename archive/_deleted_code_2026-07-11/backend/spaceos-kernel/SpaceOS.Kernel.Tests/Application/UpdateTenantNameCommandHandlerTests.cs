using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Tenants.Commands;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;
using Ardalis.Result;

namespace SpaceOS.Kernel.Tests.Application;

public class UpdateTenantNameCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock;
    private readonly UpdateTenantNameCommandHandler _handler;

    public UpdateTenantNameCommandHandlerTests()
    {
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        _handler = new UpdateTenantNameCommandHandler(
            _tenantRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSuccess()
    {
        // Arrange
        var tenant = Tenant.Create("Old Name");
        tenant.PopDomainEvents(); // clear creation event
        var command = new UpdateTenantNameCommand(tenant.Id.Value, "New Name");

        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("New Name", tenant.Name.Value);
        _tenantRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var command = new UpdateTenantNameCommand(Guid.NewGuid(), "New Name");

        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
