using Ardalis.Result;
using MediatR;
using Microsoft.Extensions.Logging;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetMaterialRequirements;
using SpaceOS.Modules.Joinery.Application.Orders.Queries.GetMaterialReqPdf;
using SpaceOS.Modules.Joinery.Application.Orders.Repositories;
using SpaceOS.Modules.Joinery.Domain.Services;

namespace SpaceOS.Modules.Joinery.Infrastructure.Handlers;

/// <summary>
/// Handles <see cref="GetMaterialReqPdfQuery"/>.
/// Delegates data retrieval to <see cref="GetMaterialRequirementsQueryHandler"/> via IMediator,
/// then generates a PDF via <see cref="IProductionSheetGenerator"/>.
/// </summary>
public sealed class GetMaterialReqPdfQueryHandler
    : IRequestHandler<GetMaterialReqPdfQuery, Result<Stream>>
{
    private readonly IDoorOrderRepository _repository;
    private readonly IMediator _mediator;
    private readonly IProductionSheetGenerator _generator;
    private readonly ILogger<GetMaterialReqPdfQueryHandler> _logger;

    public GetMaterialReqPdfQueryHandler(
        IDoorOrderRepository repository,
        IMediator mediator,
        IProductionSheetGenerator generator,
        ILogger<GetMaterialReqPdfQueryHandler> logger)
    {
        _repository = repository;
        _mediator = mediator;
        _generator = generator;
        _logger = logger;
    }

    public async Task<Result<Stream>> Handle(GetMaterialReqPdfQuery query, CancellationToken ct)
    {
        var order = await _repository.GetByIdAsync(query.OrderId, query.TenantId, ct).ConfigureAwait(false);
        if (order is null)
            return Result<Stream>.NotFound("Order not found");

        var dataResult = await _mediator
            .Send(new GetMaterialRequirementsQuery(query.TenantId, query.OrderId), ct)
            .ConfigureAwait(false);

        if (!dataResult.IsSuccess)
            return Result<Stream>.Error(string.Join("; ", dataResult.Errors));

        var pdfStream = _generator.GenerateMaterialReqPdf(order, dataResult.Value.Requirements);

        _logger.LogInformation(
            "Generated material requirements PDF for order {OrderId} ({ReqCount} requirements)",
            query.OrderId, dataResult.Value.Requirements.Count);

        return Result<Stream>.Success(pdfStream);
    }
}
