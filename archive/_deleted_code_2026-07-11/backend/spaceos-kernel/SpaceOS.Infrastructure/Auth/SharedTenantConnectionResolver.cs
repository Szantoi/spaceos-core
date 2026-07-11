// SpaceOS.Infrastructure/Auth/SharedTenantConnectionResolver.cs
using Microsoft.Extensions.Configuration;
using SpaceOS.Kernel.Domain.Auth;

namespace SpaceOS.Infrastructure.Auth;

/// <summary>
/// Level 1 tenant connection resolver: all tenants share the same database.
/// The connection string is read once at construction time from the
/// <c>ConnectionStrings:DefaultConnection</c> configuration key.
/// </summary>
internal sealed class SharedTenantConnectionResolver : ITenantConnectionResolver
{
    private readonly string _connectionString;

    /// <summary>
    /// Initialises a new instance of <see cref="SharedTenantConnectionResolver"/>.
    /// </summary>
    /// <param name="configuration">Application configuration supplying the connection string.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <c>ConnectionStrings:DefaultConnection</c> is not configured.
    /// </exception>
    public SharedTenantConnectionResolver(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Level 1 implementation — ignores <paramref name="tenantId"/> and returns
    /// the shared connection string for all tenants.
    /// </remarks>
    public string Resolve(Guid tenantId) => _connectionString;
}
