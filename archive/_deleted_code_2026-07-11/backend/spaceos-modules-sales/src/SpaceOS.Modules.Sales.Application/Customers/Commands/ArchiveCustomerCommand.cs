using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Domain.Common;
using SpaceOS.Modules.Sales.Domain.Interfaces;

namespace SpaceOS.Modules.Sales.Application.Customers.Commands;

public sealed record ArchiveCustomerCommand(Guid CustomerId) : IRequest<Result>;

public sealed class ArchiveCustomerCommandHandler(
    ICustomerRepository customers,
    ITenantContext tenantContext,
    IClock clock) : IRequestHandler<ArchiveCustomerCommand, Result>
{
    public async Task<Result> Handle(ArchiveCustomerCommand cmd, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(cmd.CustomerId, ct).ConfigureAwait(false);
        if (customer is null) return Result.NotFound();
        var guard = tenantContext.EnsureSameTenant(customer);
        if (!guard.IsSuccess) return guard;

        var result = customer.Archive(clock);
        if (!result.IsSuccess) return result;

        customers.Update(customer);
        await customers.SaveChangesAsync(ct).ConfigureAwait(false);
        return Result.Success();
    }
}
