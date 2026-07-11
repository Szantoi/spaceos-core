using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Sales.Abstractions.Ports;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Enums;
using SpaceOS.Modules.Sales.Domain.Interfaces;

namespace SpaceOS.Modules.Sales.Application.Customers.Commands;

/// <summary>
/// SEC-S-02: Promotes a Pending link to Verified if the Kernel handshake is now confirmed.
/// </summary>
public sealed record RefreshCustomerLinkCommand(Guid CustomerId) : IRequest<Result<CustomerResponse>>;

public sealed class RefreshCustomerLinkCommandHandler(
    ICustomerRepository customers,
    IActorDirectoryPort actorDirectory,
    ITenantContext tenantContext,
    IClock clock) : IRequestHandler<RefreshCustomerLinkCommand, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> Handle(RefreshCustomerLinkCommand cmd, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(cmd.CustomerId, ct).ConfigureAwait(false);
        if (customer is null) return Result.NotFound();
        var guard = tenantContext.EnsureSameTenant(customer);
        if (!guard.IsSuccess) return guard;

        if (customer.LinkStatus != LinkVerificationStatus.Pending)
            return Result.Invalid(new ValidationError("No pending link to refresh."));

        var actorResult = await actorDirectory
            .GetTenantActorAsync(tenantContext.TenantId, customer.LinkedTenantId!.Value, ct)
            .ConfigureAwait(false);
        if (!actorResult.IsSuccess)
            return Result.Invalid(new ValidationError("Platform actor not accessible."));

        if (!actorResult.Value.HasVerifiedHandshakeWithRequester)
            return Result.Success(CreateCustomerCommandHandler.MapToResponse(customer)); // still Pending

        var verifyResult = customer.MarkLinkVerified(clock);
        if (!verifyResult.IsSuccess) return Result.Invalid(verifyResult.ValidationErrors.ToArray());

        customers.Update(customer);
        await customers.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success(CreateCustomerCommandHandler.MapToResponse(customer));
    }
}
