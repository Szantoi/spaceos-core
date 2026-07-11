using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Tenants;
using SpaceOS.Kernel.Application.Tenants.Queries;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

public class GetTenantByIdQueryHandlerTests
{
    private readonly Mock<ITenantRepository> _tenantRepositoryMock;
    private readonly GetTenantByIdQueryHandler _handler;

    public GetTenantByIdQueryHandlerTests()
    {
        _tenantRepositoryMock = new Mock<ITenantRepository>();
        _handler = new GetTenantByIdQueryHandler(_tenantRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenTenantExists_ShouldReturnSuccess()
    {
        // Arrange
        var tenant = Tenant.Create("ACME Corp");
        var query = new GetTenantByIdQuery(tenant.Id.Value);

        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(tenant.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(tenant);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(tenant.Id.Value, result.Value.Id);
        Assert.Equal("ACME Corp", result.Value.Name);
    }

    [Fact]
    public async Task Handle_WhenTenantNotFound_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetTenantByIdQuery(Guid.NewGuid());

        _tenantRepositoryMock.Setup(r => r.GetByIdAsync(It.IsAny<TenantId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Tenant?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
