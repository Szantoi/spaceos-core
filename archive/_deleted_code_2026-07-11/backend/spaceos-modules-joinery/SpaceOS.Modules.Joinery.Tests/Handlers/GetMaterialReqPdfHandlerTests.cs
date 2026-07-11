using Ardalis.Result;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetMaterialRequirements;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetMaterialReqPdf;
using SpaceOS.Modules.Joinery.Domain.Aggregates;
using SpaceOS.Modules.Joinery.Domain.Entities;
using SpaceOS.Modules.Joinery.Domain.Enums;
using SpaceOS.Modules.Joinery.Domain.Results;
using SpaceOS.Modules.Joinery.Domain.Services;
using SpaceOS.Modules.Joinery.Domain.ValueObjects;
using SpaceOS.Modules.Joinery.Infrastructure.Handlers;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence;
using SpaceOS.Modules.Joinery.Infrastructure.Persistence.Repositories;

namespace SpaceOS.Modules.Joinery.Tests.Handlers;

public class GetMaterialReqPdfHandlerTests : IDisposable
{
    private readonly JoineryDbContext _db;
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<IProductionSheetGenerator> _generator = new();
    private readonly GetMaterialReqPdfQueryHandler _sut;

    private static readonly Guid TenantId = Guid.NewGuid();

    public GetMaterialReqPdfHandlerTests()
    {
        var opts = new DbContextOptionsBuilder<JoineryDbContext>()
            .UseInMemoryDatabase($"mat-pdf-tests-{Guid.NewGuid()}")
            .Options;
        _db = new JoineryDbContext(opts);

        var repo = new DoorOrderRepository(_db);

        _sut = new GetMaterialReqPdfQueryHandler(
            repo,
            _mediator.Object,
            _generator.Object,
            NullLogger<GetMaterialReqPdfQueryHandler>.Instance);
    }

    public void Dispose() => _db.Dispose();

    private static DoorOrder MakeOrder()
    {
        var order = DoorOrder.Create(TenantId, "PRJ-MAT", "Mat Test", Guid.NewGuid()).Value;
        var dims = DoorDimensions.Create(900m, 860m, 2100m, 2060m, 120m, 40m).Value;
        order.AddItem(DoorItem.Create(order.Id, "M01", 1, DoorType.FAF_T, OpeningDirection.Left, dims));
        return order;
    }

    [Fact]
    public async Task Handle_OrderNotFound_ReturnsNotFound()
    {
        var result = await _sut.Handle(
            new GetMaterialReqPdfQuery(Guid.NewGuid(), TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_WrongTenant_ReturnsNotFound()
    {
        var order = MakeOrder();
        _db.DoorOrders.Add(order);
        await _db.SaveChangesAsync();

        var result = await _sut.Handle(
            new GetMaterialReqPdfQuery(order.Id, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Status.Should().Be(ResultStatus.NotFound);
    }

    [Fact]
    public async Task Handle_ValidOrder_CallsGeneratorAndReturnsPdfStream()
    {
        var order = MakeOrder();
        _db.DoorOrders.Add(order);
        await _db.SaveChangesAsync();

        var reqs = new List<MaterialRequirement>
        {
            new("MDF 18mm", 18m, 4.320m, 0m)
        };

        _mediator
            .Setup(m => m.Send(It.IsAny<GetMaterialRequirementsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<MaterialRequirementsResponse>.Success(
                new MaterialRequirementsResponse(order.Id, reqs)));

        _generator
            .Setup(g => g.GenerateMaterialReqPdf(It.IsAny<DoorOrder>(), It.IsAny<IReadOnlyList<MaterialRequirement>>()))
            .Returns(new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 }));

        var result = await _sut.Handle(
            new GetMaterialReqPdfQuery(order.Id, TenantId),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        _generator.Verify(g => g.GenerateMaterialReqPdf(
            It.IsAny<DoorOrder>(),
            It.IsAny<IReadOnlyList<MaterialRequirement>>()), Times.Once);
    }
}
