// SpaceOS.Kernel.Application/StageRegistry/Queries/ListStageChainTemplatesQueryHandler.cs
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Result;
using Ardalis.Specification;
using MediatR;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;

namespace SpaceOS.Kernel.Application.StageRegistry.Queries;

/// <summary>Handles <see cref="ListStageChainTemplatesQuery"/>: returns all chain templates for a tenant.</summary>
internal sealed class ListStageChainTemplatesQueryHandler
    : IRequestHandler<ListStageChainTemplatesQuery, Result<IReadOnlyList<StageChainTemplateDto>>>
{
    private readonly IStageChainTemplateRepository _repository;

    /// <summary>Initialises a new <see cref="ListStageChainTemplatesQueryHandler"/>.</summary>
    public ListStageChainTemplatesQueryHandler(IStageChainTemplateRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    /// <inheritdoc/>
    public async Task<Result<IReadOnlyList<StageChainTemplateDto>>> Handle(
        ListStageChainTemplatesQuery request, CancellationToken ct)
    {
        var spec = new ChainTemplatesByTenantSpec(request.TenantId);
        var templates = await _repository.ListAsync(spec, ct).ConfigureAwait(false);

        var dtos = templates
            .Select(t => new StageChainTemplateDto(
                t.Id,
                t.TenantId,
                t.Name,
                t.IsDefault,
                t.Steps.Count,
                t.CreatedAt,
                t.UpdatedAt))
            .ToList();

        return Result.Success<IReadOnlyList<StageChainTemplateDto>>(dtos);
    }

    // Inline spec — only used in this query handler
    private sealed class ChainTemplatesByTenantSpec : Specification<StageChainTemplate>
    {
        public ChainTemplatesByTenantSpec(System.Guid tenantId)
        {
            Query.Where(t => t.TenantId == tenantId).OrderBy(t => t.Name);
        }
    }
}
