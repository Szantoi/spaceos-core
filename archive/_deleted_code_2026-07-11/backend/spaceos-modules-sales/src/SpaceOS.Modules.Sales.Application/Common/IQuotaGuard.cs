using Ardalis.Result;

namespace SpaceOS.Modules.Sales.Application.Common;

/// <summary>Scopes for per-tenant quota enforcement (SEC-S-12).</summary>
public enum QuotaScope { Customer, Quote }

/// <summary>
/// Guards against per-tenant resource over-creation (SEC-S-12).
/// Default limits: 10,000 customers and 50,000 quotes per tenant.
/// </summary>
public interface IQuotaGuard
{
    /// <summary>
    /// Returns <see cref="Result.Forbidden()"/> if the tenant has reached the quota for <paramref name="scope"/>.
    /// </summary>
    Task<Result> EnsureCanCreateAsync(Guid tenantId, QuotaScope scope, CancellationToken ct);
}
