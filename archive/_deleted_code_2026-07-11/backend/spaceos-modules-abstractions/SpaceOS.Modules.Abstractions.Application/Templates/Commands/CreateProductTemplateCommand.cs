using Ardalis.Result;
using FluentValidation;
using MediatR;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;

namespace SpaceOS.Modules.Abstractions.Application.Templates.Commands;

public sealed record CreateProductTemplateCommand(
    Guid TenantId,
    string TradeType,
    string Name) : IRequest<Result<ProductTemplate>>;

public sealed class CreateProductTemplateValidator : AbstractValidator<CreateProductTemplateCommand>
{
    private static readonly string[] AllowedTradeTypes = { "door", "cabinet", "window", "generic" };

    public CreateProductTemplateValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.TradeType).NotEmpty()
            .Must(t => AllowedTradeTypes.Contains(t?.ToLowerInvariant()))
            .WithMessage("TradeType must be one of: door, cabinet, window, generic");
        RuleFor(x => x.TenantId).NotEmpty();
    }
}

public sealed class CreateProductTemplateHandler : IRequestHandler<CreateProductTemplateCommand, Result<ProductTemplate>>
{
    private readonly IAbstractionsRepository _repository;

    public CreateProductTemplateHandler(IAbstractionsRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductTemplate>> Handle(
        CreateProductTemplateCommand request, CancellationToken cancellationToken)
    {
        var result = ProductTemplate.Create(request.TenantId, request.TradeType, request.Name);
        if (!result.IsSuccess) return result;

        await _repository.AddTemplateAsync(result.Value, cancellationToken).ConfigureAwait(false);
        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }
}
