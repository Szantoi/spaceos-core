// SpaceOS.Kernel.Application/Tenants/Commands/UpdateTenantModulesCommandHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.Services;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Tenants.Commands;

/// <summary>
/// Handles <see cref="UpdateTenantModulesCommand"/>: validates and replaces the enabled module list
/// of an existing <see cref="Domain.Entities.Tenant"/>.
/// </summary>
internal sealed class UpdateTenantModulesCommandHandler : IRequestHandler<UpdateTenantModulesCommand, Result>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;
    private readonly IModuleRegistryService _moduleRegistry;

    /// <summary>Initialises a new <see cref="UpdateTenantModulesCommandHandler"/>.</summary>
    public UpdateTenantModulesCommandHandler(
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

    /// <summary>Executes the update-modules command.</summary>
    public async Task<Result> Handle(UpdateTenantModulesCommand request, CancellationToken ct)
    {
        var tenantId = TenantId.From(request.TenantId);
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, ct).ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound($"Tenant not found: {request.TenantId}");

        var validation = tenant.UpdateEnabledModules(request.Modules, _moduleRegistry);
        if (!validation.IsValid)
            return Result.Error(validation.ErrorMessage!);

        await _tenantRepository.UpdateAsync(tenant, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = tenant.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.Success();
    }
}
