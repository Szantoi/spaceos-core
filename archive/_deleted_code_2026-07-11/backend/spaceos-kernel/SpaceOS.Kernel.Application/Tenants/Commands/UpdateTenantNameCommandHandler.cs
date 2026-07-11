using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Tenants.Commands;

/// <summary>
/// Handles <see cref="UpdateTenantNameCommand"/>: loads the tenant, updates its name,
/// persists the change, and dispatches any raised domain events.
/// </summary>
public class UpdateTenantNameCommandHandler : IRequestHandler<UpdateTenantNameCommand, Result>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    public UpdateTenantNameCommandHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher)
    {
        ArgumentNullException.ThrowIfNull(tenantRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(domainEventDispatcher);
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
    }

    public async Task<Result> Handle(UpdateTenantNameCommand request, CancellationToken ct)
    {
        var tenantId = TenantId.From(request.TenantId);
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, ct).ConfigureAwait(false);

        if (tenant is null)
        {
            return Result.NotFound($"Tenant not found: {request.TenantId}");
        }

        tenant.UpdateName(request.NewName);

        await _tenantRepository.UpdateAsync(tenant, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = tenant.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
