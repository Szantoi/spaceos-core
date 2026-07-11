// SpaceOS.Kernel.Application/StageRegistry/Queries/ListStageDefinitionsQueryHandler.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Specifications;

namespace SpaceOS.Kernel.Application.StageRegistry.Queries;

/// <summary>Handles <see cref="ListStageDefinitionsQuery"/>: returns all active stage definitions.</summary>
internal sealed class ListStageDefinitionsQueryHandler
    : IRequestHandler<ListStageDefinitionsQuery, Result<IReadOnlyList<StageDefinitionDto>>>
{
    private readonly IStageDefinitionRepository _repository;

    /// <summary>Initialises a new <see cref="ListStageDefinitionsQueryHandler"/>.</summary>
    public ListStageDefinitionsQueryHandler(IStageDefinitionRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<StageDefinitionDto>>> Handle(
        ListStageDefinitionsQuery request, CancellationToken ct)
    {
        var definitions = await _repository
            .ListAsync(new ActiveStageDefinitionsSpec(), ct)
            .ConfigureAwait(false);

        var dtos = definitions
            .Select(sd => new StageDefinitionDto(
                sd.Id,
                sd.TenantId,
                sd.StageCode,
                sd.DisplayName,
                sd.ModuleEndpoint,
                sd.IsActive,
                sd.CreatedAt,
                sd.UpdatedAt))
            .ToList();

        return Result.Success<IReadOnlyList<StageDefinitionDto>>(dtos);
    }
}
