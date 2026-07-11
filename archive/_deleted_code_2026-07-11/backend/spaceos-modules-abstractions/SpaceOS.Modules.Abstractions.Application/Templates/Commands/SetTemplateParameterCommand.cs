using Ardalis.Result;
using FluentValidation;
using MediatR;

namespace SpaceOS.Modules.Abstractions.Application.Templates.Commands;

public sealed record SetTemplateParameterCommand(
    Guid TemplateId,
    Guid TenantId,
    string Key,
    decimal Value,
    string? Description) : IRequest<Result>;

public sealed class SetTemplateParameterValidator : AbstractValidator<SetTemplateParameterCommand>
{
    public SetTemplateParameterValidator()
    {
        RuleFor(x => x.TemplateId).NotEmpty();
        RuleFor(x => x.Key).NotEmpty().MaximumLength(50);
    }
}

public sealed class SetTemplateParameterHandler : IRequestHandler<SetTemplateParameterCommand, Result>
{
    private readonly IAbstractionsRepository _repository;

    public SetTemplateParameterHandler(IAbstractionsRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result> Handle(SetTemplateParameterCommand request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetTemplateWithAllAsync(request.TemplateId, request.TenantId, cancellationToken)
                                        .ConfigureAwait(false);
        if (template == null) return Result.NotFound($"Template {request.TemplateId} not found");
        if (template.TenantId != request.TenantId) return Result.Forbidden();

        var result = template.SetParameter(request.Key, request.Value, request.Description);
        if (!result.IsSuccess) return result;

        await _repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return Result.Success();
    }
}
