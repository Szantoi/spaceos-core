// SpaceOS.Kernel.Tests/Application/UpdateTenantModulesCommandHandlerTests.cs
using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Application.Tenants.Commands;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Enums;
using SpaceOS.Kernel.Domain.Primitives;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Services;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

/// <summary>Unit tests for <see cref="UpdateTenantModulesCommandHandler"/>.</summary>
public sealed class UpdateTenantModulesCommandHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IDomainEventDispatcher> _dispatcher = new();
    private readonly Mock<IModuleRegistryService> _registry = new();
    private readonly UpdateTenantModulesCommandHandler _handler;

    public UpdateTenantModulesCommandHandlerTests()
    {
        _handler = new UpdateTenantModulesCommandHandler(
            _tenantRepository.Object,
            _unitOfWork.Object,
            _dispatcher.Object,
            _registry.Object);
    }

    [Fact]
    public async Task Handle_TenantNotFound_ReturnsNotFound()
    {
        // Arrange
        _tenantRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        var command = new UpdateTenantModulesCommand(Guid.NewGuid(), new[] { "door" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidModules_SavesAndDispatchesEvents()
    {
        // Arrange
        var tenant = Tenant.Register("Ajtógyár Kft.", TenantType.Manufacturer);
        _tenantRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _registry
            .Setup(r => r.ValidateModulesForTenantType(TenantType.Manufacturer, It.IsAny<IReadOnlyList<string>>()))
            .Returns(ModuleValidationResult.Success());

        var command = new UpdateTenantModulesCommand(tenant.Id.Value, new[] { "door" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _tenantRepository.Verify(r => r.UpdateAsync(tenant, It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dispatcher.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidModules_ReturnsErrorWithoutSaving()
    {
        // Arrange
        var tenant = Tenant.Register("Szabász Kft.", TenantType.PanelCutter);
        _tenantRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);
        _registry
            .Setup(r => r.ValidateModulesForTenantType(TenantType.PanelCutter, It.IsAny<IReadOnlyList<string>>()))
            .Returns(ModuleValidationResult.Failure("Modules not allowed for PanelCutter: orders"));

        var command = new UpdateTenantModulesCommand(tenant.Id.Value, new[] { "orders" });

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.Error, result.Status);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _dispatcher.Verify(d => d.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
