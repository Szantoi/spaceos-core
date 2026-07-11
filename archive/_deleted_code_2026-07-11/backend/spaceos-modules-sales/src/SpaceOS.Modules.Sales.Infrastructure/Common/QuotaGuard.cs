using Ardalis.Result;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SpaceOS.Modules.Sales.Application.Common;
using SpaceOS.Modules.Sales.Infrastructure.Persistence;

namespace SpaceOS.Modules.Sales.Infrastructure.Common;

/// <summary>
/// Enforces per-tenant resource quotas (SEC-S-12).
/// Limits are configurable via <c>Sales:Quota:MaxCustomersPerTenant</c> and
/// <c>Sales:Quota:MaxQuotesPerTenant</c>; defaults to 10,000 / 50,000.
/// </summary>
internal sealed class QuotaGuard(SalesDbContext db, IConfiguration cfg) : IQuotaGuard
{
    private int MaxCustomers => cfg.GetValue<int>("Sales:Quota:MaxCustomersPerTenant", 10_000);
    private int MaxQuotes    => cfg.GetValue<int>("Sales:Quota:MaxQuotesPerTenant",    50_000);

    /// <inheritdoc/>
    public async Task<Result> EnsureCanCreateAsync(
        Guid tenantId, QuotaScope scope, CancellationToken ct)
    {
        var count = scope == QuotaScope.Customer
            ? await db.Customers
                .CountAsync(c => c.TenantId == tenantId && !c.IsArchived, ct)
                .ConfigureAwait(false)
            : await db.Quotes
                .CountAsync(q => q.TenantId == tenantId && !q.IsArchived, ct)
                .ConfigureAwait(false);

        var limit = scope == QuotaScope.Customer ? MaxCustomers : MaxQuotes;
        return count >= limit ? Result.Forbidden() : Result.Success();
    }
}
