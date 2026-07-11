using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Tenants.Commands;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Services;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

public class CreateTenantCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDomainEventDispatcher> _domainEventDispatcherMock;
    private readonly Mock<IModuleRegistryService> _moduleRegistryMock;
    private readonly CreateTenantCommandHandler _handler;

    public CreateTenantCommandHandlerTests()
    {
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _domainEventDispatcherMock = new Mock<IDomainEventDispatcher>();
        _moduleRegistryMock = new Mock<IModuleRegistryService>();
        _moduleRegistryMock
            .Setup(r => r.ValidateModulesForTenantType(It.IsAny<TenantType>(), It.IsAny<IReadOnlyList<string>>()))
            .Returns(ModuleValidationResult.Success());
        _handler = new CreateTenantCommandHandler(
            _tenantRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _domainEventDispatcherMock.Object,
            _moduleRegistryMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnSuccessWithTenantId()
    {
        // Arrange
        var command = new CreateTenantCommand("ACME Corp");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _tenantRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcherMock.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExplicitTenantType_ShouldReturnSuccess()
    {
        // Arrange
        var command = new CreateTenantCommand("Lap-Expert Kft.", TenantType.PanelCutter);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
    }

    [Fact]
    public async Task Handle_WithInvalidModulesForType_ShouldReturnError()
    {
        // Arrange
        _moduleRegistryMock
            .Setup(r => r.ValidateModulesForTenantType(TenantType.PanelCutter, It.IsAny<IReadOnlyList<string>>()))
            .Returns(ModuleValidationResult.Failure("Modules not allowed for PanelCutter: orders"));

        var command = new CreateTenantCommand("Szabász Kft.", TenantType.PanelCutter, new[] { "orders" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        _tenantRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Tenant>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
