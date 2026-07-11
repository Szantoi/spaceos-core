using Ardalis.Result;
using FluentValidation;
using MediatR;
using SpaceOS.Modules.Abstractions.Domain.Entities;
using SpaceOS.Modules.Abstractions.Domain.Enums;
using SpaceOS.Modules.Abstractions.Domain.Services;

namespace SpaceOS.Modules.Abstractions.Application.Templates.Commands;

public sealed record AddSlotConnectionCommand(
    Guid TemplateId,
    Guid TenantId,
    Guid ParentSlotId,
    Guid ChildSlotId,
    DimensionAxis Axis,
    RuleOperator Operator,
    decimal Operand,
    int? MultiplierCount,
    Guid? SecondaryParentSlotId,
    JointType JointType,
    MachiningOperation MachiningOp,
    ProcessPhase ProcessPhase) : IRequest<Result<SlotConnection>>;

public sealed class AddSlotConnectionValidator : AbstractValidator<AddSlotConnectionCommand>
{
    public AddSlotConnectionValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.ParentSlotId).NotEmpty();
        RuleFor(x => x.ChildSlotId).NotEmpty();
        RuleFor(x => x).Must(x => x.ParentSlotId != x.ChildSlotId)
            .WithMessage("ParentSlotId must differ from ChildSlotId");
        RuleFor(x => x.Axis).IsInEnum();
        RuleFor(x => x.Operator).IsInEnum();
    }
}

public sealed class AddSlotConnectionHandler : IRequestHandler<AddSlotConnectionCommand, Result<SlotConnection>>
{
    private readonly IAbstractionsRepository _repository;
    private readonly ITemplateValidator _validator;

    public AddSlotConnectionHandler(IAbstractionsRepository repository, ITemplateValidator validator)
    {
        _repository = repository;
        _validator = validator;
    }

    public async Task<Result<SlotConnection>> Handle(
        AddSlotConnectionCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetTemplateWithAllAsync(request.TemplateId, request.TenantId, cancellationToken)
                                        .ConfigureAwait(false);
        if (template == null) return Result<SlotConnection>.NotFound($"Template {request.TemplateId} not found");
        if (template.TenantId != request.TenantId) return Result<SlotConnection>.Forbidden();

        var result = template.AddConnection(
            request.ParentSlotId, request.ChildSlotId, request.Axis,
            request.Operator, request.Operand, request.MultiplierCount,
            request.SecondaryParentSlotId,
            request.JointType, request.MachiningOp, request.ProcessPhase);
        if (!result.IsSuccess) return result;

        var validation = _validator.Validate(template);
        if (!validation.IsSuccess)
            return Result<SlotConnection>.Invalid(validation.ValidationErrors.First());

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }
}
