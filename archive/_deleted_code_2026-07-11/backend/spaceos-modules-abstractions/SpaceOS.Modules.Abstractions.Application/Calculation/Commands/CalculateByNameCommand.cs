using Ardalis.Result;
using FluentValidation;
using MediatR;
using SpaceOS.Modules.Abstractions.Domain.Events;
using SpaceOS.Modules.Abstractions.Domain.Results;
using SpaceOS.Modules.Abstractions.Domain.Services;
using SpaceOS.Modules.Abstractions.Domain.ValueObjects;

namespace SpaceOS.Modules.Abstractions.Application.Calculation.Commands;

public sealed record CalculateByNameCommand(
    string TemplateName,
    Guid TenantId,
    decimal Width,
    decimal Height,
    decimal Depth,
    IReadOnlyDictionary<string, decimal>? ParameterOverrides = null) : IRequest<Result<CalculationResult>>;

public sealed class CalculateByNameValidator : AbstractValidator<CalculateByNameCommand>
{
    public CalculateByNameValidator()
    {
        RuleFor(x => x.TemplateName).NotEmpty();
        RuleFor(x => x.Width).GreaterThan(0);
        RuleFor(x => x.Height).GreaterThan(0);
        RuleFor(x => x.Depth).GreaterThan(0);
    }
}

public sealed class CalculateByNameHandler : IRequestHandler<CalculateByNameCommand, Result<CalculationResult>>
{
    private readonly IAbstractionsRepository _repository;
    private readonly IProductCalculationEngine _engine;

    public CalculateByNameHandler(IAbstractionsRepository repository, IProductCalculationEngine engine)
    {
        _repository = repository;
        _engine = engine;
    }

    public async Task<Result<CalculationResult>> Handle(
        CalculateByNameCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetTemplateByNameWithAllAsync(
            request.TemplateName, request.TenantId, cancellationToken).ConfigureAwait(false);

        if (template == null)
            return Result<CalculationResult>.NotFound($"Template '{request.TemplateName}' not found");

        if (template.TenantId != request.TenantId)
            return Result<CalculationResult>.Forbidden();

        var root = new DimensionInput(request.Width, request.Height, request.Depth);
        var result = _engine.Calculate(template, root, request.ParameterOverrides);

        template.AddDomainEvent(new CalculationCompleted(template.Id, template.TenantId, result.CuttingList.Count));

        return Result<CalculationResult>.Success(result);
    }
}
