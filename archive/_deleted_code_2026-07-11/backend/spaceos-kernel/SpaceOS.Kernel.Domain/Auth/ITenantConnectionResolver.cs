// SpaceOS.Kernel.Domain/Auth/ITenantConnectionResolver.cs
namespace SpaceOS.Kernel.Domain.Auth;

/// <summary>
/// Resolves the database connection string for a given tenant.
/// Supports three isolation levels:
/// <list type="bullet">
///   <item><description>Level 1 — shared database: all tenants use the same connection string.</description></item>
///   <item><description>Level 2 — schema-per-tenant: connection string selects the tenant's schema.</description></item>
///   <item><description>Level 3 — database-per-tenant: each tenant has a dedicated database.</description></item>
/// </list>
/// </summary>
public interface ITenantConnectionResolver
{
    /// <summary>
    /// Returns the connection string to use for the given tenant.
    /// </summary>
    /// <param name="tenantId">The tenant whose connection string to resolve.</param>
    /// <returns>A non-null, non-empty connection string.</returns>
    string Resolve(Guid tenantId);
}
