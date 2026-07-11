using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Sales.Abstractions.Ports;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Interfaces;

namespace SpaceOS.Modules.Sales.Application.Customers.Commands;

public sealed record LinkCustomerToActorCommand(Guid CustomerId, Guid PlatformTenantId)
    : IRequest<Result<CustomerResponse>>;

public sealed class LinkCustomerToActorCommandHandler(
    ICustomerRepository customers,
    IActorDirectoryPort actorDirectory,
    ITenantContext tenantContext,
    IClock clock) : IRequestHandler<LinkCustomerToActorCommand, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> Handle(LinkCustomerToActorCommand cmd, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(cmd.CustomerId, ct).ConfigureAwait(false);
        if (customer is null) return Result.NotFound();
        var guard = tenantContext.EnsureSameTenant(customer);
        if (!guard.IsSuccess) return guard;

        // SEC-S-02: query Kernel to determine if a verified B2B handshake exists
        var actorResult = await actorDirectory
            .GetTenantActorAsync(tenantContext.TenantId, cmd.PlatformTenantId, ct)
            .ConfigureAwait(false);
        if (!actorResult.IsSuccess)
            return Result.Invalid(new ValidationError("Platform actor not found or not accessible."));

        var linkResult = customer.LinkToPlatformActor(
            cmd.PlatformTenantId,
            actorResult.Value.HasVerifiedHandshakeWithRequester,
            clock);

        if (!linkResult.IsSuccess) return Result.Invalid(linkResult.ValidationErrors.ToArray());

        customers.Update(customer);
        await customers.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success(CreateCustomerCommandHandler.MapToResponse(customer));
    }
}
