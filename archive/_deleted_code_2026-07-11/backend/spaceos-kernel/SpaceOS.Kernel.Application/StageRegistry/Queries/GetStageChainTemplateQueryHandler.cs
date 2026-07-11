// SpaceOS.Kernel.Application/StageRegistry/Queries/GetStageChainTemplateQueryHandler.cs
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Kernel.Application.StageRegistry.Queries;

/// <summary>Handles <see cref="GetStageChainTemplateQuery"/>: returns a chain template with its steps.</summary>
internal sealed class GetStageChainTemplateQueryHandler
    : IRequestHandler<GetStageChainTemplateQuery, Result<StageChainTemplateDetailDto>>
{
    private readonly IStageChainTemplateRepository _repository;

    /// <summary>Initialises a new <see cref="GetStageChainTemplateQueryHandler"/>.</summary>
    public GetStageChainTemplateQueryHandler(IStageChainTemplateRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<Result<StageChainTemplateDetailDto>> Handle(
        GetStageChainTemplateQuery request, CancellationToken ct)
    {
        var template = await _repository.GetByIdWithStepsAsync(request.Id, ct).ConfigureAwait(false);
        if (template is null)
            return Result.NotFound();

        var steps = template.Steps
            .OrderBy(s => s.SortOrder)
            .Select(s => new StageChainStepDto(
                s.Id,
                s.StageDefinitionId,
                s.StageCode,
                s.SortOrder,
                s.IsOptional))
            .ToList();

        var dto = new StageChainTemplateDetailDto(
            template.Id,
            template.TenantId,
            template.Name,
            template.IsDefault,
            steps,
            template.CreatedAt,
            template.UpdatedAt);

        return Result.Success(dto);
    }
}
