using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Interfaces;
using SpaceOS.Modules.Sales.Domain.ValueObjects;

namespace SpaceOS.Modules.Sales.Application.Customers.Commands;

public sealed record UpdateCustomerAddressesCommand(
    Guid CustomerId,
    AddressDto? BillingAddress,
    AddressDto? ShippingAddress) : IRequest<Result<CustomerResponse>>;

public sealed class UpdateCustomerAddressesCommandHandler(
    ICustomerRepository customers,
    ITenantContext tenantContext,
    IClock clock) : IRequestHandler<UpdateCustomerAddressesCommand, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> Handle(UpdateCustomerAddressesCommand cmd, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(cmd.CustomerId, ct).ConfigureAwait(false);
        if (customer is null) return Result.NotFound();
        var guard = tenantContext.EnsureSameTenant(customer);
        if (!guard.IsSuccess) return guard;

        var billing = cmd.BillingAddress is null
            ? null
            : new Address(cmd.BillingAddress.Street, cmd.BillingAddress.City, cmd.BillingAddress.ZipCode, cmd.BillingAddress.Country);
        var shipping = cmd.ShippingAddress is null
            ? null
            : new Address(cmd.ShippingAddress.Street, cmd.ShippingAddress.City, cmd.ShippingAddress.ZipCode, cmd.ShippingAddress.Country);

        var result = customer.UpdateAddresses(billing, shipping, clock);
        if (!result.IsSuccess) return Result.Invalid(result.ValidationErrors.ToArray());

        customers.Update(customer);
        await customers.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success(CreateCustomerCommandHandler.MapToResponse(customer));
    }
}
