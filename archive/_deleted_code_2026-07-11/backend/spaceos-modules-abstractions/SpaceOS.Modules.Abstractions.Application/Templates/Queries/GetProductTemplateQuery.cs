using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;

namespace SpaceOS.Modules.Abstractions.Application.Templates.Queries;

public sealed record GetProductTemplateQuery(Guid TemplateId, Guid TenantId) : IRequest<Result<ProductTemplate>>;

public sealed class GetProductTemplateHandler : IRequestHandler<GetProductTemplateQuery, Result<ProductTemplate>>
{
    private readonly IAbstractionsRepository _repository;

    public GetProductTemplateHandler(IAbstractionsRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<ProductTemplate>> Handle(
        GetProductTemplateQuery request, CancellationToken cancellationToken)
    {
        var template = await _repository.GetTemplateAsync(request.TemplateId, request.TenantId, cancellationToken)
                                        .ConfigureAwait(false);
        if (template == null) return Result<ProductTemplate>.NotFound($"Template {request.TemplateId} not found");
        if (template.TenantId != request.TenantId) return Result<ProductTemplate>.Forbidden();
        return Result<ProductTemplate>.Success(template);
    }
}
