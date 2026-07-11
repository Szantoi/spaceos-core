using FluentAssertions;
using Moq;
using SpaceOS.Modules.Joinery.Application.Orders.DTOs;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.ListDoorOrders;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;

namespace SpaceOS.Modules.Joinery.Tests.Handlers;

public class ListDoorOrdersHandlerTests
{
    private readonly Mock<IDoorOrderRepository> _repo = new();
    private readonly ListDoorOrdersQueryHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();

    public ListDoorOrdersHandlerTests()
    {
        _sut = new ListDoorOrdersQueryHandler(_repo.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedResult()
    {
        var dto = new DoorOrderDto(Guid.NewGuid(), TenantId, Guid.NewGuid(), "PRJ-001", "Test", "Draft", 0, null, DateTime.UtcNow);
        _repo.Setup(r => r.ListAsync(TenantId, 1, 20, It.IsAny<CancellationToken>()))
             .ReturnsAsync(((IReadOnlyList<DoorOrderDto>)[dto], 1));

        var result = await _sut.Handle(new ListDoorOrdersQuery(TenantId, 1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(1);
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Handle_EmptyPage_ReturnsEmptyList()
    {
        _repo.Setup(r => r.ListAsync(TenantId, 1, 20, It.IsAny<CancellationToken>()))
             .ReturnsAsync(((IReadOnlyList<DoorOrderDto>)[], 0));

        var result = await _sut.Handle(new ListDoorOrdersQuery(TenantId, 1, 20), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task Handle_PassesTenantIdToRepository()
    {
        _repo.Setup(r => r.ListAsync(TenantId, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync(((IReadOnlyList<DoorOrderDto>)[], 0));

        await _sut.Handle(new ListDoorOrdersQuery(TenantId, 1, 10), CancellationToken.None);

        _repo.Verify(r => r.ListAsync(TenantId, 1, 10, It.IsAny<CancellationToken>()), Times.Once);
    }
}
