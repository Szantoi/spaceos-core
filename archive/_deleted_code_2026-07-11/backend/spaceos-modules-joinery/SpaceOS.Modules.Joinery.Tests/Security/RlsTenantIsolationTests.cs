using Ardalis.Result;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetDoorOrder;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.ListDoorOrders;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence.Repositories;

namespace SpaceOS.Modules.Joinery.Tests.Security;

/// <summary>
/// Repository-level tenant isolation tests.
/// Verifies that the application code correctly filters by TenantId even without real
/// PostgreSQL RLS — an in-memory database is used to confirm the application-layer invariant.
/// </summary>
public class RlsTenantIsolationTests : IDisposable
{
    private readonly JoineryDbContext _db;
    private readonly DoorOrderRepository _repository;

    private static readonly Guid TenantA = Guid.NewGuid();
    private static readonly Guid TenantB = Guid.NewGuid();

    public RlsTenantIsolationTests()
    {
        var opts = new DbContextOptionsBuilder<JoineryDbContext>()
            .UseInMemoryDatabase($"rls-isolation-{Guid.NewGuid()}")
            .Options;
        _db = new JoineryDbContext(opts);
        _repository = new DoorOrderRepository(_db);
    }

    public void Dispose() => _db.Dispose();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static DoorOrder MakeDraftOrder(Guid tenantId, string projectId = "PRJ-001")
    {
        return DoorOrder.Create(tenantId, projectId, "Test Project", Guid.NewGuid()).Value;
    }

    private static DoorOrder MakeCalculatedOrder(Guid tenantId)
    {
        var order = DoorOrder.Create(tenantId, "PRJ-CALC", "Calculated Order", Guid.NewGuid()).Value;
        var dims = DoorDimensions.Create(900m, 850m, 2100m, 2050m, 200m, 180m).Value;
        order.AddItem(DoorItem.Create(order.Id, "P01", 1, DoorType.FAF_T, OpeningDirection.Left, dims));
        order.Submit();
        order.MarkCalculating();
        order.MarkCalculated();
        return order;
    }

    // ── GetDoorOrder cross-tenant isolation ───────────────────────────────────

    [Fact]
    public async Task GetDoorOrder_CrossTenant_ReturnsNotFound()
    {
        // Arrange — create an order belonging to TenantA
        var order = MakeDraftOrder(TenantA);
        await _repository.AddAsync(order, CancellationToken.None);

        var handler = new GetDoorOrderQueryHandler(_repository);

        // Act — query with TenantB should not see TenantA's order
        var result = await handler.Handle(
            new GetDoorOrderQuery(TenantB, order.Id),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse(
            because: "TenantB must not be able to read TenantA's order");
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task GetDoorOrder_SameTenant_ReturnsOrder()
    {
        // Arrange — create an order belonging to TenantA
        var order = MakeDraftOrder(TenantA, "PRJ-OWN");
        await _repository.AddAsync(order, CancellationToken.None);

        var handler = new GetDoorOrderQueryHandler(_repository);

        // Act — query with TenantA should succeed
        var result = await handler.Handle(
            new GetDoorOrderQuery(TenantA, order.Id),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(because: "TenantA must be able to read its own order");
        result.Value.Id.Should().Be(order.Id);
        result.Value.TenantId.Should().Be(TenantA);
    }

    // ── ListDoorOrders cross-tenant isolation ─────────────────────────────────

    [Fact]
    public async Task ListDoorOrders_CrossTenant_ReturnsEmpty()
    {
        // Arrange — create 3 orders for TenantA
        for (var i = 0; i < 3; i++)
        {
            var order = MakeDraftOrder(TenantA, $"PRJ-A{i:D2}");
            await _repository.AddAsync(order, CancellationToken.None);
        }

        var handler = new ListDoorOrdersQueryHandler(_repository);

        // Act — query with TenantB should see no results
        var result = await handler.Handle(
            new ListDoorOrdersQuery(TenantB, Page: 1, PageSize: 20),
            CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(because: "ListDoorOrders always returns success (empty list is not an error)");
        result.Value.Items.Should().BeEmpty(because: "TenantB must not see TenantA's orders");
        result.Value.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task ListDoorOrders_SameTenant_ReturnsOnlyOwnOrders()
    {
        // Arrange — 3 orders for TenantA, 2 for TenantB
        for (var i = 0; i < 3; i++)
            await _repository.AddAsync(MakeDraftOrder(TenantA, $"PRJ-AA{i}"), CancellationToken.None);
        for (var i = 0; i < 2; i++)
            await _repository.AddAsync(MakeDraftOrder(TenantB, $"PRJ-BB{i}"), CancellationToken.None);

        var handler = new ListDoorOrdersQueryHandler(_repository);

        // Act
        var resultA = await handler.Handle(
            new ListDoorOrdersQuery(TenantA, Page: 1, PageSize: 20),
            CancellationToken.None);

        // Assert — TenantA sees exactly its own 3 orders
        resultA.IsSuccess.Should().BeTrue();
        resultA.Value.TotalCount.Should().Be(3, because: "TenantA has exactly 3 orders");
        resultA.Value.Items.Should().AllSatisfy(dto =>
            dto.TenantId.Should().Be(TenantA, because: "all returned orders must belong to TenantA"));
    }

    // ── GetDoorOrder by repo directly: TenantId filter ────────────────────────

    [Fact]
    public async Task Repository_GetByIdAsync_CrossTenant_ReturnsNull()
    {
        // Arrange
        var order = MakeDraftOrder(TenantA);
        await _repository.AddAsync(order, CancellationToken.None);

        // Act — fetch with TenantB's ID
        var fetched = await _repository.GetByIdAsync(order.Id, TenantB, CancellationToken.None);

        // Assert
        fetched.Should().BeNull(
            because: "the repository must enforce TenantId filtering so cross-tenant reads return null");
    }
}
