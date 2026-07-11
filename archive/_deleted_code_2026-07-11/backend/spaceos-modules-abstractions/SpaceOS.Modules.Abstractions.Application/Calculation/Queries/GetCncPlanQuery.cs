using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Abstractions.Domain.Results;
using SpaceOS.Modules.Abstractions.Domain.Services;
using SpaceOS.Modules.Abstractions.Domain.ValueObjects;

namespace SpaceOS.Modules.Abstractions.Application.Calculation.Queries;

/// <summary>
/// Query to derive a CNC operation plan for a product template at given dimensions.
/// </summary>
public sealed record GetCncPlanQuery(
    Guid TemplateId,
    Guid TenantId,
    decimal Width,
    decimal Height,
    decimal Depth) : IRequest<Result<IReadOnlyList<CncOperation>>>;

/// <summary>
/// Handles <see cref="GetCncPlanQuery"/>.
/// </summary>
public sealed class GetCncPlanHandler : IRequestHandler<GetCncPlanQuery, Result<IReadOnlyList<CncOperation>>>
{
    private readonly IAbstractionsRepository _repository;
    private readonly IProductCalculationEngine _engine;
    private readonly IManufacturingDerivation _derivation;

    public GetCncPlanHandler(
        IAbstractionsRepository repository,
        IProductCalculationEngine engine,
        IManufacturingDerivation derivation)
    {
        _repository = repository;
        _engine = engine;
        _derivation = derivation;
    }

    public async Task<Result<IReadOnlyList<CncOperation>>> Handle(
        GetCncPlanQuery request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetTemplateWithAllAsync(request.TemplateId, request.TenantId, cancellationToken)
                                        .ConfigureAwait(false);
        if (template == null)
            return Result<IReadOnlyList<CncOperation>>.NotFound($"Template {request.TemplateId} not found");
        if (template.TenantId != request.TenantId)
            return Result<IReadOnlyList<CncOperation>>.Forbidden();

        var root = new DimensionInput(request.Width, request.Height, request.Depth);
        var calc = _engine.Calculate(template, root);
        return Result<IReadOnlyList<CncOperation>>.Success(_derivation.DeriveCncPlan(calc));
    }
}
