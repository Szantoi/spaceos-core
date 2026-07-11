using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Gyartasilap.Repositories;
using SpaceOS.Modules.Joinery.Domain.Core;

namespace SpaceOS.Modules.Joinery.Application.Gyartasilap.Queries.ListGyartasilapByOrder;

public sealed record ListGyartasilapItem(
    Guid Id,
    string LabelVariant,
    GyartasilapStatus Status,
    string? StorageUrl,
    DateTimeOffset CreatedAt);

public sealed class ListGyartasilapByOrderQueryHandler
    : IRequestHandler<ListGyartasilapByOrderQuery, Result<IReadOnlyList<ListGyartasilapItem>>>
{
    private readonly IGyartasilapRepository _repository;

    public ListGyartasilapByOrderQueryHandler(IGyartasilapRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<IReadOnlyList<ListGyartasilapItem>>> Handle(
        ListGyartasilapByOrderQuery request,
        CancellationToken ct)
    {
        var items = await _repository
            .ListByOrderAsync(request.JoineryOrderId, request.TenantId, request.Status, ct)
            .ConfigureAwait(false);

        var result = items
            .Select(g => new ListGyartasilapItem(g.Id, g.LabelVariant, g.Status, g.StorageUrl, g.CreatedAt))
            .ToList();

        return Result<IReadOnlyList<ListGyartasilapItem>>.Success(result);
    }
}
