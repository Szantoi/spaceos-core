using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Abstractions.Domain.Aggregates;

namespace SpaceOS.Modules.Abstractions.Application.Templates.Queries;

public sealed record ListProductTemplatesQuery(
    Guid TenantId,
    string? TradeTypeFilter = null,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<IReadOnlyList<ProductTemplate>>>;

public sealed class ListProductTemplatesHandler
    : IRequestHandler<ListProductTemplatesQuery, Result<IReadOnlyList<ProductTemplate>>>
{
    private readonly IAbstractionsRepository _repository;

    public ListProductTemplatesHandler(IAbstractionsRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<ProductTemplate>>> Handle(
        ListProductTemplatesQuery request, CancellationToken cancellationToken)
    {
        var items = await _repository.ListTemplatesAsync(
            request.TenantId, request.TradeTypeFilter, request.Page, request.PageSize, cancellationToken)
            .ConfigureAwait(false);
        return Result<IReadOnlyList<ProductTemplate>>.Success(items);
    }
}
