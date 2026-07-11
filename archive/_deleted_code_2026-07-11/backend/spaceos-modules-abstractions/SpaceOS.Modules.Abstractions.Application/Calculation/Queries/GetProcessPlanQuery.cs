using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Abstractions.Domain.Results;
using SpaceOS.Modules.Abstractions.Domain.Services;
using SpaceOS.Modules.Abstractions.Domain.ValueObjects;

namespace SpaceOS.Modules.Abstractions.Application.Calculation.Queries;

/// <summary>
/// Query to derive a production process plan for a product template at given dimensions.
/// </summary>
public sealed record GetProcessPlanQuery(
    Guid TemplateId,
    Guid TenantId,
    decimal Width,
    decimal Height,
    decimal Depth) : IRequest<Result<IReadOnlyList<ProductionStep>>>;

/// <summary>
/// Handles <see cref="GetProcessPlanQuery"/>.
/// </summary>
public sealed class GetProcessPlanHandler : IRequestHandler<GetProcessPlanQuery, Result<IReadOnlyList<ProductionStep>>>
{
    private readonly IAbstractionsRepository _repository;
    private readonly IProductCalculationEngine _engine;
    private readonly IManufacturingDerivation _derivation;

    public GetProcessPlanHandler(
        IAbstractionsRepository repository,
        IProductCalculationEngine engine,
        IManufacturingDerivation derivation)
    {
        _repository = repository;
        _engine = engine;
        _derivation = derivation;
    }

    public async Task<Result<IReadOnlyList<ProductionStep>>> Handle(
        GetProcessPlanQuery request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetTemplateWithAllAsync(request.TemplateId, request.TenantId, cancellationToken)
                                        .ConfigureAwait(false);
        if (template == null)
            return Result<IReadOnlyList<ProductionStep>>.NotFound($"Template {request.TemplateId} not found");
        if (template.TenantId != request.TenantId)
            return Result<IReadOnlyList<ProductionStep>>.Forbidden();

        var root = new DimensionInput(request.Width, request.Height, request.Depth);
        var calc = _engine.Calculate(template, root);
        return Result<IReadOnlyList<ProductionStep>>.Success(_derivation.DeriveProcessPlan(calc));
    }
}
