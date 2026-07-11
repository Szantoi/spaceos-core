using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;

namespace SpaceOS.Modules.Abstractions.Application.Templates.Queries;

public sealed record GetTemplateGraphQuery(Guid TemplateId, Guid TenantId) : IRequest<Result<ProductTemplate>>;

public sealed class GetTemplateGraphHandler : IRequestHandler<GetTemplateGraphQuery, Result<ProductTemplate>>
{
    private readonly IAbstractionsRepository _repository;

    public GetTemplateGraphHandler(IAbstractionsRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductTemplate>> Handle(
        GetTemplateGraphQuery request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetTemplateWithAllAsync(request.TemplateId, request.TenantId, cancellationToken)
                                        .ConfigureAwait(false);
        if (template == null) return Result<ProductTemplate>.NotFound($"Template {request.TemplateId} not found");
        if (template.TenantId != request.TenantId) return Result<ProductTemplate>.Forbidden();
        return Result<ProductTemplate>.Success(template);
    }
}
