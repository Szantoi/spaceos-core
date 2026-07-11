using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Interfaces;

namespace SpaceOS.Modules.Sales.Application.Customers.Commands;

public sealed record DeactivateCustomerCommand(Guid CustomerId) : IRequest<Result<CustomerResponse>>;

public sealed class DeactivateCustomerCommandHandler(
    ICustomerRepository customers,
    ITenantContext tenantContext,
    IClock clock) : IRequestHandler<DeactivateCustomerCommand, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> Handle(DeactivateCustomerCommand cmd, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(cmd.CustomerId, ct).ConfigureAwait(false);
        if (customer is null) return Result.NotFound();
        var guard = tenantContext.EnsureSameTenant(customer);
        if (!guard.IsSuccess) return guard;

        var result = customer.Deactivate(clock);
        if (!result.IsSuccess) return Result.Invalid(result.ValidationErrors.ToArray());

        customers.Update(customer);
        await customers.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success(CreateCustomerCommandHandler.MapToResponse(customer));
    }
}
