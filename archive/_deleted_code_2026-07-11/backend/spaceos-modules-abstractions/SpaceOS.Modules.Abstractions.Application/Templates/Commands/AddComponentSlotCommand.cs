using Ardalis.Result;
using FluentValidation;
using MediatR;
using SpaceOS.Modules.Abstractions.Domain.Entities;
using SpaceOS.Modules.Abstractions.Domain.Enums;

namespace SpaceOS.Modules.Abstractions.Application.Templates.Commands;

public sealed record AddComponentSlotCommand(
    Guid TemplateId,
    Guid TenantId,
    string Name,
    string ComponentType,
    string? DefaultMaterial,
    decimal? DefaultThickness,
    int Quantity,
    bool IsVirtual,
    SemanticRole? SemanticRole,
    int SortOrder) : IRequest<Result<ComponentSlot>>;

public sealed class AddComponentSlotValidator : AbstractValidator<AddComponentSlotCommand>
{
    public AddComponentSlotValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.ComponentType).NotEmpty()
            .Must(ct => ComponentSlot.GetAllowedComponentTypes().Contains(ct))
            .WithMessage("Invalid ComponentType");
        RuleFor(x => x.Quantity).InclusiveBetween(1, 100);
    }
}

public sealed class AddComponentSlotHandler : IRequestHandler<AddComponentSlotCommand, Result<ComponentSlot>>
{
    private readonly IAbstractionsRepository _repository;

    public AddComponentSlotHandler(IAbstractionsRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ComponentSlot>> Handle(
        AddComponentSlotCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetTemplateWithAllAsync(request.TemplateId, request.TenantId, cancellationToken)
                                        .ConfigureAwait(false);
        if (template == null)
            return Result<ComponentSlot>.NotFound($"Template {request.TemplateId} not found");
        if (template.TenantId != request.TenantId)
            return Result<ComponentSlot>.Forbidden();

        var result = template.AddSlot(request.Name, request.ComponentType, request.DefaultMaterial,
            request.DefaultThickness, request.Quantity, request.IsVirtual,
            request.SemanticRole, request.SortOrder);
        if (!result.IsSuccess) return result;

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }
}
