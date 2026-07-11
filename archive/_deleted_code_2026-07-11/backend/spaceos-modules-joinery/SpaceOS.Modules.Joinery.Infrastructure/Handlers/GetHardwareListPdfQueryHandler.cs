using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetHardwareList;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetHardwareListPdf;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Infrastructure.Handlers;

/// <summary>
/// Handles <see cref="GetHardwareListPdfQuery"/>.
/// Delegates data retrieval to <see cref="GetHardwareListQueryHandler"/> via IMediator,
/// then generates a PDF via <see cref="IProductionSheetGenerator"/>.
/// </summary>
public sealed class GetHardwareListPdfQueryHandler
    : IRequestHandler<GetHardwareListPdfQuery, Result<Stream>>
{
    private readonly IDoorOrderRepository _repository;
    private readonly IMediator _mediator;
    private readonly IProductionSheetGenerator _generator;
    private readonly ILogger<GetHardwareListPdfQueryHandler> _logger;

    public GetHardwareListPdfQueryHandler(
        IDoorOrderRepository repository,
        IMediator mediator,
        IProductionSheetGenerator generator,
        ILogger<GetHardwareListPdfQueryHandler> logger)
    {
        _repository = repository;
        _mediator = mediator;
        _generator = generator;
        _logger = logger;
    }

    public async Task<Result<Stream>> Handle(GetHardwareListPdfQuery query, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(query.OrderId, query.TenantId, ct).ConfigureAwait(false);
        if (order is null)
            return Result<Stream>.NotFound("Order not found");

        var dataResult = await _mediator
            .Send(new GetHardwareListQuery(query.TenantId, query.OrderId), ct)
            .ConfigureAwait(false);

        if (!dataResult.IsSuccess)
            return Result<Stream>.Error(string.Join("; ", dataResult.Errors));

        var pdfStream = _generator.GenerateHardwareListPdf(order, dataResult.Value.Items);

        _logger.LogInformation(
            "Generated hardware list PDF for order {OrderId} ({ItemCount} items)",
            query.OrderId, dataResult.Value.Items.Count);

        return Result<Stream>.Success(pdfStream);
    }
}
