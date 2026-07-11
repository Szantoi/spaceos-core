using Ardalis.Result;
using MediatR;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Application.Customers.Commands;
using SpaceOS.Modules.Sales.Application.DTOs;
using SpaceOS.Modules.Sales.Application.Specifications.Customers;
using SpaceOS.Modules.Sales.Domain.Enums;
using SpaceOS.Modules.Sales.Domain.Interfaces;

namespace SpaceOS.Modules.Sales.Application.Customers.Queries;

public sealed record ListCustomersQuery(
    CustomerStatus? Status,
    string? Search,
    int Skip = 0,
    int Take = 50) : IRequest<Result<SalesPagedResult<CustomerSummary>>>;

public sealed class ListCustomersQueryHandler(
    ICustomerRepository customers,
    ITenantContext tenantContext) : IRequestHandler<ListCustomersQuery, Result<SalesPagedResult<CustomerSummary>>>
{
    public async Task<Result<SalesPagedResult<CustomerSummary>>> Handle(ListCustomersQuery query, CancellationToken ct)
    {
        var spec = new CustomersByTenantSpec(
            tenantContext.TenantId, query.Status, query.Search, query.Skip, query.Take);
        var list = await customers.ListAsync(spec, ct).ConfigureAwait(false);
        var total = await customers.CountActiveAsync(tenantContext.TenantId, ct).ConfigureAwait(false);

        var items = list.Select(c => new CustomerSummary(
            c.Id, c.Type, c.DisplayName, c.ContactName, c.ContactEmail?.Value, c.Status, c.IsArchived, c.CreatedAt))
            .ToList();

        return Result.Success(new SalesPagedResult<CustomerSummary>(items, total, query.Skip, query.Take));
    }
}
