using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.Customers.Commands;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Domain.Interfaces;

namespace SpaceOS.Modules.Sales.Application.Customers.Queries;

public sealed record GetCustomerQuery(Guid CustomerId) : IRequest<Result<CustomerResponse>>;

public sealed class GetCustomerQueryHandler(
    ICustomerRepository customers,
    ITenantContext tenantContext) : IRequestHandler<GetCustomerQuery, Result<CustomerResponse>>
{
    public async Task<Result<CustomerResponse>> Handle(GetCustomerQuery query, CancellationToken ct)
    {
        var customer = await customers.GetByIdAsync(query.CustomerId, ct).ConfigureAwait(false);
        if (customer is null) return Result.NotFound();
        var guard = tenantContext.EnsureSameTenant(customer);
        if (!guard.IsSuccess) return guard;
        return Result.Success(CreateCustomerCommandHandler.MapToResponse(customer));
    }
}
