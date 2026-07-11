using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Entities;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Services;

namespace SpaceOS.Kernel.Application.Tenants.Commands;

/// <summary>
/// Handles <see cref="CreateTenantCommand"/>: creates a new <see cref="Tenant"/> aggregate,
/// persists it, and returns its identifier.
/// </summary>
public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Result<Guid>>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IModuleRegistryService _moduleRegistry;

    /// <summary>Initialises a new <see cref="CreateTenantCommandHandler"/>.</summary>
    public CreateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher domainEventDispatcher,
        IModuleRegistryService moduleRegistry)
    {
        ArgumentNullException.ThrowIfNull(tenantRepository);
        ArgumentNullException.ThrowIfNull(unitOfWork);
        ArgumentNullException.ThrowIfNull(domainEventDispatcher);
        ArgumentNullException.ThrowIfNull(moduleRegistry);
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _domainEventDispatcher = domainEventDispatcher;
        _moduleRegistry = moduleRegistry;
    }

    /// <summary>Executes the create-tenant command.</summary>
    public async Task<Result<Guid>> Handle(CreateTenantCommand request, CancellationToken ct)
    {
        // Validate modules upfront if provided
        if (request.EnabledModules is { Length: > 0 })
        {
            var validation = _moduleRegistry.ValidateModulesForTenantType(
                request.TenantType, request.EnabledModules);
            if (!validation.IsValid)
                return Result<Guid>.Error(validation.ErrorMessage!);
        }

        var tenant = Tenant.Register(request.Name, request.TenantType, request.EnabledModules);

        await _tenantRepository.AddAsync(tenant, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = tenant.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.Success(tenant.Id.Value);
    }
}
