using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Domain.Interfaces;

namespace SpaceOS.Modules.Sales.Application.Customers.Commands;

public sealed record UnlinkCustomerFromActorCommand(Guid CustomerId) : IRequest<Result<CustomerResponse>>;

public sealed class UnlinkCustomerFromActorCommandHandler(
    ICustomerRepository customers,
    ITenantContext tenantContext) : IRequestHandler<UnlinkCustomerFromActorCommand, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> Handle(UnlinkCustomerFromActorCommand cmd, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(cmd.CustomerId, ct).ConfigureAwait(false);
        if (customer is null) return Result.NotFound();
        var guard = tenantContext.EnsureSameTenant(customer);
        if (!guard.IsSuccess) return guard;

        var result = customer.UnlinkFromPlatformActor();
        if (!result.IsSuccess) return Result.Invalid(result.ValidationErrors.ToArray());

        customers.Update(customer);
        await customers.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success(CreateCustomerCommandHandler.MapToResponse(customer));
    }
}
