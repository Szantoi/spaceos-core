using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Joinery.Application.Anyaglista.Repositories;

namespace SpaceOS.Modules.Joinery.Application.Anyaglista.Queries.GetAnyaglista;

/// <summary>Returns the latest Anyaglista for the given order.</summary>
public sealed class GetAnyaglistaQueryHandler
    : IRequestHandler<GetAnyaglistaQuery, Result<GetAnyaglistaResponse>>
{
    private readonly IAnyaglistaRepository _repository;

    public GetAnyaglistaQueryHandler(IAnyaglistaRepository repository)
    {
        _repository = repository;
    }

    public async Task<Result<GetAnyaglistaResponse>> Handle(
        GetAnyaglistaQuery request,
        CancellationToken ct)
    {
        var anyaglista = await _repository
            .GetByOrderIdAsync(request.OrderId, request.TenantId, ct)
            .ConfigureAwait(false);

        if (anyaglista is null)
            return Result<GetAnyaglistaResponse>.NotFound(
                $"Anyaglista for order {request.OrderId} not found.");

        return Result<GetAnyaglistaResponse>.Success(new GetAnyaglistaResponse(
            anyaglista.Id,
            anyaglista.StorageUrl ?? string.Empty,
            anyaglista.CreatedAt));
    }
}
