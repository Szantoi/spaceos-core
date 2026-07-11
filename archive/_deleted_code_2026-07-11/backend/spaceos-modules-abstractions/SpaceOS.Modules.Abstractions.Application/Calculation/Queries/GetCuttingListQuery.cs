using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Abstractions.Domain.Results;
using SpaceOS.Modules.Abstractions.Domain.Services;
using SpaceOS.Modules.Abstractions.Domain.ValueObjects;

namespace SpaceOS.Modules.Abstractions.Application.Calculation.Queries;

public sealed record GetCuttingListQuery(
    Guid TemplateId,
    Guid TenantId,
    decimal Width,
    decimal Height,
    decimal Depth) : IRequest<Result<IReadOnlyList<CuttingListItem>>>;

public sealed class GetCuttingListHandler : IRequestHandler<GetCuttingListQuery, Result<IReadOnlyList<CuttingListItem>>>
{
    private readonly IAbstractionsRepository _repository;
    private readonly IProductCalculationEngine _engine;

    public GetCuttingListHandler(IAbstractionsRepository repository, IProductCalculationEngine engine)
    {
        _repository = repository;
        _engine = engine;
    }

    public async Task<Result<IReadOnlyList<CuttingListItem>>> Handle(
        GetCuttingListQuery request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetTemplateWithAllAsync(request.TemplateId, request.TenantId, cancellationToken)
                                        .ConfigureAwait(false);
        if (template == null)
            return Result<IReadOnlyList<CuttingListItem>>.NotFound($"Template {request.TemplateId} not found");
        if (template.TenantId != request.TenantId) return Result<IReadOnlyList<CuttingListItem>>.Forbidden();

        var root = new DimensionInput(request.Width, request.Height, request.Depth);
        var calc = _engine.Calculate(template, root);
        return Result<IReadOnlyList<CuttingListItem>>.Success(calc.CuttingList);
    }
}
