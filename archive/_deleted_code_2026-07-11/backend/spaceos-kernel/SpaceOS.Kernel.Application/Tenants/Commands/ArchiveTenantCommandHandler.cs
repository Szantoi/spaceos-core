// SpaceOS.Kernel.Application/Tenants/Commands/ArchiveTenantCommandHandler.cs
using Ardalis.Result;
using MediatR;
using SpaceOS.Kernel.Application.Common;
using SpaceOS.Kernel.Domain.Exceptions;
using SpaceOS.Kernel.Domain.Repositories;
using SpaceOS.Kernel.Domain.ValueObjects;

namespace SpaceOS.Kernel.Application.Tenants.Commands;

/// <summary>Handles <see cref="ArchiveTenantCommand"/>: soft-deletes a <see cref="Domain.Entities.Tenant"/> by setting <c>IsArchived = true</c>.</summary>
internal sealed class ArchiveTenantCommandHandler : IRequestHandler<ArchiveTenantCommand, Result>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _domainEventDispatcher;

    /// <summary>Initialises a new <see cref="ArchiveTenantCommandHandler"/>.</summary>
    /// <param name="tenantRepository">Repository for tenant persistence.</param>
    /// <param name="unitOfWork">Unit of work for committing changes.</param>
    /// <param name="domainEventDispatcher">Dispatcher for domain events raised by the aggregate.</param>
    public ArchiveTenantCommandHandler(
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

    /// <summary>Executes the archive command.</summary>
    public async Task<Result> Handle(ArchiveTenantCommand request, CancellationToken ct)
    {
        var tenantId = TenantId.From(request.Id);
        var tenant = await _tenantRepository.GetByIdAsync(tenantId, ct).ConfigureAwait(false);

        if (tenant is null)
            return Result.NotFound($"Tenant not found: {request.Id}");

        try
        {
            tenant.Archive();
        }
        catch (DomainException ex)
        {
            return Result.Conflict(ex.Message);
        }

        await _tenantRepository.UpdateAsync(tenant, ct).ConfigureAwait(false);
        await _unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);

        var domainEvents = tenant.PopDomainEvents();
        await _domainEventDispatcher.DispatchAsync(domainEvents, ct).ConfigureAwait(false);

        return Result.NoContent();
    }
}
