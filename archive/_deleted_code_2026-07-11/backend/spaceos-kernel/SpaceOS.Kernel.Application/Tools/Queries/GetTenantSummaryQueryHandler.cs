// SpaceOS.Kernel.Application/Tools/Queries/GetTenantSummaryQueryHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Specifications;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Tools.Queries;

/// <summary>
/// Handles <see cref="GetTenantSummaryQuery"/>.
/// BE-P2-02: Uses 3 independent count queries — no FULL OUTER JOIN cartesian product.
/// </summary>
internal sealed class GetTenantSummaryQueryHandler
    : IRequestHandler<GetTenantSummaryQuery, Result<TenantSummaryDto>>
{
    private readonly IFlowEpicRepository _flowEpicRepository;
    private readonly IWorkStationRepository _workStationRepository;
    private readonly IFacilityRepository _facilityRepository;

    /// <summary>Initialises the handler.</summary>
    /// <param name="flowEpicRepository">The flow epic repository.</param>
    /// <param name="workStationRepository">The workstation repository.</param>
    /// <param name="facilityRepository">The facility repository.</param>
    public GetTenantSummaryQueryHandler(
        IFlowEpicRepository flowEpicRepository,
        IWorkStationRepository workStationRepository,
        IFacilityRepository facilityRepository)
    {
        ArgumentNullException.ThrowIfNull(flowEpicRepository);
        ArgumentNullException.ThrowIfNull(workStationRepository);
        ArgumentNullException.ThrowIfNull(facilityRepository);
        _flowEpicRepository    = flowEpicRepository;
        _workStationRepository = workStationRepository;
        _facilityRepository    = facilityRepository;
    }

    /// <inheritdoc/>
    public async Task<Result<TenantSummaryDto>> Handle(
        GetTenantSummaryQuery request,
        CancellationToken ct)
    {
        var tenantId = TenantId.From(request.TenantId);

        // BE-P2-02: 3 independent count subqueries — no cartesian product, no JOIN.
        var flowEpicCount = await _flowEpicRepository
            .CountAsync(new FlowEpicsByTenantIdSpec(tenantId), ct)
            .ConfigureAwait(false);

        var activeWorkstationCount = await _workStationRepository
            .CountAsync(new WorkStationsByTenantIdSpec(tenantId), ct)
            .ConfigureAwait(false);

        var facilityCount = await _facilityRepository
            .CountAsync(new FacilitiesByTenantIdSpec(tenantId), ct)
            .ConfigureAwait(false);

        return Result.Success(new TenantSummaryDto(
            flowEpicCount,
            activeWorkstationCount,
            facilityCount));
    }
}
