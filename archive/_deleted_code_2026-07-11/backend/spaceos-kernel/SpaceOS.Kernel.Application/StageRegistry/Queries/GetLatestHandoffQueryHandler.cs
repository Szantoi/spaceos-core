// SpaceOS.Kernel.Application/StageRegistry/Queries/GetLatestHandoffQueryHandler.cs
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Specifications;

namespace SpaceOS.Kernel.Application.StageRegistry.Queries;

/// <summary>Handles <see cref="GetLatestHandoffQuery"/>: returns the latest handoff for a (FlowEpic, source, target) triple.</summary>
internal sealed class GetLatestHandoffQueryHandler
    : IRequestHandler<GetLatestHandoffQuery, Result<StageHandoffDto>>
{
    private readonly IStageHandoffRepository _repository;

    /// <summary>Initialises a new <see cref="GetLatestHandoffQueryHandler"/>.</summary>
    public GetLatestHandoffQueryHandler(IStageHandoffRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<Result<StageHandoffDto>> Handle(GetLatestHandoffQuery request, CancellationToken ct)
    {
        var spec    = new LatestHandoffSpec(request.FlowEpicId, request.SourceStageCode, request.TargetStageCode);
        var handoff = await _repository.FirstOrDefaultAsync(spec, ct).ConfigureAwait(false);

        if (handoff is null)
            return Result.NotFound();

        return Result.Success(new StageHandoffDto(
            handoff.Id,
            handoff.TenantId,
            handoff.FlowEpicId,
            handoff.SourceStageCode,
            handoff.TargetStageCode,
            handoff.Version,
            handoff.IdempotencyKey,
            handoff.PayloadHash,
            handoff.HashAlgorithm,
            handoff.SourceActorId,
            handoff.TargetActorId,
            handoff.HandshakeId,
            handoff.CreatedAt));
    }
}
