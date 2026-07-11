using Ardalis.Result;
using Moq;
using SpaceOS.Kernel.Application.Facilities;
using SpaceOS.Kernel.Application.Facilities.Queries;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;
using Xunit;

namespace SpaceOS.Kernel.Tests.Application;

public class GetFacilityByIdQueryHandlerTests
{
    private readonly Mock<IFacilityRepository> _facilityRepositoryMock;
    private readonly GetFacilityByIdQueryHandler _handler;

    public GetFacilityByIdQueryHandlerTests()
    {
        _facilityRepositoryMock = new Mock<IFacilityRepository>();
        _handler = new GetFacilityByIdQueryHandler(_facilityRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenFacilityExists_ShouldReturnSuccessWithCorrectDto()
    {
        // Arrange
        var tenantId = TenantId.New();
        var facility = Facility.Create("Main Hall", tenantId);
        var query = new GetFacilityByIdQuery(facility.Id.Value);

        _facilityRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<FacilityId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(facility);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(facility.Id.Value, result.Value.Id);
        Assert.Equal("Main Hall", result.Value.Name);
        Assert.Equal(tenantId.Value, result.Value.TenantId);
    }

    [Fact]
    public async Task Handle_WhenFacilityDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var query = new GetFacilityByIdQuery(Guid.NewGuid());

        _facilityRepositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<FacilityId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Facility?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.Equal(ResultStatus.NotFound, result.Status);
    }
}
