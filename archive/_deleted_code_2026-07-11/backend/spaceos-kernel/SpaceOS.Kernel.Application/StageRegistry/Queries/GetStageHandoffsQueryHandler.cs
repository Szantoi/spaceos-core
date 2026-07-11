// SpaceOS.Kernel.Application/StageRegistry/Queries/GetStageHandoffsQueryHandler.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Specifications;

namespace SpaceOS.Kernel.Application.StageRegistry.Queries;

/// <summary>Handles <see cref="GetStageHandoffsQuery"/>: returns handoffs for a flow epic ordered by creation time.</summary>
internal sealed class GetStageHandoffsQueryHandler
    : IRequestHandler<GetStageHandoffsQuery, Result<IReadOnlyList<StageHandoffDto>>>
{
    private readonly IStageHandoffRepository _repository;

    /// <summary>Initialises a new <see cref="GetStageHandoffsQueryHandler"/>.</summary>
    public GetStageHandoffsQueryHandler(IStageHandoffRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<StageHandoffDto>>> Handle(
        GetStageHandoffsQuery request, CancellationToken ct)
    {
        var handoffs = await _repository
            .ListAsync(new HandoffsByFlowEpicSpec(request.FlowEpicId), ct)
            .ConfigureAwait(false);

        var dtos = handoffs
            .Select(h => new StageHandoffDto(
                h.Id,
                h.TenantId,
                h.FlowEpicId,
                h.SourceStageCode,
                h.TargetStageCode,
                h.Version,
                h.IdempotencyKey,
                h.PayloadHash,
                h.HashAlgorithm,
                h.SourceActorId,
                h.TargetActorId,
                h.HandshakeId,
                h.CreatedAt))
            .ToList();

        return Result.Success<IReadOnlyList<StageHandoffDto>>(dtos);
    }
}
